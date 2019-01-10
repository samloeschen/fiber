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


// [Update After(typeof(BatchedLineSystem))]
[UpdateBefore(typeof(GenerateVerticesSystem))]
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
    public struct ClearTriangleBufferJob : IJobProcessComponentDataWithEntity<IsActive>
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<TriangleBuffer> triangleBuffers;
        public void Execute(Entity entity, int jobIdx, ref IsActive isActive)
        {
            if (!isActive.value) return;
            triangleBuffers[entity].Clear();
        }
    }
}
