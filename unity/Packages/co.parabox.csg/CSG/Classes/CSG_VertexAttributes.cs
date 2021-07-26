namespace Parabox.CSG
{
    /// <summary>
    /// Mesh attributes bitmask.
    /// </summary>
    [System.Flags]
    enum CSG_VertexAttributes
    {
        /// <summary>
        /// Vertex positions.
        /// </summary>
        Position = 0x1,
        /// <summary>
        /// First UV channel.
        /// </summary>
        Texture0 = 0x2,
        /// <summary>
        /// Second UV channel. Commonly called UV2 or Lightmap UVs in Unity terms.
        /// </summary>
        Texture1 = 0x4,
        /// <summary>
        /// Second UV channel. Commonly called UV2 or Lightmap UVs in Unity terms.
        /// </summary>
        Lightmap = 0x4,
        /// <summary>
        /// Third UV channel.
        /// </summary>
        Texture2 = 0x8,
        /// <summary>
        /// Vertex UV4.
        /// </summary>
        Texture3 = 0x10,
        /// <summary>
        /// Vertex colors.
        /// </summary>
        Color = 0x20,
        /// <summary>
        /// Vertex normals.
        /// </summary>
        Normal = 0x40,
        /// <summary>
        /// Vertex tangents.
        /// </summary>
        Tangent = 0x80,
        /// <summary>
        /// All stored mesh attributes.
        /// </summary>
        All = 0xFF
    };
}
