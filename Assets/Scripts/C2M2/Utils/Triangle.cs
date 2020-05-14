namespace C2M2.Utils
{
    /// <summary>
    /// Stores the indices of a trinagle in a simpler to understand way
    /// </summary>
    public struct Triangle
    {
        // TODO: Have this store a hit point/positions of the indices,
        // or store a lambda for each point so that you know where the raycast hit the triangle
        public int v1 { get; private set; }
        public int v2 { get; private set; }
        public int v3 { get; private set; }
        public Triangle(int v1, int v2, int v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }
}
