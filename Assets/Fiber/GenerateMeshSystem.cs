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

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.PostLateUpdate))]
public class GenerateMeshSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new GenerateMeshJob
        {
            triangleBuffers         = GetBufferFromEntity<TriangleBuffer>(isReadOnly: false),
            vertexBuffers           = GetBufferFromEntity<VertexBuffer> (isReadOnly: false),
            entityBuffers           = GetBufferFromEntity<EntityBuffer>(isReadOnly: true),
        };
        return job.Schedule(this, inputDeps);
    }
    
    [BurstCompile]
    [RequireComponentTag(typeof(VertexBuffer), typeof(TriangleBuffer), typeof(EntityBuffer))]
    public struct GenerateMeshJob : IJobProcessComponentDataWithEntity<IsActive>
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<VertexBuffer> vertexBuffers;
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<TriangleBuffer> triangleBuffers;
        [ReadOnly]
        public BufferFromEntity<EntityBuffer> entityBuffers; 

        public void Execute (Entity meshEntity, int jobIdx, [ReadOnly] ref IsActive isActive)
        {
            var vertexBuffer        = vertexBuffers[meshEntity].Reinterpret<float3>();
            var triangleBuffer      = triangleBuffers[meshEntity].Reinterpret<int>();
            var entityBuffer        = entityBuffers[meshEntity].Reinterpret<Entity>();
            int currentVertex       = 0;

            for (int i = 0; i < entityBuffer.Length; i++)
            {
                // append vertices
                var lineEntity = entityBuffer[i];
                var lineVertexBuffer = vertexBuffers[lineEntity].Reinterpret<float3>();

                vertexBuffer.AddRange(lineVertexBuffer.ToNativeArray());

                // append triangles
                int start   = currentVertex;
                int end     = currentVertex + lineVertexBuffer.Length - 2;
                for (int vertex = start; vertex < end; vertex += 2)
                {
                    triangleBuffer.Add(vertex    );
                    triangleBuffer.Add(vertex + 1);
                    triangleBuffer.Add(vertex + 2);
                    triangleBuffer.Add(vertex + 2);
                    triangleBuffer.Add(vertex + 1);
                    triangleBuffer.Add(vertex + 3);
                }
                currentVertex += lineVertexBuffer.Length;
            }        
        }   
    }
}