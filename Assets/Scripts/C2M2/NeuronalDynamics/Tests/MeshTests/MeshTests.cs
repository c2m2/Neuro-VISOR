using System.Collections;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using C2M2.NeuronalDynamics.UGX;

namespace C2M2.NeuronalDynamics.Tests
{	
	using DiameterAttachment = IAttachment<DiameterData>;
	using MappingAttachment = IAttachment<MappingData>;
	using Grid = C2M2.NeuronalDynamics.UGX.Grid;
      
	/// <summary>
	/// Behaviour can be attached to test mesh consistency
	/// </summary>
	public class MeshTests : MonoBehaviour
	{
	    #region Test selection
	    [Header ( "Grid generation tests" )]
	    public bool isModelGeometryVisible = false;
	    public bool isMappingChecked = true;
	    public bool isWindingConsistent = true;
	    public bool areNormalsConsistent = true;
	    public float eps = 1e-6f;
	    private float thresh = 5f;
	    #endregion
	    #region private
	    private GameObject go;
	    private static string hhCellFolder = "HHSolver";
	    private static string activeCellFolder = "ActiveCell";
	    private static string ugxExt = ".ugx";
	    private static string cngExt = ".CNG";
	    private static string spec1D = "_1d";
	    private static string specTris = "_tris";
	    #endregion

	    /// Start
	    void Start() {
		/// Read in grid files from ActiveCell folder
		string[] cellFiles = GetCellFiles ( hhCellFolder, activeCellFolder );
		Grid grid1d = new Grid ( new Mesh(), "1D cell" );
		grid1d.Attach ( new DiameterAttachment() );
		VertexAttachementAccessor<DiameterData> diams = new VertexAttachementAccessor<DiameterData> ( grid1d );
		
		UGXReader.Validate = false;
		UGXReader.ReadUGX ( cellFiles[1], ref grid1d );
		Grid grid2d = new Grid ( new Mesh(), "2D cell" );
		UGXReader.Validate = false;
		UGXReader.ReadUGX ( cellFiles[2], ref grid2d );

		/// Add visual components
		go = new GameObject ( grid1d.Mesh.name );
		go.AddComponent<MeshFilter>();
		go.AddComponent<MeshRenderer>();
		Vector3[] vertices = grid1d.Mesh.vertices;
		Vector3[] vertices2d = grid2d.Mesh.vertices;
		
		/// 1D within 3D cell test
		if (isModelGeometryVisible) {
		  foreach ( var edge in grid1d.Edges ) {
		    int from = edge.From.Id;
		    int to = edge.To.Id;
		    UnityEngine.Debug.Log ( "Draw line" );
		    ///  DrawLine(vertices[from], vertices[to], Color.red, Color.red);
		  }
		 }

		go.GetComponent<MeshRenderer>().material = new Material ( Shader.Find ( "Particles/Standard Surface" ) );
		if (isMappingChecked) {

		  MappingInfo mapping = MapUtils.BuildMap ( cellFiles[1], cellFiles[0], false, cellFiles[2] );
		  foreach ( var item in mapping.Data ) {
		       if (Vector3.Distance(vertices2d[item.Key], vertices[item.Value.Item1]) > (thresh * diams[item.Key].Diameter)) {
		       UnityEngine.Debug.LogError("Above threhsold");
		      }
		   }
		}

		/// Check winding order
		if (CheckWindingOrderConsistency(grid2d.Mesh)) {
		    UnityEngine.Debug.LogError("Inconsistent winding order");
		}
		
		/// Check read-in normals from ug4 with calculated from Unity
		if (!CheckNormalsConsistency(grid2d)) {
		    UnityEngine.Debug.LogError("inconsistent normals read-in vs. computed normals from Unity");
		}
	    }
	    ///////////////////////////////////////////////////////////////////
	    private bool CheckNormalsConsistency(in Grid grid2d) {
	     
	      VertexAttachementAccessor<NormalData> normalsUG = new VertexAttachementAccessor<NormalData> ( grid2d );
	      
	      Vector3[] normalsUnity = grid2d.Mesh.normals;
	      int n = normalsUnity.Length;
	      for (int i = 0; i < n; i++) {
		if (Vector3.Distance(normalsUG[i].Normal, normalsUnity[i]) > eps) {
		   UnityEngine.Debug.LogError("$Normals disagree at vertex >>{mesh.vertices[i]}<< with normals " + 
		   ">>{normalsUG[i]}<< and >>{normalsUnity[i]}<<");
		  return false;
		}
	      }
	      return true;
	    }
	    
