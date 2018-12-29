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
    public UnityEngine.Vector3 Vertex;
}
[InternalBufferCapacity(0)]
public struct TriangleData : IBufferElementData
{
    public int Triangle;
}

[InternalBufferCapacity(0)]
public struct PointData : IBufferElementData
{
    public float3 Point;
}

[InternalBufferCapacity(0)]
public struct EntityData : IBufferElementData
{
    public Entity entity;
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
public struct IntRange : IComponentData
{
    public int lowerBound;
    public int upperBound;
}
public struct EntityRef : IComponentData
{
    public Entity entity;
}



