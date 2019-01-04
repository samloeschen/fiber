using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(0)]
public struct VertexData : IBufferElementData
{
    public float3 Vertex;
}

[InternalBufferCapacity(0)]
public struct BatchedVertexData : IBufferElementData
{
    public float3 Vertex;
}

[InternalBufferCapacity(0)]
public struct TriangleData : IBufferElementData
{
    public int Triangle;
}


[InternalBufferCapacity(0)]
public struct BatchedTriangleData : IBufferElementData
{
    public int Triangle;
}
[InternalBufferCapacity(0)]
public struct VertexCountData : IBufferElementData
{
    public int pointCount;
}

[InternalBufferCapacity(0)]
public struct PointData : IBufferElementData
{
    public float3 Point;
}


[InternalBufferCapacity(0)]
public struct FacingData : IBufferElementData
{
    public float3 Facing;
}

[InternalBufferCapacity(0)]
public struct WidthData : IBufferElementData
{
    public float width;
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
