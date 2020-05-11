#if UNITY_EDITOR
#pragma warning disable 0414 // warning CS0414: The field 'VTKBehaviour.count' is assigned but its value is never used
#endif

using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.Serialization.Formatters.Binary;
namespace C2M2
{
    /* 
      0. Address TODOs in code and the following bullet points in descending order of priority:
      1. Speed-up and slow-down animations in Update() method.
      2. Support multiple stacks of vtu files or more than one pvd file? Combine with Jacob's code.
      (3.) Get dataPath with audio input form user... more user-friendly potentially
      (4.) Handle non-triangular surfaces
      (5.) See if UV wrappings for textures can be used from VTK files to enhance the visualization
      (6.) Handle volumes
    */

    /*
      BUILD STANDALONE APPLICATION:
      1. Change from Net 2.0 Subset to Net 2.0 in the Build-Setting-Player tab
      2. Assets are not included automatically, the resource folder is included. Use the resource folder API to
      include data or to offer a folder for user-specified location. Note that vtu files are not known by Unity
      thus one has to use/place them in the Assets/StreamingAssets folder or a user-specified folder has to put
      in interactively in Unity / with the Oculus Rift.
      3. If enabling the debug flag in the build settings tab one can enable debug in the built application
    */

    /* 
      GENERAL NOTES:
      1. We used Activiz version: ActiViz.NET-5.8.0.607-win64-OpenSource
      2. All C#-Dlls (Kitware.*) from ActiViz' bin-folder have to be in a Plugins-folder in Unity
      3. For development in Unity need to add Activiz to the Windows Path: 
      https://www.architectryan.com/2018/03/17/add-to-the-path-on-windows-10/ 
      4. Latest Activiz version has to be cross-compiled for Windows if necessary
    */

