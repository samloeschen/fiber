
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
public class SharedLineSystem : ComponentSystem
{
    private EntityManager _entityManager;
    private static SharedLineSystem _instance;
    private EntityArchetype _lineArchetype;
    private EntityArchetype _sharedMeshArchetype;
    private UpdateSharedLineSystem _updateSharedLineSystem;
    private Dictionary<Entity, ManagedMeshData> _managedMeshDataDict;
    private HashSet<Entity> _processedMeshEntities;
    public ComponentGroup meshDirtyQuery;

    protected override void OnCreateManager()
    {
        _instance = this;
        _entityManager = World.Active.GetOrCreateManager<EntityManager>();
        _lineArchetype = _entityManager.CreateArchetype(
            typeof(Line),
            typeof(SharedLine),
            typeof(PointData),
            typeof(FacingData),
            typeof(WidthData)
        );
        _sharedMeshArchetype = _entityManager.CreateArchetype(
            typeof(VertexData),
            typeof(TriangleData),
            typeof(MeshDirty)
        );

        meshDirtyQuery = this.GetComponentGroup(new EntityArchetypeQuery
        {
            All = new[] { ComponentType.Create<MeshDirty>(), }
        });
        _updateSharedLineSystem = World.Active.GetOrCreateManager<UpdateSharedLineSystem>();

        _managedMeshDataDict = new Dictionary<Entity, ManagedMeshData>();
        _processedMeshEntities = new HashSet<Entity>();
    }

