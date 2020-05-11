using UnityEngine;
namespace C2M2
{
    /// <summary>
    /// 
    /// </summary>
    /// TODO: Shift key should only captialize while it is held down
    public class RaycastKeyboard : MonoBehaviour
    {
        public GameObject keyPrefab;
        public GameObject specialKeyContainer;
        public Transform lowerKeyContainer;
        public Transform upperKeyContainer;
        public Transform menuAnchor;
        public RaycastInputField activeField { get; private set; }
        private bool hasActiveField = false;
        private RaycastHit lastHit;
        private float[] keyPositions = { };
        // Rect transform position (x, y) for specialKeys[0] = (specialKeyLocations[0], specialKeyLocations[1]), and so on
        private static string[] specialKeys =
            { "tab", "del", "cap", "ent", "shift", "shift", "space" };
        private static float[] specialKeyLocations = { 258f, -413f, 2702f, -230f, 258f, -596f, 2663.25f, -596f, 258f, -779f, 2585.75f, -779f, 1023.107f, -962f };

        private static string[] lowerCharRow0 = new string[] { "'", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };
        private static string[] lowerCharRow1 = new string[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p", "[", "]", "\\" };
        private static string[] lowerCharRow2 = new string[] { "a", "s", "d", "f", "g", "h", "j", "k", "l", ";", "'" };
        private static string[] lowerCharRow3 = new string[] { "z", "x", "c", "v", "b", "n", "m", ",", ".", "/" };

        private static string[] upperCharRow0 = new string[] { "~", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "_", "+" };
        private static string[] upperCharRow1 = new string[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "{", "}", "|" };
        private static string[] upperCharRow2 = new string[] { "A", "S", "D", "F", "G", "H", "J", "K", "L", ":", "\"" };
        private static string[] upperCharRow3 = new string[] { "Z", "X", "C", "V", "B", "N", "M", "<", ">", "?" };

        private static float[] positionj = { -230f, -413f, -596f, -779f };
        private static float[] position0i = { 258f, 446f, 634f, 822f, 1010f, 1198f, 1386f, 1574f, 1762f, 1950f, 2138f, 2326f, 2514f };
        private static float[] position1i = { 523.5f, 711.5f, 899.5f, 1087.5f, 1275.5f, 1463.5f, 1651.5f, 1839.5f, 2027.5f, 2215.5f, 2403.5f, 2591.5f, 2779.5f };
        private static float[] position2i = { 565f, 755.7497f, 946.4996f, 1137.25f, 1328f, 1518.75f, 1709.5f, 1900.25f, 2091f, 2281.75f, 2472.5f };
        private static float[] position3i = { 645.75f, 839.7496f, 1033.75f, 1227.75f, 1421.75f, 1615.75f, 1809.75f, 2003.75f, 2197.75f, 2391.75f };

        private void Awake()
        {
            BuildKeyboard();
        }
        public bool PassChar(string c)
        {
            if (hasActiveField)
            { // I fwe have an input field, pass the character and return true to the key
                activeField.CharacterIntake(c);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void InputFieldActivate(RaycastInputField newActiveField, RaycastHit hit)
        {
            if (activeField != null)
            {
                activeField.Deactivate();
                lastHit = new RaycastHit();
            }
            if (newActiveField != null)
            {
                activeField = newActiveField;
                hasActiveField = true;
                lastHit = hit;
            }
            else
            {
                hasActiveField = false;
                lastHit = new RaycastHit();
            }
        }

        private void OnEnable()
        { // When the keyboard is active, snap it between 
            if (menuAnchor != null)
            {
                transform.position = menuAnchor.position;
                transform.eulerAngles = menuAnchor.eulerAngles;
            }
            else
            {
                Debug.LogError("No menu anchor found.");
                transform.position = Vector3.zero;
            }
        }
        private void BuildKeyboard()
        {
            // Instantiate key prefab for each key in each row and parent it under its respective parent
            float widthOffset = 1635f;
            float heightOffset = 514f;
            GameObject curObj;
            Vector3 curPos;
            for (int i = 0; i < lowerCharRow0.Length; i++)
            { // Build row 0
              // Find the key position
                curPos = new Vector3(position0i[i] - widthOffset, positionj[0] + heightOffset, 0);
                // Initialize the lowercase key
                curObj = Instantiate(keyPrefab, lowerKeyContainer);
                curObj.GetComponent<RaycastKey>().character = lowerCharRow0[i];
                curObj.name = "Key " + lowerCharRow0[i];
                curObj.GetComponent<RectTransform>().localPosition = curPos;
                //curObj.transform.localPosition = curPos;
                // Initialize the uppercase key
                curObj = Instantiate(keyPrefab, upperKeyContainer);
                curObj.GetComponent<RaycastKey>().character = upperCharRow0[i];
                curObj.name = "Key" + upperCharRow0[i];
                //curObj.transform.localPosition = curPos;
                curObj.GetComponent<RectTransform>().localPosition = curPos;
            }
            for (int i = 0; i < lowerCharRow1.Length; i++)
            { // Build row 1
              // Find the key position
                curPos = new Vector3(position1i[i] - widthOffset, positionj[1] + heightOffset, 0);
                // Initialize the lowercase key
                curObj = Instantiate(keyPrefab, lowerKeyContainer);
                curObj.GetComponent<RaycastKey>().character = lowerCharRow1[i];
                curObj.name = "Key " + lowerCharRow1[i];
                //curObj.transform.localPosition = curPos;
                curObj.GetComponent<RectTransform>().localPosition = curPos;
                // Initialize the uppercase key
                curObj = Instantiate(keyPrefab, upperKeyContainer);
                curObj.GetComponent<RaycastKey>().character = upperCharRow1[i];
                curObj.name = "Key" + upperCharRow1[i];
                //curObj.transform.localPosition = curPos;
                curObj.GetComponent<RectTransform>().localPosition = curPos;
            }
            for (int i = 0; i < lowerCharRow2.Length; i++)
            { // Build row 2
              // Find the key position
                curPos = new Vector3(position2i[i] - widthOffset, positionj[2] + heightOffset, 0);
                // Initialize the lowercase key
                curObj = Instantiate(keyPrefab, lowerKeyContainer);
                curObj.GetComponent<RaycastKey>().character = lowerCharRow2[i];
                curObj.name = "Key " + lowerCharRow2[i];
                curObj.transform.localPosition = curPos;
                // Initialize the uppercase key
                curObj = Instantiate(keyPrefab, upperKeyContainer);
                curObj.GetComponent<RaycastKey>().character = upperCharRow2[i];
                curObj.name = "Key" + upperCharRow2[i];
                //curObj.transform.localPosition = curPos;
                curObj.GetComponent<RectTransform>().localPosition = curPos;
            }
            for (int i = 0; i < lowerCharRow3.Length; i++)
            { // Build row 3
              // Find the key position
                curPos = new Vector3(position3i[i] - widthOffset, positionj[3] + heightOffset, 0);
                // Initialize the lowercase key
                curObj = Instantiate(keyPrefab, lowerKeyContainer);
                curObj.GetComponent<RaycastKey>().character = lowerCharRow3[i];
                curObj.name = "Key " + lowerCharRow3[i];
                // curObj.transform.localPosition = curPos;
                curObj.GetComponent<RectTransform>().localPosition = curPos;
                // Initialize the uppercase key
                curObj = Instantiate(keyPrefab, upperKeyContainer);
                curObj.GetComponent<RaycastKey>().character = upperCharRow3[i];
                curObj.name = "Key" + upperCharRow3[i];
                //curObj.transform.localPosition = curPos;
                curObj.GetComponent<RectTransform>().localPosition = curPos;
            }
            upperKeyContainer.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
