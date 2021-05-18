using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.Visualization;
using TMPro;
using UnityEngine.UI;
namespace C2M2.NeuronalDynamics.Interaction
{
    public class NDRefinementControl : MonoBehaviour
    {
        public NeuronCellPreview cellPreview = null;
        public TextMeshProUGUI displayTxt;
        public Image upDefaultImg = null;
        public Image upErrorImg = null;
        public Image downDefaultImg = null;
        public Image downErrorImg = null;
        public float errorImgTime = 0.5f;

        private void Start()
        {
            if(cellPreview == null)
            {
                Debug.LogError("No cell previewer found.");
                Destroy(this);
            }

            UpdateDisplay(true);
        }

        // TODO: These could automatically tell which refinement levels are available for a specific cell
        public void IncreaseRefinement(RaycastHit hit)
        {
            if (RefinementAvailable(cellPreview.refinement + 1))
                cellPreview.refinement++;
            else
                StartCoroutine(DrawError(true, errorImgTime));
            
            UpdateDisplay(true);
        }

        public void DecreaseRefinement(RaycastHit hit)
        {
            if (RefinementAvailable(cellPreview.refinement - 1))
                cellPreview.refinement--;
            else
                StartCoroutine(DrawError(false, errorImgTime));

            UpdateDisplay(false);
        }

        private void UpdateDisplay(bool increase)
        {
            try
            {
                cellPreview.PreviewCell();
                
            }catch(Visualization.VRN.CouldNotReadMeshFromVRNArchive e)
            {
                Debug.LogWarning("Could not read mesh for given refinement level. Removing refinement level from options.");
                Debug.LogWarning(e);
                cellPreview.RemoveRefinement(cellPreview.refinement);
                StartCoroutine(DrawError(increase, errorImgTime));
            }

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
        private IEnumerator DrawError(bool increase, float seconds)
        {
            // Error check
            if (increase)
            {
                if (upDefaultImg == null || upErrorImg == null)
                {
                    Debug.LogWarning("Images missing for NDRefinementControl");
                    yield break;
                }
            }
            else
            {
                if (downDefaultImg == null || downErrorImg == null)
                {
                    Debug.LogWarning("Images missing for NDRefinementControl");
                    yield break;
                }
            }

            float startTime = Time.time;

            // Enable error image for [seconds] + 15 frames in case performance is bad
            while(Time.time < startTime + seconds)
            {
                yield return new WaitForEndOfFrame();
                ToggleErrorImg(true);
            }
            
            for(int i = 0; i < 15; i++)
            {
                yield return new WaitForEndOfFrame();
                ToggleErrorImg(true);
            }

            ToggleErrorImg(false);

            void ToggleErrorImg(bool toggle)
            {
                if (increase)
                {
                    upDefaultImg.enabled = !toggle;
                    upErrorImg.enabled = toggle;
                }
                else
                {
                    downDefaultImg.enabled = !toggle;
                    downErrorImg.enabled = toggle;
                }
            }
        }
    }
}