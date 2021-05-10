using TMPro;
using UnityEngine;

public class ClampInfo : MonoBehaviour
{
    public TextMeshProUGUI vertexText;
    public TextMeshProUGUI clampText;

    public Vector3 GlobalSize = new Vector3(.0005f, .0005f, .0005f);

    void Update()
    {
        transform.LookAt(transform.position);
        transform.rotation = Camera.main.transform.rotation;
        if (transform.lossyScale != GlobalSize) {
            Vector3 newLocalScale = new Vector3(
                transform.localScale.x * GlobalSize.x / transform.lossyScale.x,
                transform.localScale.y * GlobalSize.y / transform.lossyScale.y,
                transform.localScale.z * GlobalSize.z / transform.lossyScale.z);
            transform.localScale = newLocalScale;
        }
    }
}
