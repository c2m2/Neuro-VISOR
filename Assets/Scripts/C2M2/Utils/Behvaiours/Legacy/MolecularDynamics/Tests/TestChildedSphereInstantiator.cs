using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using C2M2.Tests;
using C2M2.MolecularDynamics.Visualization;
namespace C2M2.MolecularDynamics.Tests
{
    public class TestChildedSphereInstantiator : AwakeTest
    {
        public int testCases = 100;

        private SphereInstantiator instantiator;
        private Transform[] transforms;
        private Transform parent;
        private Vector3[] positions;
        private float[] radii;
        private Sphere[] spheres;

        public override bool PreTest()
        {
            try
            {
                // Add a SphereInstantiator
                instantiator = gameObject.AddComponent<SphereInstantiator>();

                // Initialize spheres with given radii, positions
                positions = new Vector3[testCases];
                radii = new float[testCases];
                spheres = new Sphere[testCases];

                // Initialize random positions, radii; put them into spheres
                for (int i = 0; i < testCases; i++)
                {
                    positions[i] = new Vector3(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f));
                    radii[i] = UnityEngine.Random.Range(0f, 5f);
                    spheres[i] = new Sphere(positions[i], radii[i]);
                }
            }catch(Exception e)
            {
                Debug.LogError(e);
                return false;
            }
            // Let the manager know that the pre test ran successfully
            return true;
        }
        public override bool RunTest()
        {
            try
            {
                // Try ChildedSphereInstantiator, store the transform result
                parent = instantiator.InstantiateChildedSpheres(spheres);

                // Get each sphere transform from the hierarchy.
                transforms = parent.GetComponentsInChildren<Transform>();

                // Ensure that each instantiated sphere has the correct position and dimensions
                for (int i = 1; i < transforms.Length; i++)
                {
                    // If the sphere position isn't right,
                    if (transforms[i].position != positions[i-1])
                    {
                        // Print failure info
                        string s = "TestChildedSphereInstantiator Failure info:"
                            + "\n\tINCORRECT: transforms[" + i + "].position: " + transforms[i].position
                            + "\n\tCORRECT: positions[" + i + "]: " + positions[i-1]
                            + "\n\ttransforms.Length: " + transforms.Length
                            + "\n\ttestCases: " + testCases;
                        Debug.Log(s);

                        // Return failure
                        return false;
                    }

                    // Put radius in form that test understands
                    float diameter = radii[i - 1] * 2;
                    Vector3 scaleVector = new Vector3(diameter, diameter, diameter);
                    // If the sphere radius isn't right,
                    if (transforms[i].localScale != scaleVector)
                    {
                        // Print failure info
                        string s = "TestChildedSphereInstantiator Failure info:"
                            + "\n\tINCORRECT: transforms[" + i + "].localScale: " + transforms[i].localScale
                            + "\n\tCORRECT: scaleVector" + scaleVector
                            + "\n\ttransforms.Length: " + transforms.Length
                            + "\n\ttestCases: " + testCases;
                        Debug.Log(s);
                        // Return failure
                        return false;
                    }
                }
            }catch(Exception e)
            {
                Debug.LogError(e);
                return false;
            }
            // If every sphere was correct, our test passes
            return true;
        }
        public override bool PostTest()
        {
            try
            {
                Destroy(parent.gameObject);
                Destroy(instantiator);
                Destroy(this);
            }catch(Exception e)
            {
                Debug.LogError(e);
                return false;
            }
            // Let the manager know that the psot test ran successfully
            return true;
        }

    }
}
