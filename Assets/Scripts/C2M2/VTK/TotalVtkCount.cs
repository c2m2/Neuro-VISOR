using UnityEngine;
using UnityEngine.UI;

namespace C2M2
{
    public class TotalVtkCount : MonoBehaviour
    {

        public VTKBehaviour vtkBehaviour;
        private Text text;
        private int totalCount;

        private void Awake()
        {
            text = gameObject.GetComponent<Text>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.frameCount == 1)
            {
                totalCount = vtkBehaviour.meshes.Count - 1;
                text.text = totalCount.ToString();
            }
            if (Time.frameCount % 100 == 0)
            {
                if (totalCount != vtkBehaviour.meshes.Count)
                {
                    totalCount = vtkBehaviour.meshes.Count - 1;
                    text.text = totalCount.ToString();
                }
            }
        }
    }
}
