using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Fiber;

using static Unity.Mathematics.math;
using static Fiber.BatchedLineHelpers;

public class LineSystemTest : MonoBehaviour
{
    public MeshFilter meshFilter;
    public int pointCount = 100;
    public float frequency = 4f;
    public float height = 1f;
    public float length = 10f;

    private ManagedMeshSystem _managedMeshSystem;
    private EntityManager _entityManager;

    private Entity _lineEntity;
    private static LineMeshData _lineMeshData;


    static bool createdMeshEntity = false;

    void OnEnable ()
    {
        meshFilter = GetComponent<MeshFilter>();
        _managedMeshSystem = World.Active.GetOrCreateManager<ManagedMeshSystem>();
        _entityManager = World.Active.GetOrCreateManager<EntityManager>();

        if (!createdMeshEntity)
        {
            _lineMeshData = CreateLineMesh(vertexAllocation: 128);
            meshFilter.mesh = _lineMeshData.mesh;
            createdMeshEntity = true;
        }

        _lineEntity = CreateLine(pointCount, facingCount: 1, widthCount: 1);
        AssignLineToMesh(_lineEntity, _lineMeshData);

    }
    public JobHandle jobHandle;

    void Update ()
    {
        var points = GetBufferForEntity<PointBuffer>(_lineEntity).Reinterpret<float3>();
        var facing = GetBufferForEntity<FacingBuffer>(_lineEntity).Reinterpret<float3>();
        var widths = GetBufferForEntity<WidthBuffer>(_lineEntity).Reinterpret<float>();

        facing[0] = float3(0, 0, 1);
        
        for (int i = 0; i < points.Length; i++) {
            float t = (float) i / (points.Length - 1f);
            float t2 = (t * 0.5f) - 0.25f;
            points[i] = float3(
                t2 * length,
                Mathf.Sin(Time.time + (t * 4)), 
                0
            );
            widths[0] = 0.2f + (sin(Time.time + (t * 10f)) + 1f) * 0.25f;
        }
    }
}