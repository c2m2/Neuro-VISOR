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
            this.gameObject.transform.SetParent(null);
            this.gameObject.transform.position = Vector3.Lerp(preSynapse.position, postSynapse.position, 0.5f);
            this.gameObject.transform.LookAt(postSynapse.position);
            this.gameObject.transform.localScale = new Vector3(preSynapse.lossyScale.x / 4, preSynapse.lossyScale.x / 4, Vector3.Distance(preSynapse.position, postSynapse.position));
            this.gameObject.transform.SetParent(preSynapse);
        }

    }
}