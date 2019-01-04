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

[UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.PreLateUpdate))]
public class UpdateBatchedLineVerticesSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var updateBatchedLinedPointsJob = new UpdateBatchedLineVerticesJob
        {
            vertexBuffers       = GetBufferFromEntity<VertexData>(isReadOnly: false),
            pointBuffers        = GetBufferFromEntity<PointData>(isReadOnly: true),
            facingBuffers       = GetBufferFromEntity<FacingData>(isReadOnly: true),
            widthBuffers        = GetBufferFromEntity<WidthData>(isReadOnly: true)
        };
        return updateBatchedLinedPointsJob.Schedule(this, inputDeps);
    }

    [BurstCompile]
    [RequireComponentTag(typeof(VertexData), typeof(TriangleData), typeof(PointData), typeof(FacingData), typeof(WidthData))]
    public struct UpdateBatchedLineVerticesJob : IJobProcessComponentDataWithEntity<Line, BatchedLine>
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<VertexData> vertexBuffers;
        [ReadOnly]
        public BufferFromEntity<PointData> pointBuffers;
        [ReadOnly]
        public BufferFromEntity<FacingData> facingBuffers;
        [ReadOnly]
        public BufferFromEntity<WidthData> widthBuffers;

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
            vertexBuffer.Clear();

            // set first point
            float3 curPt        = pointBuffer[0];
            float3 nextPt       = pointBuffer[1];
            float3 facing       = facingBuffer[0];
            float3 dir          = normalize(nextPt - curPt);
            float width         = widthBuffer[0];
            float3 miter        = normalize(cross(dir, facing)) * widthBuffer[0];

            vertexBuffer.Add(curPt + miter);
            vertexBuffer.Add(curPt - miter);
            
            // set remaining points
            int pointRange          = pointBuffer.Length - 1;
            float normalizedIdx     = 0f;
            int facingIdx           = 0;
            int widthIdx            = 0;
            float3 prevPt           = float3(0);
            float3 ab               = float3(0);
            float3 bc               = float3(0);

            for (int i = 1; i < pointRange; i++)
            {
                normalizedIdx       = (float)i / pointRange;
                facingIdx           = (int)floor((facingBuffer.Length - 1f) * normalizedIdx);
                widthIdx            = (int)floor((widthBuffer.Length - 1f) * normalizedIdx);
                facing              = facingBuffer[facingIdx];
                width               = widthBuffer[widthIdx];
                curPt               = pointBuffer[i];
                nextPt              = pointBuffer[i + 1];
                prevPt              = pointBuffer[i - 1];
                ab                  = normalize(curPt - prevPt);
                bc                  = normalize(nextPt - curPt);
                miter               = normalize(cross(ab + bc, facing)) * width;

                vertexBuffer.Add(curPt + miter);
                vertexBuffer.Add(curPt - miter);
            }
            
            // set end point
            prevPt          = pointBuffer[pointRange - 1];
            curPt           = pointBuffer[pointRange];
            facing          = facingBuffer[facingBuffer.Length - 1];
            dir             = normalize(curPt - prevPt);
            miter           = normalize(cross(dir, facing)) * widthBuffer[widthBuffer.Length - 1];
            int vIdx        = vertexBuffer.Length - 2;

            vertexBuffer.Add(curPt + miter);
            vertexBuffer.Add(curPt - miter);
        }
    }
}




