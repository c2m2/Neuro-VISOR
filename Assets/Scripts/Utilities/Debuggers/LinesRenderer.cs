using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace C2M2
{
    using UGX;
    namespace Utilities
    {
        /// <summary>
        /// Draw a series of disjointed lines given a list of edges using the native LineRenderer class
        /// </summary>
        public class LinesRenderer : MonoBehaviour
        {
            [Header("LineRenderer Settings")]
            public Color color = Color.green;
            private LineRenderer[] lineRenderers;
            private GameObject renderersGo;
            
            /// <summary>
            /// Build line renderers and get parent GameObject back
            /// </summary>
            public GameObject Constr(Grid grid, Color color, float lineWidth)
            {
                this.color = color;

                //return InitializeRenderers(grid.Edges, grid.Mesh.vertices, lineWidth);
                return InitializeRenderers(grid.Vertices, grid.Mesh.vertices, lineWidth);
            }
            private GameObject InitializeRenderers(List<Vertex> verts, Vector3[] vertPos, float lineWidth)
            {
                char slash = Path.DirectorySeparatorChar;
                GameObject renderersGo = Instantiate(Resources.Load("Prefabs" + slash + "LineRenderer"), transform) as GameObject;
               // renderersGo = InstantiateChild(transform);
                LineRenderer lr = renderersGo.GetComponent<LineRenderer>();
                lr.startColor = color;
                lr.endColor = color;
                lr.widthMultiplier = lineWidth;

                // Make positions for the linerenderer
                List<Vector3> lrPos = new List<Vector3>(verts.Count);


                bool[] visited = new bool[verts.Count];
                int startId = 0;
   
                // Fill our position graph
                AddVertsRecursive(verts[startId]);

                lr.positionCount = lrPos.Count;
                lr.SetPositions(lrPos.ToArray());

                //      Add n1's position to the list,
                //      Get n1's neighbors
                //          for each neighbor n2
                //              Add n2's position to the list,
                //              Get n2's neighbors
                //              for each neighbor n3
                //                  Add n3's position to the list
                //                  ...
                //              add n3 to the list again, move to next neighbor
                //          add n2 to the list again, move to next neighbor
                //      add n1 to the list again, move to next neighbor


                renderersGo.transform.parent = transform;

                return renderersGo;
                void AddVertsRecursive(Vertex vert)
                {
                    // Skip this vert if we've already visited it
                    if (visited[vert.Id]) { return; }
                    else { visited[vert.Id] = true; }
                    // Add base position
                    lrPos.Add(vertPos[vert.Id]);
                    // Get neighbors
                    List<Vertex> neighbors = vert.Neighbors;
                    // Recursively add each neighbor to pos list
                    foreach (Vertex neighbor in neighbors)
                    {
                        // Add the chain of all neighbors until there are no more, then wind back around
                        AddVertsRecursive(neighbor);
                        // Add parent position again to remain contiguous for LineRenderer
                        lrPos.Add(vertPos[vert.Id]);
                    }
                }
            }

            private GameObject InitializeRenderers(List<Edge> edges, Vector3[] vertices, float lineWidth)
            {
                renderersGo = InstantiateChild(transform);

                lineRenderers = new LineRenderer[edges.Count]; // Two points per line

                for (int i = 0; i < edges.Count; i++)
                {
                    // Get vertex positions
                    Vector3 v1 = vertices[edges[i].From.Id];
                    Vector3 v2 = vertices[edges[i].To.Id];
                    
                    // Render positions in new LineRenderer
                    GameObject renderGo = new GameObject("LineRenderer");
                    renderGo.transform.parent = renderersGo.transform;
                    renderGo.transform.position = Vector3.zero;
                    renderGo.transform.eulerAngles = Vector3.zero;

                    // Instantiate a prefab instance of our linerenderer, make the lineRenderers object its parent so it doesn't flood the editor
                    char slash = Path.DirectorySeparatorChar;
                    GameObject renderer = Instantiate(Resources.Load("Prefabs" + slash + "LineRenderer"), transform) as GameObject;

                    // Set LineRenderer settings
                    lineRenderers[i] = renderer.GetComponent<LineRenderer>();
                    lineRenderers[i].startColor = color;
                    lineRenderers[i].endColor = color;
                    lineRenderers[i].SetPositions(new Vector3[] { v1, v2 });
                    lineRenderers[i].widthMultiplier = lineWidth;

                    // Move the new renderer down the hierarchy for organization
                    renderer.transform.parent = renderersGo.transform;                   
                }
                return renderersGo;
            }
            private GameObject InstantiateChild(Transform parent)
            {
                GameObject go = new GameObject();
                go.name = "LinesRenderer";
                go.transform.parent = parent;
                go.transform.position = Vector3.zero;
                go.transform.eulerAngles = Vector3.zero;
                go.transform.localScale = Vector3.one;
                return go;
            }

            public void Toggle(bool on) => renderersGo.SetActive(on);
            private bool enabledPrev = true;
            private IEnumerator CheckToggle()
            {
                // If 'enabled' was toggled, 
                if(enabled != enabledPrev)
                {
                    Toggle(enabled);
                }
                enabledPrev = enabled;
                yield return null;
            }
        }
    }
}
