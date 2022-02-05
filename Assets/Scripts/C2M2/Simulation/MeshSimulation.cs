using UnityEngine;
using C2M2.Visualization;
using C2M2.Interaction;
using System;

namespace C2M2.Simulation
{
    using Utils;
    using Interaction.VR;
    /// <summary>
    /// Simulation of type double[] for visualizing scalar fields on mesh surfaces
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public abstract class MeshSimulation : Simulation<float[], Mesh, VRRaycastableMesh, VRGrabbableMesh>
    {
        public MeshSimulationManager Manager { get { return GameManager.instance.simulationManager; } }
        #region Variables   

        /// <summary>
        /// Lookup table for more efficient color calculations on the gradient
        /// </summary>
        public ColorLUT ColorLUT { get { return Manager.colorLUT; } }

        public float GlobalMax
        {
            get { return Manager.GlobalMax; }
            set { Manager.GlobalMax = value; }
        }

        public float GlobalMin
        {
            get { return Manager.GlobalMin; }
            set { Manager.GlobalMin = value; }
        }

        /// <summary>
        /// Unit display string that can be manually set by the user
        /// </summary>
        [Tooltip("Unit display string that can be manually set by the user")]
        public string unit = "mV";
        /// <summary>
        /// Can be used to manually convert Gradient Display values to match unit string
        /// </summary>
        [Tooltip("Can be used to manually convert Gradient Display values to match unit string")]
        public float unitScaler = 1000f;

        public string lengthUnit = "μm";

        public Vector3 rulerInitPos = new Vector3(-0.5f, 0.443f, -0.322f);
        public Vector3 rulerInitRot = new Vector3(90, 0, 0);

        public double raycastHitValue = 55;
        public Tuple<int, double>[] raycastHits = new Tuple<int, double>[0];

        public RulerMeasure ruler = null;

        private Mesh visualMesh = null;
        public Mesh VisualMesh
        {
            get
            {
                return visualMesh;
            }
            protected set
            {
                //if (value == null) return;
                visualMesh = value;

                var mf = GetComponent<MeshFilter>();
                if (mf == null) gameObject.AddComponent<MeshFilter>();
                if (GetComponent<MeshRenderer>() == null)
                    gameObject.AddComponent<MeshRenderer>().sharedMaterial = GameManager.instance.vertexColorationMaterial;
                mf.sharedMesh = visualMesh;
            }
        }
        private Mesh colliderMesh = null;
        public Mesh ColliderMesh
        {
            get { return colliderMesh; }
            protected set
            {
                //if (value == null) return;
                colliderMesh = value;

                var cont = GetComponent<VRRaycastableMesh>();
                if (cont == null) { cont = gameObject.AddComponent<VRRaycastableMesh>(); }
                cont.SetSource(colliderMesh);
            }
        }

        protected MeshFilter mf;
        protected MeshRenderer mr;
        #endregion

        /// <summary>
        /// Update vertex colors based on simulation values
        /// </summary>
        protected override void UpdateVisualization(in float[] newValues)
        {
            Color32[] newCols = ColorLUT.Evaluate(newValues);
            if(newCols != null)
            {
                mf.mesh.colors32 = newCols;
            }
        }

        protected override void OnAwakePost(Mesh viz) //refactor
        {
            if (!dryRun)
            {
                InitRenderer();
                InitColors();
                InitInteraction();
            }
            return;

            void InitRenderer()
            {
                // Safe check for existing MeshFilter, MeshRenderer
                mf = GetComponent<MeshFilter>();
                if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = viz;

                mr = GetComponent<MeshRenderer>();
                if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();

                // Ensure the renderer has a vertex coloring material     
                mr.material = GameManager.instance.vertexColorationMaterial;
            }
            void InitColors()
            {
                ColorLUT.GlobalMax = GlobalMax;
                ColorLUT.GlobalMin = GlobalMin;
            }

            

            void InitInteraction()
            {
                VRRaycastableMesh raycastable = gameObject.GetComponent<VRRaycastableMesh>();
                if(raycastable == null) raycastable = gameObject.AddComponent<VRRaycastableMesh>();

                if (ColliderMesh != null) raycastable.SetSource(ColliderMesh);
                else raycastable.SetSource(viz);
                
                gameObject.AddComponent<VRGrabbableMesh>();
                GrabRescaler rescaler =  gameObject.AddComponent<GrabRescaler>();
                gameObject.AddComponent<PositionResetControl>();

                defaultRaycastEvent.OnHover.AddListener((hit) =>
                {
                    rescaler.Rescale();
                });
                defaultRaycastEvent.OnHoldPress.AddListener((hit) =>
                {
                    ShiftRaycastValue();
                });
                defaultRaycastEvent.OnEndPress.AddListener((hit) =>
                {
                    ResetRaycastHits();
                });

                // Instantiate ruler
                GameObject rulerObj = Resources.Load("Prefabs/Ruler") as GameObject;
                GameObject rulerGameObj = GameObject.FindGameObjectWithTag("Ruler");
                if (rulerGameObj == null)
                {
                    ruler = GameObject.Instantiate(rulerObj).GetComponent<RulerMeasure>();
                }
                else
                {
                    ruler = rulerGameObj.GetComponent<RulerMeasure>();
                }

                ruler.sim = this;
                rulerObj.transform.position = rulerInitPos;
                rulerObj.transform.eulerAngles = rulerInitRot;
                rulerObj.name = gameObject.name + "Ruler";
            }
        }

        public KeyCode powerModifierPlusKey = KeyCode.UpArrow;
        public KeyCode powerModifierMinusKey = KeyCode.DownArrow;

        // Sensitivity of the clamp power control. Lower sensitivity means clamp power changes more quickly
        public float sensitivity = 200f;
        public float ThumbstickScaler { get { return (ColorLUT.GlobalMax - ColorLUT.GlobalMin) / sensitivity; } }
        public float PowerModifier
        {
            get
            {
                if (GameManager.instance.vrDeviceManager.VRActive)
                {
                    // Uses the value of both joysticks added together
                    float scaler = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y + OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;

                    return ThumbstickScaler * scaler;
                }
                else
                {
                    if (Input.GetKey(powerModifierPlusKey)) return ThumbstickScaler;
                    if (Input.GetKey(powerModifierMinusKey)) return -ThumbstickScaler;
                    else return 0;
                }
            }
        }
        public void ShiftRaycastValue()
        {
            raycastHitValue += PowerModifier;
            raycastHitValue = Math.Clamp(raycastHitValue, ColorLUT.GlobalMin, ColorLUT.GlobalMax);
        }

        public void CloseRuler()
        {
            if (ruler != null)
            {
                Destroy(ruler.gameObject);
            }
        }

        public void ResetRaycastHits()
        {
            raycastHits = new Tuple<int, double>[0];
        }
    }
}