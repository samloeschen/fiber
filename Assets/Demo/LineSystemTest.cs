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

    private BatchedLineSystem _batchedLineSystem;
    private EntityManager _entityManager;
    private static Entity _meshEntity;
    private Entity _lineEntity;
    private DynamicBuffer<float3> _pointsBuf;
    public DynamicBuffer<float> _widthBuf;

    static bool createdMeshEntity = false;

    void OnEnable ()
    {
        _batchedLineSystem = World.Active.GetOrCreateManager<BatchedLineSystem>();

        if (!createdMeshEntity)
        {
            _meshEntity = _batchedLineSystem.CreateBatchedMesh(meshFilter);
            createdMeshEntity = true;
        }
        
        _entityManager = World.Active.GetOrCreateManager<EntityManager>();

        // set up our line entity and associated buffers
        _lineEntity = _entityManager.CreateEntity(BatchedLineSystem.BatchedLineArchetype);
        var initialPoints = new NativeArray<float3>(pointCount, Allocator.Temp);
        var initialFacing = new NativeArray<float3>(pointCount, Allocator.Temp);
        var initialWidths = new NativeArray<float>(pointCount, Allocator.Temp);

        for (int i = 0; i < initialFacing.Length; i++) {
            initialFacing[i] = float3(0, 0, 1);
        }

        _entityManager.GetBuffer<PointData>(_lineEntity).Reinterpret<float3>().AddRange(initialPoints);
        _entityManager.GetBuffer<FacingData>(_lineEntity).Reinterpret<float3>().AddRange(initialFacing);
        _entityManager.GetBuffer<WidthData>(_lineEntity).Reinterpret<float>().AddRange(initialWidths);

        // make line active
        var line = new Line
        {
            isActive = 1,
        };
        _entityManager.SetComponentData(_lineEntity, line);
        var batchedLine = new BatchedLine
        {
            batchEntity = _meshEntity,
        };
        _entityManager.SetComponentData(_lineEntity, batchedLine);
    }
    public JobHandle jobHandle;

    void Update ()
    {
        var points = _entityManager.GetBuffer<PointData>(_lineEntity).Reinterpret<float3>();
        var widths = _entityManager.GetBuffer<WidthData>(_lineEntity).Reinterpret<float>();
        for (int i = 0; i < points.Length; i++) {
            float t = (float) i / (points.Length - 1f);
            float t2 = (t * 0.5f) - 0.25f;
            points[i] = float3(
                t2 * length,
                Mathf.Sin(Time.time + (t * 4)), 
                0
            );
            points[i] += offset;
            widths[i] = 0.2f + (sin(Time.time + (t * 10f)) + 1f) * 0.25f;
        }
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
        public NativeSlice<float3> slice;
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