using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(0)]
public struct VertexBuffer : IBufferElementData
{
    public float3 Vertex;
}

[InternalBufferCapacity(0)]
public struct BatchedVertexBuffer : IBufferElementData
{
    public float3 Vertex;
}

[InternalBufferCapacity(0)]
public struct TriangleBuffer : IBufferElementData
{
    public int Triangle;
}


[InternalBufferCapacity(0)]
public struct BatchedTriangleBuffer : IBufferElementData
{
    public int Triangle;
}
[InternalBufferCapacity(0)]
public struct VertexCountBuffer : IBufferElementData
{
    public int pointCount;
}

[InternalBufferCapacity(0)]
public struct PointBuffer : IBufferElementData
{
    public float3 Point;
}

[InternalBufferCapacity(0)]
public struct FacingBuffer : IBufferElementData
{
    public float3 Facing;
}

[InternalBufferCapacity(0)]
public struct WidthBuffer : IBufferElementData
{
    public float width;
}

[InternalBufferCapacity(0)]
public struct BatchQueue : IBufferElementData
{
    public Entity entity;
}

public struct MeshDirty : IComponentData
{
    public Entity entity;
}
public struct Line : IComponentData
{
    public byte isActive;
}

public struct BatchedLine : IComponentData
{
    public Entity batchEntity;
}
