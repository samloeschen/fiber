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
public class ClearTriangleBufferSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new ClearTriangleBufferJob
        {
            triangleBuffers = GetBufferFromEntity<TriangleBuffer>(),
        };
        return job.Schedule(this, inputDeps);
    }
    [BurstCompile]
    [RequireComponentTag(typeof(TriangleBuffer))]
    public struct ClearTriangleBufferJob : IJobProcessComponentData<MarkUpdate>
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<TriangleBuffer> triangleBuffers;
        public void Execute(ref MarkUpdate markUpdate)
        {
            triangleBuffers[markUpdate.entity].Clear();
        }
    }
}
