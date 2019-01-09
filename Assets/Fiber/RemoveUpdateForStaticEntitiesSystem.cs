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


// Iterates over any entities that have both a MarkStatic and MarkUpdate component,
// and puts them in a queue to have their Update component removed.
public class RemoveUpdateForStaticEntitiesSystem : JobComponentSystem
{
    public NativeHashMap<Entity, byte> processedEntities;
    public NativeQueue<Entity> staticEntityQueue;
    [Inject] public ChunkModificationBarrier barrier;
    protected override void OnCreateManager()
    {
        staticEntityQueue = new NativeQueue<Entity>(Allocator.Persistent);
    }

    protected override void OnDestroyManager()
    {
        staticEntityQueue.Dispose();
        if (processedEntities.IsCreated)
        {
            processedEntities.Dispose();
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (processedEntities.IsCreated)
        {
            processedEntities.Dispose();
        }
        processedEntities = new NativeHashMap<Entity, byte>(8, Allocator.TempJob);
        var enqueueJob = new EnqueueStaticEntityJob
        {
            processedEntities   = processedEntities.ToConcurrent(),
            staticEntityQueue   = staticEntityQueue.ToConcurrent()
        };
        var processJob = new ProcessStaticQueueJob
        {
            staticEntityQueue = staticEntityQueue,
            commandBuffer = barrier.CreateCommandBuffer()
        };

        var enqueueHandle = enqueueJob.Schedule(this, inputDeps);
        var processHandle = processJob.Schedule(enqueueHandle);
        return processHandle;
    }

    [BurstCompile]
    [RequireComponentTag(typeof(MarkUpdate))]
    public struct EnqueueStaticEntityJob : IJobProcessComponentData<MarkStatic>
    {
        public NativeHashMap<Entity, byte>.Concurrent processedEntities;
        public NativeQueue<Entity>.Concurrent staticEntityQueue;

        public void Execute([ReadOnly] ref MarkStatic markStatic)
        {
            if (processedEntities.TryAdd(markStatic.entity, 1))
            {
                staticEntityQueue.Enqueue(markStatic.entity);
            }
        }
    }
    public struct ProcessStaticQueueJob : IJob
    {
        public NativeQueue<Entity> staticEntityQueue;
        public EntityCommandBuffer commandBuffer;
        public void Execute()
        {
            while(staticEntityQueue.TryDequeue(out var entity))
            {
                commandBuffer.RemoveComponent<MarkUpdate>(entity);
            }
        }
    }
}