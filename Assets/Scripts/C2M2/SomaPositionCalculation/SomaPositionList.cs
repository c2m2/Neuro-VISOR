using System.Collections.Generic;
using UnityEngine;
using C2M2.Interaction.VR;
using C2M2.NeuronalDynamics.Interaction;
using C2M2.NeuronalDynamics.Interaction.UI;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Simulation;

namespace C2M2.SomaPositionCalculation
{
    /*************************************************************************************************************
    SOMA POSITION LIST CLASS

    Class defining the list of SomaPosition objects. This is a list of all the somas in the simulation, seperate 
    from the GameManager.instance.activeSims list. It's primarily kept seperate so we can sort the list by distance
    without worrying about changing activeSims.

    @author: Adam Marx
    @date: 2025-04-28
    *************************************************************************************************************/
    public class SomaPositionList
    {
        /** PROPERTIES ********************************************************************************/
        public List<SomaPosition> somaPositions;                // Kept sorted by somas distance to target
        public bool anyReducible { get; private set; } = true;  // Are any somas characteristic distances reducible?

        // Class references
        private SpawnZone spawnZone; 
        private GameManager gm = GameManager.instance;


        /** CONSTRUCTOR ********************************************************************************/
        public SomaPositionList(SpawnZone spawnZone)
        {
            this.somaPositions = new List<SomaPosition>();
            this.spawnZone = spawnZone;

            // Populate the list with the locations of any active sims
            InitializeSomaPositionsList();
        }

        /** MUTATORS ********************************************************************************/
        // Important so that the next soma is placed as close to the target as possible
        public void SortByDistanceToTarget()
        {
            somaPositions.Sort((a, b) => a.distanceToTarget.CompareTo(b.distanceToTarget));
        }

        public void InsertSorted(SomaPosition newSoma)
        {
            int index = somaPositions.FindIndex(soma => soma.distanceToTarget > newSoma.distanceToTarget);
            if (index >= 0)
            {
                somaPositions.Insert(index, newSoma);
            }
            else
            {
                somaPositions.Add(newSoma); 
            }
        }

        // Initialize the list with the locations of any active sims
        private void InitializeSomaPositionsList()
        {
            foreach (NDSimulation sim in gm.activeSims)
            {
                Vector3 coordinates = sim.transform.position; 
                double charDist = sim.characteristicDistance.Value; 
                SomaPosition soma = new SomaPosition(coordinates, charDist, spawnZone);

                InsertSorted(soma); 
            }
        }

        // If any simulations (in activeSims) have been resized, we need to sync the characteristic distances with them.
        public void SyncAllCharDists()
        {
            foreach (NDSimulation sim in gm.activeSims)
            {
                sim.characteristicDistance.SyncWithSolver();
            }
        }

        // Called when calculating a new position to keep SomaPositionList and activeSims in sync
        public void RefreshSomaPositionList()
        {
            ClearSomaPositions();

            SyncAllCharDists(); 

            InitializeSomaPositionsList();
        }

        public void ClearSomaPositions()
        {
            somaPositions.Clear();
        }

        /*
        If possible, reduce all characteristic distances in the simulation. This is called when no more somas can be placed.
        Importantly, this updates the characteristic distances attached to the sims in activeSims, they are synchronized into 
        somaPoisitions on RefreshSomaPositionList() and InitializeSomaPositionsList().
        */
        public void TryReducingAllCharacteristicDistances()
        {
            anyReducible = false;

            // TODO: loop through activeSims instead, then refresh the list
            foreach (NDSimulation sim in gm.activeSims)
            {
                sim.characteristicDistance.Reduce(); 

                if (sim.characteristicDistance.IsReducible)
                {
                    // If it's still reducible, there must be at least one soma that can be reduced after this loop
                    anyReducible = true;
                }
            }

            RefreshSomaPositionList(); // Refresh the list of soma positions in the simulation
        }
    }
}
