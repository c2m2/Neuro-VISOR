using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using C2M2.NeuronalDynamics.Simulation;

namespace C2M2.SomaPositionCalculation
{
    /*************************************************************************************************************
    SOMA POSITION CALCULATOR CLASS

    This is the core class of the SomaPositionCalculation namespace. It handles the position of a new SomaPosition,
    given a SomaPositionList with N SomaPositions.

    Sampled unit sphere points are pre-generated and stored in untSphereEdgePoints.txt - functions are included 
    to modify this if we wish.

    @author: Adam Marx
    @date: 2025-04-28
    *************************************************************************************************************/
    public class SomaPositionCalculator
    {
        /** PROPERTIES ********************************************************************************/
        private List<Vector3> fixedSpherePoints; // List of sampled points on the unit sphere

        // Constants
        private const int NUM_POINTS = 150; // Number of points to sample on the unit sphere

        // Class references
        private SpawnZone spawnZone;
        public SomaPositionList somaPositionList { get; set; } = null;


        /** CONSTRUCTOR ********************************************************************************/
        public SomaPositionCalculator()
        {
            this.spawnZone = new SpawnZone();
            this.somaPositionList = new SomaPositionList(spawnZone);

            // Generates NUM_POINTS points on the unit sphere and saves them to a file. Uncomment to change saved points.
            //SavePointsToFile(GenerateFixedSpherePoints(NUM_POINTS), "unitSphereEdgePoints.txt");

            // Reads pre-sampled unit sphere points into list
            this.fixedSpherePoints = LoadPointsFromFile("unitSphereEdgePoints.txt");
        }


        /** PRIMARY FUNCTIONS ***********************************************************************************************/
        /*
        Main function of the namespace. Calculate the "best" position to next place a soma. The idea is
        to try and place at the target (center by default), and place at the next closest point if that fails.
        By the Karush-Kuhn-Tucker property, that next closest point will be on the edge of an exclusion zone 
        (ie the sphere around a soma with a radius of its characteristic distance).

        In the case where no more somas can be placed, this function returns the origin vector (0,0,0).
        This avoids a crash if the user tries to place too many cells, although this happening is very rare 
        given how low CharacteristicDistance.Value is allowed to go.

        @param charDistObj object containing the value that represents the characteristic distance
        of the soma being placed.
        @param target The target position to place the soma. This is the center of the spawn zone by default.
        */
        public Vector3? CalculateNextSomaPosition(NDSimulation sim, Vector3 target)
        {
            // Set the simulations position to be arbitrarily far away. This is a bit silly, but prevents
            // a bug where SomaPositionList thinks there is an extra soma at the origin
            sim.transform.position = new Vector3(500,500,500);

            // somaPositionList must be synced with GameManager.Instance.active
            somaPositionList.RefreshSomaPositionList();

            Vector3? nextCoordinates;
            CharacteristicDistance charDistObj = sim.characteristicDistance;
            double charDist = charDistObj.Value;

            if (spawnZone.IsFull)
            {
                UnityEngine.Debug.Log("Spawn zone is full! Please remove some cells to make room.");
                return new Vector3(0,0,0);
            }

            // If characteristic distance is too big...
            if (!spawnZone.CanContainLength(charDist))
            {
                // ...set it to half of the smallest dimension of spawnZone
                charDist = spawnZone.GetSmallestDimension() / 2;
                charDistObj.Value = charDist;
            }      
            
            // Try target first
            if (IsTargetAvailable(target))
            {
                nextCoordinates = target;
                somaPositionList.InsertSorted(new SomaPosition((Vector3)nextCoordinates, charDist, spawnZone));
                return target;
            }
            
            // Otherwise, try the closest point on the edge of an exclusion zone (google Karush-Kuhn-Tucker property)
            foreach (SomaPosition soma in somaPositionList.somaPositions)
            {
                /*
                We're now concerned with two characteristic distances: the one of the soma we're checking against,
                and the one of the soma we're trying to place. We want to try placing our soma the maximum of these
                two distances away from the soma we're checking against.
                */ 
                double maxCharDist = Math.Max(charDist, soma.characteristicDistance);

                // Find next valid point around soma (will also be closest to the target)
                nextCoordinates = FindFirstValidPointAround(soma, maxCharDist, charDist);

                // If it's not null, we found a valid point
                if (nextCoordinates != null){
                    somaPositionList.InsertSorted(new SomaPosition((Vector3)nextCoordinates, charDist, spawnZone));
                    return nextCoordinates;
                }
            }

            // If we still are unsuccessful, try lowering all characteristic distances
            if (somaPositionList.anyReducible)
            {
                somaPositionList.TryReducingAllCharacteristicDistances();

                // We haven't placed a soma yet, so we need to reduce the charDist here too
                charDist = Math.Max(charDist * CharacteristicDistance.REDUCTION_SCALE, CharacteristicDistance.MIN_VALUE);
                charDistObj.Value = charDist;

                // Try again with the new characteristic distances
                return CalculateNextSomaPosition(sim, target);
            }

            // If we can't, mark the space as full and return null
            else
            {
                spawnZone.IsFull = true;
                return new Vector3(0,0,0);
            }
        }

