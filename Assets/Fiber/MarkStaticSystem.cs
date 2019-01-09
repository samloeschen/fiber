using Unity.Jobs;
using Unity.Entities;
using Unity.Burst;



// iterates over any new entities with a MarkStatic component 
// and sets the MarkStatic component to point to the entity
public class MarkStaticSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new MarkStaticJob().Schedule(this, inputDeps);
    }

    // TODO find some way to get [ChangedFilter] working here
    [BurstCompile]
    public struct MarkStaticJob : IJobProcessComponentDataWithEntity<MarkStatic>
    {
        public void Execute(Entity entity, int jobIdx, ref MarkStatic markStatic)
        {
            markStatic.entity = entity;
        }
    }
}