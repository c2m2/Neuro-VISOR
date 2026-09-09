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
        }
    }
}
