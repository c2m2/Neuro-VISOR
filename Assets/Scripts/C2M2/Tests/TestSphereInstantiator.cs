using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace C2M2
{
    namespace Tests
    {
        using Readers;
        public class TestSphereInstantiator : AwakeTest
        {
            public int testCases = 100;

            private SphereInstantiator instantiator;
            private Transform[] transforms;
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
                // Let the TestManager know that PreTest ran successfully
                return true;
            }

            public override bool RunTest()
            {
                try
                {
                    // Try SphereInstantiator, store the transform result
                    transforms = instantiator.InstantiateSpheres(spheres);

                    // Ensure that each instantiated sphere has the correct position and dimensions
                    for (int i = 0; i < transforms.Length; i++)
                    {
                        float diameter = radii[i] * 2;
                        Vector3 scaleVector = new Vector3(diameter, diameter, diameter);
                        if (transforms[i].position != positions[i])
                        {
                            string s = "TestSphereInstantiator Failure info:"
                                + "\n\tINCORRECT: transforms[" + i + "].position: " + transforms[i].position
                                + "\n\tCORRECT: positions[" + i + "]: " + positions[i];
                            Debug.Log(s);
                            return false;
                        }
                        if(transforms[i].localScale != scaleVector)
                        {
                            string s = "TestSphereInstantiator Failure info:"
                                + "\n\tINCORRECT: transforms[" + i + "].localScale: " + transforms[i].localScale
                                + "\n\tCORRECT: scaleVector" + scaleVector;
                            Debug.Log(s);
                            return false;
                        }
                    }
                }
                catch(Exception e)
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
                    for (int i = 0; i < transforms.Length; i++)
                    {
                        Destroy(transforms[i].gameObject);
                    }
                    Destroy(instantiator);
                }
                catch(Exception e)
                {
                    Debug.LogError(e);
                    return false;
                }
                // Let the TestManager know that PreTest ran successfully
                return true;
            }
        }
    }
}
