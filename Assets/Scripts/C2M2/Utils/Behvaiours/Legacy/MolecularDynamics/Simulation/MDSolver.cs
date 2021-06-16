﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.Random;
using MathNet.Numerics.Distributions;
using C2M2.MolecularDynamics.Visualization;
namespace C2M2.MolecularDynamics.Simulation
{
    public class MDSolver : MDSimulation
    {
        public float kb = 0.001987f; //kcal per mol
        public float T = 100.0f; //K

        public float kappa = 6f;
        public float r0 = 3.65f;
        
        // OPTION 2:
        //RaycastHit lastHit = new RaycastHit();
        public override Vector3[] GetValues()
        {
            return coord;
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
        }

        /// Evaluates the forces.
        /// First will try doing for system of oscillators.
        /// Then will extend to non-bonded interactions
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector3[] Force(Vector3[] pos, int[][] bond_topo)
	    {
		    Vector3[] f = new Vector3[bond_topo.Length];
            Vector3 r = new Vector3(0,0,0);

		    for(int i = 0; i < bond_topo.Length; i++)
		    {
                for(int j = 0; j < bond_topo[i].Length; j++)
		        {
                	// U(x) = sum kappa_ij*(|x_i-x_j|-r_0)^2
                    r = pos[i] - pos[bond_topo[i][j]];
                    f[i] += -kappa*(r.magnitude-r0)*r;
                    //GameManager.instance.DebugLogSafe(f[i]);
                }
            }
 		    return f;
	    }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Compute angle forces
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

        int t;
        float dt;
        Normal normal;
        Vector3[] force;
        float a;
        protected override void PreSolve()
        {
            int nT = timestepCount;
            dt = timestepSize;
            //float m = 40.0f;
            float gamma = 0.1f;
            a = ((1 - gamma * dt / 2) / (1 + gamma * dt / 2));


            //hard code the bond info
            //int[][] bond_topo = new int[x.Length][];
            //bond_topo[0]= new int[] {1};
            //	    bond_topo[1]= new int[] {0,2};
            //	    bond_topo[2]= new int[] {1};

            //hard code the angle info
            //int[][] angle_topo = new int[x.Length][];
            //angle_topo[0]= new int[] {};
            //angle_topo[1]= new int[] {0,1,2};
            //angle_topo[2]= new int[] {};

            //instantiate a normal dist.
            normal = Normal.WithMeanPrecision(0.0, 1.0);
            force = Force(coord, bond_topo); // + angle_Force(x,angle_topo);
                                                       //Vector3[] angle = angle_Force(x);
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Molecular dynamics simulation code
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        protected override void SolveStep(int t)
        {    
            // iterate over the atoms
            for(int i = 0; i < coord.Length; i++)
            {
                float coeff = Convert.ToSingle(Math.Sqrt(kb*T*(1-a*a)/mass[i]));
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
                vel[i] = vel[i] + (dt*dt/2/mass[i]) * (force[i]);
		        coord[i] = coord[i] + (dt/2) * vel[i];
                vel[i]=a*vel[i] + coeff * r;
		        coord[i]=coord[i] + (dt/2) * vel[i];
            }

		    force = Force(coord,bond_topo);

            /*GameManager.instance.DebugLogSafe("force[1043]: " + force[1043]
                + "\nx[1043]: " + x[1043]
                + "\nv[1043]: " + v[1043]
		+ "\nm[1043]: " + mass[1043]);
            //angle = angle_Force(x); */

            for(int i = 0; i < coord.Length; i++)
            {
                vel[i] = vel[i] + (dt*dt/2/mass[i]) * (force[i]);
            }
        }
    }
}