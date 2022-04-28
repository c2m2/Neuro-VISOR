using UnityEngine;

public class CopyMaterial : MonoBehaviour
{
    public MeshRenderer meshRendererToCopy = null;
    private MeshRenderer mr = null;

    // Start is called before the first frame update
    void Start()
    {
        mr = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (mr == null) Debug.LogError("No MeshRenderer on Object");
        else if (meshRendererToCopy == null) Debug.LogError("No MeshRenderer to Copy Set");
        else if (mr.material != meshRendererToCopy.material) mr.material = meshRendererToCopy.material;
    }
}
