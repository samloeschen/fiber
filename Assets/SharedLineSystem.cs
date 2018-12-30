
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

using static Unity.Mathematics.math;
public class SharedLineSystem : ComponentSystem
{
    private EntityManager _entityManager;
    private static SharedLineSystem _instance;
    private EntityArchetype _lineArchetype;
    private EntityArchetype _sharedMeshArchetype;
    private UpdateSharedLineSystem _updateSharedLineSystem;
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

        _updateSharedLineSystem = World.Active.GetOrCreateManager<UpdateSharedLineSystem>();
    }

    public int sharedLineMeshVertexCap = 4000;

    public struct SharedLineMeshComponentGroup
    {
        public BufferArray<VertexData> vertexBufferLookup;
        public BufferArray<TriangleData> triangleBufferLookup; 
        public EntityArray meshEntities;
    }
    [Inject] SharedLineMeshComponentGroup sharedLineMeshComponentGroup;

    protected override void OnUpdate() { }

    public static SharedLineSystem GetSystem()
    {
        return _instance;
    }

    public void AddUpdateDependency (JobHandle jobHandle) {
        if (_updateSharedLineSystem.updateJobDependency.IsCompleted) {
            _updateSharedLineSystem.updateJobDependency = jobHandle;
        } else {
            JobHandle.CombineDependencies(_updateSharedLineSystem.updateJobDependency, jobHandle);
        }
    }

    // Creates a line data with buffers that can be accessed by the main thread immediately, instead of waiting on the creation system
    public LineData CreateLine(int pointCount, bool startActive = true)
    {
        // set up our new line entity
        var lineEntity = _entityManager.CreateEntity(_lineArchetype);

        // set up line component
        var line = new Line
        {
            pointCount = pointCount,
            isActive = (byte)(startActive ? 1 : 0)
        };
        _entityManager.SetComponentData(lineEntity, line);

        // search the injected shared mesh entities to see if we can find a suitable mesh entity
        // this could be a job
        var meshEntity = new Entity();
        bool makeNewSharedMesh = true;
        var sharedVertexBuffer = new DynamicBuffer<float3>();
        var sharedTriangleBuffer = new DynamicBuffer<int>();
        for (int i = 0; i < sharedLineMeshComponentGroup.vertexBufferLookup.Length; i++)
        {
            var buffer = sharedLineMeshComponentGroup.vertexBufferLookup[i];
            if (buffer.Length + line.pointCount < sharedLineMeshVertexCap)
            {
                meshEntity           = sharedLineMeshComponentGroup.meshEntities[i];
                sharedVertexBuffer   = sharedLineMeshComponentGroup.vertexBufferLookup[i].Reinterpret<float3>();
                sharedTriangleBuffer = sharedLineMeshComponentGroup.triangleBufferLookup[i].Reinterpret<int>();
                makeNewSharedMesh = false;
                break;
            }
        }

        // we didn't find a suitable shared mesh entity so we need to make a new one
        if (makeNewSharedMesh)
        {
            meshEntity           = _entityManager.CreateEntity(_sharedMeshArchetype);
            sharedVertexBuffer   = _entityManager.GetBuffer<VertexData>(meshEntity).Reinterpret<float3>();
            sharedTriangleBuffer = _entityManager.GetBuffer<TriangleData>(meshEntity).Reinterpret<int>();
        }
        
        // temporary until i get dirty meshes/lines working
        var meshDirty = new MeshDirty
        {
            entity = meshEntity,
        };
        _entityManager.SetComponentData(meshEntity, meshDirty);

        // set up shared line component
        var sharedLine = new SharedLine
        {
            vertexLowerBound = sharedVertexBuffer.Length,
            vertexUpperBound = sharedVertexBuffer.Length + ((line.pointCount * 2) - 1),
            parentMeshEntity = meshEntity,
            isActiveInMesh = 1,
        };
        _entityManager.SetComponentData(lineEntity, sharedLine);

        // set up our line data that we will return
        var lineData = new LineData
        {
            entity       = lineEntity,
            pointBuffer  = _entityManager.GetBuffer<PointData>(lineEntity).Reinterpret<float3>(),
            facingBuffer = _entityManager.GetBuffer<FacingData>(lineEntity).Reinterpret<float3>(),
            widthBuffer  = _entityManager.GetBuffer<WidthData>(lineEntity).Reinterpret<float>(),
        };
        
        // schedule jobs to populate buffers with initialization data
        var jobHandles = new NativeArray<JobHandle>(5, Allocator.Temp);
        jobHandles[0] = new AppendJob<float3>
        {
            count = pointCount,
            buffer = lineData.pointBuffer,
            defaultValue = float3(0)
        }.Schedule();
        jobHandles[1] = new AppendJob<float3>
        {
            count = pointCount,
            buffer = lineData.facingBuffer,
            defaultValue = float3(0, 0, 1)
        }.Schedule();
        jobHandles[2] = new AppendJob<float>
        {
            count = pointCount,
            buffer = lineData.widthBuffer,
            defaultValue = 0.25f
        }.Schedule();
        jobHandles[3] = new AppendJob<float3>
        {
            count = pointCount * 2,
            buffer = sharedVertexBuffer,
            defaultValue = float3(0),
        }.Schedule();
        jobHandles[4] = new AppendTrianglesJob
        {
            startVertex = sharedLine.vertexLowerBound,
            endVertex = sharedLine.vertexUpperBound - 2,
            triangleBuffer = sharedTriangleBuffer
        }.Schedule();

        // we have to return control to main thread immediately
        JobHandle.CompleteAll(jobHandles);
        return lineData;
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
public struct LineData
{
    public Entity entity;
    public DynamicBuffer<float3> pointBuffer;
    public DynamicBuffer<float3> facingBuffer;
    public DynamicBuffer<float> widthBuffer;
}
