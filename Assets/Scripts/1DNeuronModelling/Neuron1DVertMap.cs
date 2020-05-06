using System.Collections.Generic;
using System.Linq;
/// <summary> Holds two dictionaries: One to lookup a 1D vertex given a 3D vertes, one to lookup a list of 3D vertices given their associated 1D neuron </summary>
public class Neuron1DVertMap
{
    public int count1D { get; private set; }
    public int count3D { get; private set; }
    public int[] verts1D { get; private set; }
    private Dictionary<int, List<int>> oneToThree = new Dictionary<int, List<int>>();
    private Dictionary<int, int> threeToOne = new Dictionary<int, int>();
    public Neuron1DVertMap(Dictionary<int, List<int>> oneToThree, Dictionary<int, int> threeToOne)
    {
        this.oneToThree = oneToThree;
        this.threeToOne = threeToOne;
        verts1D = oneToThree.Keys.ToArray();
        count1D = oneToThree.Keys.Count;
        count3D = threeToOne.Keys.Count;
    }
    /// <summary>
    /// Given a 1D vertex index, return the associated 3D vertices
    /// </summary>
    public List<int> Get3DVerts(int vert1D)
    {
        if (oneToThree.ContainsKey(vert1D)) { return oneToThree[vert1D]; }
        else { throw new System.Exception("Could not find 3D verts associated with 1D vert " + vert1D); }
    }
    /// <summary>
    /// Given a 3D vertex index, return the associated 1D vertices
    /// </summary>
    public int Get1DVert(int vert3D)
    {
        if (threeToOne.ContainsKey(vert3D)) { return threeToOne[vert3D] - 1; }
        else { throw new System.Exception("Could not find 1D vert associated with 3D vert " + vert3D); }
    }
}
