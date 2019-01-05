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
        public BufferArray<BatchedVertexBuffer> batchedVertexBuffers;
        public BufferArray<BatchedTriangleBuffer> batchedTriangleBuffers;
        public BufferArray<VertexCountBuffer> vertexCountBuffers;
    }
    [Inject] public BatchMeshComponentGroup batchMeshComponentGroup;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var generateTrianglesJob = new GenerateTrianglesJob
        {
            batchedVertexBuffers = batchMeshComponentGroup.batchedVertexBuffers,
            batchedTriangleBuffers = batchMeshComponentGroup.batchedTriangleBuffers,
            vertexCountBuffers = batchMeshComponentGroup.vertexCountBuffers
        };

        int length = batchMeshComponentGroup.batchedVertexBuffers.Length;
        int idxCount = 1;
        return generateTrianglesJob.Schedule(length, idxCount, inputDeps);
    }

    [BurstCompile]
    public struct GenerateTrianglesJob : IJobParallelFor
    {
        [ReadOnly]
        public BufferArray<BatchedVertexBuffer> batchedVertexBuffers;
        [ReadOnly]
        public BufferArray<VertexCountBuffer> vertexCountBuffers;
        [NativeDisableParallelForRestriction]
        public BufferArray<BatchedTriangleBuffer> batchedTriangleBuffers;


        public void Execute (int bufIdx)
        {
            var vertexBuffer = batchedVertexBuffers[bufIdx];
            var vertexCountBuffer = vertexCountBuffers[bufIdx].Reinterpret<int>();
            var triangleBuffer = batchedTriangleBuffers[bufIdx].Reinterpret<int>();

            if (vertexBuffer.Length < 4) return;

            int skipIdx = 0;
            int nextSkip = vertexCountBuffer[skipIdx] - 2;
            for (int vertex = 0; vertex < vertexBuffer.Length - 2; vertex += 2)
            {
                if(vertex == nextSkip)
                {
                    skipIdx++;
                    nextSkip += vertexCountBuffer[skipIdx];
                    continue;
                }
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