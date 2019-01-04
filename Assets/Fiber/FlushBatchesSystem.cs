using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;  
using UnityEngine.Experimental;
using static Unity.Mathematics.math;

[UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.EarlyUpdate))]
public class FlushBatchedVerticesSystem : JobComponentSystem
{
    public struct BatchComponentGroup
    {
        public BufferArray<BatchedVertexBuffer> batchedVertexBuffers;
        public BufferArray<VertexCountBuffer> vertexCountBuffers;
    }
    [Inject] public BatchComponentGroup batchedLineComponentGroup;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var flushBatchedVerticesJob = new FlushBatchedVerticesJob
        {
            batchedVertexBuffers = batchedLineComponentGroup.batchedVertexBuffers,
            vertexCountBuffers = batchedLineComponentGroup.vertexCountBuffers
        };
        int length = batchedLineComponentGroup.batchedVertexBuffers.Length;
        int idxCount = Mathf.NextPowerOfTwo(length / (SystemInfo.processorCount + 1));
        return flushBatchedVerticesJob.Schedule(length, idxCount, inputDeps);
    }

    [BurstCompile]
    public struct FlushBatchedVerticesJob : IJobParallelFor
    {
        public BufferArray<BatchedVertexBuffer> batchedVertexBuffers;
        public BufferArray<VertexCountBuffer> vertexCountBuffers;
        public BufferArray<BatchQueue> entityBatchBuffers;
        public void Execute (int i)
        {
            batchedVertexBuffers[i].Clear();
            vertexCountBuffers[i].Clear();
        }
    }
}