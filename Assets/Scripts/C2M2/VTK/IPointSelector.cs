/// Author: Stephan Grein (grein@temple.edu) 
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for mesh selections
/// </summary>
interface IPointSelector
{
    /// <summary>
    /// Returns the selection object
    /// </summary>
    /// <returns> a selection </returns>
    /// <seealso cref="Selection"/>
    List<PointSelection> GetSelections();
}

/// <summary>
/// Stores indices, Name, Color and the Mesh of the selected triangles
/// Immutable.
/// </summary>
class PointSelection
{
    /// Properties (readonly)
    public int[] Indices { get; private set; }
    public Vector3 Point { get; private set; }
    public string Name { get; private set; }
    public Mesh Mesh { get; private set; }

    /// <summary>
    /// Constructor for Selection
    /// </summary>
    /// <param name="indices">Triangle indices of the selection</param>
    /// <param name="name">Name of the selection</param>
    /// <param name="mesh">Mesh that has been selected</param>
    /// <param name="color">Nearest point to the selected triangle</param>
    public PointSelection(int[] indices, string name, Mesh mesh, Vector3 point)
    {
        Indices = indices;
        Name = name;
        Mesh = mesh;
        Point = point;
    }
}