    /// <summary>
    /// VTKBehaviour implements methods to visualize VTK data within Unity
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VTKBehaviour : MonoBehaviour
    {
        /// <summary>
        /// public members go here
        /// </summary>
        public List<Mesh> meshes;
        public MeshFilter meshf = new MeshFilter();
        public string component = "test";
        public string dataPath = "data";
        /// <summary> Type of file to read in </summary>
        [Tooltip("Type of file to read in")]
        public string fileExtension = "*.vtu";
        public string filePath;
        public int numVtus;
        //public vtkLookupTable lut = vtkLookupTable.New();
        public int animationSpeed { get; set; }
        public int counter = 0;
        public int stepCode = 0;
        public VTUPlayer buttonInfo;
        public int compoundColliderResolution = 75;

        public GameObject SphereColliderPrefab;

        /// <summary>
        /// private members go here 
        /// </summary>

        // private vtkDataArray testScalar = null;
        private bool initVRinteraction = true;
        private int count = 0;

        private Mesh mesh;

        private long numVertices;
        long l;


        /// <summary>
        /// Initialize the data path to the VTU data folder location
        /// </summary>
        /*
         TODO: improve this path. should work if building as a standalone 
               and as well if the data is provided at an arbitrary location
               not necessarily in the Assets folder or Resources folder
        */
        private void Awake()
        {
            filePath = Application.streamingAssetsPath + "/" + dataPath + "/";
        }

        /// <summary>
        /// Called when the game starts, thus prepares all meshes in this step
        /// </summary>
        private void Start()
        {
            PrepareMeshes();
            /// Then add VR interaction once, and pass the bounds of the first (static mesh)
            if (initVRinteraction)
            {

                // AddInteractionComponents vrComponents = gameObject.AddComponent<AddInteractionComponents>();
                // vrComponents.compoundColliderResolution = this.compoundColliderResolution;
                // vrComponents.Build();
                GetComponent<RaycastMeshCollider>().Build(gameObject);
                meshes[0].Rescale(transform);
                initVRinteraction = false;
            }
            SetMesh();
        }

        // Update is called once per frame
        private void Update()
        {
            /// Need a mesh first as reference (TODO: This could be improved if scale of geometry changes over time!)
            SetMesh();
        }

        /// <summary> Prepares the meshes, i.e. read VTU files and store them in a list of type Mesh </summary>
        private void PrepareMeshes()
        {
            numVtus = System.IO.Directory.GetFiles(filePath, fileExtension).Length;
            foreach (string file in System.IO.Directory.GetFiles(filePath, fileExtension))
            {
                PrepareMesh(file);
            }
            meshf = GetComponent<MeshFilter>();
            meshf.sharedMesh = meshes[0];
        }

        /// <summary> Prepares a single mesh, cf. PrepareMeshes function above </summary>
        /// <param name="file"> .vtu file to be converted </param>
        private void PrepareMesh(string file)
        {
            // initialize empty mesh
            /*  Mesh mesh = new Mesh();
              // try to create vtk reader
              //Debug.Log("Trying to create vtkReader object...");
             // vtkXMLUnstructuredGridReader vtkReader = vtkXMLUnstructuredGridReader.New();
              //Debug.Log("success!");

              // check for file existence 
              if (!File.Exists(file))
              {
                  Debug.Log("Cannot read vtk file \"" + file + "\"");
                  return;
              }

              // actually read the file and measure time elapsed
              // 1 file marked at 31 ms
             // vtkReader.SetFileName(file);
             // vtkReader.Update();

              //Debug.Log("numberOfPoints: " + vtkReader.GetNumberOfPoints());

              // filter geometry to polydata
             // vtkGeometryFilter geometryFilter = vtkGeometryFilter.New();
              //geometryFilter.SetInput(vtkReader.GetOutput());
              //geometryFilter.Update();
              //vtkPolyData polydata = geometryFilter.GetOutput();
              //Debug.Log("numberOfPolyFilterPoints: " + polydata.GetNumberOfPoints());

              // get points
             // vtkPoints vtk_points = polydata.GetPoints();
             // Debug.Log("size of Points: " + vtk_points.GetNumberOfPoints());

              // get scalar with name of component
             // testScalar = polydata.GetPointData().GetArray(component);
              // assert for components = numberofpoints
              //Debug.Log("test scalar has: " + testScalar.GetNumberOfComponents() + " component(s).");
             // Debug.Assert(testScalar.GetNumberOfComponents() == 1, "array \"test\" (assumed as a scalar field) has not exactly one component.");
             // Debug.Assert(testScalar.GetNumberOfTuples() == polydata.GetNumberOfPoints(), "scalar field \"test\" has not enough values");

              // print point coordinates for debugging purposes
              for (int i = 0; i < polydata.GetNumberOfPoints(); i++)
              {
                  double[] p = polydata.GetPoint(i);
                  double val = testScalar.GetTuple1(i);

                  if (!Debug.isDebugBuild)
                  {
                      Debug.Log("Point " + i + " : (" + p[0] + " " + p[1] + " " + p[2] + ")" + " has value: \"" + val + "\"");
                  }
              }

              // get the triangles from the geometry
              // TODO: account for possible occuring other surface polygons 
              vtkTriangleFilter triangleFilter = vtkTriangleFilter.New();
              triangleFilter.SetInput(polydata);
              triangleFilter.Update();

              // iterate over all points and store them in the Unity mesh
              numVertices = polydata.GetNumberOfPoints();
              Vector3[] vertices = new Vector3[numVertices];
              double[] pnt;
              for (l = 0; l < numVertices; ++l)
              {
                  pnt = polydata.GetPoint(l);
                  vertices[l] = new Vector3((float)pnt[0], (float)pnt[2], (float)pnt[1]);
              }
              mesh.vertices = vertices;

              // iterate over triangles and store them in the Unity mesh
              long numTriangles = polydata.GetNumberOfPolys();
              vtkCellArray polys = polydata.GetPolys();
              if (polys.GetNumberOfCells() > 0)
              {
                  int[] triangles = new int[numTriangles * 3];
                  int prim = 0;
                  vtkIdList pts = vtkIdList.New();
                  polys.InitTraversal();
                  while (polys.GetNextCell(pts) != 0)
                  {
                      // TODO: add assert for long to int (unsafe) downcast
                      for (l = 0; l < pts.GetNumberOfIds(); ++l)
                          triangles[prim * 3 + l] = (int)pts.GetId(l);

                      ++prim;
                  }
                  mesh.triangles = triangles;
                  mesh.RecalculateNormals();
                  mesh.RecalculateBounds();
              }

              // iterate over vertices, assign colors and store them in the Unity mesh
              SetLut(LutPreset.RAINBOW);
              if (numVertices > 0 && testScalar != null)
              {
                  Color32[] colors32 = new Color32[numVertices];

                  for (int i = 0; i < numVertices; ++i)
                  {
                      colors32[i] = GetColor32AtIndex(i);
                  }

                 mesh.colors32 = colors32;
              }

              // iterate over lines (might have line graph geometry not only surface triangles)
              vtkCellArray lines = polydata.GetLines();
              if (lines.GetNumberOfCells() > 0)
              {
                  ArrayList idList = new ArrayList();
                  vtkIdList pts = vtkIdList.New();
                  lines.InitTraversal();
                  while (lines.GetNextCell(pts) != 0)
                  {
                      for (int i = 0; i < pts.GetNumberOfIds() - 1; ++i)
                      {
                          idList.Add(pts.GetId(i));
                          idList.Add(pts.GetId(i + 1));
                      }
                  }

                  mesh.SetIndices(idList.ToArray(typeof(int)) as int[], MeshTopology.Lines, 0);
                  mesh.RecalculateBounds();
              }

              vtkCellArray points = polydata.GetVerts();
              long numPointCells = points.GetNumberOfCells();
              if (numPointCells > 0)
              {
                  ArrayList idList = new ArrayList();
                  Kitware.VTK.vtkIdList pts = Kitware.VTK.vtkIdList.New();
                  points.InitTraversal();
                  while (points.GetNextCell(pts) != 0)
                  {
                      for (int i = 0; i < pts.GetNumberOfIds(); ++i)
                      {
                          idList.Add(pts.GetId(i));
                      }
                  }

                  mesh.SetIndices(idList.ToArray(typeof(int)) as int[], MeshTopology.Points, 0);
                  mesh.RecalculateBounds();
              }

              // Debug.Log("Time elapsed [ms]: " + sw.Elapsed.Milliseconds);
              count++;
              mesh.name = "Geometry " + count; 
              meshes.Add(mesh);*/
        }

        /// <summary>
        /// Sets the main mesh to the current mesh from the list of Meshes (Depending on current frame)
        /// </summary>
        private void SetMesh()
        {
            // Display a new mesh every (animationSpeed) frames
            // TODO: add possibility to speed up or slow down animation here?
            if (animationSpeed == stepCode)
            {
                if (counter < (meshes.Count - 1))
                {
                    counter++;
                }

                animationSpeed = 0;
                //buttonInfo.playing = false;
            }
            else if (animationSpeed == -stepCode)
            {
                if (counter > 0)
                {
                    counter--;
                }

                animationSpeed = 0;
                // buttonInfo.playing = false;
            }
            else if ((counter < meshes.Count) && (counter >= 0) && (Time.frameCount % (11 - Mathf.Abs(animationSpeed)) == 0))
            {
                mesh = meshes[counter];
                /// move to start
                //meshf = new MeshFilter();
                //meshf = GetComponent<MeshFilter>();
                meshf.mesh = mesh;
                meshf.sharedMesh = mesh;

                //Only increment or deincrement counter if you aren't at the first or last element
                if (animationSpeed > 0 && counter < (meshes.Count - 1))
                {
                    counter++;
                }
                else if (animationSpeed < 0 && counter > 0)
                {
                    counter--;
                }
                else if (counter == meshes.Count - 1)
                {
                    //  buttonInfo.playing = false;
                    animationSpeed = 0;
                }
                else if (counter == 0)
                {
                    // buttonInfo.playing = false;
                    animationSpeed = 0;
                }
                else if (animationSpeed == 0)
                {

                }
            }
        }

        /// <summary>
        /// Get a byte color form VTK lookup for Unity
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        /*private Color32 GetColor32AtIndex(int i)
        {
           byte[] color = GetByteColorAtIndex(i);
           // return new Color(0, 1, 0, 1); // solid green RGBA
           return new Color32(color[0], color[1], color[2], 255);
        }*/

        /// <summary>
        /// Helper method to get color for vertex with index i
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        /* private Color GetColorAtIndex(int i)
         {
             return GetColor32AtIndex(i);
         }*/

        /*
        /// <summary>
        /// Helper method to get byte[] color for vertex with index i
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private byte[] GetByteColorAtIndex(int i)
        {
            double scalar = testScalar.GetTuple1(i);
            double[] dcolor = lut.GetColor(scalar);
            byte[] color = new byte[3];
            for (uint j = 0; j < 3; j++)
            {
                color[j] = (byte)(255 * dcolor[j]);
            }
            return color;
        }

        /// <summary>
        /// possible vtk color lookup tables
        /// </summary>
        public enum LutPreset
        {
            BLUE_RED,
            RED_BLUE,
            RAINBOW
        }

        /// <summary>
        /// assign a vtk color lookup table
        /// </summary>
        /// <param name="preset"></param>
        public void SetLut(LutPreset preset)
        {
            double[] range = { 0.0, 1.0 };
            if (testScalar != null)
            {
                range = testScalar.GetRange();
            } else { 
            //   Debug.Log("VtkToUnity.SetLut(): No color array set!");
            } 

           // Debug.Log("Color range: " + range[0] + ", " + range[1]);
            SetLut(preset, range[0], range[1]);
        }

        /// <summary>
        /// assign a vtk color lookup table with min range and max range
        /// </summary>
        /// <param name="preset"></param>
        /// <param name="rangeMin"></param>
        /// <param name="rangeMax"></param>
        public void SetLut(LutPreset preset, double rangeMin, double rangeMax)
        {
            lut.SetTableRange(rangeMin, rangeMax);
            switch (preset)
            {
                case LutPreset.BLUE_RED:
                    lut.SetHueRange(0.66, 1.0);
                    lut.SetNumberOfColors(128);
                    break;
                case LutPreset.RED_BLUE:
                    lut.SetHueRange(1.0, 0.66);
                    lut.SetNumberOfColors(128);
                    // lut.SetNumberOfTableValues(2);
                    // lut.SetTableValue(0, 1.0, 0.0, 0.0, 1.0);
                    // lut.SetTableValue(1, 0.0, 0.0, 1.0, 1.0);
                    break;
                case LutPreset.RAINBOW:
                    lut.SetHueRange(0.0, 0.66);
                    lut.SetNumberOfColors(256);
                    break;
                default:
                    break;
            }
            lut.Build();
        }*/
    }
}