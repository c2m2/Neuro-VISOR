using UnityEngine;
using System.Collections;
using Unity.Collections;
using System.Linq;
using System;

// These are the MathNet Numerics Libraries needed
// They need to dragged and dropped into the Unity assets plugins folder!
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using Double = MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Data.Text;

using Grid = C2M2.NeuronalDynamics.UGX.Grid;
namespace C2M2.NeuronalDynamics.Simulation
{
    //TODO: The summary/remakrs here should be written more clearly
    /// <summary>
    /// Solve Hodkin-Huxley equations using Method of Lines on a 1D rod
    /// </summary>
    /// <remarks>
    /// Uses a system Stencil matrix where the spatial solve is done using a
    /// 1D 3 point center difference to approximate the second derivative d^2V/dx^2
    /// And Forward Euler is used for the time stepping.
    /// 
    /// The system matrix will solve the system of center difference equations in
    ///space and then step through time using FE 
    /// </remarks>
    public class MOL_Matrix_v2 : NeuronSimulation1D
    {
        //----------------------Public constants for the solver-------------------//
        public const double nX = 119;            // Number of spatial steps
        public const double nT = 55000;            // Number of time steps
        public const double X = 1;              // End position (assume start at 0)
        public const double T = 25;             // End time (assume start at 0)
        public const double E = 2.7182818284590452353602874713526624977572; // Base Natural Log
                                                                            //------------------------------------------------------------------------//

        //-----------------------Biological constants-----------------------------//
        public const double a = 0.001, R = 10.0;                // radius and resistance
        public const double gk = 36.0, gna = 120.0, gl = 0.3;     // ion conductances
        public const double ek = -12.0, ena = 112.0, el = 0.6;     // ion potentials
        public const double ni = 0.5, mi = 0.4, hi = 0.2;       // probabilities

