using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;  

// public class CreateBatchesSystem : JobComponentSystem
// {

//     protected override JobHandle OnUpdate(JobHandle inputDeps)
//     {
//         var job = new CreateBatchesJob
//         {
//             batchBuffers = GetBufferFromEntity<


            
//         };

//         var list = new NativeList<float3>(0, Allocator.Persistent);
//     }

//     [BurstCompile]
//     public struct CreateBatchesJob : IJobProcessComponentDataWithEntity<BatchedLine>
//     {
//         public BufferFromEntity<EntityBuffer> batchBuffers;
//         public void Execute (Entity lineEntity, int jobIdx, ref BatchedLine batchedLine)
//         {
//             if (batchedLine.isBatched != 0) return;
//             var batchEntity = batchedLine.batchEntity;
//             var batchBuffer = batchBuffers[batchEntity].Reinterpret<Entity>();
//             batchBuffer.Add(lineEntity);
//             batchedLine.isBatched = 1;
//         }
//     }
// }


