using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace C2M2
{
    using Utilities;
    using static Utilities.Math;
    /// <summary> Provide a general access point for information regarding a live simulation. Simulations should inherit from this class and match its abstract methods </summary>
    /// TODO: This script should either replace DiffusionManager, or there should be some new SimulationManager script
    /// TODO: Create a get/set values requirement
    /// TODO: Solve code should possibly be wrapped in a method here that tracks pauses/unpauses, pauseTime, hasChanged, etc, for launching user solve code. This would simplify user's coding requirements
    /// TODO: This really should just be an interface for simulations happening somewhere else, it should only send and receive values to the simulation, not manage how the simulation runs
    public abstract class SimulationDiffuseReact : MonoBehaviour
    {
        protected ObjectManager objectManager;
        /// <summary> Scalar value corresponding to one 3d point on a simulation's mesh. </summary>
        public double[] simulationConditions { get; protected set; }
        /// <summary> Maximum allowed value of simulation conditions </summary>
        protected double max = 1;
        /// <summary> Minimum allowed value of simulation conditions </summary>
        protected double min = 0;
        /// <summary> Should outside forces, such as hand interaction, be allowed to affect simulation conditions? </summary>
        protected bool allowOutsideChanges = true;
        public enum PlayState { NULL, PLAYING, PAUSED }
        public PlayState playState { get; private set; } = PlayState.NULL;
        // Simulation stop/start utilities
        // TODO: Replace this with an enum,
        // Defer pause time tracking to the child script (Create NotifyPause, NotifyUnpause, NotifyStart, NotifyStop, etc
        public bool paused { get; private set; } = false;
        public bool started { get; private set; } = false;
        protected DateTime pauseStartTime;
        protected double pauseTime;
        protected bool newUnpause = false;

        #region AdaptiveSimulationCalculationRate
        [Header("Adaptive Simulation Calculation Rate")]
        [Tooltip("Change number of simulation steps calculated per second if performance is too poor (or good enough to allow for higher StepsPerSec). Simulation steps should be time-independent from stepsPerSec")]
        public bool adaptStepsPerSec = true;
        [Tooltip("World FPS to adapt stepsPerSec to")]
        public int idealRenderFPS = 50;
        [Tooltip("Diffusion Reaction steps simulated per second")]
        public int stepsPerSec = 20;
        #endregion

        protected Coroutine simulationRoutine;
        /// <summary> Has the simulation changed since the last time the flag was set to false? </summary>
        public bool hasChanged { get; protected set; }
        private void Update() { if (hasChanged) { hasChanged = false; } }
        /// <summary> Store the calling object manager, then defer to the custom simulation's initialization </summary>
        /// <param name="objectManager"> Object manager that creates this script </param>
        public void Initialize(ObjectManager objectManager)
        {
            this.objectManager = objectManager;
            Initialize();
        }
        /// <summary> Details initialization process of simulations that inherit from Simulation </summary>
        protected abstract void Initialize();
        public void StartSimulation()
        {
            if (playState != PlayState.NULL)
            {
                playState = PlayState.PLAYING;
                pauseTime = DateTime.Now.Subtract(pauseStartTime).TotalSeconds;
                newUnpause = true;
            }
            else
            {
                simulationRoutine = StartCoroutine(SolveWrapper(1 / stepsPerSec));
                playState = PlayState.PLAYING;
            }
        }
        public void StopSimulation()
        {
            playState = PlayState.PAUSED;
            pauseStartTime = DateTime.Now;
        }
        /// <summary> Wrap user simulation code in order to provide useful information and control play/pause functionality </summary>
        private IEnumerator SolveWrapper(float waitTime)
        {
            DateTime timeOld;
            DateTime timeNow = DateTime.Now;
            TimeSpan dtSpan;
            double dtReal = 0.02;
            while (true)
            { // If our previous frame resulted in 0 total flux, then we've reached total equilibrium and can stop
              // Clear the previous simulation step's flux values for each edge
                if (playState == PlayState.PLAYING)
                {
                    timeOld = timeNow;
                    timeNow = DateTime.Now;
                    dtSpan = timeNow.Subtract(timeOld);
                    dtReal = dtSpan.TotalSeconds;
                    // TODO: This is sloppy and should be driven by the Gauss add
                    if (objectManager.diffusionManager.gaussAddCond != null) { objectManager.diffusionManager.gaussAddCond.PlayFrame(); }
                    if (objectManager.diffusionManager.gaussSubCond != null) { objectManager.diffusionManager.gaussSubCond.PlayFrame(); }
                    if (newUnpause)
                    {
                        dtReal -= pauseTime;
                        newUnpause = false;
                    }
                    // Defer to simulation code, provide the real dt, cast result to a float array, send changes to mesh for coloring
                    objectManager.meshInfo.SubmitComponentDataChanges(Solve(dtReal).ToFloat());
                    hasChanged = true;
                    if (adaptStepsPerSec)
                    { /* Adapt FPS depending on FPS rate*/
                        AdaptStepsPerSec();
                        waitTime = 1f / stepsPerSec;
                    }
                }
                yield return new WaitForSecondsRealtime(waitTime);
            }
        }
        protected abstract double[] Solve(double dtReal);
        /// <summary> Adapt computation update speeds based on average frames per second </summary>
        private void AdaptStepsPerSec()
        {
            // If we're running too slow, do fewer simulation steps per second 
            if (GameManager.instance.fpsCounter.AverageFPS < idealRenderFPS) { stepsPerSec--; }
            // If we're above our ideal FPS, we can afford to do more simulation steps    
            else { stepsPerSec++; }
            // 5 < stepsPerSec < 150
            
            stepsPerSec = Min(Max(5, stepsPerSec), 150);
        }
        /// <summary> Send out changes to the mesh for visualization </summary>
        protected void ApplyChanges()
        {
            objectManager.meshInfo.SubmitComponentDataChanges(simulationConditions.ToFloat());
        }
        /// <summary> Attempt to change simulation values </summary>
        public void ChangeValues(double[] changes)
        {
            if (allowOutsideChanges)
            {
                if (changes.Length == simulationConditions.Length)
                {
                    for (int i = 0; i < changes.Length; i++)
                    {
                        simulationConditions[i] += changes[i];
                        simulationConditions[i] = Max(Min(simulationConditions[i], max), min);  // min < scalars[current] < max
                    }
                    ApplyChanges();
                }
                else
                {
                    throw new IndexOutOfRangeException("Changes array is not the same length as simulation value array");
                }
            }
        }
    }
}