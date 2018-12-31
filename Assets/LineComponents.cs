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
public struct BatchedVertexData : IBufferElementData
{
    public UnityEngine.Vector3 Vertex;
}

[InternalBufferCapacity(0)]
public struct BatchedTriangleData : IBufferElementData
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

public struct MeshDirty : IComponentData
{
    public Entity entity;
}
public struct LineDirty : IComponentData
{
    public Entity entity;
}

public struct SharedLine : IComponentData
{
    public int vertexLowerBound;
    public int vertexUpperBound;
    public int vertexRange {
        get { return vertexUpperBound - vertexLowerBound + 1; }
    }
    public byte isActiveInMesh;
    public Entity parentMeshEntity;
}
public struct Line : IComponentData
{
    public byte isActive;
}

public struct BatchedLine : IComponentData
{
    public Entity batchEntity;
}

public struct PointsDirty : IComponentData
{
    public Entity lineEntity;
}
