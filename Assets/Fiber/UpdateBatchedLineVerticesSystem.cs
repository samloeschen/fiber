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

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.PreLateUpdate))]
public class UpdateBatchedLineVerticesSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var updateBatchedLinedPointsJob = new UpdateBatchedLineVerticesJob
        {
            vertexBuffers       = GetBufferFromEntity<VertexBuffer> (isReadOnly: false),
            pointBuffers        = GetBufferFromEntity<PointBuffer>  (isReadOnly:  true),
            facingBuffers       = GetBufferFromEntity<FacingBuffer> (isReadOnly: true),
            widthBuffers        = GetBufferFromEntity<WidthBuffer>  (isReadOnly:  true)
        };
        return updateBatchedLinedPointsJob.Schedule(this, inputDeps);
    }

    [BurstCompile]
    [RequireComponentTag(typeof(VertexBuffer), typeof(PointBuffer), typeof(FacingBuffer), typeof(WidthBuffer))]
    public struct UpdateBatchedLineVerticesJob : IJobProcessComponentDataWithEntity<Line, BatchedLine>
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<VertexBuffer> vertexBuffers;
        [ReadOnly]
        public BufferFromEntity<PointBuffer> pointBuffers;
        [ReadOnly]
        public BufferFromEntity<FacingBuffer> facingBuffers;
        [ReadOnly]
        public BufferFromEntity<WidthBuffer> widthBuffers;

        public void Execute(Entity lineEntity, int jobIdx, ref Line line, ref BatchedLine batchedLine)
        {
            if (line.isActive == 0) return;

            var pointBuffer = pointBuffers[lineEntity].Reinterpret<float3>();
            if (pointBuffer.Length < 2) return;

            var facingBuffer = facingBuffers[lineEntity].Reinterpret<float3>();
            if (facingBuffer.Length < 1) return;

            var widthBuffer = widthBuffers[lineEntity].Reinterpret<float>();
            if (widthBuffer.Length < 1) return;

            var vertexBuffer = vertexBuffers[lineEntity].Reinterpret<float3>();

            // set first point
            float4 curPt        = float4(pointBuffer[0], 0);
            float4 nextPt       = float4(pointBuffer[1], 0);
            float4 facing       = float4(facingBuffer[0], 0);
            float4 dir          = normalize(nextPt - curPt);
            float width         = widthBuffer[0];
            float3 miter        = normalize(cross(dir.xyz, facing.xyz)) * widthBuffer[0];

            vertexBuffer.Add(curPt.xyz + miter);
            vertexBuffer.Add(curPt.xyz - miter);
            
            // set remaining points
            int pointRange          = pointBuffer.Length - 1;
            float normalizedIdx     = 0f;
            int facingIdx           = 0;
            int widthIdx            = 0;
            float4 prevPt           = float4(0);
            float4 ab               = float4(0);
            float4 bc               = float4(0);

            for (int i = 1; i < pointRange; i++)
            {
                normalizedIdx       = (float)i / pointRange;
                facingIdx           = (int)floor((facingBuffer.Length - 1f) * normalizedIdx);
                widthIdx            = (int)floor((widthBuffer.Length - 1f) * normalizedIdx);
                facing              = float4(facingBuffer[facingIdx], 0);
                width               = widthBuffer[widthIdx];
                curPt               = float4(pointBuffer[i], 0);
                nextPt              = float4(pointBuffer[i + 1], 0);
                prevPt              = float4(pointBuffer[i - 1], 0);
                ab                  = normalize(curPt - prevPt);
                bc                  = normalize(nextPt - curPt);
                miter               = normalize(cross((ab + bc).xyz, facing.xyz)) * width;

                vertexBuffer.Add(curPt.xyz + miter);
                vertexBuffer.Add(curPt.xyz - miter);
            }
            
            // set end point
            prevPt          = float4(pointBuffer[pointRange - 1], 0);
            curPt           = float4(pointBuffer[pointRange], 0);
            facing          = float4(facingBuffer[facingBuffer.Length - 1], 0);
            dir             = (curPt - prevPt);
            miter           = normalize(cross(dir.xyz, facing.xyz)) * widthBuffer[widthBuffer.Length - 1];
            int vIdx        = vertexBuffer.Length - 2;

            vertexBuffer.Add(curPt.xyz + miter);
            vertexBuffer.Add(curPt.xyz - miter);
        }
    }
}




