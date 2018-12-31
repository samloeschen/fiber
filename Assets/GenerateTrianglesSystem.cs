using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[UpdateAfter(typeof(CombineVerticesSystem))]
public class GenerateTrianglesSystem : JobComponentSystem
{   
    public struct BatchMeshComponentGroup
    {
        public BufferArray<BatchedVertexData> batchedVertexBuffers;
        public BufferArray<BatchedTriangleData> batchedTriangleBuffers;
    }
    [Inject] public BatchMeshComponentGroup batchMeshComponentGroup;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var generateTrianglesJob = new GenerateTrianglesJob
        {
            batchedVertexBuffers = batchMeshComponentGroup.batchedVertexBuffers,
            batchedTriangleBuffers = batchMeshComponentGroup.batchedTriangleBuffers
        };

        int length = batchMeshComponentGroup.batchedVertexBuffers.Length;
        int idxCount = 1;
        return generateTrianglesJob.Schedule(length, idxCount, inputDeps);
    }

    // [BurstCompile]
    public struct GenerateTrianglesJob : IJobParallelFor
    {
        [ReadOnly]
        public BufferArray<BatchedVertexData> batchedVertexBuffers;
        [NativeDisableParallelForRestriction]
        public BufferArray<BatchedTriangleData> batchedTriangleBuffers;

        public void Execute (int bufIdx)
        {
            var vertexBuffer = batchedVertexBuffers[bufIdx];
            var triangleBuffer = batchedTriangleBuffers[bufIdx].Reinterpret<int>();

            triangleBuffer.Clear();
            for (int vertex = 0; vertex < vertexBuffer.Length - 2; vertex += 2)
            {
                triangleBuffer.Add(vertex    );
                triangleBuffer.Add(vertex + 1);
                triangleBuffer.Add(vertex + 2);
                triangleBuffer.Add(vertex + 2);
                triangleBuffer.Add(vertex + 1);
                triangleBuffer.Add(vertex + 3);
            }
        }
    }
}