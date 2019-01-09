using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(0)]
public struct EntityBuffer : IBufferElementData
{
    public Entity entity;
}

[InternalBufferCapacity(0)]
public struct TriangleBuffer : IBufferElementData
{
    public int Triangle;
}
[InternalBufferCapacity(0)]
public struct VertexCountBuffer : IBufferElementData
{
    public int pointCount;
}

[InternalBufferCapacity(128)]
public struct PointBuffer : IBufferElementData
{
    public float3 Point;
}

[InternalBufferCapacity(128)]
public struct FacingBuffer : IBufferElementData
{
    public float3 Facing;
}

[InternalBufferCapacity(128)]
public struct WidthBuffer : IBufferElementData
{
    public float width;
}

[InternalBufferCapacity(256)]
public struct VertexBuffer : IBufferElementData
{
    public float3 Vertex;
}

public struct MarkUpdate : IComponentData
{
    public Entity entity;
}

public struct MarkStatic : IComponentData
{
    public Entity entity;
}

public struct MeshAssigner : IComponentData
{
    public Entity targetMesh;
}
