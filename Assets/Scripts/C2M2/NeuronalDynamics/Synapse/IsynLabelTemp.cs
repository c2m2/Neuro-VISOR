using UnityEngine;
using TMPro;

public class IsynLabelTemp : MonoBehaviour
{
    public Synapse self;
    private Synapse pre;
    private Synapse post;
    public TMP_Text label;

    void Awake()
    {
        if (label != null)
        {
            label.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (post == null)
        {
            SynapseManager manager = self.SynapseManager;

            var pairs = manager.FindSynapsePair(self);

            var pair = pairs[0];
            pre = pair.Item1;
            post = pair.Item2;

            if (self == post)
            {
                label.gameObject.SetActive(true);
            }
        }
        float iSyn = (float)post.currentIsyn;
        float iMax = (float)post.currentModel.Value.getImax();
        float iSynDivided = iSyn / iMax;

        double taud = (float)post.currentModel.Value.getTaud();


        label.text = "ISyn: " + iSynDivided.ToString("F3") + "\n (t - ts) / taud: " + ((self.simulation.GetSimulationTime() - pre.ActivationTime) / taud).ToString("F3");
        label.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);


    }
}
