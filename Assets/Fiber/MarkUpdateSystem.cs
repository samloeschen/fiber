using Unity.Jobs;
using Unity.Entities;
using Unity.Burst;

// iterates over any new entities with a MarkUpdate component 
// and sets the MarkUpdate component to point to the entity
public class MarkUpdateSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new MarkUpdateJob().Schedule(this, inputDeps);
    }


    // TODO find some way to get [ChangedFilter] working here
    [BurstCompile]
    public struct MarkUpdateJob : IJobProcessComponentDataWithEntity<MarkUpdate>
    {
        public void Execute(Entity entity, int jobIdx, ref MarkUpdate markUpdate)
        {
            markUpdate.entity = entity;
        }
    }
}