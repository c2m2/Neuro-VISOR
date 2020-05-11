using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.Random;
using MathNet.Numerics.Distributions;


namespace C2M2 {
    namespace Simulation {
        public class ExampleMDSimulation : MDSimulation
        {
            // Define number of example spheres
            public int numSpheres = 3;
            // Example radius for spheres
            public float radius = .5f;
            public float kb=0.001987f; //kcal per mol
            public float T=1.0f; //K

            public int timestepCount = 50000;
            public float timestepSize = .1f;
            
            private Vector3[] x = null;
	        private Vector3[] v = null;
            private Vector3[] r = null;
	        private int[][] bond_topo = null;
	        private int[][] angle_topo = null;

            Dictionary<Transform, int> molLookup;

            // OPTION 2:
            //RaycastHit lastHit = new RaycastHit();

            public override Vector3[] GetValues()
            {
                return x;
            }


            ////////////////////////////////////////////////////////////////////////////////////////////////////
            /// <summary>
            /// Receive an interaction request, and translate it onto the proper sphere
            /// </summary>
            ////////////////////////////////////////////////////////////////////////////////////////////////////
            public override void SetValues(RaycastHit hit)
            {
                // You have two options: add the force here, or in your solve thread.
                // 
                // 
                // OPTION 1: You may run into mutual exclusion issues with this method, but maybe not
                // Get the molecule index that was hit by the interaction Raycast
                //int molHit = molLookup[hit.transform];  // Now v[molHit] or x[molHit] should affect the molecule hit by the raycast
                //Vector3 hitDirection = hit.normal;
                //v[molHit] += hitDirection or whatever. 

                // OPTION 2:
                //lastHit = hit;
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            /// <summary>
            /// In the future, MDSimulation should this method with the PDBReader, 
            /// so you won't need to worry about coding it every time. 
            /// We don't have a PDB file, so we create so make-believe positions
            /// </summary>
            ////////////////////////////////////////////////////////////////////////////////////////////////////
            protected override Transform[] BuildTransforms()
            {
                // Create spheres
                Sphere[] spheres = new Sphere[numSpheres];
                x = new Vector3[numSpheres];
                v = new Vector3[numSpheres];
                for(int i = 0; i < spheres.Length; i++)
                {
                    // Put our new spheres in a straight line and store their positions as simulation values
                    Vector3 pos = new Vector3(4*i, 0, 0);
                    Vector3 vel = new Vector3(0, 0, 0);
                    spheres[i] = new Sphere(pos, radius);
                    x[i] = pos;
                    v[i] = vel;
                }

                // Instantiate the created spheres and return their transform components
                SphereInstantiator instantiator = gameObject.AddComponent<SphereInstantiator>();
                Transform[] transforms = instantiator.InstantiateSpheres(spheres);

                // Create a lookup so that given a transform hit by a raycast we can get the molecule's index
                molLookup = new Dictionary<Transform, int>(transforms.Length);
                for(int i = 0; i < transforms.Length; i++)
                {
                    molLookup.Add(transforms[i], i);
                }

                return transforms;
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            /// <summary>
            /// Evaluates the forces. 
            /// First will try doing for system of oscillators. 
            /// Then will extend to non-bonded interactions
            /// </summary>
            ////////////////////////////////////////////////////////////////////////////////////////////////////
            public Vector3[] Force(Vector3[] pos, int[][] bond_topo)
	        {
		        Vector3[] f = new Vector3[x.Length];
                float kappa=.1f;
		        float r0=4.0f;
                Vector3 r = new Vector3(0,0,0);

		        for(int i = 0; i < x.Length; i++)         
		        {
                    for(int j = 0; j < bond_topo[i].Length; j++)
		            {
                        // U(x) = sum kappa_ij*(|x_i-x_j|-r_0)^2  
                        r = pos[i]-pos[bond_topo[i][j]];
                        f[i] += -kappa*(r.magnitude-r0)*r;                    
                    }
                }
 		        return f;
	        }

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            /// <summary>
            /// What does this method do?
            /// </summary>
            ////////////////////////////////////////////////////////////////////////////////////////////////////
	        public Vector3[] angle_Force(Vector3[] pos) //, int[][] angle_topo)
	        {
		        Vector3[] f = new Vector3[3];
                float kappa_theta=.01f;
                Vector3 r12 = pos[1]-pos[0]; 
		        Vector3 r23 = pos[2]-pos[1]; 
                Vector3 r13 = pos[2]-pos[0]; 
                
                float g = 2*Vector3.Dot(r12,r13);
                float h = (r12.magnitude+r13.magnitude-r23.magnitude);
		        float rad2deg = 180/Mathf.PI;
		        float theta=rad2deg*Mathf.Atan(g/h);
		        float theta_0=180.0f;		

		        Vector3 g_x1 = -(r13+r12);
                Vector3 g_x2 = r13;
                Vector3 g_x3 = r12;
                Vector3 h_x1 = -(r12/r12.magnitude)-(r13/r13.magnitude);
		        Vector3 h_x2 = (r12/r12.magnitude)+(r23/r23.magnitude);
		        Vector3 h_x3 = (r13/r13.magnitude)-(r23/r23.magnitude);
		        //for(int i = 0; i < x.Length; i++)         
		        //{
                            //for(int j = 0; j < angle_topo[i].Length; j++)
		            //{                        
                            //f[i]=
                                //r[j] = pos[i]-pos[angle_topo[i][j]];	
                                //f[i] += angle_Force(pos,angle_topo); //harmonic angle forces                      
                            //}
                float pre_factor = -kappa_theta*(theta-theta_0)/(1+(g/h)*(g/h));
		        Debug.Log(theta);
  		        f[0] = pre_factor*(h*g_x1-g*h_x1)/h/h;
		        f[1] = pre_factor*(h*g_x2-g*h_x2)/h/h;
		        f[2] = pre_factor*(h*g_x3-g*h_x3)/h/h;
 		        return f;
	        }

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            /// <summary>
            /// Molecular dynamics simulation code   
            /// </summary>
            ////////////////////////////////////////////////////////////////////////////////////////////////////
            protected override void Solve()
            {
                int nT = timestepCount;
                float dt = timestepSize;
                float m = 10.0f;
		        float gamma=0.1f;
                float a=((1-gamma*dt/2)/(1+gamma*dt/2));
                float coeff=Convert.ToSingle(Math.Sqrt(kb*T*(1-a*a)/m));
                
	            //hard code the bond info
                int[][] bond_topo = new int[x.Length][];
                bond_topo[0]= new int[] {1};
		        bond_topo[1]= new int[] {0,2};
		        bond_topo[2]= new int[] {1};

                //hard code the angle info
                //int[][] angle_topo = new int[x.Length][];
                //angle_topo[0]= new int[] {};
		        //angle_topo[1]= new int[] {0,1,2};
		        //angle_topo[2]= new int[] {};

		        //instantiate a normal dist.
		        var normal = Normal.WithMeanPrecision(0.0, 1.0);

		        Vector3[] force = Force(x,bond_topo); // + angle_Force(x,angle_topo);
		        Vector3[] angle = angle_Force(x);

                // OPTION 2:
                //lastHit.distance = float.PositiveInfinity;
                
                // Iterate over time
                for (int t = 0; t < nT; t++)
		        {
                    // iterate over the atoms
                    for(int i = 0; i < x.Length; i++)
                    {
                        double rxx = normal.Sample();
                        float rx = Convert.ToSingle(rxx);
 		                double ryy = normal.Sample();
                        float ry = Convert.ToSingle(ryy);
                        double rzz = normal.Sample();
                        float rz = Convert.ToSingle(rzz);
                        Vector3 r = new Vector3(rx,ry,rz);

                        // OPTION 2:
                        /*
                        Vector3 pushTerm = Vector3.zero;
                        if (lastHit.distance != float.PositiveInfinity)
                        {
                            pushTerm = lastHit.normal;
                            lastHit.distance = float.PositiveInfinity;
                        }
                        v[i]=v[i]+(dt*dt/2/m)*(force[i]+angle[i] + pushTerm);
                        */
                        v[i]=v[i]+(dt*dt/2/m)*(force[i]+angle[i]);
		                x[i]=x[i]+(dt/2)*v[i];
                        v[i]=a*v[i]+coeff*r;
			            x[i]=x[i]+(dt/2)*v[i];
                    }

		            force = Force(x,bond_topo);
                    angle = angle_Force(x);
                    //Debug.Log(angle[0]);

                    for(int i = 0; i < x.Length; i++)
                    {

                        v[i]=v[i]+(dt*dt/2/m)*(force[i]+angle[i]);
                    }
                }
                Debug.Log("ExampleMDSimulation complete.");
            }
        }
    }
}
