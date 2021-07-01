using System.Text;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace C2M2.Utils.DebugUtils
{
    /// <summary>
    /// Gets scripts and other asset dependencies for all scenes.
    /// </summary>
    /// <remarks>
    /// Can be called from the toolbar (Assets/Get All Asset Dependencies),
    /// or used as a monobehaviour. 
    /// The menu item will only get dependencies at the moment that it is called.
    /// The monobehaviour will get all dependencies used at runtime.
    /// </remarks>
    public class GetAllDependencies : MonoBehaviour
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Get all dependencies for all assets placed in scenes 
        ///             in the project and prints them to the console. 
        /// </summary>
        /// <remarks>   
        ///             Jacob Wells, 5/6/2020.
        ///             Adapted from:
        ///             https://docs.unity3d.com/ScriptReference/AssetDatabase.GetDependencies.html
        ///             https://stackoverflow.com/questions/26615480/how-to-transform-an-array-of-file-paths-into-a-hierarchical-json-structure
        /// </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        [MenuItem("Assets/Get All Asset Dependencies")]
        static void GetAllDependenciesForScenes()
        {

            // Print result
            Debug.Log("All direct and indirect dependencies from all scenes in project:\n\n" + FindDependencies().ToString());
        }

        static Dir FindDependencies()
        {
            string[] allScenes = AssetDatabase.FindAssets("t:Scene");
            string[] allPaths = new string[allScenes.Length];
            int curSceneIndex = 0;

            // Find and store the path to every scene
            foreach (string guid in allScenes)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                allPaths[curSceneIndex] = path;
                ++curSceneIndex;
            }
            // Find all asset dependencies of every scene recursively
            string[] dependencies = AssetDatabase.GetDependencies(allPaths, true);

            // Build a folder hierarchy string for every asset dependency
            StringBuilder dependenciesString = new StringBuilder();
            dependenciesString.AppendLine();
            Dir root = new Dir("");
            foreach (string dependency in dependencies)
            {
                root.FindOrCreate(dependency);
            }
            return root;
        }

        private List<Dir> assetRecord = new List<Dir>(1000);

        private void Awake()
        {
            assetRecord.Add(FindDependencies());
        }
        private void Start()
        {
            assetRecord.Add(FindDependencies());
        }

        private void Update()
        {
            // Each frame, find all dependencies
            assetRecord.Add(FindDependencies());
        }

        private void OnApplicationQuit()
        {
            MergeRecords(assetRecord);
        }

        // Given multiple lists of dependencies, merges directories with like names and removes duplicate assets
        private Dir MergeRecords(List<Dir> records)
        {
            Dir root = new Dir("");
            foreach(Dir record in records)
            {

            }
            return null;
        }
        /// <summary>
        /// Stores information about a directory and the files/directories nested in it
        /// </summary>
        class Dir
        {
            public string Name { get; set; }
            public Dictionary<string, Dir> Dirs { get; set; }
            public HashSet<string> Files { get; set; }

            public Dir(string name)
            {
                Name = name;
                Dirs = new Dictionary<string, Dir>();
                Files = new HashSet<string>();
            }

            /// <summary>
            /// Scan full paths and pick out new files/directories from it
            /// </summary>
            public Dir FindOrCreate(string path, bool mightBeFile = true)
            {
                int i = path.IndexOf('/');
                if (i > -1)
                {
                    Dir dir = FindOrCreate(path.Substring(0, i), false);
                    return dir.FindOrCreate(path.Substring(i + 1), true);
                }

                if (path == "") return this;

                // if the name is at the end of a path and contains a "." 
                // we assume it is a file (unless it is "." by itself)
                if (mightBeFile && path != "." && path.Contains("."))
                {
                    Files.Add(path);
                    return this;
                }

                Dir child;
                if (Dirs.ContainsKey(path))
                {
                    child = Dirs[path];
                }
                else
                {
                    child = new Dir(path);
                    Dirs.Add(path, child);
                }
                return child;
            }
            public override string ToString()
            {
                string s = Name;
                foreach (var kvp in Dirs)
                {
                    string name = kvp.Key;

                    s += kvp.Value.ToString(1);
                }
                return s;
            }
            private string ToString(int tabCount)
            {
                string s = Name + '/';
                foreach (var kvp in Dirs)
                {
                    string name = kvp.Key;
                    //s += name;
                    string tabs = '\n' + String.Concat(Enumerable.Repeat("    ", tabCount));
                    s += tabs + kvp.Value.ToString(tabCount + 1);
                    foreach (string fileName in kvp.Value.Files)
                    {
                        s += tabs + "    " + fileName;
                    }
                }
                return s;
            }
            public Dir Merge(Dir other)
            {

                return null;
            }
        }
    }
}