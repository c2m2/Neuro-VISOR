#pragma warning disable 0618 // Ignore obsolete script warning

using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
namespace C2M2.Interaction.UI
{
    using Simulation;
    /// <summary> Store useful information about individual simulation vertices, and spawn info panels to display that info </summary>
    public class PointInfo : MonoBehaviour
    {
        private ObjectManager objectManager;
        #region displayUtils
        public TextMeshProUGUI vertNum;
        public TextMeshProUGUI vertPos;
        public TextMeshProUGUI curValReading;
        public Image curColReading;
        public Transform infoPanel;
        public Transform lineRendInfoPanelAnchor;
        public Transform pointFollower;
        private LineRenderer lineRend;
        #endregion
        #region infoStorage
        private double curVal;
        private int vertToWatch;
        private Color curCol;
        #endregion

        void Awake()
        {
            lineRend = GetComponent<LineRenderer>();
            menuSnapPosition = GameManager.instance.menuSnapPosition;
        }
        void Update()
        {
            lineRend.SetPosition(0, pointFollower.position);
            lineRend.SetPosition(1, lineRendInfoPanelAnchor.position);
        }
        public void Close() { Destroy(gameObject); }
        public void InitializeInfoPanel(ObjectManager objectManager, RaycastHit hit)
        {
            this.objectManager = objectManager;
            transform.position = Vector3.zero;
            // Get the vertex to monitor and the adjacencyList
            vertToWatch = objectManager.meshInfo.FindNearestUniqueVert(hit);
            // Initialize info text and image
            vertNum.text = "Unique Vertex\n" + vertToWatch.ToString();
            Vector3 pos = this.objectManager.meshInfo.uniqueVerts[vertToWatch];
            pointFollower.position = this.objectManager.meshInfo.transform.TransformPoint(pos);
            InitializePanelLocation(); // Resolve initial info panel position and rotation
            vertPos.text = pos.ToString("F2"); // Reflect vert position info
            UpdateInfo();
        }
        private double addVal = 0;
        public void ReadValue(string s)
        { // If we receive a valid double value, insert it into the diffusion via the adjacency list
            Color curCol = curValReading.color;
            if (double.TryParse(s, out addVal))
            { // If we have a valid input, change value reading to green and set a timer to switch the color back to default
                StartCoroutine(CurValReadingDefaultColorTimed(0.3f, curValReading.color));
                curValReading.color = Color.green;
                objectManager.diffusionManager.DiffusionInsertValue(vertToWatch, addVal);
            }
            else
            { // Otherwise change the color to red and do NOT insert the value
                StartCoroutine(CurValReadingDefaultColorTimed(0.3f, curValReading.color));
                curValReading.color = Color.red;
            }
        }
        private IEnumerator CurValReadingDefaultColorTimed(float delayTime, Color defaultCol)
        {
            yield return new WaitForSeconds(delayTime);
            curValReading.color = defaultCol;
        }
        // TODO: We should trigger a hasChanged event from the diffusion to the object/diffusion manager, which then triggers UpdateInfo here. Then we wont have any busy waiting
        private void LateUpdate()
        {
            if (objectManager.diffusionManager.activeDiffusion.hasChanged)
            {
                UpdateInfo();
            }
        }
        private Transform menuSnapPosition;
        private void InitializePanelLocation()
        {
            infoPanel.position = Vector3.Lerp(infoPanel.position, menuSnapPosition.position, (1f / 3f));
            infoPanel.LookAt(Camera.main.transform);
            infoPanel.Rotate(0, 180, 0);
        }
        private void UpdateInfo()
        {
            curVal = objectManager.meshInfo.scalars[vertToWatch];
            // TODO: Is there a way to cache these strings? Maybe store a dicitonary lookup of 50-100 doubles to their F6 strings, remove infrequently used strings
            curValReading.text = objectManager.meshInfo.scalars[vertToWatch].ToString("F6");    // Update point scalar value display
            curCol = objectManager.meshInfo.ColorFromUniqueIndex(vertToWatch);                  // Get color of the current point
            curColReading.color = curCol;
            curCol.a = 0.5f;        // Turn down the alpha for the line renderer
            lineRend.startColor = curCol;
            lineRend.endColor = curCol;
        }
    }
}
