using UnityEngine;
using System.Collections.Generic;

namespace C2M2.SomaPositionCalculation
{
    /*************************************************************************************************************
    SOMA POSITION CLASS

    A class for objects that represent the position of existing somas in the simulation. These objects are 
    derived from the actual simulations / solvers, but are not the same.

    @author: Adam Marx
    @date: 2025-04-28
    *************************************************************************************************************/
    public class SomaPosition
    {
        /** PROPERTIES ********************************************************************************/
        public double distanceToTarget {get; private set; }       // So SomaPositionList can sort by distance
        public double characteristicDistance {get; private set; } // Other somas can't spawn closer than this distance
        public Vector3 coordinates {get; private set; }           // Position of the Soma in 3D space
        public Vector3 target {get; private set; }                // Soma is placed as close to target as possible

        // Object references
        SpawnZone spawnZone;


        /** CONSTRUCTORS ********************************************************************************/
        public SomaPosition(Vector3 coordinates, double characteristicDistance, SpawnZone spawnZone){
            this.coordinates = coordinates;
            this.characteristicDistance = characteristicDistance;
            this.spawnZone = spawnZone;

            // Target is the center of the SpawnZone by default
            target = spawnZone.CenterPoint;
        }


        /** MUTATORS ********************************************************************************/
        /*
        Function is not used right now, but will be useful for generating custom neuronal systems
        (we will likely want to set a custom target). Somas will be placed as close to the target as possible.
        */
        public void SetTarget(Vector3 target){
            this.target = target;
        }
    }
}