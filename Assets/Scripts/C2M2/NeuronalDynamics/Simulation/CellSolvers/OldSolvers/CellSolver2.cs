using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using System;
using UnityEngine;

// These are the MathNet Numerics Libraries needed
// They need to dragged and dropped into the Unity assets plugins folder!
using SparseMatrix = MathNet.Numerics.LinearAlgebra.Double.SparseMatrix;
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using Double = MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Data.Text;
using solvers = MathNet.Numerics.LinearAlgebra.Double.Solvers;
using C2M2.NeuronalDynamics.UGX;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
namespace C2M2.NeuronalDynamics.Simulation
{
    public class CellSolver2 : NeuronSimulation1D
    {
        //Set cell biological constants
        public const double res = 10.0;
        public const double gk = 36.0;
        public const double gna = 120.0;
        public const double gl = 0.3;
        public const double ek = -12.0;
        public const double ena = 220; //70;//112.0;
        public const double el = 0.6;
        public const double cap = 0.09;
        public const double ni = 0.5, mi = 0.4, hi = 0.2;       //state probabilities

        public const double vstart = 55;

        private Matrix U;
        // NeuronCellSimulation handles reading the UGX file
        private NeuronCell myCell;
        protected override void SetNeuronCell(Grid grid)
        {
            myCell = new NeuronCell(grid);
        }
        // Keep track of i locally so that we know which simulation frame to send to other scripts
        private int i = -1;

        protected override double[] Get1DValues()
        {
            return (U != null && i > -1) ? U.SubMatrix(0, U.RowCount, i, 1).ToColumnMajorArray() : null;
        }
        // Receive new simulation 1D index/value pairings
        protected override void Set1DValues(Tuple<int, double>[] newValues)
        {
            if (i > -1)
            {
                foreach (Tuple<int, double> newVal in newValues)
                {
                    int j = newVal.Item1;
                    double val = newVal.Item2 * vstart;
                    U[j, i] += val;
                }
            }
        }

