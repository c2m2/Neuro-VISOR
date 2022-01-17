using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Simulation;
using TMPro;
using UnityEngine.UI;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDSimulationController : MonoBehaviour
    {
        public List<NDSimulation> Sims
        {
            get
            {
                if(GameManager.instance.activeSims.Count < 1) { Debug.LogError("No simulations found."); }
                List<NDSimulation> sims = new List<NDSimulation>(GameManager.instance.activeSims.Count);
                foreach(Interactable inter in GameManager.instance.activeSims)
                {
                    sims.Add((NDSimulation)inter);
                }

                return sims;
            }
        }
        public Color defaultCol = new Color(1f, 0.75f, 0f);
        public Color highlightCol = new Color(1f, 0.85f, 0.4f);
        public Color pressedCol = new Color(1f, 0.9f, 0.6f);
        public Color errorCol = Color.red;
        public Image[] defColTargets = new Image[0];
        public Image[] hiColTargets = new Image[0];
        public Image[] pressColTargets = new Image[0];
        public Image[] errColTargets = new Image[0];

        private void Start()
        {
            /*
            if(sim == null)
            {
                Debug.LogError("No simulation given.");
                Destroy(gameObject);
            }
            */
            StartCoroutine(UpdateColRoutine(0.5f));
        }

        private void UpdateCols()
        {
            foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.color = defaultCol;
            }
            foreach(Image i in defColTargets)
            {
                i.color = defaultCol;
            }
            foreach (Image i in hiColTargets)
            {
                i.color = highlightCol;
            }
            foreach (Image i in pressColTargets)
            {
                i.color = pressedCol;
            }
            foreach(Image i in errColTargets)
            {
                i.color = errorCol;
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

        public void CloseCell(NDSimulation sim)
        {
            if (sim != null)
            {
                GameManager.instance.activeSims.Remove(sim);
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

        public void CloseAllCells()
        {
            foreach(NDSimulation sim in Sims)
            {
                CloseCell(sim);
            }
        }

        public void AddCell()
        {
            // Minimize the control board

            // Reopen the cell previewer

            // Listen for new cell

            // Add new cell to list of cells

            // Instantiate cell according to existing cells


        }
    }
}