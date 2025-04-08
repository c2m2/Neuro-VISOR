using System.Collections.Generic;
using UnityEngine;
namespace C2M2.NeuronalDynamics.Simulation
{
    public class IonChannelManager : MonoBehaviour
    {
        public List<IonChannel> ionChannels { get; private set; }
        public List<IonChannel> activeIonChannels { get; private set; }

        void Awake()
        {
            // Initialize lists
            ionChannels = new List<IonChannel>();
            activeIonChannels = new List<IonChannel>();
        }
        // public void UpdateConductance(string channelName, double newConductance)
        // {
        //     IonChannel channel = ionChannels.FirstOrDefault(ch => ch.Name.Equals(channelName));
        //     if (channel != null)
        //     {
        //         channel.Conductance = newConductance;
        //     }
        // }


        public void AddIonChannel(IonChannel channel)
        {
            ionChannels.Add(channel);
            if (channel.IsActive)
                activeIonChannels.Add(channel);
        }

        public void ActivateIonChannel(IonChannel channel)
        {
            if (!channel.IsActive)
            {
                channel.Activate();
                activeIonChannels.Add(channel);
            }
        }

        public void DeactivateIonChannel(IonChannel channel)
        {
            if (channel.IsActive)
            {
                channel.Deactivate();
                activeIonChannels.Remove(channel);
            }
        }

        public bool IsIonChannelActive(string channelName)
        {
            // Check active list for a matching channel name.
            foreach (var channel in activeIonChannels)
            {
                if (channel.Name.Equals(channelName))
                    return true;
            }
            return false;
        }
    }
}