        protected override void Solve()
        {
            //Set solving parameters standard run set endTime = 2 and nT = 1000;
            double endTime = 25; //25;//32;    // End time value
            int nT = 9000; //9000;  //16000;          // Number of Time steps
            double k = endTime / (double)nT; //Time step size
                                             //double h = 0.008; //myCell.edgeLengths.Average()*1e4; //Spatial Step Size
            double h = myCell.edgeLengths.Average() * 1e4;

            Debug.Log("h = " + h);
            Debug.Log("Ave Edge Length = " + myCell.edgeLengths.Average());

            //This is where I begin using MathNumerics Library
            //Make grid function as a matrix for now! and then I initialize the voltage at one
            //location. Note: the row index will correspond to the vertex number! (hopefully)
            U = Matrix.Build.Dense(myCell.vertCount, nT);

            //State variable grid functions
            Matrix NN = Matrix.Build.Dense(myCell.vertCount, nT);
            Matrix MM = Matrix.Build.Dense(myCell.vertCount, nT);
            Matrix HH = Matrix.Build.Dense(myCell.vertCount, nT);

            U.SetSubMatrix(0, 0, SetBoundaryConditions(U.SubMatrix(0, myCell.vertCount, 0, 1), myCell.boundaryID));

            //Initialize state variables
            NN[0, 0] = ni;
            MM[0, 0] = mi;
            HH[0, 0] = hi;

            alglib.sparsematrix rhsM;
            alglib.sparsematrix lhsM;

            alglib.sparsecreate(myCell.vertCount, myCell.vertCount, out rhsM);
            alglib.sparsecreate(myCell.vertCount, myCell.vertCount, out lhsM);

            //Internal variables for CN coefficients
            double sig;
            double rad;
            int nghbr;

            for (int p = 0; p < myCell.vertCount; p++)
            {
                rad = myCell.nodeData[p].nodeRadius;
                //rad = 1e-3;

                if (myCell.nodeData[p].neighborIDs.Count == 1)
                {
                    alglib.sparseset(rhsM, p, p, 1);
                    alglib.sparseset(lhsM, p, p, 1);

                }
                else
                {
                    sig = (rad * k) / (4.0 * res * cap * h * h);
                    alglib.sparseset(rhsM, p, p, 1 - 2 * sig);
                    alglib.sparseset(lhsM, p, p, 1 + 2 * sig);


                    for (int j = 0; j < myCell.nodeData[p].neighborIDs.Count; j++)
                    {
                        nghbr = myCell.nodeData[p].neighborIDs[j];
                        alglib.sparseset(rhsM, p, nghbr, sig);
                        alglib.sparseset(lhsM, p, nghbr, -sig);

                    }
                }
            }

            alglib.sparseconverttocrs(rhsM);
            alglib.sparseconverttocrs(lhsM);

            //This is the solving loop
            //Using operator splitting
            Matrix temp1 = Matrix.Build.Dense(myCell.vertCount, 1);
            Matrix temp2 = Matrix.Build.Dense(myCell.vertCount, 1);
            Matrix probTemp = Matrix.Build.Dense(myCell.vertCount, 1);
            Matrix react = Matrix.Build.Dense(myCell.vertCount, 1);

            double[] tempa = new double[myCell.vertCount];

            alglib.sparsesolverreport rep;

            DateTime t0;
            List<double> osTimes = new List<double>();
            List<double> feTimes = new List<double>();
            DateTime t1;

            t0 = DateTime.Now;
            Debug.Log("nT - 1:" + (nT - 1));
            //UnityEngine.Profiling.Profiler.BeginSample("For Loop Start");
            for (i = 0; i < nT - 1; i++)
            {
                t1 = DateTime.Now;
                //Crank Nicolson: First multiply rhsM*b, then solve lhsM*x = rhsM*b
                alglib.sparsemv(rhsM, GetColumn(U, i, myCell.vertCount), ref tempa);
                alglib.sparsesolve(lhsM, myCell.vertCount, tempa, out tempa, out rep);

                temp1.SetColumn(0, tempa);

                //Reaction Calculation Step
                //Use solution from diffusion solve (temp1) as input
                react = ReactF(temp1, NN.SubMatrix(0, myCell.vertCount, i, 1),
                MM.SubMatrix(0, myCell.vertCount, i, 1), HH.SubMatrix(0, myCell.vertCount, i, 1));

                //This is the solution from the reaction FE solve
                temp2 = temp1 + k / cap * react;  //FE step

                //set B.C of cell 
                temp2.SetSubMatrix(0, 0, SetBoundaryConditions(temp2, myCell.boundaryID));

                U.SetSubMatrix(0, i + 1, temp2); //Set to next grid function step for Voltage
                osTimes.Add((DateTime.Now - t1).TotalSeconds);

                t1 = DateTime.Now;
                //Do FE steps on state prob below using the intermediate solution temp1 from diffusion solve
                probTemp = NN.SubMatrix(0, myCell.vertCount, i, 1) +
                        k * (an(temp1).PointwiseMultiply(1 - NN.SubMatrix(0, myCell.vertCount, i, 1)) -
                        bn(temp1).PointwiseMultiply(NN.SubMatrix(0, myCell.vertCount, i, 1)));
                NN.SetSubMatrix(0, i + 1, probTemp);

                probTemp = MM.SubMatrix(0, myCell.vertCount, i, 1) +
                        k * (am(temp1).PointwiseMultiply(1 - MM.SubMatrix(0, myCell.vertCount, i, 1)) -
                        bm(temp1).PointwiseMultiply(MM.SubMatrix(0, myCell.vertCount, i, 1)));
                MM.SetSubMatrix(0, i + 1, probTemp);

                probTemp = HH.SubMatrix(0, myCell.vertCount, i, 1) +
                        k * (ah(temp1).PointwiseMultiply(1 - HH.SubMatrix(0, myCell.vertCount, i, 1)) -
                        bh(temp1).PointwiseMultiply(HH.SubMatrix(0, myCell.vertCount, i, 1)));
                HH.SetSubMatrix(0, i + 1, probTemp);

                feTimes.Add((DateTime.Now - t1).TotalSeconds);

                //Debug.Log(U.SubMatrix(0,myCell.vertCount,i+1,1));
            }
            //UnityEngine.Profiling.Profiler.EndSample();
            Debug.Log(DateTime.Now - t0);
            Debug.Log("Average OS time =  " + osTimes.Average());
            Debug.Log("Max OS time =  " + osTimes.Max());
            Debug.Log("Total OS time =  " + osTimes.Sum());

            Debug.Log("Average FE time =  " + feTimes.Average());
            Debug.Log("Max FE time =  " + feTimes.Max());
            Debug.Log("Total FE time =  " + feTimes.Sum());

            //DelimitedWriter.Write("/Users/jamesrosado/Desktop/VR_Code_Files/cell2073.txt", U," ");
        }