	    ///////////////////////////////////////////////////////////////////
	    private bool CheckWindingOrderConsistency ( in Mesh mesh ) {
		Vector3[] vertices = mesh.vertices;
		
		int[] tris = mesh.triangles;

		/// mesh out of 1 triangle has always consistent winding order
		if ( tris.Length == 3 ) {
		    return true;
		}

		int index = tris.Length / 3;
		int numTris = tris.Length / 3;


		Stack<Tuple<int, int, int>> triStack = new Stack<Tuple<int, int, int>>();
		triStack.Push ( new Tuple<int, int, int> ( tris[0], tris[1], tris[2] ) );
		List<Tuple<int, int, int>> allTrianglesSoFar = new List<Tuple<int, int, int>>();
		while ( triStack.Count > 0 ) {
		    var ( i, j, k ) = triStack.Pop();
		    if ( CheckWinding ( i, j, k, allTrianglesSoFar ) ) return false;
		    allTrianglesSoFar.Add ( new Tuple<int, int, int> ( i, j, k ) );

		    /// get neighbor triangle
		    for ( int l = 0; l < tris.Length; l++ ) {
			if ( ( ( l + 0 ) == i && ( l + 1 ) == j ) ) { // l+0, l+1 is edge i, j
			    triStack.Push ( new Tuple<int, int, int> ( l, l + 1, l + 2 ) );
			}

			if ( ( ( l + 0 ) == j && ( l + 1 ) == k ) ) { // l+0, l+1 is edge j, k
			    triStack.Push ( new Tuple<int, int, int> ( l, l + 1, l + 2 ) );
			}

			if ( ( ( l + 0 ) == k && ( l + 1 ) == i ) ) { // l+0, l+1 is edge k, i
			    triStack.Push ( new Tuple<int, int, int> ( l, l + 1, l + 2 ) );
			}
		    }
		}
		return true;
	    }

	    /// CheckWinding
	    bool CheckWinding ( in int i, in int j, in int k, in List<Tuple<int, int, int>> tris ) {
		foreach ( var tri in tris ) {
		    if ( CheckWinding ( i, j, k, tri ) ) return false;
		}
		return true;
	    }


	    /// CheckWinding: Use Ordered edge rule
	    bool CheckWinding ( in int i, in int j, in int k, in Tuple<int, int, int> tri ) {
		for ( int l = 0; l < 3; l++ ) {
		    int lpl = ( l + 1 ) % 3;
		    for ( int m = 0; m < 3; m++ ) {
			int kpl = ( m + 1 ) % 3;
			if ( m == l && kpl == lpl ) {
			    return true;
			}
		    }
		}
		return false;
	    }

	    /// DrawLine helper
	    private static void DrawLine ( in Vector3 start, in Vector3 end, in Color colorStart, in Color colorEnd ) {
		    GameObject myLine = new GameObject();
		    myLine.transform.position = start;
		    myLine.AddComponent<LineRenderer>();
		    LineRenderer lr = myLine.GetComponent<LineRenderer>();
		    lr.material = new Material ( Shader.Find ( "Particles/Standard Surface" ) );

            lr.startColor = colorStart;
            lr.endColor = colorEnd;

            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;

		    lr.SetPosition ( 0, start );
		    lr.SetPosition ( 1, end );
	    }

	    
	    /// Helper method
	    private string[] GetCellFiles ( in string hhCellFolder, in string activeCellFolder ) {
		string[] cells = new string[3];

		char slash = Path.DirectorySeparatorChar;
		string cellPath = Application.streamingAssetsPath + slash + hhCellFolder + slash + activeCellFolder + slash;
		// Only take the first cell found
		cellPath = Directory.GetDirectories ( cellPath ) [0];

		string[] files = Directory.GetFiles ( cellPath );
		foreach ( string file in files ) {
		    // If this isn't a non-metadata ugx file,
		    if ( !file.EndsWith ( ".meta" ) && file.EndsWith ( ugxExt ) ) {
			if ( file.EndsWith ( cngExt + ugxExt ) ) {
			    // If it ends with .CNG.ugx, it's the 3D cell
			    cells[0] = file;
			} else if ( file.EndsWith ( cngExt + spec1D + ugxExt ) ) {
			    // If it ends with .CNG_1d.ugx, it's the 3D cell
			    cells[1] = file;
			} else if ( file.EndsWith ( cngExt + specTris + ugxExt ) ) {
			    // If it ends with .CNG_tris.ugx, it's the 3D cell
			    cells[2] = file;
			}
		    }
		}
		return cells;
	    }
	}
}
