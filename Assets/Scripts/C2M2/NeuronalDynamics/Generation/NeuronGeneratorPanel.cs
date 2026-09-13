using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using C2M2.Interaction;
using C2M2.Interaction.VR;
using C2M2.NeuronalDynamics.Interaction;
using C2M2.NeuronalDynamics.UGX;
using C2M2.NeuronalDynamics.Visualization.VRN;
using UnityEngine;
using UnityEngine.SceneManagement;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
using Edge = C2M2.NeuronalDynamics.UGX.Edge;
using DiameterAttachment = C2M2.NeuronalDynamics.UGX.IAttachment<C2M2.NeuronalDynamics.UGX.DiameterData>;

namespace C2M2.NeuronalDynamics.Generation
{
    /// <summary>
    /// Drives NeuronGeneratorScene end to end: scans
    /// StreamingAssets/NeuronalDynamics/SWCSource for .swc files (the same Directory.GetFiles-
    /// based picker approach Menu.cs already uses for its own Save/Load screen), lets the user
    /// step through the same options `neuronmesh --vrn` takes on the command line via simple
    /// cycle/stepper buttons, then calls NeuronGeneratorService.GenerateAsync on Generate and
    /// returns to MainScene on Back.
    ///
    /// The entire UI is built procedurally in Start() rather than hand-placed in the scene file:
    /// every button here uses exactly the same component recipe (RectTransform + world-space
    /// Canvas + CanvasRenderer + Image + BoxCollider + RaycastPressEvents + RaycastEventManager)
    /// this project's own hand-authored world-space UI already uses elsewhere (see
    /// NeuronGeneratorButton in MainScene.unity, built the same way but serialized directly into
    /// that scene file) - building it here in code instead avoids hand-serializing this scene's own
    /// YAML for a whole panel's worth of controls, and makes the layout trivial to rearrange later.
    ///
    /// Per the user's explicit direction, this panel takes over the CellPreviewer wall's own spot
    /// (which NeuronGeneratorButton hides before this scene loads) rather than floating wherever the
    /// camera happens to be - the rest of the main room stays exactly as the player left it. Its
    /// position/rotation are a fixed constant tuned in-editor for that spot (see Start()), not read
    /// from the CellPreviewer transform itself - that transform's rotation is identity and doesn't
    /// face outward into the room, and anything computed from a moving reference (Camera.main, etc.)
    /// made the panel's orientation depend on wherever the player happened to be standing.
    /// </summary>
    public class NeuronGeneratorPanel : MonoBehaviour
    {
        private const string SwcSourceSubpath = "NeuronalDynamics/SWCSource";
        private static readonly string[] Methods = { "PG", "CP", "HC", "PC", "BS", "SG", "CC" };

        // World-space size of a button's RectTransform (200x60 units) once scaled by ButtonScale -
        // used to lay out rows/columns with real gaps instead of guessed offsets (an earlier
        // version used offsets smaller than the buttons' own scaled size, so adjacent buttons and
        // rows visibly overlapped).
        private const float ButtonScale = 0.0025f;
        private static readonly Vector2 FullSize = new Vector2(200, 60);
        private static readonly Vector2 HalfSize = new Vector2(96, 60);
        private const float RowGap = 0.02f;
        private const float ColGap = 0.02f;
        private static readonly float FullWorldW = FullSize.x * ButtonScale;
        private static readonly float HalfWorldW = HalfSize.x * ButtonScale;
        private static readonly float RowStep = FullSize.y * ButtonScale + RowGap;

        // Roughly how large (in world-space meters) a generated wireframe preview should appear,
        // regardless of the real neuron's own physical size - see BuildLinePreview.
        private const float PreviewWorldSize = 3.2f;

        private static readonly Color DarkGreen = new Color(0.0f, 0.28f, 0.12f, 1f);

        private string selectedSwcPath;
        private NeuronGeneratorService.Options options = NeuronGeneratorService.Options.Default();
        private int methodIndex;
        private int previewLevel;
        private bool generating;
        private bool previewing;

        private GameObject swcListParent;

        // The three preview styles are all children of one shared, positioned/scaled/grabbable
        // previewGroup (Rigidbody + PublicOVRGrabbable + ObjectMovementControl live on the group,
        // not on the individual layers) so grabbing any one of them moves all three together.
        // previewGroupCenter/previewGroupMaxDim are established once, by whichever layer creates the
        // group first, and reused unchanged by every layer added afterward - centering each layer's
        // vertices on that same shared reference (rather than each layer's own independently
        // computed bounds) is what makes them land exactly on top of each other instead of each
        // being centered/scaled to its own slightly different extent. A parameter change
        // (SelectSwc/CycleMethod/AdjustN/AdjustLevel/AdjustDelta/AdjustSides/ToggleRepair) or Back
        // tears the whole group down via ClearPreviewGroup, since a stale layer left over from a
        // different configuration could no longer be assumed to align with a freshly built one.
        private GameObject previewGroup;
        private Vector3 previewGroupCenter;
        private float previewGroupMaxDim;