        public const double b = MOL_Matrix_v2.a / (2 * MOL_Matrix_v2.R);    // coefficient for diffusion solve
        public const double c = 0.09;                           // capacitance
                                                                //------------------------------------------------------------------------//
        private Matrix U;
        private int i = -1;
        private double[] newValues;
        // Initialize one end of the rod
        private double vstart = 55;
        /// <summary>
        /// Get the current simulation values
        /// </summary>
        /// <returns>
        /// Simulation scalars at current timestep. Returns null if simulation isn't started
        /// </returns>
        protected override double[] Get1DValues()
        {
            if (U != null && i > -1) return U.SubMatrix(0, U.RowCount, i, 1).ToColumnMajorArray();
            else return null;
        }
        // Receive new simulation 1D index/value pairings
        protected override void Set1DValues(Tuple<int, double>[] newValues)
        {
            foreach(Tuple<int, double> newVal in newValues)
            {
                int j = newVal.Item1;
                double val = newVal.Item2;
                U[j, i] = val;
            }
        }
        protected override void SetNeuronCell(Grid grid) { }
        protected override void Solve()
        {
            // Compute step sizes
            double k = MOL_Matrix_v2.T / MOL_Matrix_v2.nT;    // Time step size
            double h = MOL_Matrix_v2.X / MOL_Matrix_v2.nX;  // Spatial step size

            // Note this is how I converted a double to int
            // Precompute the size of the x vector and t vector
            int sizeX = (int)MOL_Matrix_v2.nX + 1;  // Number of spatial points
            int sizeT = (int)MOL_Matrix_v2.nT + 1;  // Number of time points
                                                    // Make a vector for the spatial points and temporal points
            Vector x = Vector.Build.Dense(sizeX, i => 0 + i * h);
            Vector t = Vector.Build.Dense(sizeT, i => 0 + i * k);
            // Declare solution variables
            U = Matrix.Build.Dense(sizeX, sizeT);
            Matrix NN = Matrix.Build.Dense(sizeX, sizeT);
            Matrix MM = Matrix.Build.Dense(sizeX, sizeT);
            Matrix HH = Matrix.Build.Dense(sizeX, sizeT);
            // Initialize last half of simulation values to vstart
            for (i = 0; i < Mathf.CeilToInt((int)(MOL_Matrix_v2.nT + 1) / 2); i++)
            {
                U[0, i] = vstart;
            }
            // TODO: What does this do?
            NN[0, 0] = MOL_Matrix_v2.ni;
            MM[0, 0] = MOL_Matrix_v2.mi;
            HH[0, 0] = MOL_Matrix_v2.hi;
            // store row count, column count locally for readability
            int nRows = U.RowCount;
            int nCols = U.ColumnCount;
            // Get the system matrix for solving
            Matrix SYS = Matrix.Build.Dense(nRows, nRows);
            SYS = BuildStencil() * (b / (h * h));
            // TODO: Why are we doing this?
            /// Multiply the system matrix with a column of U and put in the next column of U
            Matrix temp = Matrix.Build.Dense(nRows, 1);
            Matrix temp2 = Matrix.Build.Dense(nRows, 1);
            Matrix react = Matrix.Build.Dense(nRows, 1);
            // Initialize the change array
            newValues = new double[U.RowCount];
            // Start simulation
            for (i = 0; i < MOL_Matrix_v2.nT; i++)
            {
                // See if we have new values to incorporate into the simulation
                // AddNewValues();

                // Plug in to the reactant term of the PDE
                react = ReactF(U.SubMatrix(0, nRows, i, 1), NN.SubMatrix(0, nRows, i, 1),
                MM.SubMatrix(0, nRows, i, 1), HH.SubMatrix(0, nRows, i, 1));

                // TODO: What is FE?
                // This is where I FE on the spatial component
                // U(next)= U(curr)+ k/c*(S*U(curr)+react(...))
                SYS.Multiply(U.SubMatrix(0, nRows, i, 1), temp2); // Put S*U in temp2
                temp = U.SubMatrix(0, nRows, i, 1) + (k / MOL_Matrix_v2.c) *
                    (temp2 + react);

                U.SetSubMatrix(0, i + 1, temp);

                /* The following is the forward euler step on n,m,h
                    Becareful! You need to pass U(current col) to
                    the functions an, bn, am,bm, ah, bh
                    or else you will decouple the system! and the AP doesn't 'travel'
                    Each solve corresponds to FE: U(next)= U(curr)+k*f(U(curr)) */
                // TODO: temp, temp2 are poorly named, and it is unclear why they're being used
                // Forward Euler Step on NN
                temp2 = NN.SubMatrix(0, nRows, i, 1);
                temp = temp2 + k * (An(U.SubMatrix(0, nRows, i, 1)).PointwiseMultiply(1 - temp2) -
                        Bn(U.SubMatrix(0, nRows, i, 1)).PointwiseMultiply(temp2));
                NN.SetSubMatrix(0, i + 1, temp);

                // Forward Euler Step on MM
                temp2 = MM.SubMatrix(0, nRows, i, 1);
                temp = temp2 + k * (Am(U.SubMatrix(0, nRows, i, 1)).PointwiseMultiply(1 - temp2) -
                        Bm(U.SubMatrix(0, nRows, i, 1)).PointwiseMultiply(temp2));
                MM.SetSubMatrix(0, i + 1, temp);

                // Forward Euler Step on HH
                temp2 = HH.SubMatrix(0, nRows, i, 1);
                temp = temp2 + k * (Ah(U.SubMatrix(0, nRows, i, 1)).PointwiseMultiply(1 - temp2) -
                    Bh(U.SubMatrix(0, nRows, i, 1)).PointwiseMultiply(temp2));
                HH.SetSubMatrix(0, i + 1, temp);
            }
        }
        #region Local Functions
        static Matrix PointwiseExpb(Matrix V, double bb)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);
            for (int i = 0; i < V.RowCount; i++)
            {
                for (int j = 0; j < V.ColumnCount; j++)
                {
                    output[i, j] = System.Math.Pow(bb, V[i, j]);
                }
            }
            return output;
        }
        /// <summary>
        /// Build the system matrix needed for the spatial solves along the rod
        /// </summary>
        private static Matrix BuildStencil()
        {
            // The following code will 
            // 1D in X variable Center difference stencil for second derivative
            double[,] sten = { { 1, -2, 1 } };
            int nRows = (int)MOL_Matrix_v2.nX + 1;

            // Turn the array into a 1D matrix
            Matrix stn = Matrix.Build.DenseOfArray(sten);

            // Declare system matrix
            Matrix S = Matrix.Build.Dense(nRows, nRows);

            // Set the first and last rows of the matrix
            S.SetSubMatrix(0, 0, stn.SubMatrix(0, 1, 1, 2));
            S.SetSubMatrix(nRows - 1, nRows - 2, stn.SubMatrix(0, 1, 0, 2));

            // This for loop fills in the diagonal part of matrix
            for (int i = 1; i <= nRows - 2; i++)
            {
                S.SetSubMatrix(i, i - 1, stn.SubMatrix(0, 1, 0, 3));
            }

            // Return the system matrix
            return S;
        }
        /// TODO: Fill in the parameters here, especially since they are not named clearly
        /// <summary>
        /// computes the reaction term of the HH equation
        /// </summary>
        /// <param name="V"></param>
        /// <param name="NN"></param>
        /// <param name="MM"></param>
        /// <param name="HH"></param>
        /// <returns></returns>
        private static Matrix ReactF(Matrix V, Matrix NN, Matrix MM, Matrix HH)
        {
            // The inputs are all column vectors
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);
            Matrix prod = Matrix.Build.Dense(V.RowCount, V.ColumnCount);

            // Add current due to potassium
            prod = NN.PointwisePower(4);
            prod = (V - MOL_Matrix_v2.ek).PointwiseMultiply(prod);
            output = MOL_Matrix_v2.gk * prod;

            // Add current due to sodium
            prod = MM.PointwisePower(3);
            prod = HH.PointwiseMultiply(prod);
            prod = (V - MOL_Matrix_v2.ena).PointwiseMultiply(prod);
            output = output + MOL_Matrix_v2.gna * prod;

            // Add leak current
            output = output + MOL_Matrix_v2.gl * (V - MOL_Matrix_v2.el);

            // Return the negative of the total
            return -1 * output;
        }
        /// <summary>
        /// TODO: What does this do? What is V? What does it return?
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        private static Matrix An(Matrix V)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);
            Matrix temp = 10 - V;

            output = 0.01 * temp.PointwiseDivide(PointwiseExpb(temp / 10, MOL_Matrix_v2.E) - 1);
            return output;
        }
        /// <summary>
        /// TODO: What does this do? What is V? What does it return?
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        private static Matrix Bn(Matrix V)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);

            output = 0.125 * PointwiseExpb(-1 * V / 80, MOL_Matrix_v2.E);
            return output;
        }
        /// <summary>
        /// TODO: What does this do? What is V? What does it return?
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        private static Matrix Am(Matrix V)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);
            Matrix temp = 25 - V;

            output = 0.1 * temp.PointwiseDivide(PointwiseExpb(temp / 10, MOL_Matrix_v2.E) - 1);
            return output;
        }
        /// <summary>
        /// TODO: What does this do? What is V? What does it return?
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        private static Matrix Bm(Matrix V)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);

            output = 4 * PointwiseExpb(-1 * V / 18, MOL_Matrix_v2.E);
            return output;
        }
        /// <summary>
        /// TODO: What does this do? What is V? What does it return?
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        private static Matrix Ah(Matrix V)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);

            output = 0.07 * PointwiseExpb(-1 * V / 20, MOL_Matrix_v2.E);
            return output;
        }
        /// <summary>
        /// TODO: What does this do? What is V? What does it return?
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        private static Matrix Bh(Matrix V)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);
            Matrix temp = 30 - V;

            output = 1 / (PointwiseExpb(temp / 10, MOL_Matrix_v2.E) + 1);
            return output;
        }
        #endregion
    }
}