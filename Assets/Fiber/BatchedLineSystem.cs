
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

[UpdateAfter(typeof(GenerateMeshSystem))]
public class BatchedLineSystem : ComponentSystem
{
    public static EntityArchetype DynamicLineArchetype;
    public static EntityArchetype StaticLineArchetype;

    public static EntityArchetype DynamicMeshArchetype;
    public static EntityArchetype StaticMeshArchetype;

    private EntityArchetype _batchedMeshArchetype;
    private ComponentGroup _meshQuery;
    private HashSet<Entity> _processedEntities;
    private Dictionary<Entity, ManagedMeshData> _managedMeshes;
    private EntityManager _entityManager;
    

    protected override void OnCreateManager()
    {
        _entityManager = World.Active.GetOrCreateManager<EntityManager>();
        _meshQuery = this.GetComponentGroup(new EntityArchetypeQuery
        {
            All = new[]
            { 
                ComponentType.Create<IsActive>(),
                ComponentType.Create<VertexBuffer>(),
                ComponentType.Create<TriangleBuffer>() 
            }
        });

        DynamicLineArchetype = _entityManager.CreateArchetype(
            typeof(IsActive),
            typeof(VertexBuffer),
            typeof(PointBuffer),
            typeof(FacingBuffer),
            typeof(WidthBuffer)
        );

        DynamicMeshArchetype = _entityManager.CreateArchetype(
            typeof(IsActive),
            typeof(VertexBuffer),
            typeof(TriangleBuffer),
            typeof(EntityBuffer)
        );

        _managedMeshes = new Dictionary<Entity, ManagedMeshData>();
        _processedEntities = new HashSet<Entity>();
    }
    protected override void OnUpdate()
    {
        _processedEntities.Clear();
        // var meshDirtyType = GetArchetypeChunkComponentType<Self>(true);
        var isActiveType        = GetArchetypeChunkComponentType<IsActive>(isReadOnly: true);
        var vertexBufferType    = GetArchetypeChunkBufferType<VertexBuffer>(isReadOnly: false);
        var triangleBufferType  = GetArchetypeChunkBufferType<TriangleBuffer>(isReadOnly: false);
        var entityType          = GetArchetypeChunkEntityType();
        var chunks              = _meshQuery.CreateArchetypeChunkArray(Allocator.TempJob);

        for (int chunkIdx = 0; chunkIdx < chunks.Length; chunkIdx++)
        {
            var chunk               = chunks[chunkIdx];
            var isActiveBuffer      = chunk.GetNativeArray(isActiveType);
            var vertexBuffers       = chunk.GetBufferAccessor(vertexBufferType);
            var triangleBuffers     = chunk.GetBufferAccessor(triangleBufferType);
            var entityBuffer        = chunk.GetNativeArray(entityType);

            for (int i = 0; i < chunk.Count; i++)
            {
                if (!isActiveBuffer[i].value) continue;

                var meshEntity = entityBuffer[i];
                if (_processedEntities.Contains(meshEntity)) continue;
                _processedEntities.Add(meshEntity);
                
                // make sure the mesh exists in our dictionary
                if (!_managedMeshes.ContainsKey(meshEntity)) continue;
                var managedMeshData = _managedMeshes[meshEntity];

                // update mesh arrays
                var managedVertices         = managedMeshData.vertices;
                var managedTriangles        = managedMeshData.triangles;
                var mesh                    = managedMeshData.mesh;
                var nativeVertexBuffer      = vertexBuffers[i].Reinterpret<Vector3>();
                var nativeTriangleBuffer    = triangleBuffers[i].Reinterpret<int>();

                if (nativeVertexBuffer.Length == 0 || nativeTriangleBuffer.Length == 0) continue;

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
        var lineEntity      = _entityManager.CreateEntity(DynamicLineArchetype);
        var pointBuffer     = _entityManager.GetBuffer<PointBuffer>(lineEntity).Reinterpret<float3>();
        var facingBuffer    = _entityManager.GetBuffer<FacingBuffer>(lineEntity).Reinterpret<float3>();
        var widthBuffer     = _entityManager.GetBuffer<WidthBuffer>(lineEntity).Reinterpret<float>();

        pointBuffer.AddRange(initialPoints);
        facingBuffer.Add(facing);
        widthBuffer.Add(initialWidth);

        _entityManager.SetComponentData(lineEntity, new IsActive
        {
            value = true
        });

        var entityBuffer = _entityManager.GetBuffer<EntityBuffer>(batchMeshEntity).Reinterpret<Entity>();
        entityBuffer.Add(lineEntity);

        return lineEntity;
    }
    public Entity CreateBatchedLine(Entity batchMeshEntity, int initialPointCount, float3 facing, float initialWidth = defaultStartWidth, bool startActive = true)
    {
        var initialPoints = new NativeArray<float3>(initialPointCount, Allocator.Temp);
        var lineEntity = CreateBatchedLine(batchMeshEntity, initialPoints, facing, initialWidth, startActive);
        initialPoints.Dispose();
        return lineEntity;
    }

    // TODO this should take a material instead of a mesh filter
    public Entity CreateBatchedMesh (MeshFilter meshFilter)
    {
        // create the new mesh entity
        var meshEntity = _entityManager.CreateEntity(DynamicMeshArchetype);
        _entityManager.SetComponentData(meshEntity, new IsActive
        {
            value = true
        });
        var m = new Mesh();
        m.MarkDynamic();
        meshFilter.mesh = m;

        // create a container for the managed data we want to associate with this entity
        var managedMeshData = new ManagedMeshData
        {
            mesh        = m,
            meshFilter  = meshFilter,
            vertices    = new List<Vector3>(),
            triangles   = new List<int>()
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