        // Convenience function that assumes the target is the center of the spawn zone
        public Vector3? CalculateNextSomaPosition(NDSimulation sim)
        {
            return CalculateNextSomaPosition(sim, spawnZone.CenterPoint);
        }

        /*
        Given a SomaPosition, sample points around it radiusFromDistance away. Sort these points based
        on their distance to the target, and iterate through them. Use characteristicDistance (of the soma
        we'd like to place) to check if each point is valid). The first valid point we find will be the
        closest to the target.

        @param origin The soma we're checking for a valid point around
        @param radiusFromOrigin the distance from the origin we're sampling points (in a sphere around origin)
        @param characteristicDistance The characteristic distance of the soma we're trying to place
        @return The first valid point we find, or null if no valid point is found
        */
        private Vector3? FindFirstValidPointAround(SomaPosition origin, double radiusFromOrigin, double characteristicDistance)
        {
            // Sort our list of fixed sphere points based on distance to the target
            SortSpherePoints(origin.coordinates, origin.target);

            foreach (Vector3 point in fixedSpherePoints)
            {
                // Scale the point to the relative location of and distance from the origin
                Vector3 scaledPoint = origin.coordinates + point * (float)radiusFromOrigin;

                if (IsValidPlacement(scaledPoint, characteristicDistance))
                {
                    return scaledPoint;
                }
            }

            return null;
        }

        /** BOOL CHECKS ************************************************************************************/
        /*
        Determines if a given position is valid for a new soma with a given characteristic distance.
        A point is valid if:
        1. It's within the spawn zone
        2. It's not within the reach of the characteristic distance of any other somas
        3. No other somas are within the reach of the characteristic distance of the point

        @param origin 
        @param characteristicDistance 
        */
        private bool IsValidPlacement(Vector3 candidatePoint, double characteristicDistance) {
            if (!spawnZone.Contains(candidatePoint)) {
                return false;
            }

            // TODO: There is likely a more efficient way to do this
            foreach (SomaPosition soma in somaPositionList.somaPositions) {
                double distance = Vector3.Distance(candidatePoint, soma.coordinates);
                double sumOfRadii = characteristicDistance + soma.characteristicDistance;

                // Check if the candidate point is inside the sphere of a given soma
                if (IsInsideSphere(candidatePoint, soma.coordinates, soma.characteristicDistance)) {
                    return false;
                }

                // Check if the a given soma is inside the sphere of the candidate point.
                if (IsInsideSphere(soma.coordinates, candidatePoint, characteristicDistance)) {
                    return false;
                }
            }

            return true;
        }

        private bool IsTargetAvailable(Vector3 target)
        {
            // Target needs to be within the spawn zone
            if (!spawnZone.Contains(target))
            {
                return false;
            }

            // Target can't be in an exclusion zone
            foreach (SomaPosition soma in somaPositionList.somaPositions)
            {
                if (IsInsideSphere(target, soma.coordinates, soma.characteristicDistance))
                {
                    return false;
                }
            }

            return true;
        }


        /** HELPERS ************************************************************************************/
        // Sorts by distance to target
        private void SortSpherePoints(Vector3 origin, Vector3 target)
        {
            fixedSpherePoints.Sort((a, b) =>
                Vector3.Distance(origin + a, target)
                    .CompareTo(Vector3.Distance(origin + b, target))
            );
        }

        bool IsInsideSphere(Vector3 point, Vector3 sphereCenter, double radius)
        {
            double sqrDistance = (point - sphereCenter).sqrMagnitude;
            return sqrDistance <= radius * radius;
        }