        #region Local Functions
        private static double[] GetColumn(Matrix V, int column, int nrows)
        {
            double[] x = new double[nrows];

            for (int i = 0; i < nrows; i++)
            {
                x[i] = V[i, column];
            }

            return x;
        }

        private static Matrix SetBoundaryConditions(Matrix V, List<int> bcIndices)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, 1);
            output.SetSubMatrix(0, 0, V);

            for (int i = 0; i < bcIndices.Count; i++)
            {
                if (bcIndices[i] == 0)
                {
                    output[bcIndices[i], 0] = CellSolver2.vstart;
                }
                else
                {
                    output[bcIndices[i], 0] = 0;
                }
            }

            return output;
        }

        private static Matrix ReactF(Matrix V, Matrix NN, Matrix MM, Matrix HH)
        {
            // The inputs are all column vectors, but I store them as column matrix
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);
            Matrix prod = Matrix.Build.Dense(V.RowCount, V.ColumnCount);

            // Add current due to potassium
            prod = NN.PointwisePower(4);
            prod = (V - CellSolver2.ek).PointwiseMultiply(prod);
            output = CellSolver2.gk * prod;

            // Add current due to sodium
            prod = MM.PointwisePower(3);
            prod = HH.PointwiseMultiply(prod);
            prod = (V - CellSolver2.ena).PointwiseMultiply(prod);
            output = output + CellSolver2.gna * prod;

            // Add leak current
            output = output + CellSolver2.gl * (V - CellSolver2.el);

            // Return the negative of the total
            return -1 * output;
        }

        private static Matrix an(Matrix V)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);
            Matrix temp = 10 - V;

            output = 0.01 * temp.PointwiseDivide((temp / 10).PointwiseExp() - 1);

            //output = 0.01 * temp.PointwiseDivide(PointwiseExpb(temp / 10, MOL_Matrix_v2.E) - 1);
            return output;
        }

        private static Matrix bn(Matrix V)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);

            output = 0.125 * (-1 * V / 80).PointwiseExp();
            return output;
        }

        private static Matrix am(Matrix V)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);
            Matrix temp = 25 - V;

            output = 0.1 * temp.PointwiseDivide((temp / 10).PointwiseExp() - 1);
            return output;
        }

        private static Matrix bm(Matrix V)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);

            output = 4 * (-1 * V / 18).PointwiseExp();
            return output;
        }

        private static Matrix ah(Matrix V)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);

            output = 0.07 * (-1 * V / 20).PointwiseExp();
            return output;
        }

        private static Matrix bh(Matrix V)
        {
            Matrix output = Matrix.Build.Dense(V.RowCount, V.ColumnCount);
            Matrix temp = 30 - V;

            output = 1 / ((temp / 10).PointwiseExp() + 1);
            return output;
        }

        //Function for initialize voltage on cell
        public static Matrix initialConditions(int size)
        {
            Matrix ic = Matrix.Build.Dense(size, 1);
            ic[0, 0] = 55;

            return ic;
        }
        #endregion
    }
}