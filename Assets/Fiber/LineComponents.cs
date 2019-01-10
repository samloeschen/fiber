using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;

namespace Fiber
{
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

    public struct ActiveFlag : IComponentData
    {
        public byte isActive;
    }

    public struct MeshAssigner : IComponentData
    {
        public Entity targetMesh;
    }

    public struct IsActive : IComponentData
    {
        public Flag value;
    }

    public struct Flag
    {
            private readonly byte _value;
            public Flag(bool value) { _value = (byte)(value ? 1 : 0); }
            public static implicit operator Flag(bool value) { return new Flag(value); }
            public static implicit operator bool(Flag value) { return value._value != 0; }
    }
}