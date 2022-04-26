using UnityEngine;
using UnityEngine.UI;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDPauseButton : MonoBehaviour
    {
        public NDBoardController boardController = null;
        public Color defaultCol;
        public Color DefaultCol
        {
            get
            {
                return (boardController == null) ? defaultCol : boardController.defaultCol;
            }
        }
        public Color hoverCol;
        public Color HoverCol
        {
            get
            {
                return (boardController == null) ? hoverCol : boardController.highlightCol;
            }
        }
        public Color pressCol;
        public Color PressCol
        {
            get
            {
                return (boardController == null) ? pressCol : boardController.pressedCol;
            }
        }

        public Image playButton = null;
        public Image pauseButton = null;
        public int buttonSize = 25;

        // get and set the pause state of the simulation
        public bool PauseState
        {
            get { return GameManager.instance.simulationManager.Paused; }
            set { GameManager.instance.simulationManager.Paused = value; }
        }

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
                if (boardController == null)
                {
                    boardController = GetComponentInParent<NDBoardController>();
                    if (boardController == null)
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
            if (pause != GameManager.instance.simulationManager.Paused) TogglePause();
        }

        // Use Paused as a shorthand
        private bool Paused
        {
            get { return GameManager.instance.simulationManager.Paused; }
            set { GameManager.instance.simulationManager.Paused = value; }
        }

        public void TogglePause()
        {
            // Toggle pause state for all simulatuons
            Paused = !Paused;

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            pauseButton.enabled = !Paused;
            playButton.enabled = Paused;
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