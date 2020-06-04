using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System.Threading;

namespace C2M2
{
    namespace Utils
    {
        /// <summary>
        /// Writes general data to a CSV file
        /// </summary>
        public class CSVBuilder
        {
            public string filePath = null;
            private StringBuilder csv;

            /// <summary>
            /// Format data and save it to a csv file
            /// </summary>
            /// <param name="charCapacity"> (Optional) Number of characters your file will take. Improves performance if supplied befor ebuilding. </param>
            /// <param name="filePath"> (Optional) Supply a filepath. Otherwise file will be saved in the assets folder. </param>
            public CSVBuilder(string filePath = null, int charCapacity = 0)
            {
                // If the user supplies a file path, use that
                if (filePath != null) this.filePath = filePath;

                // If the user supplies a desired size, save enough StringBuilder space for that
                if (charCapacity > 0) csv = new StringBuilder(charCapacity);
                else csv = new StringBuilder();
            }
            /// <summary> Append a new line to the existing CSV file </summary>
            public void AddRow<T>(T[] newData)
            {
                string newLine = "";
                for (int i = 0; i < newData.Length; i++)
                {
                    newLine += newData[i].ToString() + ",";
                }
                // Remove last delimiter
                newLine.Remove(newLine.Length - 1);
                csv.AppendLine(newLine);
            }
            public void AddRow<T>(List<T> newData) => AddRow(newData.ToArray());
            /// <summary> Append several new lines to the existing CSV file </summary>
            public void AddMatrix<T>(T[][] newData)
            {
                for (int j = 0; j < newData.Length; j++)
                {
                    AddRow(newData[j]);
                }
            }
            public void AddMatrix<T>(List<T[]> newData) => AddMatrix(newData.ToArray());
            /// <summary> Save the built CSV-format data to a CSV file </summary>
            public void ExportCSV(string filePath = null, bool overwrite = true) => ExportCSV(csv.ToString(), filePath, overwrite);
            /// <summary> Provide fully-formatted CSV data and save it to a CSV file </summary>
            public void ExportCSV(string fullFile, string filePath = null, bool overwrite = true)
            {
                Debug.Log("Writing CSV file...");
                filePath += ".csv";
                if (overwrite) File.WriteAllText(filePath, fullFile);
                else File.AppendAllText(filePath, fullFile);
                Debug.Log("CSV written to " + filePath);
            }
        }

    }
}
