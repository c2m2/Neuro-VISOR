using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
/// <summary>
/// VTUCache
/// </summary>
public class VTUCache<T>
{
    /// <summary>
    ///  TODO: Dummy method to test a cache mechanism
    /// </summary>
    private void Start()
    {
        Mesh mesh = new Mesh();
        VTUCache<Mesh> vtuCache = new VTUCache<Mesh>();
        /// Wait until 10 meshes available add them to the cache
        for (int i = 0; i < 10; i++)
        {
            VTUCache<Mesh>.AddElement(new Mesh());
        }
        /// Then keep adding each new arriving VTU file
    }

    private static Queue<T> data = new Queue<T>();
    private static readonly long MaxValue = 100;
    /// <summary>
    /// Add an element
    /// </summary>
    /// <param name="t"></param>
    public static void AddElement(T t)
    {
        if (data.Count < MaxValue)
        {
            data.Enqueue(t);
        }
    }

    /// <summary>
    /// Return the latest element unless 
    /// </summary>
    /// <returns></returns>
    public static T GetElement()
    {
        if (data.Count() > MaxValue)
        {
            return data.Dequeue();
        }
        return data.First();
    }

    /// <summary>
    /// Clear the data
    /// </summary>
    public static void Clear()
    {
        data.Clear();
    }

}
