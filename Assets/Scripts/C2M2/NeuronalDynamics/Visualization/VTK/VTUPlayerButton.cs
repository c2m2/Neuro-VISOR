using UnityEngine;
using UnityEngine.UI;

namespace C2M2.Visualization.VTK
{
    public class VTUPlayerButton : MonoBehaviour
    {

        public enum ButtonType { PlayOrPause = 0, ForwardStep, BackwardStep, FastForward, FastBackward }

        public ButtonType buttonType;
        public VTUManager vtuManager;
        public Image mainImage;
        public Image clickedImage;

        private VTUPlayer vtuPlayer;
        // private int stepCode = 1000;
        private int animationSpeed;
        private int fastMultiplier;

        private void Awake()
        {
            vtuPlayer = gameObject.transform.parent.GetComponent<VTUPlayer>();
        }

        public void ClickButton()
        {
            switch (buttonType)
            {

                case ButtonType.PlayOrPause:

                    break;
                case ButtonType.ForwardStep:

                    break;
                case ButtonType.BackwardStep:

                    break;
                case ButtonType.FastForward:

                    break;
                case ButtonType.FastBackward:

                    break;
            }
        }
    }
}