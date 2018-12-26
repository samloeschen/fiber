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
public struct PointData : IBufferElementData
{
    public float3 Point;
}
