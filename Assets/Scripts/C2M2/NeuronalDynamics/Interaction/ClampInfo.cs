using TMPro;
using UnityEngine;

public class ClampInfo : MonoBehaviour
{
    public TextMeshProUGUI vertexText;
    public TextMeshProUGUI clampText;

    void Update()
    {
        transform.LookAt(transform.position);
        transform.rotation = Camera.main.transform.rotation;
    }
}
