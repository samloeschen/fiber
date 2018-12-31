using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

using static Unity.Mathematics.math;

public class LineSystemTest : MonoBehaviour
{
    public MeshFilter meshFilter;
    public int pointCount = 100;
    public float frequency = 4f;
    public float height = 1f;
    public float length = 10f;
    public float3 offset;

    private SharedLineSystem _sharedLineSystem;
    private EntityManager _entityManager;
    private Entity _meshEntity;
    private Entity _lineEntity;
    private DynamicBuffer<float3> _pointsBuf;
    public DynamicBuffer<float> _widthBuf;

    void OnEnable ()
    {
        _sharedLineSystem = World.Active.GetOrCreateManager<SharedLineSystem>();
        _meshEntity = _sharedLineSystem.CreateSharedMesh(meshFilter);
        _entityManager = World.Active.GetOrCreateManager<EntityManager>();
        _lineEntity = _sharedLineSystem.CreateLineEntity(pointCount);
        _sharedLineSystem.ActivateLine(_lineEntity, _meshEntity);


    }
    public JobHandle jobHandle;

    void Update ()
    {
        Debug.Log(_lineEntity);
        _pointsBuf = _entityManager.GetBuffer<PointData>(_lineEntity).Reinterpret<float3>();
        _widthBuf = _entityManager.GetBuffer<WidthData>(_lineEntity).Reinterpret<float>(); 

        var activeJob = new TestPointsJob
        {
            length = length,
            points = _pointsBuf,
            widths = _widthBuf,
            offset = offset,
            time = Time.time,
        };
        jobHandle = activeJob.Schedule();
        _sharedLineSystem.AddUpdateDependency(jobHandle);
    }
    void LateUpdate()
    {   
        jobHandle.Complete();
    }
    [BurstCompile]
    public struct TestPointsJob : IJob
    {
        public float length;
        [NativeDisableParallelForRestriction]
        public DynamicBuffer<float3> points;
        [NativeDisableParallelForRestriction]
        public DynamicBuffer<float> widths;
        public float3 offset;
        public float time;
        public void Execute ()
        {
            for (int i = 0; i < points.Length; i++) {
                float t = (float) i / (points.Length - 1f);
                float t2 = (t * 0.5f) - 0.25f;
                points[i] = float3(
                    t2 * length,
                    Mathf.Sin(time + (t * 4)), 
                    0
                );
                points[i] += offset;
                widths[i] = 0.2f + (sin(time + (t * 10f)) + 1f) * 0.25f;
            } 
        }
    }
}