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

[UpdateAfter(typeof(CombineVerticesSystem))]
public class FlushLineVerticesSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new FlushLineVerticesJob
        {
            vertexBuffers = GetBufferFromEntity<VertexBuffer>(),
        }.Schedule(this, inputDeps);
    }

    [BurstCompile]
    [RequireComponentTag(typeof(VertexBuffer))]
    public struct FlushLineVerticesJob : IJobProcessComponentDataWithEntity<Line>
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<VertexBuffer> vertexBuffers;

        public void Execute(Entity lineEntity, int jobIdx, [ReadOnly] ref Line line)
        {
            var vertexBuffer = vertexBuffers[lineEntity];
            vertexBuffer.Clear();
        }
    }
}