        private GameObject previewWireframeObject;
        private GameObject preview1DObject;
        private GameObject previewSurfaceObject;

        private enum PreviewLayerKind { Wireframe, OneD, Surface }

        /// <summary>
        /// A layer's own (already group-centered) vertex positions and edges, kept around purely so
        /// RegenerateGroupGrabColliders can rebuild every currently-visible layer's grab colliders
        /// whenever any one layer is added, replaced, or (in principle) removed.
        /// </summary>
        private struct PreviewLayer
        {
            public Vector3[] vertices;
            public List<Edge> edges;
        }
        private PreviewLayer? wireframeLayer;
        private PreviewLayer? oneDLayer;
        private PreviewLayer? surfaceLayer;

        private TMPro.TextMeshProUGUI methodLabel;
        private TMPro.TextMeshProUGUI nLabel;
        private TMPro.TextMeshProUGUI deltaLabel;
        private TMPro.TextMeshProUGUI sidesLabel;
        private TMPro.TextMeshProUGUI levelLabel;
        private TMPro.TextMeshProUGUI repairLabel;
        private TMPro.TextMeshProUGUI statusLabel;

        private void Start()
        {
            options.method = Methods[methodIndex];

            // Place this panel at a FIXED position and rotation on the CellPreviewer wall, rather
            // than computing either from a moving reference (the CellPreviewer transform's own
            // rotation is identity and doesn't face outward into the room at all, its position sits
            // slightly off from where the panel actually needs to be to read flat and square, and
            // facing the panel toward Camera.main or any other point made its orientation depend on
            // wherever the player happened to be standing when this scene loaded - all three produced
            // a skewed or misplaced panel depending on run). This exact position/rotation pair was
            // confirmed in-editor (manually dialed in on the Transform component in Play mode) to
            // render this wall's spot as flat, square, and readable from a normal standing position.
            transform.position = new Vector3(-1.6f, 1f, 0.75f);
            transform.rotation = Quaternion.Euler(0, -90, 0);

            swcListParent = new GameObject("SwcList");
            swcListParent.transform.SetParent(transform, false);

            BuildControls();
            PopulateSwcList();
            RefreshStatus();
        }

        private void BuildControls()
        {
            float y = 0f;
            float pairOffset = HalfWorldW / 2f + ColGap / 2f;

            methodLabel = CreateButton("Button_MethodCycle", "Method: " + options.method,
                new Vector3(0, y, 0), FullSize, CycleMethod).label;
            y -= RowStep;

            nLabel = CreateButton("Button_N_Minus", "N -", new Vector3(-pairOffset, y, 0), HalfSize,
                () => AdjustN(-1)).label;
            CreateButton("Button_N_Plus", "N +", new Vector3(pairOffset, y, 0), HalfSize, () => AdjustN(1));
            y -= RowStep;

            deltaLabel = CreateButton("Button_Delta_Minus", "Delta -", new Vector3(-pairOffset, y, 0), HalfSize,
                () => AdjustDelta(0.5)).label;
            CreateButton("Button_Delta_Plus", "Delta +", new Vector3(pairOffset, y, 0), HalfSize,
                () => AdjustDelta(2.0));
            y -= RowStep;

            sidesLabel = CreateButton("Button_Sides_Minus", "Sides -", new Vector3(-pairOffset, y, 0), HalfSize,
                () => AdjustSides(-1)).label;
            CreateButton("Button_Sides_Plus", "Sides +", new Vector3(pairOffset, y, 0), HalfSize,
                () => AdjustSides(1));
            y -= RowStep;

            repairLabel = CreateButton("Button_RepairToggle", "Repair: " + options.repair,
                new Vector3(0, y, 0), FullSize, ToggleRepair).label;
            y -= RowStep;

            // Which of the n refinement levels (0..n-1) Preview below actually renders - a level's
            // own delta is options.delta / 2^level, the same halving write_vrn_levels/Generate use.
            levelLabel = CreateButton("Button_Level_Minus", "Level -", new Vector3(-pairOffset, y, 0), HalfSize,
                () => AdjustLevel(-1)).label;
            CreateButton("Button_Level_Plus", "Level +", new Vector3(pairOffset, y, 0), HalfSize,
                () => AdjustLevel(1));
            y -= RowStep;

            CreateButton("Button_Preview", "Preview", new Vector3(0, y, 0), FullSize, OnPreviewPressed,
                new Color(0.16f, 0.55f, 0.72f, 1f));
            y -= RowStep;

            // Two alternate renders of the same level's geometry Preview above already generates:
            // View 1D re-derives the true 1D skeleton (Preview's own wireframe is really the surface
            // mesh's edges, not the 1D graph - see BuildLinePreview/GeneratePreview1DVrnAsync), and
            // View Surface re-renders that same surface solid instead of as a wireframe.
            CreateButton("Button_View1D", "View 1D", new Vector3(-pairOffset, y, 0), HalfSize, OnView1DPressed,
                new Color(0.72f, 0.64f, 0.06f, 1f));
            CreateButton("Button_ViewSurface", "View Surface", new Vector3(pairOffset, y, 0), HalfSize,
                OnViewSurfacePressed, DarkGreen);
            y -= RowStep;

            CreateButton("Button_Generate", "Generate", new Vector3(0, y, 0), FullSize, OnGeneratePressed,
                new Color(0.16f, 0.72f, 0.34f, 1f));
            y -= RowStep;

            CreateButton("Button_Back", "Back to Main Room", new Vector3(0, y, 0), FullSize, OnBackPressed,
                new Color(0.72f, 0.24f, 0.16f, 1f));
            y -= RowStep;

            statusLabel = CreateButton("StatusLabel", "", new Vector3(0, y, 0), FullSize, null,
                new Color(0.1f, 0.1f, 0.1f, 0.85f)).label;

            // N/Delta/Sides show their live value on their own "-" button rather than doubling every
            // row's worth of buttons with a separate always-visible value label.
            UpdateStepperLabels();
        }

