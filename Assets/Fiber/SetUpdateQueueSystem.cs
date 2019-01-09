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
public class SetUpdateQueueSystem : JobComponentSystem
{
    [Inject] public ChunkModificationBarrier barrier;
    public NativeQueue<Entity> markUpdateQueue;
    protected override void OnCreateManager()
    {
        markUpdateQueue = new NativeQueue<Entity>(Allocator.Persistent);
    }

    protected override void OnDestroyManager()
    {
        markUpdateQueue.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (markUpdateQueue.Count > 0)
        {
            var job = new ProcessMarkUpdateQueueJob
            {
                markUpdateQueue     = markUpdateQueue,
                commandBuffer       = barrier.CreateCommandBuffer()
            };
            return job.Schedule(inputDeps);
        }
        return inputDeps;
    }

    public struct ProcessMarkUpdateQueueJob : IJob
    {
        public NativeQueue<Entity> markUpdateQueue;
        public EntityCommandBuffer commandBuffer;
        public void Execute()
        {
            while(markUpdateQueue.TryDequeue(out var entity))
            {
                commandBuffer.AddComponent<MarkUpdate>(entity, new MarkUpdate
                {
                    entity = entity
                });
            }
        }
    }
}

