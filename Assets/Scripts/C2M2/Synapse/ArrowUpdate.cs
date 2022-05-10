using UnityEngine;

public class ArrowUpdate : MonoBehaviour
{
    public Transform preSynapse;
    public Transform postSynapse;

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

            //TODO rewrite this section
            Color synapseColor = preSynapse.gameObject.GetComponent<Synapse>().meshRenderer.material.color;
            foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
            {
                mr.material.color = synapseColor;
            }
        }

    }
}