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

public class RemoveSharedLineSystem : JobComponentSystem
{
    public class RemoveSharedLineBarrier : BarrierSystem { }

    public struct SharedLineMeshComponentGroup
    {
        public BufferArray<VertexData> vertexBufferLookup;
        public BufferArray<TriangleData> triangleBufferLookup; 
        public EntityArray meshEntities;
    }
    [Inject] SharedLineMeshComponentGroup sharedLineMeshComponentGroup;
    [Inject] RemoveSharedLineBarrier barrier;
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var removeLineJob = new RemoveLineJob
        {
            commandBuffer = barrier.CreateCommandBuffer().ToConcurrent(),
            sharedLineMeshComponentGroup = sharedLineMeshComponentGroup
        };
        return removeLineJob.Schedule(this, inputDeps);
    }

    [BurstCompile]
    public struct RemoveLineJob : IJobProcessComponentDataWithEntity<Line, SharedLine>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;
        public SharedLineMeshComponentGroup sharedLineMeshComponentGroup;
        public void Execute (Entity lineEntity, int jobIdx, ref Line line, ref SharedLine sharedLine)
        {
            if (line.isActive > 0) return;
            if (sharedLine.isActiveInMesh == 0) return;
            
            var meshEntities = sharedLineMeshComponentGroup.meshEntities;
            var sharedVertexBuf = new DynamicBuffer<float3>();
            var sharedTriangleBuf = new DynamicBuffer<int>();
            byte foundParentMesh = 0;
            for (int i = 0; i < meshEntities.Length; i++)
            {
                if (meshEntities[i] == sharedLine.parentMeshEntity)
                {
                    sharedVertexBuf = sharedLineMeshComponentGroup.vertexBufferLookup[i].Reinterpret<float3>();
                    sharedTriangleBuf = sharedLineMeshComponentGroup.triangleBufferLookup[i].Reinterpret<int>();
                    foundParentMesh = 1;
                }
            }
            if (foundParentMesh == 0) {
                return;
            }
            sharedVertexBuf.RemoveRange(sharedLine.vertexLowerBound, sharedLine.vertexRange + 1);

            int pointCount = line.pointCount;
            int triangleIdx = sharedLine.vertexLowerBound * 3;
            int triangleRange = (pointCount - 1) * 6;
            int triangleShift = pointCount * 2;

            sharedTriangleBuf.RemoveRange(triangleIdx, triangleRange);
            for (int i = triangleIdx; i < sharedTriangleBuf.Length; i++) {
                sharedTriangleBuf[i] -= triangleShift;
            }
            sharedLine.isActiveInMesh = 0;
        }
    }
}