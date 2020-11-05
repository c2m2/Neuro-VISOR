using System;
using UnityEngine;
using MathNet.Numerics.Distributions;
using C2M2.MolecularDynamics.Visualization;
namespace C2M2.MolecularDynamics.Simulation
{
    public class ExampleMDSimulation : MDSimulation
    {
        [Header("Simulation Parameters")]
        public float kb = 0.001987f; //kcal per mol
        public float T = 100.0f; //K

        public float kappa = 6f;
        public float r0 = 3.65f;

        private Vector3[] force = null;

        
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
            if (force == null) return;
            int particleHit = particleLookup[hit.transform.parent];
            force[particleHit] += (-100 * hit.normal) / hit.distance; // Dividing by distance allows closer touches to have stronger effect
        }

        /// <summary>
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

	        //instantiate a normal dist.
	        var normal = Normal.WithMeanPrecision(0.0, 1.0);
	        force = Force(coord,bond_topo); // + angle_Force(x,angle_topo);
            // Iterate over time
            for (int t = 0; t < nT; t++)
	        {
                // iterate over the atoms
                for(int i = 0; i < coord.Length; i++)
                {
                    float coeff = Convert.ToSingle(Math.Sqrt(kb*T*(1-c*c)/mass[i]));
                    double rxx = normal.Sample();
                    float rx = Convert.ToSingle(rxx);

 		            double ryy = normal.Sample();
                    float ry = Convert.ToSingle(ryy);

                    double rzz = normal.Sample();
                    float rz = Convert.ToSingle(rzz);
                     
                    Vector3 r = new Vector3(rx,ry,rz);

                    vel[i] = vel[i] + (dt*dt/2/mass[i]) * (force[i]);
		            coord[i] = coord[i] + (dt/2) * vel[i];

                    vel[i] = c * vel[i] + coeff * r;
		            coord[i] = coord[i] + (dt/2) * vel[i];
                   
                }

                ResolvePBC();

                force = Force(coord,bond_topo);

                for(int i = 0; i < coord.Length; i++)
                {
                    vel[i] = vel[i] + (dt*dt/2/mass[i]) * (force[i]);
                }
            }
            Debug.Log("ExampleMDSimulation complete.");
        }

        void ResolvePBC()
        {
            float boxLengthXx2 = boxLengthX * 2;
            float boxLengthYx2 = boxLengthY * 2;
            float boxLengthZx2 = boxLengthZ * 2;

            // Find if any position has gone beyond box limits, set flag if so
            for (int i = 0; i < coord.Length; i++)
            {
                // Number of times that coord[i] has crossed the boundary in this timestep
                int x = (int)(coord[i].x / boxLengthX);
                int y = (int)(coord[i].y / boxLengthY);
                int z = (int)(coord[i].z / boxLengthZ);

                // Reset coord[i] to the beginning of the box if necessary
                coord[i].x -= boxLengthXx2 * x;
                coord[i].y -= boxLengthXx2 * y;
                coord[i].z -= boxLengthXx2 * z;

                // Net cumulative times that coord[i] has crossed the boundary
                pbcFlag[i].x += x;
                pbcFlag[i].y += y;
                pbcFlag[i].z += z;
            }
        }
    }
}
