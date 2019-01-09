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

[UpdateAfter(typeof(BatchedLineSystem))]
public class ClearVertexBufferSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new ClearVertexBufferJob
        {
            vertexBuffers = GetBufferFromEntity<VertexBuffer>(isReadOnly: false),
        };
        return job.Schedule(this, inputDeps);
    }
    [BurstCompile]
    [RequireComponentTag(typeof(VertexBuffer))]
    public struct ClearVertexBufferJob : IJobProcessComponentData<MarkUpdate>
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<VertexBuffer> vertexBuffers;
        public void Execute(ref MarkUpdate markUpdate)
        {
            vertexBuffers[markUpdate.entity].Clear();
        }
    }
}
