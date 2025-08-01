using System.Collections;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using TMPro;
using UnityEngine.UI;
using C2M2.Utils;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDBoardController : MonoBehaviour
    {
        public Color defaultCol = new Color(1f, 0.75f, 0f);
        public Color highlightCol = new Color(1f, 0.85f, 0.4f);
        public Color pressedCol = new Color(1f, 0.9f, 0.6f);
        public Color errorCol = Color.red;
        public Color textCol = new Color(1f, 0.75f, 0f);
        public Image[] defColTargets = new Image[0];
        public Image[] hiColTargets = new Image[0];
        public Image[] pressColTargets = new Image[0];
        public Image[] errColTargets = new Image[0];

        public GameObject defaultBackground;
        public GameObject minimizedBackground;

        
        public GameObject SaveButton;
        public GameObject StopButton;
        private TextMeshProUGUI[] textElements = null;
        private CSVWriter csv = null;


        private bool Minimized
        {
            get
            {
                return !defaultBackground.activeSelf;
            }
        }

        private void Start()
        {
            textElements = GetComponentsInChildren<TextMeshProUGUI>(true);

            StartCoroutine(UpdateColRoutine(0.5f));
            
            StopButton = gameObject.transform.GetChild(6).gameObject;
            StopButton.SetActive(false);
        }

        private void UpdateCols()
        {
            foreach (TextMeshProUGUI text in textElements)
            {
                if(text != null) text.color = textCol;
            }
            foreach(Image i in defColTargets)
            {
                if(i != null) i.color = defaultCol;
            }
            foreach (Image i in hiColTargets)
            {
                if(i != null) i.color = highlightCol;
            }
            foreach (Image i in pressColTargets)
            {
                if(i != null) i.color = pressedCol;
            }
            foreach(Image i in errColTargets)
            {
                if(i != null) i.color = errorCol;
            }
        }

        IEnumerator UpdateColRoutine(float waitTime)
        {
            while (true)
            {
                UpdateCols();
                yield return new WaitForSeconds(waitTime);
            }
        }

        public void AddSimulation()
        {
            // Minimize control board if there is one
            MinimizeBoard(true);

            // Reactivate cell previewer
            GameManager.instance.cellPreviewer.SetActive(true);
        }

        public void CloseAllSimulations()
        {
            // set paused to false; it's needed for load function to work properly
            if (GameManager.instance.simulationManager.Paused)
                GameManager.instance.simulationManager.Paused = false;

            // delete Synapse scripts under SynapseManager object
            for (int i = GameManager.instance.simulationManager.synapseManager.synapses.Count - 1; i >= 0; i--)
            {
                GameManager.instance.simulationManager.synapseManager.DeleteSyn(GameManager.instance.simulationManager.synapseManager.synapses[i].Item1);
            }

            // disable Save button
            Menu m = FindObjectOfType<Menu>();
            m.CloseFileList();
            m.SaveButtonVisible(false);

            for (int i = GameManager.instance.activeSims.Count-1; i >= 0; i--)
            {
                CloseSimulation(i);
            }
        }

        public void CloseSimulation(int simIndex)
        {
            NDSimulation sim = (NDSimulation)GameManager.instance.activeSims[simIndex];
            if (sim != null)
            {
                GameManager.instance.activeSims.Remove(sim);

                // Destroy the cell
                Destroy(sim.gameObject);
                
                if (GameManager.instance.cellPreviewer != null)
                {
                    // Reenable the cell previewer
                    GameManager.instance.cellPreviewer.SetActive(true);

                    // Destroy this control panel
                    Destroy(transform.root.gameObject);
                }

                // Destroy ruler if no cells are left
                // TODO See NDSimulationLoader for note on ruler generation and removal improvement
                if (GameManager.instance.activeSims.Count == 0) Destroy(GameObject.Find("Ruler"));
            }
        }

        public void MinimizeBoard(bool minimize)
        {
            if (defaultBackground == null || minimizedBackground == null)
            {
                Debug.LogWarning("Can't find minimize targets");
                return;
            }
            defaultBackground.SetActive(!minimize);
            minimizedBackground.SetActive(minimize);

            // Ensure cell previewer is not present if board is expanded 
            if (!minimize) GameManager.instance.cellPreviewer.SetActive(false);
        }

        public void StartCSV(bool single)
        {   //restrict user from saving multiple csv files, disable button after clicking
            csv = GameManager.instance.activeSims[0].gameObject.AddComponent<CSVWriter>();
            NDSimulation sim = (NDSimulation)GameManager.instance.activeSims[0];
            sim.solver = (SparseSolverTestv1)GameManager.instance.activeSims[0];
            sim.csv = csv;
            csv.single = single;
            
        }
        public void StopCSV()
        {   
            NDSimulation sim = (NDSimulation)GameManager.instance.activeSims[0];
            sim.convert = true;
            
            
        }

        public void MinimizeToggle()
        {
            MinimizeBoard(!Minimized);
        }
    }
}