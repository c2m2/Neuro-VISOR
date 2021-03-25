using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.Visualization;
using TMPro;
namespace C2M2.NeuronalDynamics.Interaction
{
    public class NDRefinementControl : MonoBehaviour
    {
        public NeuronCellPreview cellPreview = null;
        public TextMeshProUGUI displayTxt;

        private void Start()
        {
            if(cellPreview == null)
            {
                Debug.LogError("No cell previewer found.");
                Destroy(this);
            }

            UpdateDisplay();
        }

        // TODO: These could automatically tell which refinement levels are available for a specific cell
        public void IncreaseRefinement(RaycastHit hit)
        {
            if (RefinementAvailable(cellPreview.refinement + 1))
                cellPreview.refinement++;
            
            UpdateDisplay();
        }

        public void DecreaseRefinement(RaycastHit hit)
        {
            if (RefinementAvailable(cellPreview.refinement - 1))
                cellPreview.refinement--;

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            cellPreview.PreviewCell();

            displayTxt.text = "Refinement: " + cellPreview.refinement.ToString();
        }

        private bool RefinementAvailable(int refinement)
        {
            bool valid = false;
            // If the new refinement level is contained in the refinement options, we keep it
            foreach (int option in cellPreview.refinements)
            {
                if (option == refinement) valid = true;
            }
            return valid;
        }
    }
}