using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using System.Collections.Generic;
using C2M2.NeuronalDynamics.Interaction.UI;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Interaction;
using C2M2.Simulation;
using Debug = UnityEngine.Debug;



namespace C2M2.Utils
{
    
    public class CSVWriter : MonoBehaviour
    {
        private string filename;
        private string csvFilename;
        private SparseSolverTestv1 sim;
        private GameManager gm = null;
        public bool single = false;
        private int numRows=0;
        private Stopwatch stopwatch;
        private long elapsed;
        private int times = 1;
        private float wtime;
        private float ctime;
        private long elapsedMilliseconds;
        private int size;

        private bool append = false;
     
        
        //Start is called on the frame when a script is enabled just before any of the Update methods are called the first time. This function can be a coroutine.
        public void Start()
        {   
            gm = GameManager.instance;
            sim = (SparseSolverTestv1)gm.activeSims[0];
            size = sim.Neuron.nodes.Count;
            UnityEngine.Debug.Log("File size: " + size);
            DateTime date = DateTime.Now;
            String formatted = date.ToString("MM-dd-yyyy-hh-mm-ss");
            String fname = "/neuro_visor_recording_"+formatted;
            String path = Application.dataPath + "/";
            if (!Directory.Exists(path + "CSV_Files")) Directory.CreateDirectory(path + "CSV_Files");
            path += "CSV_Files/";
            filename = path + fname+".bin";
            csvFilename = path + fname + ".csv";
            
            stopwatch = new Stopwatch();
            append = false;
            
            




        }

        public String getFileName()
        {
            return filename;
        }

        public void WriteToCSV(float sTime, double [] cellData)
        {
            Stopwatch stopWatch = new Stopwatch();
            
            stopWatch.Restart();
            
            
         
            if (cellData.Length!=0& numRows<10000*10000)
            {
                numRows++;
                
                BinaryWriter bw = new BinaryWriter(File.Open(filename, append ? FileMode.Append : FileMode.Create));
                
              
                    bw.Write(sTime);

           
                    for (int j = 1; j <= times; j++)
                    {
                        for (int i = 0; i < size; i++)
                        {
                            bw.Write(cellData[i] * sim.unitScaler);
                        }
                    }

                stopWatch.Stop();
                
                
                elapsedMilliseconds = stopWatch.ElapsedMilliseconds;
                
                wtime += elapsedMilliseconds;
                bw.Close();
                append = true;

            }
            else
            {
                //UnityEngine.Debug.Log("Update avg: " + (utime/100)+ " M: " + M.Length+ " H " + H.Length+ " N: " + N.Length);
                UnityEngine.Debug.Log("Write time: " + (wtime));
            }
            
            
        }
        public void ConvertToCSV()
        {
            stopwatch.Restart();
            using (StreamWriter csvWriter = new StreamWriter(csvFilename))
            {
                BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open));
                csvWriter.Write("Time (ms),");
                // csvWriter.Write("Gating Variables");
                for (int i = 0; i < size; i++) csvWriter.Write($"Vert[{i}],");
                csvWriter.WriteLine();

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
        
                    float sTime = reader.ReadSingle();
                    csvWriter.Write(sTime + ",");

             
                    for (int i = 0; i < size; i++)
                    {
                        double cellValue = reader.ReadDouble();
                        csvWriter.Write(cellValue + ",");
                    }
                    csvWriter.WriteLine();
                }
                reader.Close();
                csvWriter.Close();
            }

            ctime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log("Conversion time: "+ ctime);
            //delete the binary version of the file
            File.Delete(filename);
        }
        
    }

}