using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using System.IO;
namespace C2M2.NeuronalDynamics.Interaction
{
    public class LoadSimulation : MonoBehaviour
    {
        public string vrnFileName = "10-dkvm2_1d";
        public Gradient gradient;
        private bool loaded = false;


        public void Load(RaycastHit hit)
        {
            if (!loaded)
            {
                GameObject solveObj = new GameObject();
                solveObj.name = "Solver";
                solveObj.AddComponent<MeshFilter>();
                solveObj.AddComponent<MeshRenderer>();
                NDSimulation solver = solveObj.AddComponent<SparseSolverTestv1>();
                solver.vrnFileName = vrnFileName;
                solver.gradient = gradient;
                solver.Initialize();

                loaded = true;
                transform.parent.gameObject.SetActive(false);
            }
        }
        private void Awake()
        {
            string[] geoms = GetGeometryNames();
            string s = "Available geometries:";
            foreach(string geom in geoms)
            {
                s += "\n" + geom;
            }
            Debug.Log(s);
        }
        private string[] GetGeometryNames()
        {
            char sl = Path.DirectorySeparatorChar; ;
            string targetDir = Application.streamingAssetsPath + sl + "NeuronalDynamics" + sl + "Geometries";
            DirectoryInfo d = new DirectoryInfo(targetDir);

            FileInfo[] files = d.GetFiles("*.vrn");
            if (files.Length == 0) return null;

            string[] fileNames = new string[files.Length];
            for(int i = 0; i < files.Length; i++)
            {
                fileNames[i] = files[i].Name;
            }
            return fileNames;
        }
    }
}