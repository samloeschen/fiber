using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Burst;  

namespace Fiber
{
    public class AssignToMeshSystem : JobComponentSystem
    {
        [Inject] public LineModificationBarrier barrier;

        public NativeQueue<MeshAssignmentInfo> meshAssignmentQueue;

        protected override void OnCreateManager()
        {
            meshAssignmentQueue = new NativeQueue<MeshAssignmentInfo>(Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            meshAssignmentQueue.Dispose();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var queueJobHandle = new AssignToMeshJob
            {
                meshAssignmentQueue = meshAssignmentQueue.ToConcurrent()
            }.Schedule(this, inputDeps);

            var processJobHandle = new ProcessMeshAssignmentQueueJob
            {
                meshAssignmentQueue = meshAssignmentQueue,
                commandBuffer       = barrier.CreateCommandBuffer(),
                entityBuffers       = GetBufferFromEntity<EntityBuffer>()
            }.Schedule(queueJobHandle);
            return processJobHandle;
        }

        public struct MeshAssignmentInfo
        {
            public Entity lineEntity;
            public Entity meshEntity;
        }

        [BurstCompile]
        public struct AssignToMeshJob : IJobProcessComponentDataWithEntity<MeshAssigner>
        {
            public NativeQueue<MeshAssignmentInfo>.Concurrent meshAssignmentQueue;
            public void Execute (Entity entity, int jobIdx, ref MeshAssigner meshAssigner)
            {
                meshAssignmentQueue.Enqueue(new MeshAssignmentInfo
                {
                    lineEntity = entity,
                    meshEntity = meshAssigner.targetMesh
                });
            }
        };

        public struct ProcessMeshAssignmentQueueJob : IJob
        {
            public NativeQueue<MeshAssignmentInfo> meshAssignmentQueue;
            public EntityCommandBuffer commandBuffer;
            public BufferFromEntity<EntityBuffer> entityBuffers;

            public void Execute()
            {   
                while (meshAssignmentQueue.TryDequeue(out var meshAssignment))
                {
                    var entityBuffer = entityBuffers[meshAssignment.meshEntity].Reinterpret<Entity>();
                    entityBuffer.Add(meshAssignment.lineEntity);
                    commandBuffer.RemoveComponent<MeshAssigner>(meshAssignment.lineEntity);
                }
            }
        }
    }
}