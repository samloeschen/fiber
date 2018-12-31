using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;  

using static Unity.Mathematics.math;

[UpdateAfter(typeof(UpdateBatchedLineVerticesSystem))]
public class CombineVerticesSystem : JobComponentSystem
{
    public struct BatchedLineComponentGroup
    {
        public BufferArray<VertexData> lineVertexBuffers;
        public ComponentDataArray<BatchedLine> batchedLineData;
    }

    [Inject] public BatchedLineComponentGroup batchedLineComponentGroup;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var combineVerticesJob = new CombineVerticesJob
        {
            batchedVertexBuffers = GetBufferFromEntity<BatchedVertexData>(isReadOnly: false),
            vertexCountBuffers = GetBufferFromEntity<VertexCountData>(isReadOnly: false),
            componentGroup = batchedLineComponentGroup
        };
        int length = batchedLineComponentGroup.batchedLineData.Length;
        int idxCount = Mathf.NextPowerOfTwo(length / (SystemInfo.processorCount + 1));
        return combineVerticesJob.Schedule(inputDeps);
    }

    [BurstCompile]
    // TODO make this not sequential
    public struct CombineVerticesJob : IJob
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<BatchedVertexData> batchedVertexBuffers;
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<VertexCountData> vertexCountBuffers;
        public BatchedLineComponentGroup componentGroup;
        public void Execute ()
        {
            for (int i = 0; i < componentGroup.batchedLineData.Length; i++)
            {
                var batchEntity = componentGroup.batchedLineData[i].batchEntity;
                if (!batchedVertexBuffers.Exists(batchEntity)) return;

                var batchedVertexBuffer = batchedVertexBuffers[batchEntity];
                var vertexCountBuffer = vertexCountBuffers[batchEntity].Reinterpret<int>();

                var vertexArr = componentGroup.lineVertexBuffers[i].Reinterpret<BatchedVertexData>().ToNativeArray();
                batchedVertexBuffer.AddRange(vertexArr);
                vertexCountBuffer.Add(vertexArr.Length);
            }
        }
    }
}
