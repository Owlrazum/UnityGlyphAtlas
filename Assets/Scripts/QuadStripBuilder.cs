using Unity.Mathematics;
using Unity.Collections;

/// <summary>
/// This is a bit modified version from my Rubik's Figure
/// </summary>
public struct QuadStripBuilder
{
    NativeArray<Vertex> vertices;
    NativeArray<short> indices;

    int2 prevIndices;
    int2 startIndices;

    public QuadStripBuilder(in NativeArray<Vertex> vertices, in NativeArray<short> indices)
    {
        this.vertices = vertices;
        this.indices = indices;
        prevIndices = int2.zero;
        startIndices = int2.zero;
    }
    public void Start(in float3x2 lineSegment, ref int2 buffersIndexers)
    {
        prevIndices.x = AddVertex(lineSegment[0], ref buffersIndexers);
        prevIndices.y = AddVertex(lineSegment[1], ref buffersIndexers);
        startIndices = prevIndices;
    }

    public void Continue(in float3x2 lineSegment, ref int2 buffersIndexers)
    {
        int2 newIndices = int2.zero;
        newIndices.x = AddVertex(lineSegment[0], ref buffersIndexers);
        newIndices.y = AddVertex(lineSegment[1], ref buffersIndexers);

        int4 quadIndices = new int4(prevIndices, newIndices.yx);
        AddQuadIndices(quadIndices, ref buffersIndexers);

        prevIndices = newIndices;
    }

    /// connects last added segment with the first one
    public void Finish(ref int2 buffersIndexers)
    {
        int4 quadIndices = new int4(prevIndices, startIndices.yx);
        AddQuadIndices(quadIndices, ref buffersIndexers);
    }

    private void AddQuadIndices(int4 quadIndicesToCheck, ref int2 buffersIndexers)
    {
        int4 quadIndices = quadIndicesToCheck;
        AddIndex(quadIndices.x, ref buffersIndexers);
        AddIndex(quadIndices.y, ref buffersIndexers);
        AddIndex(quadIndices.z, ref buffersIndexers);
        AddIndex(quadIndices.x, ref buffersIndexers);
        AddIndex(quadIndices.z, ref buffersIndexers);
        AddIndex(quadIndices.w, ref buffersIndexers);
    }

    private short AddVertex(float3 pos, ref int2 buffersIndexers)
    {
        Vertex vertex = new Vertex
        {
            Pos = pos
        };

        vertices[buffersIndexers.x] = vertex;
        short addedVertexIndex = (short)buffersIndexers.x;
        buffersIndexers.x++;

        return addedVertexIndex;
    }

    private void AddIndex(int vertexIndex, ref int2 buffersIndexers)
    {
        indices[buffersIndexers.y++] = (short)vertexIndex;
    }
}