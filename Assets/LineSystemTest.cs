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
    public LineData lineData;
    public int pointCount = 100;
    public float frequency = 4f;
    public float height = 1f;
    public float length = 10f;

    private LineMeshSystem lineMeshSystem;

    void OnEnable ()
    {

        lineMeshSystem = LineMeshSystem.GetSystem();
        lineData = lineMeshSystem.CreateLine(pointCount, this.meshFilter);
    }

    JobHandle activeJobHandle;

    void Update ()
    {
        var activeJob = new TestPointsJob {
            length = length,
            points = lineData.pointBuffer,
            widths = lineData.widthBuffer,
            time = Time.time,
        };
        activeJobHandle = activeJob.Schedule(dependsOn: JobHandle.CombineDependencies(lineMeshSystem.meshManagementJobs, lineMeshSystem.meshUpdateJob));
        lineMeshSystem.AddUpdateDependency(activeJobHandle);
    }

    [BurstCompile]
    public struct TestPointsJob : IJob
    {
        public float length;
        public DynamicBuffer<float3> points;
        public DynamicBuffer<float> widths;
        public float time;
        public void Execute ()
        {
            for (int i = 0; i < points.Length; i++)
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
}