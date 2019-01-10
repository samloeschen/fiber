using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;  
using static Unity.Mathematics.math;

using System.Diagnostics;
namespace Fiber
{
    [UpdateBefore(typeof(GenerateMeshSystem))]
    public class GenerateVerticesSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var updateBatchedLinedPointsJob = new GenerateVerticesJob
            {
                vertexBuffers       = GetBufferFromEntity<VertexBuffer> (isReadOnly: false),
                pointBuffers        = GetBufferFromEntity<PointBuffer>  (isReadOnly: true),
                facingBuffers       = GetBufferFromEntity<FacingBuffer> (isReadOnly: true),
                widthBuffers        = GetBufferFromEntity<WidthBuffer>  (isReadOnly: true)
            };
            return updateBatchedLinedPointsJob.Schedule(this, inputDeps);
        }

        [BurstCompile]
        [RequireComponentTag(typeof(VertexBuffer), typeof(PointBuffer), typeof(FacingBuffer), typeof(WidthBuffer))]
        public struct GenerateVerticesJob : IJobProcessComponentDataWithEntity<IsActive>
        {
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<VertexBuffer> vertexBuffers;
            [ReadOnly]
            public BufferFromEntity<PointBuffer> pointBuffers;
            [ReadOnly]
            public BufferFromEntity<FacingBuffer> facingBuffers;
            [ReadOnly]
            public BufferFromEntity<WidthBuffer> widthBuffers;
            
            public void Execute(Entity lineEntity, int jobIdx, [ReadOnly] ref IsActive isActive)
            {
                if (!isActive.value) return;
                
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
                float3 facing       = facingBuffer[0];
                float3 dir          = normalize(nextPt - curPt).xyz;

                float width         = widthBuffer[0];
                float3 miter        = normalizesafe(cross(dir, facing)) * widthBuffer[0];
                vertexBuffer.Add(curPt.xyz + miter);
                vertexBuffer.Add(curPt.xyz - miter);
                
                // set remaining points
                int pointRange          = pointBuffer.Length - 1;
                float normalizedIdx     = 0f;
                int facingIdx           = 0;
                int widthIdx            = 0;
                float4 prevPt           = float4(0);
                for (int i = 1; i < pointRange; i++)
                {
                    normalizedIdx   = (float)i / pointRange;
                    facingIdx       = (int)floor((facingBuffer.Length - 1f) * normalizedIdx);
                    widthIdx        = (int)floor((widthBuffer.Length - 1f) * normalizedIdx);
                    facing          = facingBuffer[facingIdx];
                    width           = widthBuffer[widthIdx];
                    curPt           = float4(pointBuffer[i], 0);
                    nextPt          = float4(pointBuffer[i + 1], 0);
                    prevPt          = float4(pointBuffer[i - 1], 0);
                    dir             = (normalize(curPt - prevPt) + normalize(nextPt - curPt)).xyz;
                    miter           = normalizesafe(cross(dir, facing)) * width;
                    vertexBuffer.Add(curPt.xyz + miter);
                    vertexBuffer.Add(curPt.xyz - miter);
                }
                
                // set end point
                prevPt          = float4(pointBuffer[pointRange - 1], 0);
                curPt           = float4(pointBuffer[pointRange], 0);
                facing          = facingBuffer[facingBuffer.Length - 1];
                dir             = (curPt - prevPt).xyz;
                miter           = normalizesafe(cross(dir, facing.xyz)) * widthBuffer[widthBuffer.Length - 1];
                vertexBuffer.Add(curPt.xyz + miter);
                vertexBuffer.Add(curPt.xyz - miter);
            }
        }
    }
}