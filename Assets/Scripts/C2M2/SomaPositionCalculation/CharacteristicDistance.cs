using System;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.SomaPositionCalculation
{    
    /*************************************************************************************************************
    CHARACTERISTIC DISTANCE CLASS

    A class for objects that contain a value representing a "characteristic distance" of a soma. The idea of this
    value is to define a distance from the soma that no somas may be automatically placed into (so there is an 
    exclusion zone around each soma with a radius of CharacteristicDistance.Value). This is useful for preventing 
    somas from overlapping and for discouraging dendrites from overlapping.

    Currently the value is set to be a percentage of the maximum dimension of the simulation size. The class also 
    deals with reducing the characteristic distance, and for updating the value when the simulation size changes.

    @author: Adam Marx
    @date: 2025-04-28
    *************************************************************************************************************/
    public class CharacteristicDistance
    {
        /** PROPERTIES ********************************************************************************/
        public double Value { get; set; }
        public bool IsReducible { get; private set; } = true; // Flag to check if the characteristic distance is reducible
        private int TimesReduced = 0; // Number of times the characteristic distance has been reduced

        // Constants
        public const double MIN_VALUE = 0.01; // Minimum value for characteristic distance
        public const double REDUCTION_SCALE = 0.9; // Scale for reducing the characteristic distance
        public const double DEFAULT_VALUE = 0.5; // Default characteristic distance
        private const double PERCENT_OF_MAX_LENGTH = 0.15; // Used for setting value based on size (ie 15% of max length)

        // Class references
        private Transform solverTransform = null; // Optional reference
        private System.Random random = new System.Random(); 


        /** CONSTRUCTORS ********************************************************************************/
        /*
        New CharacteristicDistance with value based on the maximum dimension of cellSize. .

        @param cellSize The size of the cell in 3D space.
        @param solver The solver transform. This is needed to keep the characteristic distance 
        in sync with any changes to the simulation size.
        */
        public CharacteristicDistance(Vector3 cellSize, Transform solver = null)
        {
            SetValueBasedOnSize(cellSize);
            solverTransform = solver;
        }

        // Constructor with default value, possibly useful for edge-cases
        public CharacteristicDistance()
        {
            Value = DEFAULT_VALUE;
        }


        /** MUTATORS ********************************************************************************/
        /*
        Reduces the characteristic distance by multiplying it by REDUCTION_SCALE. This should be called
        when no more somas can be placed, implying that we can reduce all distances to make room. 
        */
        public void Reduce()
        {
            IsReducible = false;

            // If we can reduce, do so and increase the counter
            if (Value > MIN_VALUE)
            {
                Value = Math.Max(MIN_VALUE, Value * REDUCTION_SCALE);
                TimesReduced++;
            }            

            // If value is still above the minimum after reducing, the distance is still reducible
            IsReducible = Value > MIN_VALUE;
        }

        /*
        If the distance has been synced with the solver, any reductions that have been applied to the
        characteristic distance must be reapplied.
        */
        public void ReApplyReductions()
        {
            IsReducible = false;

            // Multiply value by REDUCTION_SCALE^TimesReduced, with a lower bound of MIN_VALUE
            Value = Math.Max(MIN_VALUE, Value * Math.Pow(REDUCTION_SCALE, TimesReduced));

            // If value is still above the minimum after reducing, the distance is still reducible
            IsReducible = Value > MIN_VALUE;
        }

        /*
        Call this function when the solver is resized. It will update the characeristic distance according
        to its new size.
        */
        public void SyncWithSolver()
        {
            if (solverTransform != null)
            {
                var size = solverTransform.GetComponent<MeshRenderer>().bounds.size;
                SetValueBasedOnSize(size);

                // Reapply any reductions that have been made to the characteristic distance
                ReApplyReductions(); 
            }
        }

        /*
        Given a size, set value to be a percentage of the maximum dimension.

        @param size The size of the cell in 3D space (bounding box)
        */
        public void SetValueBasedOnSize(Vector3 size)
        {
            Value = Math.Max(size.x, Math.Max(size.y, size.z)) * PERCENT_OF_MAX_LENGTH;
        }
    }
}