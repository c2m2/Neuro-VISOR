using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace C2M2
{
    namespace SimulationScripts
    {
        public abstract class ThreadableSimulation : SealedSimulation
        {
            public bool startOnAwake = true;
            private Thread solveThread = null;
            protected Mutex mutex = new Mutex();

            // Computation code should be contained within Solve(). Simulation.cs will launch Solve() on its own thread.
            protected abstract void Solve();
            
            #region Simulation Thread Controls
            // Stop any current simulation thread and launch a new one
            public void StartSimulation()
            {
                StopSimulation();
                solveThread = new Thread(Solve);
                solveThread.Start();
                Debug.Log("Solve() launched on thread " + solveThread.ManagedThreadId);
            }
            // Stop current simulation thread
            public void StopSimulation() { if (solveThread != null) solveThread.Abort(); }

            #endregion

            #region Unity Methods
            protected sealed override void AwakeA()
            {
                AwakeB();
                if (startOnAwake)
                {
                    StartSimulation();
                }
            }
            protected sealed override void StartA() { StartB(); }
            protected sealed override void UpdateA() { UpdateB(); }
            protected virtual void AwakeB() { }
            protected virtual void StartB() { }
            protected virtual void UpdateB() { }

            // Don't allow threads to keep running on pause, quit
            private void OnApplicationPause(bool pause)
            {
                if (pause && solveThread != null) solveThread.Abort();
            }
            private void OnApplicationQuit()
            {
                if (solveThread != null) solveThread.Abort();
            }
            #endregion
        }
    }
}
