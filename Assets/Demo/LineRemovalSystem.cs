using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;  

[UpdateBefore(typeof(MarkUpdateSystem))]
public class RemoveFromMeshSystem : JobComponentSystem
{
    public NativeQueue<RemoveFromMeshInfo> lineRemovalQueue;
    protected override void OnCreateManager()
    {
        lineRemovalQueue = new NativeQueue<RemoveFromMeshInfo>(Allocator.Persistent);
    }

    protected override void OnDestroyManager()
    {
        lineRemovalQueue.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (lineRemovalQueue.Count > 0)
        {
            var job = new RemoveFromMeshJob
            {
                entityBuffers = GetBufferFromEntity<EntityBuffer>(),
                lineRemovalQueue = lineRemovalQueue
            };
            return job.Schedule(inputDeps);
        }
        return inputDeps;
    }

    public struct RemoveFromMeshInfo
    {
        public Entity lineEntity;
        public Entity meshEntity;
    }

    public struct RemoveFromMeshJob : IJob
    {
        public BufferFromEntity<EntityBuffer> entityBuffers;
        public NativeQueue<RemoveFromMeshInfo> lineRemovalQueue;

        public void Execute ()
        {
            while(lineRemovalQueue.TryDequeue(out var lineRemovalInfo))
            {
                // slow, O(n) removal for now
                var entityBuffer = entityBuffers[lineRemovalInfo.meshEntity].Reinterpret<Entity>();
                for (int i = 0; i < entityBuffer.Length; i++)
                {
                    if (entityBuffer[i] == lineRemovalInfo.lineEntity)
                    {
                        entityBuffer.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    } 
}