        /** ROTATION ************************************************************************************/
        public void SetRandomYRotation(Transform transform)
        {
            // Set a random rotation around the Y axis
            float randomYRotation = UnityEngine.Random.Range(0f, 360f);
            Quaternion rotation = Quaternion.Euler(0f, randomYRotation, 0f);
            transform.rotation = rotation;
        }


        /** SAMPLED UNIT SPHERE POINTS **************************************************************/
        /*
        Not used during runtime, but should be kept around in case we decide to modify the number of points
        sampled per sphere. Uses a Fibonacci Lattice technique to sample roughly equidistant points along the
        perimeter of the unit sphere. These points are stored in unitSphereEdgePoints.txt as the computation
        can be somewhat expensive. They're read and stored at runtime

        Made referencing these sources:
        - https://stackoverflow.com/questions/9600801/evenly-distributing-n-points-on-a-sphere
        - https://arxiv.org/pdf/0912.4540
        */
        private static List<Vector3> GenerateFixedSpherePoints(int numPoints)
        {
            List<Vector3> points = new List<Vector3>(numPoints);

            // Offset determines the vertical step between each point along the Y axis ([-1, 1])
            float offset = 2f / numPoints;

            // Increment is based on the golden angle, which gives even angular spacing along a spiral
            float increment = Mathf.PI * (3f - Mathf.Sqrt(5));

            for (int i = 0; i < numPoints; i++)
            {
                // y goes from near -1 to near 1, spaced evenly
                float y = ((i * offset) - 1) + (offset / 2);

                // Compute radius of circle at height y (based on unit sphere formula: x^2 + y^2 + z^2 = 1)
                float r = Mathf.Sqrt(1 - y * y);

                // Spiral angle around the Y axis
                float phi = i * increment;

                // Convert spherical coordinates to Cartesian
                float x = Mathf.Cos(phi) * r;
                float z = Mathf.Sin(phi) * r;

                // Each point lies on the surface of the unit sphere
                points.Add(new Vector3(x, y, z));
            }

            return points;
        }

        /*
        For the sampled unit sphere points. Not used during runtime, but should be kept for if we want to change the number of points 
        sampled. Reads to filePath.
        */
        private static void SavePointsToFile(List<Vector3> points, string fileName)
        {
            string folderPath = Path.Combine(Application.dataPath, "UnitSpherePoints");
            Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, fileName);

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (Vector3 point in points)
                {
                    writer.WriteLine($"{point.x} {point.y} {point.z}");
                }
            }

        #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
        #endif

            Debug.Log($"Saved {points.Count} points to: {filePath}");
        }


        /*
        For the sampled unit sphere points. Assumes " " as delimiter
        */
        private static List<Vector3> LoadPointsFromFile(string fileName)
        {
            // Construct full path to the file in the "Assets/UnitSpherePoints" folder
            string folderPath = Path.Combine(Application.dataPath, "UnitSpherePoints");
            string filePath = Path.Combine(folderPath, fileName);

            List<Vector3> points = new List<Vector3>();

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"File not found at path: {filePath}");
                return points;
            }

            foreach (string line in File.ReadLines(filePath))
            {
                string[] tokens = line.Split(' ');
                if (tokens.Length == 3)
                {
                    if (float.TryParse(tokens[0], out float x) &&
                        float.TryParse(tokens[1], out float y) &&
                        float.TryParse(tokens[2], out float z))
                    {
                        points.Add(new Vector3(x, y, z));
                    }
                }
            }

            Debug.Log($"Loaded {points.Count} points from: {filePath}");
            return points;
        }



        /** TESTING ************************************************************************************/
        // Functions here should not be used except to test
        public void ShowSpherePoints()
        {
            float radius = .5f; // Set the radius of the sphere
            Vector3 origin = new Vector3(0, 0, 0); // Set the origin point of the sphere

            for (int i = 0; i < fixedSpherePoints.Count; i++)
            {
                Vector3 worldPoint = origin + fixedSpherePoints[i] * radius;
                AddDotAt(worldPoint);
            }
        }

        public void ShowSpherePointsAround(Vector3 origin, float radius)
        {
            for (int i = 0; i < fixedSpherePoints.Count; i++)
            {
                Vector3 worldPoint = origin + fixedSpherePoints[i] * radius;
                AddDotAt(worldPoint);
            }
        }

        private void AddDotAt(Vector3 point)
        {
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            dot.transform.position = point;
            dot.transform.localScale = Vector3.one * 0.1f; 
        }
    }
}