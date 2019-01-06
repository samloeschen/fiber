
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using System.Diagnostics;

using static Unity.Mathematics.math;

public class BatchedLineSystem : ComponentSystem
{
    public static EntityArchetype BatchedLineArchetype;
    private EntityArchetype _batchedMeshArchetype;
    private ComponentGroup _meshDirtyQuery;
    private HashSet<Entity> _processedEntities;
    private Dictionary<Entity, ManagedMeshData> _managedMeshes;
    private EntityManager _entityManager;
    protected override void OnCreateManager()
    {
        _entityManager = World.Active.GetOrCreateManager<EntityManager>();
        _meshDirtyQuery = this.GetComponentGroup(new EntityArchetypeQuery
        {
            All = new[] { ComponentType.Create<MeshDirty>(), }
        });

        BatchedLineArchetype = _entityManager.CreateArchetype(
            typeof(Line),
            typeof(BatchedLine),
            typeof(VertexBuffer),
            typeof(PointBuffer),
            typeof(FacingBuffer),
            typeof(WidthBuffer)
        );

        _batchedMeshArchetype = _entityManager.CreateArchetype(
            typeof(BatchedVertexBuffer),
            typeof(TriangleBuffer),
            typeof(VertexCountBuffer),
            typeof(EntityBuffer),
            typeof(MeshDirty)
        );

        _managedMeshes = new Dictionary<Entity, ManagedMeshData>();
        _processedEntities = new HashSet<Entity>();
    }
    protected override void OnUpdate()
    {
        _processedEntities.Clear();
        var meshDirtyType = GetArchetypeChunkComponentType<MeshDirty>(true);
        var chunks = _meshDirtyQuery.CreateArchetypeChunkArray(Allocator.TempJob);
        for (int chunkIdx = 0; chunkIdx < chunks.Length; chunkIdx++)
        {
            var chunk = chunks[chunkIdx];
            var meshDirtyBuffer = chunk.GetNativeArray(meshDirtyType);
            for (int i = 0; i < chunk.Count; i++)
            {
                var meshEntity = meshDirtyBuffer[i].entity;
                if (_processedEntities.Contains(meshEntity)) return;
                _processedEntities.Add(meshEntity);
                
                // make sure the mesh exists in our dictionary, if not, create it
                ManagedMeshData managedMeshData = new ManagedMeshData();
                if (_managedMeshes.ContainsKey(meshEntity))
                {
                    managedMeshData = _managedMeshes[meshEntity];
                }
                else
                {
                    continue;
                }

                // update mesh arrays
                var managedVertices = managedMeshData.vertices;
                var managedTriangles = managedMeshData.triangles;
                var mesh = managedMeshData.mesh;
                var nativeVertexBuffer = EntityManager.GetBuffer<BatchedVertexBuffer>(meshEntity).Reinterpret<UnityEngine.Vector3>();
                var nativeTriangleBuffer = EntityManager.GetBuffer<TriangleBuffer>(meshEntity).Reinterpret<int>();

                if (nativeVertexBuffer.Length == 0 || nativeTriangleBuffer.Length == 0)
                {
                    continue;
                }
                managedVertices.AddRange(nativeVertexBuffer);
                managedTriangles.AddRange(nativeTriangleBuffer);
                
                mesh.Clear();
                mesh.SetVertices(managedVertices);
                mesh.SetTriangles(managedTriangles, 0);

                managedVertices.Clear();
                managedTriangles.Clear();
            }
        }
        chunks.Dispose();
    }

    private const int defaultPointCount = 2;
    private const float defaultStartWidth = 0.25f;
    public Entity CreateBatchedLine(Entity batchMeshEntity, NativeArray<float3> initialPoints, float3 facing, float initialWidth = defaultStartWidth, bool startActive = true)
    {
        var lineEntity      = _entityManager.CreateEntity(BatchedLineArchetype);
        var pointBuffer     = _entityManager.GetBuffer<PointBuffer>(lineEntity).Reinterpret<float3>();
        var facingBuffer    = _entityManager.GetBuffer<FacingBuffer>(lineEntity).Reinterpret<float3>();
        var widthBuffer     = _entityManager.GetBuffer<WidthBuffer>(lineEntity).Reinterpret<float>();

        pointBuffer.AddRange(initialPoints);
        facingBuffer.Add(facing);
        widthBuffer.Add(initialWidth);

        var line = new Line
        {
            isActive = (byte)(startActive ? 1 : 0),
        };
        var batchedLine = new BatchedLine
        {
            batchEntity = batchMeshEntity
        };
        _entityManager.SetComponentData(lineEntity, line);
        _entityManager.SetComponentData(lineEntity, batchedLine);
        return lineEntity;
    }
    public Entity CreateBatchedLine(Entity batchMeshEntity, int initialPointCount, float3 facing, float initialWidth = defaultStartWidth, bool startActive = true)
    {
        var initialPoints = new NativeArray<float3>(initialPointCount, Allocator.Temp);
        var lineEntity = CreateBatchedLine(batchMeshEntity, initialPoints, facing, initialWidth, startActive);
        initialPoints.Dispose();
        return lineEntity;
    }

    public Entity CreateBatchedMesh (MeshFilter meshFilter)
    {
        // create the new mesh entity
        var meshEntity = _entityManager.CreateEntity(_batchedMeshArchetype);
        var m = new Mesh();
        m.MarkDynamic();
        meshFilter.mesh = m;

        // temporary until i get dirty meshes/lines working
        var meshDirty = new MeshDirty
        {
            entity = meshEntity,
        };
        _entityManager.SetComponentData(meshEntity, meshDirty);

        // create a container for the managed data we want to associate with this entity
        var managedMeshData = new ManagedMeshData
        {
            mesh = m,
            meshFilter = meshFilter,
            vertices = new List<Vector3>(),
            triangles = new List<int>()
        };
        _managedMeshes.Add(meshEntity, managedMeshData);
        return meshEntity;
    }
}

public struct ManagedMeshData
{
    public Mesh mesh;
    public MeshFilter meshFilter;
    public List<Vector3> vertices;
    public List<int> triangles;
}


