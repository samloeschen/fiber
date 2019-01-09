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


// system that adds a MarkUpdate component to queued entities.
// used to mark preexisiting entities to be updated by other systems
public class SetStaticQueueSystem : JobComponentSystem
{
    [Inject] public ChunkModificationBarrier barrier;

    public NativeQueue<Entity> markStaticQueue;
    protected override void OnCreateManager()
    {
        markStaticQueue = new NativeQueue<Entity>(Allocator.Persistent);
    }
    protected override void OnDestroyManager()
    {
        markStaticQueue.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (markStaticQueue.Count > 0)
        {
            var job = new ProcessMarkUpdateQueueJob
            {
                markStaticQueue = markStaticQueue,
                commandBuffer = barrier.CreateCommandBuffer()
            };
            return job.Schedule(inputDeps);
        }
        return inputDeps;
    }

    public struct ProcessMarkUpdateQueueJob : IJob
    {
        public NativeQueue<Entity> markStaticQueue;
        public EntityCommandBuffer commandBuffer;
        public void Execute()
        {
            while(markStaticQueue.TryDequeue(out var entity))
            {
                commandBuffer.AddComponent<MarkStatic>(entity, new MarkStatic
                {
                    entity = entity
                });
            }
        }
    }
}

