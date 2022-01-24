using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using UnityEngine.UI;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDPauseButton : MonoBehaviour
    {
        public NDSimulationController simController = null;
        public NDSimulation Sim { get { return (NDSimulation)GameManager.instance.activeSims[0]; } }
        public Color defaultCol;
        public Color DefaultCol
        {
            get
            {
                return (simController == null) ? defaultCol : simController.defaultCol;
            }
        }
        public Color hoverCol;
        public Color HoverCol
        {
            get
            {
                return (simController == null) ? hoverCol : simController.highlightCol;
            }
        }
        public Color pressCol;
        public Color PressCol
        {
            get
            {
                return (simController == null) ? pressCol : simController.pressedCol;
            }
        }

        public Image playButton = null;
        public Image pauseButton = null;
        public int buttonSize = 25;

        private void Awake()
        {
            NullChecks();
            ResizeButtons();
            
            void NullChecks()
            {
                bool fatal = false;
                if (playButton == null)
                {
                    Debug.LogError("No play button given.");
                    fatal = true;

                }
                if (pauseButton == null)
                {
                    Debug.LogError("No pause button given.");
                    fatal = true;
                }
                if (simController == null)
                {
                    simController = GetComponentInParent<NDSimulationController>();
                    if (simController == null)
                    {
                        Debug.LogError("No sim controller found.");
                        fatal = true;
                    }
                }
                if (fatal) Destroy(this);
            }
        }

        private void Start()
        {
            UpdateDisplay();
        }

        // Don't allow threads to keep running when application pauses or quits
        private void OnApplicationPause(bool pause)
        {
            if (pause != Sim.paused) TogglePause();
        }

        public void TogglePause()
        {
            Sim.paused = !Sim.paused;

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            pauseButton.enabled = !Sim.paused;
            playButton.enabled = Sim.paused;
        }

        public void DefaultButtonCol() => ChangeButtonCol(DefaultCol);
        public void HoverButtonCol() => ChangeButtonCol(HoverCol);
        public void PressButtonCol() => ChangeButtonCol(PressCol);
        private void ChangeButtonCol(Color col)
        {
            pauseButton.color = col;
            playButton.color = col;
        }

        private void ResizeButtons()
        {
            playButton.rectTransform.localScale = new Vector3(buttonSize / playButton.rectTransform.sizeDelta.x,
                buttonSize / playButton.rectTransform.sizeDelta.y, 1f);

            pauseButton.rectTransform.localScale = new Vector3(buttonSize / pauseButton.rectTransform.sizeDelta.x,
                buttonSize / pauseButton.rectTransform.sizeDelta.y, 1f);

            GetComponent<BoxCollider>().size = new Vector3(buttonSize, buttonSize, 1f);
        }

    }
}