    // Every OnUpdate all dirty meshes are processed and their dynamic buffers are dumped into
    // their associated managed meshes
    protected override void OnUpdate()
    {
        _processedMeshEntities.Clear();
        var meshDirtyType = GetArchetypeChunkComponentType<MeshDirty>(true);
        var chunks = meshDirtyQuery.CreateArchetypeChunkArray(Allocator.TempJob);
        for (int chunkIdx = 0; chunkIdx < chunks.Length; chunkIdx++)
        {
            var chunk = chunks[chunkIdx];
            var meshDirtyBuffer = chunk.GetNativeArray(meshDirtyType);
            for (int i = 0; i < chunk.Count; i++)
            {
                var meshEntity = meshDirtyBuffer[i].entity;
                if (_processedMeshEntities.Contains(meshEntity)) return;
                _processedMeshEntities.Add(meshEntity);
                
                // make sure the mesh exists in our dictionary, if not, create it
                ManagedMeshData managedMeshData = new ManagedMeshData();
                if (_managedMeshDataDict.ContainsKey(meshEntity))
                {
                    managedMeshData = _managedMeshDataDict[meshEntity];
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
                var nativeVertexBuffer = EntityManager.GetBuffer<VertexData>(meshEntity).Reinterpret<UnityEngine.Vector3>();
                var nativeTriangleBuffer = EntityManager.GetBuffer<TriangleData>(meshEntity).Reinterpret<int>();
                
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
    public Entity CreateSharedMesh (MeshFilter meshFilter, Mesh mesh = null)
    {
        // create the new mesh entity
        var meshEntity = _entityManager.CreateEntity(_sharedMeshArchetype);
        var m = mesh ?? new Mesh();
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
        _managedMeshDataDict.Add(meshEntity, managedMeshData);
        return meshEntity;
    }

    public void AddUpdateDependency (JobHandle jobHandle)
    {
        if (_updateSharedLineSystem.updateJobDependency.IsCompleted)
        {
            _updateSharedLineSystem.updateJobDependency = jobHandle;
        } 
        else
        {
            _updateSharedLineSystem.updateJobDependency = JobHandle.CombineDependencies(_updateSharedLineSystem.updateJobDependency, jobHandle);
        }
    }
    
    public Entity CreateLineEntity(int pointCount, bool startActive = true, bool allocateBuffers = true)
    {
        // set up our new line entity and components
        var lineEntity = _entityManager.CreateEntity(_lineArchetype);
        var line = new Line
        {
            isActive = (byte)(startActive ? 1 : 0)
        };
        
        // schedule jobs to populate buffers with initialization data
        if (allocateBuffers)
        {
            var jobHandles = new NativeArray<JobHandle>(5, Allocator.Temp);
            jobHandles[0] = new AppendJob<float3>
            {
                count = pointCount,
                buffer = _entityManager.GetBuffer<PointData>(lineEntity).Reinterpret<float3>(),
                defaultValue = float3(0)
            }.Schedule();
            jobHandles[1] = new AppendJob<float3>
            {
                count = pointCount,
                buffer = _entityManager.GetBuffer<FacingData>(lineEntity).Reinterpret<float3>(),
                defaultValue = float3(0, 0, 1)
            }.Schedule();
            jobHandles[2] = new AppendJob<float>
            {
                count = pointCount,
                buffer = _entityManager.GetBuffer<WidthData>(lineEntity).Reinterpret<float>(),
                defaultValue = 0.25f
            }.Schedule();
            JobHandle.CompleteAll(jobHandles);
        }
        return lineEntity;
    }

    public void ActivateLine(Entity lineEntity, Entity sharedMeshEntity)
    {
        // make sure we have the correct buffers on this mesh entity, and then grab them
        if (!EntityManager.HasComponent<VertexData>(sharedMeshEntity) || !EntityManager.HasComponent<TriangleData>(sharedMeshEntity)) return;
        if (!EntityManager.HasComponent<SharedLine>(lineEntity)) return;

        var sharedVertexBuffer = EntityManager.GetBuffer<VertexData>(sharedMeshEntity).Reinterpret<float3>();
        var sharedTriangleBuffer = EntityManager.GetBuffer<TriangleData>(sharedMeshEntity).Reinterpret<int>();
        var pointBuffer = EntityManager.GetBuffer<PointData>(lineEntity).Reinterpret<float3>();

        // set up our new line entity and components
        var sharedLine = new SharedLine
        {
            vertexLowerBound = sharedVertexBuffer.Length,
            vertexUpperBound = sharedVertexBuffer.Length + ((pointBuffer.Length * 2) - 1),
            parentMeshEntity = sharedMeshEntity,
            isActiveInMesh = 1,
        };
        _entityManager.SetComponentData(lineEntity, sharedLine);

        var line = EntityManager.GetComponentData<Line>(lineEntity);
        line.isActive = 1;
        EntityManager.SetComponentData(lineEntity, line);

        // schedule jobs to populate buffers with initialization data
        var jobHandles = new NativeArray<JobHandle>(2, Allocator.Temp);
        jobHandles[0] = new AppendJob<float3>
        {
            count = pointBuffer.Length * 2,
            buffer = sharedVertexBuffer,
            defaultValue = float3(0),
        }.Schedule();
        jobHandles[1] = new AppendTrianglesJob
        {
            startVertex = sharedLine.vertexLowerBound,
            endVertex = sharedLine.vertexUpperBound - 2,
            triangleBuffer = sharedTriangleBuffer
        }.Schedule();

        // we have to return control to main thread immediately
        JobHandle.CompleteAll(jobHandles);
    }

    public void DisableLine(Entity lineEntity)
    {
        if (!EntityManager.HasComponent<SharedLine>(lineEntity) || !EntityManager.HasComponent<Line>(lineEntity)) return;
        
        var line = EntityManager.GetComponentData<Line>(lineEntity);
        var sharedLine = EntityManager.GetComponentData<SharedLine>(lineEntity);
        var meshEntity = sharedLine.parentMeshEntity;
        var sharedVertexBuffer = EntityManager.GetBuffer<VertexData>(lineEntity);
        var sharedTriangleBuffer = EntityManager.GetBuffer<TriangleData>(lineEntity).Reinterpret<int>();
        
        int pointCount = EntityManager.GetBuffer<PointData>(lineEntity).Length;
        int triangleIdx = sharedLine.vertexLowerBound* 3;
        int triangleShift = pointCount * 2;
        sharedVertexBuffer.RemoveRange(sharedLine.vertexLowerBound, sharedLine.vertexRange);
        
        // TODO make this a job
        for (int i = triangleIdx; i < sharedTriangleBuffer.Length; i++) {
            sharedTriangleBuffer[i] -= triangleShift;
        }
        sharedLine.isActiveInMesh = 0;
        EntityManager.SetComponentData(lineEntity, sharedLine);
    }

    [BurstCompile]
    public struct AppendJob<T> : IJob where T : struct
    {
        public int count;
        public DynamicBuffer<T> buffer;
        public T  defaultValue;
        public void Execute()
        {
            for (int i = 0; i < count; i++) {
                buffer.Add(defaultValue);
            }
        }
    }

    [BurstCompile]
    public struct AppendTrianglesJob : IJob
    {
        public int endVertex;
        public int startVertex;
        public DynamicBuffer<int> triangleBuffer;
        public void Execute()
        {
            for (int vert = startVertex; vert <= endVertex; vert += 2)
            {            
                triangleBuffer.Add(vert    );
                triangleBuffer.Add(vert + 1);
                triangleBuffer.Add(vert + 2);
                triangleBuffer.Add(vert + 2);
                triangleBuffer.Add(vert + 1);
                triangleBuffer.Add(vert + 3);
            }
        }
    }
}
public struct ManagedMeshData
{
    public Mesh mesh;
    public MeshFilter meshFilter;
    public List<Vector3> vertices;
    public List<int> triangles;
}