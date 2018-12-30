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

    private SharedLineSystem _sharedLineSystem;
    private EntityManager _entityManager;
    void OnEnable ()
    {
        _sharedLineSystem = SharedLineSystem.GetSystem();
        _entityManager = World.Active.GetOrCreateManager<EntityManager>();
        lineData = _sharedLineSystem.CreateLine(pointCount);
        
    }
    public LineData lineData;
    public JobHandle testHandle;
    void Update ()
    {
        if (Time.time < 2f) {
            var activeJob = new TestPointsJob
            {
                length = length,
                points = lineData.pointBuffer,
                widths = lineData.widthBuffer,
                time = Time.time,
            };
            int count = Mathf.NextPowerOfTwo(lineData.pointBuffer.Length / (SystemInfo.processorCount + 1));
            testHandle = activeJob.Schedule(lineData.pointBuffer.Length, count);
            _sharedLineSystem.AddUpdateDependency(testHandle);
        } else {
            var line = _entityManager.GetComponentData<Line>(lineData.entity);
            line.isActive = 0;
            _entityManager.SetComponentData(lineData.entity, line);
            this.enabled = false;
        }
    }
    void LateUpdate()
    {
        testHandle.Complete();
    }
    [BurstCompile]
    public struct TestPointsJob : IJobParallelFor
    {
        public float length;
        [NativeDisableParallelForRestriction]
        public DynamicBuffer<float3> points;
        [NativeDisableParallelForRestriction]
        public DynamicBuffer<float> widths;
        public float time;
        public void Execute (int i)
        {
            float t = (float) i / (points.Length - 1f);
            float t2 = (t * 0.5f) - 0.25f;
            points[i] = float3(
                t2 * length,
                Mathf.Sin(time + (t * 4)), 
                0
            );
            widths[i] = 0.2f + (sin(time + (t * 10f)) + 1f) * 0.25f; 
        }
    }
}