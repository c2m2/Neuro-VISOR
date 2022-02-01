using System.Collections;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using TMPro;
using UnityEngine.UI;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDSimulationController : MonoBehaviour
    {
        public Color defaultCol = new Color(1f, 0.75f, 0f);
        public Color highlightCol = new Color(1f, 0.85f, 0.4f);
        public Color pressedCol = new Color(1f, 0.9f, 0.6f);
        public Color errorCol = Color.red;
        public Image[] defColTargets = new Image[0];
        public Image[] hiColTargets = new Image[0];
        public Image[] pressColTargets = new Image[0];
        public Image[] errColTargets = new Image[0];

        public GameObject defaultBackground;
        public GameObject minimizedBackground;

        private bool Minimized
        {
            get
            {
                return !defaultBackground.activeSelf;
            }
        }

        private void Start()
        {
            StartCoroutine(UpdateColRoutine(0.5f));
        }

        private void UpdateCols()
        {
            foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if(text != null) text.color = defaultCol;
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


            // Reactivate cell previewer
            GameObject cellPreviewer = GameObject.FindGameObjectWithTag("CellPreviewer");
            cellPreviewer.SetActive(true);
        }

        public void CloseAllSimulations()
        {
            for(int i = 0; i < GameManager.instance.activeSims.Count; i++)
            {
                CloseSimulation(i);
            }
        }

        public void CloseSimulation(int simIndex)
        {
            simIndex = Mathf.Clamp(simIndex, 0, GameManager.instance.activeSims.Count - 1);
            NDSimulation sim = (NDSimulation)GameManager.instance.activeSims[simIndex];
            if (sim != null)
            {
                // Destroy the cell's ruler
                sim.CloseRuler();

                // Destroy the cell
                Destroy(sim.gameObject);

                // Destroy this control panel
                Destroy(transform.root.gameObject);

                if (GameManager.instance.cellPreviewer != null)
                {
                    // Reenable the cell previewer
                    GameManager.instance.cellPreviewer.SetActive(true);
                }
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
        }

        public void MinimizeToggle()
        {
            MinimizeBoard(!Minimized);
        }
    }
}