using UnityEngine;

public class ArrowUpdate : MonoBehaviour
{
    public Transform preSynapse;
    public Transform postSynapse;
    
    public Synapse pre;
    public Synapse post;
    

    /// <summary>
    /// A script to update arrow direction and size when the user has moved the neuron
    /// </summary>
    void Update()
    {
        if (preSynapse != null && postSynapse != null)
        {
            transform.SetParent(null);
            transform.position = Vector3.Lerp(preSynapse.position, postSynapse.position, 0.5f);
            transform.LookAt(postSynapse.position);
            transform.localScale = new Vector3(preSynapse.lossyScale.x / 4, preSynapse.lossyScale.x / 4, Vector3.Distance(preSynapse.position, postSynapse.position));
            transform.SetParent(preSynapse);
            
            Color newColor = new Color(0,0,0);
            // Assign color based on the synapse model 
            if (pre.currentModel == Synapse.Model.NMDA)
            {
                // NMDA color scheme
                newColor.r = 0f;
                newColor.g = .576f;
                newColor.b = .391f;
            }
            else 
            {
                // GABA color scheme
                newColor.r = .627f;
                newColor.g = 0f;
                newColor.b = .145f;
            }
            
            foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
            {
                mr.material.color = newColor;
            }
        }
    }
}
