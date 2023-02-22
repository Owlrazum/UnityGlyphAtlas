using System;

using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct RectMats
{
    public Material Fill;
    public Material Stroke;

    public Material[] GetArray()
    {
        return new Material[] { Stroke, Fill };
    }
}

public class CppRectRenderer : MonoBehaviour
{
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    [SerializeField]
    RectMats rectMats;

    Mesh rectMesh;

    int maxVertexCount;
    int maxIndexCount;

    void Awake()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshRenderer.sharedMaterials = rectMats.GetArray();

        maxVertexCount = 4 + 8 * 2;
        maxIndexCount = 6 + 24 * 2;

        rectMesh = new Mesh();;
        rectMesh.name = "CustomQuad";
        rectMesh.MarkDynamic();

        rectMesh.SetVertexBufferParams(maxVertexCount, VertexBufferMemoryLayout);
        rectMesh.SetIndexBufferParams(maxIndexCount, IndexFormat.UInt16);
    }

    NativeArray<Vertex> vertexBuffer;
    NativeArray<short> indexBuffer;
    QuadStripBuilder quadStripBuilder;

    public void Render(CppRect rect, float borderWidth, float depth)
    {
        transform.position = new float3(rect.x, -rect.y, -depth);
        using (vertexBuffer = new(maxVertexCount, Allocator.Temp))
        using (indexBuffer = new NativeArray<short>(maxIndexCount, Allocator.Temp))
        {
            quadStripBuilder = new(vertexBuffer, indexBuffer);

            int2 buffersIndexers = new int2(0, 0); //x: vertex, y: index

            float2 size = new float2(rect.w, rect.h);

            float3x2 topLeft = new float3x2(
                new float3(borderWidth, -borderWidth, 0), 
                new float3(0, 0, 0));
            float3x2 topRight = new float3x2(
                new float3(rect.w - borderWidth, -borderWidth, 0), 
                new float3(rect.w, 0, 0));
            float3x2 bottomRight = new float3x2(
                new float3(rect.w - borderWidth, -rect.h + borderWidth, 0), 
                new float3(rect.w, -rect.h, 0));
            float3x2 bottomLeft = new float3x2(
                new float3(borderWidth, -rect.h + borderWidth, 0), 
                new float3(0, -rect.h, 0));

            quadStripBuilder.Start(topLeft, ref buffersIndexers);
            quadStripBuilder.Continue(topRight, ref buffersIndexers);
            quadStripBuilder.Continue(bottomRight, ref buffersIndexers);
            quadStripBuilder.Continue(bottomLeft, ref buffersIndexers);
            quadStripBuilder.Finish(ref buffersIndexers);

            int2 firstSubMeshRange = new int2(0, buffersIndexers.y);

            quadStripBuilder.Start(new float3x2(topLeft.c0, topRight.c0), ref buffersIndexers);
            quadStripBuilder.Continue(new float3x2(bottomLeft.c0, bottomRight.c0), ref buffersIndexers);

            rectMesh.subMeshCount = 2;
            int2x2 subMeshIndexers = new int2x2(
                firstSubMeshRange,
                new int2(firstSubMeshRange.y, buffersIndexers.y - firstSubMeshRange.y)
            );

            rectMesh.SetVertexBufferData(vertexBuffer, 0, 0, buffersIndexers.x);
            rectMesh.SetIndexBufferData(indexBuffer, 0, 0, buffersIndexers.y);

            rectMesh.SetSubMesh(0, new SubMeshDescriptor(subMeshIndexers[0].x, subMeshIndexers[0].y));
            rectMesh.SetSubMesh(1, new SubMeshDescriptor(subMeshIndexers[1].x, subMeshIndexers[1].y));

            
            rectMesh.bounds = new(new float3(size.x / 2, -size.y / 2, 0), new float3(size / 2, 0));

            meshFilter.mesh = rectMesh;
        }
    }

    readonly static VertexAttributeDescriptor[] VertexBufferMemoryLayout =
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, stream: 0),
    };
}

public struct Vertex
{
    public float3 Pos;
}