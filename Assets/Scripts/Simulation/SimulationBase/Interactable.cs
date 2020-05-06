using System;
using UnityEngine;

namespace C2M2
{
    namespace SimulationScripts
    { 
        using InteractionScripts;
        using Utilities;
        /// <summary>
        /// Manages interaction input to simulations
        /// </summary>
        /// <remarks>
        /// InteractableSimulation provides a public link to SetValues() so that other scripts don't need to know the type parameter of Simulation.
        /// Only Simulation.cs should derive from this class. Custom simulation code should derive from Simulation.cs or one
        /// of its derived classes such as ScalarFieldSimulation, Neuron1DSimulation, etc
        /// </remarks>
        public abstract class Interactable : ThreadableSimulation
        {
            private RaycastSimHeater simHeater = null;
            public RaycastSimHeater SimHeater
            {
                get { return simHeater; }
                protected set
                {
                    if (simHeater != null) Destroy(simHeater);
                    simHeater = value;
                }
            }
            public InteractionType interactionType = InteractionType.Discrete;

            /// <summary> Send an array of (index, newValue) pairings for hit points </summary>
            /// <remarks>
            /// In order to affect live simulations, this method must know how to add values between 0 and 1 to the current simulation values
            /// </remarks>
            public abstract void SetValues(Tuple<int, double>[] newValues);

            #region Unity Methods
            // Allow derived classes to run code in Awake/Start/Update if they choose
            protected sealed override void AwakeB()
            {
                AwakeC();
                switch (interactionType)
                {
                    case (InteractionType.Discrete): simHeater = gameObject.AddComponent<RaycastSimHeaterDiscrete>(); break;
                    case (InteractionType.Continuous): simHeater = gameObject.AddComponent<RaycastSimHeaterContinuous>(); break;
                }
                
                gameObject.AddComponent<FrameCountTransformReset>();

                RaycastEventManager eventManager = gameObject.AddComponent<RaycastEventManager>();
    
                GameObject child = new GameObject("HitInteractionEvent");
                child.transform.parent = transform;
                child.transform.position = Vector3.zero;
                child.transform.eulerAngles = Vector3.zero;

                RaycastPressEvents raycastEvents = child.AddComponent<RaycastPressEvents>();
                raycastEvents.OnHoldPress.AddListener((hit) => simHeater.Hit(hit));

                eventManager.rightTrigger = raycastEvents;
                eventManager.leftTrigger = raycastEvents;

                //Destroy(tempObj);
            }
            protected sealed override void StartB()
            {
                StartC();
                gameObject.AddComponent<VRGrabbable>();
            }
            protected sealed override void UpdateB() { UpdateC(); }
            protected virtual void AwakeC() { }
            protected virtual void StartC() { }
            protected virtual void UpdateC() { }
            #endregion

            public enum InteractionType { Discrete, Continuous }
        }
    }
}
