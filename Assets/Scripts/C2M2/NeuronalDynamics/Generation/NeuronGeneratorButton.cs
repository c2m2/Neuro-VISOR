using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace C2M2.NeuronalDynamics.Generation
{
    /// <summary>
    /// Click handler for the in-room "NeuronGenerator" button (wired to RaycastPressEvents.onPress
    /// on the same GameObject, in MainScene.unity - see this project's own OculusEventSignaler/
    /// RaycastPressEvents raycast-button pattern, e.g. NDPauseButton.cs). Loads
    /// NeuronGeneratorScene additively rather than replacing MainScene outright - MainScene (and
    /// the VR rig/GameManager singleton it holds) stays loaded throughout, since nothing in this
    /// project currently protects the rig with DontDestroyOnLoad and a full scene swap would risk
    /// destroying it.
    ///
    /// Per the user's explicit direction, the main room itself is NOT hidden - the player should
    /// see the exact same room they started in, just with the CellPreviewer wall swapped out for
    /// the generator panel (NeuronGeneratorPanel positions itself at the CellPreviewer's own
    /// transform - see that class). Hiding/restoring the CellPreviewer wall uses
    /// `GameManager.instance.cellPreviewer` - the same public field/pattern this project's own
    /// Menu.cs (`gm.cellPreviewer.SetActive(false/true)`) and NDBoardController.cs already use for
    /// exactly this purpose, rather than inventing a new reference to it.
    /// </summary>
    public class NeuronGeneratorButton : MonoBehaviour
    {
        public const string SceneName = "NeuronGeneratorScene";

        // These sit alongside CellPreviewer(Clone) as top-level MainScene siblings, not as its
        // children (CellPreviewer.cs itself finds "Arrows" this same way, via GameObject.Find, which
        // wouldn't work if it were one of CellPreviewer's own children) - so hiding
        // GameManager.instance.cellPreviewer alone leaves every one of these still visible and still
        // clickable (pointing at a previewer that's now inactive) on top of the generator panel.
        private static readonly string[] CellPreviewerSiblingUINames =
            { "Refresh", "Arrows", "Page Counter", "FPS", "SaveLoad" };

        // GameObject.Find only ever finds active objects, so the references we hide here have to be
        // cached up front - looking them up again by name in OnReturnedFromGenerator would find
        // nothing, since by then every one of them is the very thing we've just deactivated.
        private readonly List<GameObject> hiddenSiblingUI = new List<GameObject>();

        // RaycastHit parameter matches RaycastPressEvents.onPress's own UnityEvent<RaycastHit>
        // signature (EventDefined persistent-call mode) - the same pattern this project's other
        // RaycastPressEvents.onPress handlers already use (e.g. Menu.cs's LoadThisFile).
        public void OnNeuronGeneratorPressed(RaycastHit hit)
        {
            if (SceneManager.GetSceneByName(SceneName).isLoaded) { return; }
            if (GameManager.instance != null && GameManager.instance.cellPreviewer != null)
            {
                GameManager.instance.cellPreviewer.SetActive(false);
            }
            hiddenSiblingUI.Clear();
            foreach (string name in CellPreviewerSiblingUINames)
            {
                GameObject go = GameObject.Find(name);
                if (go != null)
                {
                    go.SetActive(false);
                    hiddenSiblingUI.Add(go);
                }
            }
            SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
        }

        /// <summary>
        /// Called (via FindObjectOfType, since no design-time cross-scene reference can exist
        /// before both scenes are loaded together) by the NeuronGeneratorScene's own "Back to Main
        /// Room" button once NeuronGeneratorScene has finished unloading - restores the exact state
        /// the room was in before the generator scene opened.
        /// </summary>
        public void OnReturnedFromGenerator()
        {
            if (GameManager.instance != null && GameManager.instance.cellPreviewer != null)
            {
                GameManager.instance.cellPreviewer.SetActive(true);
            }
            foreach (GameObject go in hiddenSiblingUI)
            {
                if (go != null) { go.SetActive(true); }
            }
            hiddenSiblingUI.Clear();
        }
    }
}
