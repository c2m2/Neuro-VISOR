using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using C2M2.Interaction;
namespace C2M2.NeuronalDynamics.Visualization.VTK
{
    //TODO: Convert this into a manager with Singleton's (http://wiki.unity3d.com/index.php/Singleton, https://answers.unity.com/questions/50716/how-to-make-a-global-variable-in-unity.html)
    public class VTUPlayer : MonoBehaviour
    {

        public VTUManager vtuManager;
        public TextMeshProUGUI frameCountDisplay;
        public RaycastPressEvents fullPause;
        private MeshFilter mf;
        private int maxFrame;
        private Slider slider;
        private string formatString = "[{0}/{1}]";

        private int currentFrame = 0;
        public void Initialize()
        {
            mf = vtuManager.GetComponent<MeshFilter>();
            maxFrame = vtuManager.vtuList.Count - 1;
            frameCountDisplay.text = string.Format(formatString, currentFrame.ToString(), maxFrame);
            slider = GetComponentInChildren<Slider>();
            if (slider != null)
            {
                slider.minValue = 0;
                slider.maxValue = maxFrame;
            }
            else
            {
                Debug.LogError("Could not find slider");
            }
            //Play(10);
        }
        /// <summary> Flip forward through animation frames repeatedly </summary>
        public void Play(int framesPerSecond)
        {
            StopAllCoroutines();
            if (framesPerSecond < 1)
            {
                framesPerSecond = 1;
            }
            StartCoroutine(NextFrameRepeating((1 / framesPerSecond)));
        }
        /// <summary> Pause the animation </summary>
        public void Pause()
        {
            StopAllCoroutines();
        }
        /// <summary> Flip backwards through animation frames repeatedly </summary>
        public void Rewind(int framesPerSecond)
        {
            StopAllCoroutines();
            if (framesPerSecond < 1)
            {
                framesPerSecond = 1;
            }
            StartCoroutine(PreviousFramePrevious((1 / framesPerSecond)));
        }
        /// <summary> Slide up to the next animation frame </summary>
        public void NextFrame()
        {
            currentFrame++;
            if (currentFrame <= maxFrame)
            {
                slider.value = currentFrame;
                UpdateMesh();
            }
            else
            {
                currentFrame = maxFrame;
                fullPause.Press(new RaycastHit());
            }
        }
        /// <summary> Slide back to the next animation frame </summary>
        public void PreviousFrame()
        {
            currentFrame--;
            if (currentFrame >= 0)
            {
                slider.value = currentFrame;
                UpdateMesh();
            }
            else
            {
                currentFrame = 0;
                fullPause.Press(new RaycastHit());
            }
        }
        /// <summary> Enumerator for flipping forwards through frames </summary>
        private IEnumerator NextFrameRepeating(float waitTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(waitTime);
                NextFrame();
            }
        }
        /// <summary> Enumerator for flipping backwards through frames </summary>
        private IEnumerator PreviousFramePrevious(float waitTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(waitTime);
                PreviousFrame();
            }
        }
        /// <summary> Update the mesh (and menu text) to reflect changes </summary>
        private void UpdateMesh()
        {
            mf.mesh = vtuManager.vtuList[currentFrame].mesh;
            frameCountDisplay.text = string.Format(formatString, currentFrame.ToString(), maxFrame);
        }
    }
}
