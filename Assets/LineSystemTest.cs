using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;

using static Unity.Mathematics.math;

public class LineSystemTest : MonoBehaviour
{
    public MeshFilter meshFilter;
    public DynamicBufferSlice<float3> points;
    public int pointCount = 100;
    public float frequency = 4f;
    public float height = 1f;
    public float length = 10f;

    void OnEnable ()
    {
        points = LineMeshSystem.instance.CreateLine(pointCount, this.meshFilter);
    }

    void Update ()
    {
        for (int i = 0; i < points.Length; i++)
        {
            float t = (float) i / (points.Length - 1f);
            float t2 = (t * 0.5f) - 1f;
            points[i] = float3(t * length, 0, 0);
        }
    }
}