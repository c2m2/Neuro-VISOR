using System;
using System.IO;
using System.Threading.Tasks;
using C2M2.Interaction;
using C2M2.NeuronalDynamics.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;

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
    /// (anchoring to `GameManager.instance.cellPreviewer.transform`, which NeuronGeneratorButton
    /// hides before this scene loads) rather than floating wherever the camera happens to be - the
    /// rest of the main room stays exactly as the player left it.
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
        // regardless of the real neuron's own physical size - see BuildWireframePreview.
        private const float PreviewWorldSize = 3.2f;

        private string selectedSwcPath;
        private NeuronGeneratorService.Options options = NeuronGeneratorService.Options.Default();
        private int methodIndex;
        private int previewLevel;
        private bool generating;
        private bool previewing;

        private GameObject swcListParent;
        private GameObject previewObject;

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

            // Anchor this panel's POSITION exactly where the CellPreviewer wall normally sits
            // (NeuronGeneratorButton hides GameManager.instance.cellPreviewer before loading this
            // scene, so this panel visually takes its place there) - but face it toward the camera
            // rather than blindly inheriting the CellPreviewer transform's own rotation, which
            // turned out not to face outward into the room at all (confirmed on real hardware: the
            // panel rendered as a heavily skewed, nearly edge-on sliver instead of a flat, readable
            // wall panel). This scene deliberately has no camera of its own (an earlier version
            // did, and that broke mouse clicking entirely - MouseEventSignaler always raycasts from
            // Camera.main, the rig's own persistent camera, which stays exactly where it was in the
            // room regardless of which camera actually rendered the screen, so what was visible and
            // what was clickable were computed from two different viewpoints).
            Transform anchor = GameManager.instance != null && GameManager.instance.cellPreviewer != null
                ? GameManager.instance.cellPreviewer.transform
                : null;
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            if (anchor != null)
            {
                transform.position = anchor.position;
                if (cam != null)
                {
                    transform.rotation = Quaternion.LookRotation(transform.position - cam.position, Vector3.up);
                }
            }
            else if (cam != null)
            {
                transform.position = cam.position + cam.forward * 1.5f;
                transform.rotation = Quaternion.LookRotation(transform.position - cam.position, Vector3.up);
            }

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
            RefreshStatus();
        }

        private void CycleMethod()
        {
            methodIndex = (methodIndex + 1) % Methods.Length;
            options.method = Methods[methodIndex];
            RefreshStatus();
        }

        private void AdjustN(int delta)
        {
            options.n = Mathf.Max(1, options.n + delta);
            // Level must stay a valid index into the n levels a Generate would actually produce.
            previewLevel = Mathf.Clamp(previewLevel, 0, options.n - 1);
            RefreshStatus();
        }

        private void AdjustLevel(int delta)
        {
            previewLevel = Mathf.Clamp(previewLevel + delta, 0, Mathf.Max(0, options.n - 1));
            RefreshStatus();
        }

        private void AdjustDelta(double factor)
        {
            options.delta = Math.Max(0.01, options.delta * factor);
            RefreshStatus();
        }

        private void AdjustSides(int delta)
        {
            options.sides = Mathf.Clamp(options.sides + delta, 3, 64);
            RefreshStatus();
        }

        private void ToggleRepair()
        {
            options.repair = !options.repair;
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
        /// far cheaper than a full N-level Generate) and renders it as a wireframe using the same
        /// UGXReader this project already uses to load real neuron meshes for simulation - see
        /// BuildWireframePreview for why a hand-built MeshTopology.Lines mesh is used here instead
        /// of this project's existing LinesRenderer (which recursively walks every vertex - fine
        /// for a 1D skeleton's few hundred/thousand nodes, but risks a stack overflow at surface
        /// mesh scale, tens of thousands of vertices even at coarse settings).
        /// </summary>
        private async void OnPreviewPressed()
        {
            if (previewing || generating) { return; }
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
                    BuildWireframePreview(result.ugxPath);
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
        /// Parses a single-mesh .ugx file (written by nm_generate_mesh_file) via UGXReader - the
        /// same reader this project already uses to load real neuron surface meshes for simulation
        /// - and renders its edges as a plain MeshTopology.Lines mesh, normalized to fit within
        /// PreviewWorldSize world-space meters and centered above the control panel (see
        /// PreviewWorldSize's own comment). Replaces any previous preview object outright, so
        /// repeated Preview clicks never accumulate extra geometry.
        /// </summary>
        private void BuildWireframePreview(string ugxPath)
        {
            var mesh = new Mesh();
            var grid = new Grid(mesh, "NeuronGeneratorPreview");
            C2M2.NeuronalDynamics.UGX.UGXReader.ReadUGX(ugxPath, ref grid);

            Vector3[] positions = grid.Mesh.vertices;
            var lineMesh = new Mesh();
            if (positions.Length > 65535) { lineMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; }
            lineMesh.vertices = positions;
            lineMesh.RecalculateBounds();
            Bounds bounds = lineMesh.bounds;

            // Re-center around the mesh's own bounds so it appears in front of the preview
            // GameObject's own local origin, rather than wherever the raw SWC/generated
            // coordinates happen to sit in space.
            var centered = new Vector3[positions.Length];
            for (int i = 0; i < positions.Length; i++) { centered[i] = positions[i] - bounds.center; }
            lineMesh.vertices = centered;

            var indices = new int[grid.Edges.Count * 2];
            for (int i = 0; i < grid.Edges.Count; i++)
            {
                indices[i * 2] = grid.Edges[i].From.Id;
                indices[i * 2 + 1] = grid.Edges[i].To.Id;
            }
            lineMesh.SetIndices(indices, MeshTopology.Lines, 0);
            lineMesh.RecalculateBounds();

            if (previewObject != null) { Destroy(previewObject); }
            previewObject = new GameObject("PreviewWireframe");
            // Placed at the room's own designated neuron-display center
            // (GameManager.instance.simulationSpace - the same anchor GrabRescaler uses as its
            // rescale target for simulated neurons) rather than relative to this panel, since "the
            // very center of the room" is a world-space request, not one relative to wherever the
            // panel happens to be anchored (the CellPreviewer wall). Left unparented (world space)
            // for the same reason - OnBackPressed explicitly destroys this object itself, since an
            // unparented object isn't guaranteed to belong to NeuronGeneratorScene and so wouldn't
            // necessarily be cleaned up by that scene's own unload.
            Vector3 roomCenter = GameManager.instance != null && GameManager.instance.simulationSpace != null
                ? GameManager.instance.simulationSpace.transform.position
                : transform.position;
            previewObject.transform.position = roomCenter;
            previewObject.transform.rotation = Quaternion.identity;

            float maxDim = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            float scale = maxDim > 1e-6f ? PreviewWorldSize / maxDim : 1f;
            previewObject.transform.localScale = new Vector3(scale, scale, scale);

            var meshFilter = previewObject.AddComponent<MeshFilter>();
            meshFilter.mesh = lineMesh;

            var renderer = previewObject.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            var material = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
            material.color = Color.red;
            renderer.material = material;
        }

        private void OnBackPressed()
        {
            // The preview object is now placed unparented at the room's world-space center (see
            // BuildWireframePreview), not as a child of this panel, so it isn't guaranteed to live
            // in NeuronGeneratorScene and can't be relied on to be cleaned up by that scene's own
            // unload below - destroy it explicitly.
            if (previewObject != null) { Destroy(previewObject); previewObject = null; }

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
