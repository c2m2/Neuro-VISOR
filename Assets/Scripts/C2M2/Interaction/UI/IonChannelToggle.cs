using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using C2M2.NeuronalDynamics.Simulation;

public class IonChannelToggle : MonoBehaviour
{
    // Reference to your simulation manager (assign in Inspector or via singleton)
    public IonChannelManager simulationManager;
    public GameObject togglePrefab;
    
    // Parent containers for toggles. Assign these in the Inspector.
    public Transform openPanel;
    public Transform closedPanel;

    // Lists to hold created toggles for later use if needed
    private List<Toggle> openToggles = new List<Toggle>();
    private List<Toggle> closedToggles = new List<Toggle>();

    void Start()
    {
        // First, create toggles for all ion channels in simulationManager.ionChannels.
        // We'll separate them into active and inactive based on simulationManager.activeIonChannels.

        foreach (IonChannel channel in simulationManager.ionChannels)
        {
            // Instantiate a toggle from the prefab as a child of the appropriate panel.
            Debug.Log(channel);
            GameObject toggleObj;
            if (simulationManager.IsIonChannelActive(channel.Name))
            {
                toggleObj = Instantiate(togglePrefab, openPanel);
            }
            else
            {
                toggleObj = Instantiate(togglePrefab, closedPanel);
            }

            Toggle toggle = toggleObj.GetComponent<Toggle>();
            if (toggle == null)
            {
                Debug.LogError("Toggle prefab is missing a Toggle component.");
                continue;
            }

            // Set the label text to the ion channel name.
            TextMeshProUGUI label = toggleObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = channel.Name;
            }
            else
            {
                Debug.LogWarning("Toggle prefab is missing a TextMeshProUGUI component for the label.");
            }

            // Set the toggle state based on whether the channel is active.
            toggle.isOn = simulationManager.IsIonChannelActive(channel.Name);

            // Add a listener to update the simulation manager when the toggle changes.
            // Note: We capture the current channel in a local variable for the closure.
            IonChannel currentChannel = channel;
            toggle.onValueChanged.AddListener((bool isOn) =>
            {
                if (isOn)
                {
                    simulationManager.ActivateIonChannel(currentChannel);
                    // Optionally, move the toggle to the openPanel:
                    toggle.transform.SetParent(openPanel, false);
                }
                else
                {
                    simulationManager.DeactivateIonChannel(currentChannel);
                    // Optionally, move the toggle to the closedPanel:
                    toggle.transform.SetParent(closedPanel, false);
                }
            });

            // Store the toggle in the appropriate list for later reference.
            if (toggle.isOn)
            {
                openToggles.Add(toggle);
            }
            else
            {
                closedToggles.Add(toggle);
            }
        }
    }
    void Update()
    {
        
    }
}