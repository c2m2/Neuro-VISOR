using C2M2.NeuronalDynamics.Interaction;
using TMPro;
using UnityEngine;

public class ClampInfo : MonoBehaviour
{
    public TextMeshProUGUI vertexText;
    public TextMeshProUGUI clampText;
    private NeuronClamp clamp;
    //TODO set clamp

    void Start()
    {

    }

    void Update()
    {
        vertexText.text = clamp.FocusVert.ToString();
        clampText.text = clamp.ClampPower.ToString();
    }
}
