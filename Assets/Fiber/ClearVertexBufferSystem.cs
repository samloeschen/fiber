using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Burst;  

namespace Fiber
{
    [UpdateBefore(typeof(GenerateVerticesSystem))]
    public class ClearVertexBufferSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new ClearVertexBufferJob
            {
                vertexBuffers = GetBufferFromEntity<VertexBuffer>(isReadOnly: false),
            };
            return job.Schedule(this, inputDeps);
        }
        // [BurstCompile]
        [RequireComponentTag(typeof(VertexBuffer))]
        public struct ClearVertexBufferJob : IJobProcessComponentDataWithEntity<IsActive>
        {
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<VertexBuffer> vertexBuffers;
            public void Execute(Entity entity, int jobIdx, [ReadOnly] ref IsActive isActive)
            {
                if (!isActive.value) return;
                vertexBuffers[entity].Clear();
            }
        }
    }
}