        private void UpdateStepperLabels()
        {
            if (nLabel != null) { nLabel.text = $"N -\n({options.n})"; }
            if (deltaLabel != null) { deltaLabel.text = $"Delta -\n({options.delta})"; }
            if (sidesLabel != null) { sidesLabel.text = $"Sides -\n({options.sides})"; }
            if (levelLabel != null) { levelLabel.text = $"Level -\n({previewLevel})"; }
        }

        private void PopulateSwcList()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, SwcSourceSubpath);
            if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }
            string[] files = Directory.GetFiles(dir, "*.swc");

            // Placed one full row to the right of the main control column so it never overlaps it,
            // regardless of how many rows BuildControls ends up with.
            float x = FullWorldW + ColGap * 4f;
            for (int i = 0; i < files.Length; i++)
            {
                string capturedFile = files[i];
                string fileName = Path.GetFileName(capturedFile);
                GameObject slot = CreateButton("SwcSlot_" + fileName, fileName,
                    new Vector3(x, -i * RowStep, 0), FullSize, () => SelectSwc(capturedFile)).button;
                slot.transform.SetParent(swcListParent.transform, true);
            }

            if (files.Length > 0) { SelectSwc(files[0]); }
        }

        private struct ButtonHandles
        {
            public readonly GameObject button;
            public readonly TMPro.TextMeshProUGUI label;
            public ButtonHandles(GameObject button, TMPro.TextMeshProUGUI label)
            {
                this.button = button;
                this.label = label;
            }
        }

        /// <summary>
        /// Builds one self-contained clickable world-space button: RectTransform + Canvas
        /// (WorldSpace) + CanvasRenderer + Image + BoxCollider + RaycastPressEvents +
        /// RaycastEventManager, matching NeuronGeneratorButton's own hand-serialized component set
        /// in MainScene.unity exactly, just constructed via AddComponent instead - plus a child
        /// TextMeshProUGUI label (non-raycast-target, so it never intercepts the button's own
        /// collider-based raycast) filling the button's own rect. onPress may be null for a purely
        /// informational (non-clickable) panel like the status label. `localOffset` is relative to
        /// this panel's own transform (anchored to the CellPreviewer wall in Start()), in world
        /// units - not RectTransform units - so it composes directly with `size` (also given in
        /// RectTransform units, scaled by ButtonScale here) without the caller having to do that
        /// conversion at every call site.
        /// </summary>
        private ButtonHandles CreateButton(string name, string label, Vector3 localOffset, Vector2 size,
            Action onPress, Color? color = null)
        {
            var go = new GameObject(name);
            // The custom raycast interaction system (RaycastEventSignaler.Awake) only ever raycasts
            // against the "Raycast" layer (LayerMask.GetMask("Raycast")) - a collider on any other
            // layer is invisible to it, even though it still renders fine (rendering doesn't care
            // about physics layers). Look up by name rather than hardcoding an index (8 in this
            // project's current TagManager.asset) so this doesn't silently break if layers are
            // ever renumbered.
            go.layer = LayerMask.NameToLayer("Raycast");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localOffset;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(ButtonScale, ButtonScale, ButtonScale);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            go.AddComponent<CanvasRenderer>();

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = color ?? new Color(0.25f, 0.35f, 0.55f, 1f);

            var collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(size.x, size.y, 1);

            var events = go.AddComponent<RaycastPressEvents>();
            if (onPress != null) { events.OnPress.AddListener((hit) => onPress()); }

            var manager = go.AddComponent<RaycastEventManager>();
            manager.rightTrigger = events;
            manager.leftTrigger = events;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 4);
            textRect.offsetMax = new Vector2(-4, -4);
            textGo.AddComponent<CanvasRenderer>();
            var text = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = label;
            text.color = Color.white;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 6;
            text.fontSizeMax = 24;
            text.raycastTarget = false;

            return new ButtonHandles(go, text);
        }

        private void SelectSwc(string path)
        {
            selectedSwcPath = path;
            ClearPreviewGroup();
            RefreshStatus();
        }

        private void CycleMethod()
        {
            methodIndex = (methodIndex + 1) % Methods.Length;
            options.method = Methods[methodIndex];
            ClearPreviewGroup();
            RefreshStatus();
        }

        private void AdjustN(int delta)
        {
            options.n = Mathf.Max(1, options.n + delta);
            // Level must stay a valid index into the n levels a Generate would actually produce.
            previewLevel = Mathf.Clamp(previewLevel, 0, options.n - 1);
            ClearPreviewGroup();
            RefreshStatus();
        }

        private void AdjustLevel(int delta)
        {
            previewLevel = Mathf.Clamp(previewLevel + delta, 0, Mathf.Max(0, options.n - 1));
            ClearPreviewGroup();
            RefreshStatus();
        }

        private void AdjustDelta(double factor)
        {
            options.delta = Math.Max(0.01, options.delta * factor);
            ClearPreviewGroup();
            RefreshStatus();
        }

        private void AdjustSides(int delta)
        {
            options.sides = Mathf.Clamp(options.sides + delta, 3, 64);
            ClearPreviewGroup();
            RefreshStatus();
        }

        private void ToggleRepair()
        {
            options.repair = !options.repair;
            ClearPreviewGroup();
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            string swcName = selectedSwcPath == null ? "(none)" : Path.GetFileName(selectedSwcPath);
            string msg = $"swc={swcName} method={options.method} n={options.n} delta={options.delta} " +
                        $"p={options.p} sides={options.sides} repair={options.repair}";
            Debug.Log("NeuronGeneratorPanel: " + msg);

            if (methodLabel != null) { methodLabel.text = "Method: " + options.method; }
            if (repairLabel != null) { repairLabel.text = "Repair: " + options.repair; }
            UpdateStepperLabels();

            if (statusLabel != null)
            {
                statusLabel.text = generating ? "Generating..." : previewing ? "Previewing..." : $"Selected:\n{swcName}";
            }
        }

        private async void OnGeneratePressed()
        {
            if (generating) { return; }
            if (string.IsNullOrEmpty(selectedSwcPath))
            {
                Debug.LogWarning("NeuronGeneratorPanel: pick a .swc file first.");
                return;
            }
            generating = true;
            RefreshStatus();

            NeuronGeneratorService.Result result = await NeuronGeneratorService.GenerateAsync(selectedSwcPath, options);

            generating = false;
            RefreshStatus();
            if (result.success)
            {
                Debug.Log($"NeuronGeneratorPanel: wrote {result.vrnPath}");
                if (statusLabel != null) { statusLabel.text = $"Wrote:\n{Path.GetFileName(result.vrnPath)}"; }
            }
            else
            {
                Debug.LogError($"NeuronGeneratorPanel: generation failed - {result.errorMessage}");
                if (statusLabel != null) { statusLabel.text = $"Error:\n{result.errorMessage}"; }
            }
        }

        /// <summary>
        /// Generates just the selected refinement level's 3D surface mesh (via
        /// NeuronGeneratorService.GeneratePreviewMeshAsync - a single nm_generate_mesh_file call,
        /// far cheaper than a full N-level Generate) and renders its edges as a wireframe using the
        /// same UGXReader this project already uses to load real neuron meshes for simulation - see
        /// BuildLinePreview for why a hand-built MeshTopology.Lines mesh is used here instead of this
        /// project's existing LinesRenderer (which recursively walks every vertex - fine for a 1D
        /// skeleton's few hundred/thousand nodes, but risks a stack overflow at surface mesh scale,
        /// tens of thousands of vertices even at coarse settings). Note this wireframe traces the
        /// *surface* mesh's own edges, not the true 1D skeleton - see OnView1DPressed for that.
        /// Toggles off (removing the layer, no regeneration) if the wireframe is already showing.
        /// </summary>
        private async void OnPreviewPressed()
        {
            if (previewing || generating) { return; }
            if (previewWireframeObject != null)
            {
                RemoveLayer(PreviewLayerKind.Wireframe);
                RefreshStatus();
                return;
            }
            if (string.IsNullOrEmpty(selectedSwcPath))
            {
                Debug.LogWarning("NeuronGeneratorPanel: pick a .swc file first.");
                return;
            }
            previewing = true;
            RefreshStatus();

            NeuronGeneratorService.PreviewResult result =
                await NeuronGeneratorService.GeneratePreviewMeshAsync(selectedSwcPath, options, previewLevel);

            previewing = false;
            if (result.success)
            {
                try
                {
                    Grid grid = LoadUgxGrid(result.ugxPath);
                    BuildLinePreview(PreviewLayerKind.Wireframe, "PreviewWireframe", grid, Color.red);
                    if (statusLabel != null) { statusLabel.text = $"Previewing level {previewLevel}"; }
                }
                catch (Exception e)
                {
                    Debug.LogError($"NeuronGeneratorPanel: failed to build preview mesh - {e.Message}");
                    if (statusLabel != null) { statusLabel.text = $"Preview error:\n{e.Message}"; }
                }
                finally
                {
                    try { if (File.Exists(result.ugxPath)) { File.Delete(result.ugxPath); } } catch { /* best effort */ }
                }
            }
            else
            {
                Debug.LogError($"NeuronGeneratorPanel: preview failed - {result.errorMessage}");
                if (statusLabel != null) { statusLabel.text = $"Preview error:\n{result.errorMessage}"; }
            }
            RefreshStatus();
        }

        /// <summary>
        /// Same cheap single-level surface generation Preview uses, but renders the real solid
        /// surface (its actual triangles, lit and shaded dark green) instead of just its wireframe -
        /// this is literally the same mesh Preview's red wireframe traces the edges of. Toggles off
        /// (removing the layer, no regeneration) if the surface is already showing.
        /// </summary>
        private async void OnViewSurfacePressed()
        {
            if (previewing || generating) { return; }
            if (previewSurfaceObject != null)
            {
                RemoveLayer(PreviewLayerKind.Surface);
                RefreshStatus();
                return;
            }
            if (string.IsNullOrEmpty(selectedSwcPath))
            {
                Debug.LogWarning("NeuronGeneratorPanel: pick a .swc file first.");
                return;
            }
            previewing = true;
            RefreshStatus();

            NeuronGeneratorService.PreviewResult result =
                await NeuronGeneratorService.GeneratePreviewMeshAsync(selectedSwcPath, options, previewLevel);

            previewing = false;
            if (result.success)
            {
                try
                {
                    Grid grid = LoadUgxGrid(result.ugxPath);
                    BuildSolidSurfacePreview(grid, DarkGreen);
                    if (statusLabel != null) { statusLabel.text = $"Viewing surface, level {previewLevel}"; }
                }
                catch (Exception e)
                {
                    Debug.LogError($"NeuronGeneratorPanel: failed to build surface preview - {e.Message}");
                    if (statusLabel != null) { statusLabel.text = $"Surface error:\n{e.Message}"; }
                }
                finally
                {
                    try { if (File.Exists(result.ugxPath)) { File.Delete(result.ugxPath); } } catch { /* best effort */ }
                }
            }
            else
            {
                Debug.LogError($"NeuronGeneratorPanel: surface preview failed - {result.errorMessage}");
                if (statusLabel != null) { statusLabel.text = $"Surface error:\n{result.errorMessage}"; }
            }
            RefreshStatus();
        }

        /// <summary>
        /// Preview's own wireframe traces the *surface* mesh's edges, not the actual 1D skeleton -
        /// nm_generate_mesh_file (what Preview/View Surface both call) explicitly produces
        /// surface-only output with no 1D skeleton at all. The only native entry point that writes
        /// one is nm_generate_vrn_files, so this goes through
        /// NeuronGeneratorService.GeneratePreview1DVrnAsync (a throwaway single-level .vrn) and reads
        /// the 1D mesh back out the same way NeuronCellPreview.cs already does for real saved cells -
        /// via VrnReader.Retrieve1DMeshName + ReadUGX - then renders it the same way BuildLinePreview
        /// renders Preview's own wireframe, just in yellow. Toggles off (removing the layer, no
        /// regeneration) if the 1D skeleton is already showing.
        /// </summary>
        private async void OnView1DPressed()
        {
            if (previewing || generating) { return; }
            if (preview1DObject != null)
            {
                RemoveLayer(PreviewLayerKind.OneD);
                RefreshStatus();
                return;
            }
            if (string.IsNullOrEmpty(selectedSwcPath))
            {
                Debug.LogWarning("NeuronGeneratorPanel: pick a .swc file first.");
                return;
            }
            previewing = true;
            if (statusLabel != null) { statusLabel.text = "Building 1D geometry..."; }
            RefreshStatus();

            NeuronGeneratorService.Result result =
                await NeuronGeneratorService.GeneratePreview1DVrnAsync(selectedSwcPath, options, previewLevel);

            previewing = false;
            if (result.success)
            {
                try
                {
                    var vrnReader = new VrnReader(result.vrnPath);
                    string mesh1DName = vrnReader.Retrieve1DMeshName(0);
                    var grid = new Grid(new Mesh(), mesh1DName);
                    grid.Attach(new DiameterAttachment());
                    vrnReader.ReadUGX(mesh1DName, ref grid);

                    BuildLinePreview(PreviewLayerKind.OneD, "Preview1D", grid, Color.yellow);
                    if (statusLabel != null) { statusLabel.text = $"Viewing 1D geometry, level {previewLevel}"; }
                }
                catch (Exception e)
                {
                    Debug.LogError($"NeuronGeneratorPanel: failed to build 1D preview - {e.Message}");
                    if (statusLabel != null) { statusLabel.text = $"1D error:\n{e.Message}"; }
                }
                finally
                {
                    try { if (File.Exists(result.vrnPath)) { File.Delete(result.vrnPath); } } catch { /* best effort */ }
                }
            }
            else
            {
                Debug.LogError($"NeuronGeneratorPanel: 1D preview failed - {result.errorMessage}");
                if (statusLabel != null) { statusLabel.text = $"1D error:\n{result.errorMessage}"; }
            }
            RefreshStatus();
        }

        private static Grid LoadUgxGrid(string ugxPath)
        {
            var grid = new Grid(new Mesh(), "NeuronGeneratorPreview");
            C2M2.NeuronalDynamics.UGX.UGXReader.ReadUGX(ugxPath, ref grid);
            return grid;
        }

        /// <summary>
        /// Renders a grid's edges (grid.Edges - real 1D skeleton segments when grid came from a 1D
        /// mesh, surface-triangle edges when it came from a 2D/surface mesh) as a plain
        /// MeshTopology.Lines mesh, parented under the shared previewGroup so it lines up with and
        /// moves together with the other preview styles - see previewGroup's own field comment.
        /// </summary>
        private void BuildLinePreview(PreviewLayerKind kind, string name, Grid grid, Color color)
        {
            Vector3[] positions = grid.Mesh.vertices;
            var lineMesh = new Mesh();
            if (positions.Length > 65535) { lineMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; }
            lineMesh.vertices = positions;
            lineMesh.RecalculateBounds();

            EnsurePreviewGroup(lineMesh.bounds);
            Vector3[] centered = CenterVertices(positions, previewGroupCenter);
            lineMesh.vertices = centered;

            var indices = new int[grid.Edges.Count * 2];
            for (int i = 0; i < grid.Edges.Count; i++)
            {
                indices[i * 2] = grid.Edges[i].From.Id;
                indices[i * 2 + 1] = grid.Edges[i].To.Id;
            }
            lineMesh.SetIndices(indices, MeshTopology.Lines, 0);
            lineMesh.RecalculateBounds();

            var layerObject = new GameObject(name);
            layerObject.transform.SetParent(previewGroup.transform, false);

            var meshFilter = layerObject.AddComponent<MeshFilter>();
            meshFilter.mesh = lineMesh;

            var renderer = layerObject.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            var material = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
            material.color = color;
            renderer.material = material;

            SetLayer(kind, layerObject, centered, grid.Edges);
            RegenerateGroupGrabColliders();
        }

        /// <summary>
        /// Renders a grid's actual triangulated surface (real triangles, lit and solid-shaded)
        /// instead of BuildLinePreview's wireframe - used by View Surface. grid must come from a
        /// 2D/surface mesh (grid.Mesh.triangles populated by UGXReader); a 1D skeleton mesh has none.
        /// Parented under the shared previewGroup the same way BuildLinePreview's layers are.
        /// </summary>
        private void BuildSolidSurfacePreview(Grid grid, Color color)
        {
            Vector3[] positions = grid.Mesh.vertices;
            int[] triangles = grid.Mesh.triangles;

            var surfaceMesh = new Mesh();
            if (positions.Length > 65535) { surfaceMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; }
            surfaceMesh.vertices = positions;
            surfaceMesh.RecalculateBounds();

            EnsurePreviewGroup(surfaceMesh.bounds);
            Vector3[] centered = CenterVertices(positions, previewGroupCenter);
            surfaceMesh.vertices = centered;
            surfaceMesh.triangles = triangles;
            surfaceMesh.RecalculateNormals();
            surfaceMesh.RecalculateBounds();

            var layerObject = new GameObject("PreviewSurface");
            layerObject.transform.SetParent(previewGroup.transform, false);

            var meshFilter = layerObject.AddComponent<MeshFilter>();
            meshFilter.mesh = surfaceMesh;

            var renderer = layerObject.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader);
            material.color = color;
            renderer.material = material;

            SetLayer(PreviewLayerKind.Surface, layerObject, centered, grid.Edges);
            RegenerateGroupGrabColliders();
        }

        /// <summary>
        /// Shifts vertex positions so they're centered on previewGroupCenter (the shared reference
        /// established once by whichever layer creates previewGroup first), rather than each layer's
        /// own independently computed bounds - this is what makes every layer land exactly on top of
        /// the others instead of each being centered on its own slightly different extent.
        /// </summary>
        private static Vector3[] CenterVertices(Vector3[] positions, Vector3 center)
        {
            var centered = new Vector3[positions.Length];
            for (int i = 0; i < positions.Length; i++) { centered[i] = positions[i] - center; }
            return centered;
        }

        /// <summary>
        /// Creates the shared preview container the first time any preview button is pressed (no-op
        /// if one already exists - see previewGroup's own field comment for why later layers reuse
        /// it unchanged rather than each recomputing their own position/scale): positions it at the
        /// room's own designated neuron-display center (GameManager.instance.simulationSpace - the
        /// same anchor GrabRescaler uses as its rescale target for simulated neurons) rather than
        /// relative to this panel, since "the very center of the room" is a world-space request, not
        /// one relative to wherever the panel happens to be anchored (the CellPreviewer wall); scales
        /// it to fit within PreviewWorldSize world-space meters regardless of the real neuron's own
        /// physical size; and gives it the single Rigidbody + PublicOVRGrabbable + ObjectMovementControl
        /// that make every child layer grabbable and move together as one object. Left unparented
        /// (world space) for the same reason - ClearPreviewGroup explicitly destroys it, since it
        /// isn't guaranteed to belong to NeuronGeneratorScene and so wouldn't necessarily be cleaned
        /// up by that scene's own unload.
        /// </summary>
        private void EnsurePreviewGroup(Bounds referenceBounds)
        {
            if (previewGroup != null) { return; }

            previewGroup = new GameObject("PreviewGroup");
            previewGroupCenter = referenceBounds.center;

            Vector3 roomCenter = GameManager.instance != null && GameManager.instance.simulationSpace != null
                ? GameManager.instance.simulationSpace.transform.position
                : transform.position;
            previewGroup.transform.position = roomCenter;
            previewGroup.transform.rotation = Quaternion.identity;

            previewGroupMaxDim = Mathf.Max(referenceBounds.size.x, Mathf.Max(referenceBounds.size.y, referenceBounds.size.z));
            float scale = previewGroupMaxDim > 1e-6f ? PreviewWorldSize / previewGroupMaxDim : 1f;
            previewGroup.transform.localScale = new Vector3(scale, scale, scale);

            var rb = previewGroup.AddComponent<Rigidbody>();
            C2M2.Utils.RigidbodyUtilities.SetDefaultState(rb);
            previewGroup.AddComponent<PublicOVRGrabbable>();
            previewGroup.AddComponent<C2M2.Utils.ObjectMovementControl>();
        }

        /// <summary>
        /// Destroys whichever preview object/layer currently occupies `kind`'s own slot (if any) and
        /// records the new one plus its (already group-centered) vertices/edges, so
        /// RegenerateGroupGrabColliders can rebuild that layer's own share of the group's grab
        /// colliders. Does not touch the other two slots - that's what lets, e.g., View 1D add its
        /// yellow skeleton alongside an already-showing red wireframe instead of replacing it.
        /// </summary>
        private void SetLayer(PreviewLayerKind kind, GameObject newLayerObject, Vector3[] vertices, List<Edge> edges)
        {
            var layer = new PreviewLayer { vertices = vertices, edges = edges };
            switch (kind)
            {
                case PreviewLayerKind.Wireframe:
                    if (previewWireframeObject != null) { Destroy(previewWireframeObject); }
                    previewWireframeObject = newLayerObject;
                    wireframeLayer = layer;
                    break;
                case PreviewLayerKind.OneD:
                    if (preview1DObject != null) { Destroy(preview1DObject); }
                    preview1DObject = newLayerObject;
                    oneDLayer = layer;
                    break;
                case PreviewLayerKind.Surface:
                    if (previewSurfaceObject != null) { Destroy(previewSurfaceObject); }
                    previewSurfaceObject = newLayerObject;
                    surfaceLayer = layer;
                    break;
            }
        }

        /// <summary>
        /// Tears down the entire shared preview (all three layers plus previewGroup itself, which
        /// takes its Rigidbody/grabbable/grab-colliders and every layer's mesh with it) - called
        /// whenever the selected .swc/options/level changes, since a layer left over from a different
        /// configuration could no longer be assumed to line up with a freshly built one, and on Back.
        /// </summary>
        private void ClearPreviewGroup()
        {
            if (previewGroup != null) { Destroy(previewGroup); previewGroup = null; }
            previewWireframeObject = null;
            preview1DObject = null;
            previewSurfaceObject = null;
            wireframeLayer = null;
            oneDLayer = null;
            surfaceLayer = null;
        }

        /// <summary>
        /// Turns one layer off - each of Preview/View 1D/View Surface toggles its own layer, removing
        /// it (without regenerating anything) if it's already showing rather than rebuilding it.
        /// Tears the whole previewGroup down once no layer is left, so the next button press
        /// re-establishes a fresh shared center/scale (see previewGroupCenter's own field comment)
        /// instead of anchoring new layers to an empty group's stale reference.
        /// </summary>
        private void RemoveLayer(PreviewLayerKind kind)
        {
            switch (kind)
            {
                case PreviewLayerKind.Wireframe:
                    if (previewWireframeObject != null) { Destroy(previewWireframeObject); }
                    previewWireframeObject = null;
                    wireframeLayer = null;
                    break;
                case PreviewLayerKind.OneD:
                    if (preview1DObject != null) { Destroy(preview1DObject); }
                    preview1DObject = null;
                    oneDLayer = null;
                    break;
                case PreviewLayerKind.Surface:
                    if (previewSurfaceObject != null) { Destroy(previewSurfaceObject); }
                    previewSurfaceObject = null;
                    surfaceLayer = null;
                    break;
            }

            if (previewWireframeObject == null && preview1DObject == null && previewSurfaceObject == null)
            {
                if (previewGroup != null) { Destroy(previewGroup); previewGroup = null; }
            }
            else
            {
                RegenerateGroupGrabColliders();
            }
        }

        // Capped well below real edge counts (a loaded .swc morphology can have thousands) per layer
        // so a grab-collider rebuild on every Preview/View click stays cheap - this is a rough "you
        // can grab it somewhere along its branches" approximation, not a tight fit to the geometry.
        private const int MaxGrabColliders = 300;

        /// <summary>
        /// Rebuilds previewGroup's entire set of grab colliders from scratch, from every
        /// currently-present layer (wireframe/1D/surface) - simpler and cheap enough to just redo in
        /// full than to incrementally patch in/out one layer's own colliders each time a layer is
        /// added or replaced. Each layer contributes its own small CapsuleCollider per (stride-sampled)
        /// edge, all as direct children of previewGroup - one level, not nested further, because
        /// ObjectMovementControl's desktop-mode click detection walks up exactly one parent from the
        /// hit collider (hit.collider.transform.parent.gameObject) to find the grabbable object, the
        /// same one level MeshSimulation's own colliders sit at under their mesh GameObject (via
        /// NonConvexMeshCollider's single "colliders" child). Grabbing any one segment - from any
        /// layer - moves previewGroup, and therefore every layer parented under it, together.
        /// </summary>
        private void RegenerateGroupGrabColliders()
        {
            var oldSegments = new List<GameObject>();
            foreach (Transform child in previewGroup.transform)
            {
                if (child.name == "GrabSegment") { oldSegments.Add(child.gameObject); }
            }
            foreach (var segment in oldSegments) { Destroy(segment); }

            AddGrabSegmentsForLayer(wireframeLayer);
            AddGrabSegmentsForLayer(oneDLayer);
            AddGrabSegmentsForLayer(surfaceLayer);

            var ovr = previewGroup.GetComponent<PublicOVRGrabbable>();
            ovr.M_GrabPoints = previewGroup.GetComponentsInChildren<CapsuleCollider>();
        }

        private void AddGrabSegmentsForLayer(PreviewLayer? layer)
        {
            if (layer == null) { return; }
            Vector3[] vertices = layer.Value.vertices;
            List<Edge> edges = layer.Value.edges;

            int edgeCount = edges.Count;
            int stride = edgeCount > MaxGrabColliders ? Mathf.CeilToInt((float)edgeCount / MaxGrabColliders) : 1;
            float radius = Mathf.Max(previewGroupMaxDim * 0.015f, 1e-4f);

            for (int i = 0; i < edgeCount; i += stride)
            {
                Vector3 from = vertices[edges[i].From.Id];
                Vector3 to = vertices[edges[i].To.Id];
                float length = Vector3.Distance(from, to);
                if (length < 1e-6f) { continue; }

                var segment = new GameObject("GrabSegment");
                segment.transform.SetParent(previewGroup.transform, false);
                segment.transform.localPosition = (from + to) * 0.5f;
                segment.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (to - from).normalized);

                var capsule = segment.AddComponent<CapsuleCollider>();
                capsule.direction = 1; // local Y axis, matching the rotation set above
                capsule.height = length + radius; // slight overlap so neighboring segments meet
                capsule.radius = radius;
            }
        }

        private void OnBackPressed()
        {
            ClearPreviewGroup();

            var button = FindObjectOfType<NeuronGeneratorButton>();
            if (button != null) { button.OnReturnedFromGenerator(); }

            // CellPreviewer only scans StreamingAssets/NeuronalDynamics/Geometries once at Start()
            // (its FileSystemWatcher handlers update internal bookkeeping but never actually
            // rebuild the UI) - explicitly refresh it here so a neuron generated just now shows up
            // as selectable immediately, without needing a full scene reload.
            if (GameManager.instance != null && GameManager.instance.cellPreviewer != null)
            {
                var previewer = GameManager.instance.cellPreviewer.GetComponent<CellPreviewer>();
                if (previewer != null) { previewer.Refresh(); }
            }

            SceneManager.UnloadSceneAsync(NeuronGeneratorButton.SceneName);
        }
    }
}
