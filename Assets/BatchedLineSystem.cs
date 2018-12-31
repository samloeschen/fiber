
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

[UpdateAfter(typeof(GenerateTrianglesSystem))]
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
            typeof(VertexData),
            typeof(TriangleData),
            typeof(PointData),
            typeof(FacingData),
            typeof(WidthData)
        );

        _batchedMeshArchetype = _entityManager.CreateArchetype(
            typeof(BatchedVertexData),
            typeof(BatchedTriangleData),
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
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                var managedVertices = managedMeshData.vertices;
                var managedTriangles = managedMeshData.triangles;
                var mesh = managedMeshData.mesh;
                var nativeVertexBuffer = EntityManager.GetBuffer<BatchedVertexData>(meshEntity).Reinterpret<UnityEngine.Vector3>();
                var nativeTriangleBuffer = EntityManager.GetBuffer<BatchedTriangleData>(meshEntity).Reinterpret<int>();

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

                stopwatch.Stop();
                // UnityEngine.Debug.Log("memcpy ms " + stopwatch.ElapsedTicks / 10000f);
            }
        }
        chunks.Dispose();
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


