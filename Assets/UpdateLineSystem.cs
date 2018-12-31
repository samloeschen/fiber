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

public class UpdateSharedLineSystem : JobComponentSystem
{
    public JobHandle updateJobDependency;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        updateJobDependency.Complete();

        UpdateLineJob updateLineJob = new UpdateLineJob
        {
            vertexBufferLookup = GetBufferFromEntity<VertexData>(false),
            pointsLookup = GetBufferFromEntity<PointData>(true),
            facingLookup = GetBufferFromEntity<FacingData>(true),
            widthLookup  = GetBufferFromEntity<WidthData>(true),
        };
        return updateLineJob.Schedule(this, inputDeps);
    }

    public class UpdateSharedLineBarrier : BarrierSystem { }
    [Inject] public UpdateSharedLineBarrier barrier;

    [BurstCompile]
    [RequireComponentTag(typeof(PointData), typeof(FacingData), typeof(WidthData))]
    public struct UpdateLineJob : IJobProcessComponentDataWithEntity<Line, SharedLine>
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<VertexData> vertexBufferLookup;
        [ReadOnly]
        public BufferFromEntity<PointData> pointsLookup;
        [ReadOnly]
        public BufferFromEntity<FacingData> facingLookup;
        [ReadOnly]
        public BufferFromEntity<WidthData> widthLookup;

        public void Execute(Entity lineEntity, int index, ref Line line, ref SharedLine sharedLine)
        {
            if (line.isActive == 0) return;
            if (sharedLine.isActiveInMesh == 0) return;

            // grab vertex buffer from supplied vertex buffer lookup using the index of the mesh entity associated with this line
            DynamicBuffer<float3> vertexBuffer = vertexBufferLookup[sharedLine.parentMeshEntity].Reinterpret<float3>();

            // reinterpret other buffers so we can use math on them
            DynamicBuffer<float3> pointBuffer = pointsLookup[lineEntity].Reinterpret<float3>();
            DynamicBuffer<float3> facingBuffer = facingLookup[lineEntity].Reinterpret<float3>();
            DynamicBuffer<float> widthBuffer = widthLookup[lineEntity].Reinterpret<float>();

            // set first point
            float3 curPt = pointBuffer[0];
            float3 nextPt = pointBuffer[1];
            float3 facing = facingBuffer[0];
            float3 dir = normalize(nextPt - curPt);
            float3 miter = normalize(cross(dir, facing)) * widthBuffer[0];
            int vIdx = sharedLine.vertexLowerBound;
            vertexBuffer[vIdx    ] = curPt + miter;
            vertexBuffer[vIdx + 1] = curPt - miter;

            // set end point
            int endIdx = pointBuffer.Length - 1;
            float3 prevPt = pointBuffer[endIdx - 1];
            curPt = pointBuffer[endIdx];
            facing = facingBuffer[endIdx];
            dir = normalize(curPt - prevPt);
            miter = normalize(cross(dir, facing)) * widthBuffer[endIdx];
            vIdx = sharedLine.vertexUpperBound - 1;
            vertexBuffer[vIdx    ] = curPt + miter;
            vertexBuffer[vIdx + 1] = curPt - miter;

            // set remaining points
            for (int i = 1; i < pointBuffer.Length - 1; i++)
            {
                curPt = pointBuffer[i];
                nextPt = pointBuffer[i + 1];
                prevPt = pointBuffer[i - 1];
                float3 ab = normalize(curPt - prevPt);
                float3 bc = normalize(nextPt - curPt);
                miter = normalize(cross(ab + bc, facing)) * widthBuffer[i];
                
                vIdx = sharedLine.vertexLowerBound + (i * 2);
                vertexBuffer[vIdx    ] = curPt + miter;
                vertexBuffer[vIdx + 1] = curPt - miter;
            }
        }
    }
}