using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using static Unity.Mathematics.math;


public class LineMeshSystem : JobComponentSystem
{
    public EntityArchetype lineArchetype;
    public EntityArchetype meshBucketArchetype;


    EntityManager _entityManager;
    private readonly HashSet<Entity> entitySet = new HashSet<Entity>();

    private static LineMeshSystem _instance;

    public int meshBucketVertexCap = 4000; // idk

    protected override void OnCreateManager()
    {

        meshBucketArchetype = EntityManager.CreateArchetype(
            typeof(VertexData),
            typeof(EntityData)
        );

        lineArchetype = EntityManager.CreateArchetype(
            typeof(PointData), 
            typeof(FacingData), 
            typeof(WidthData), 
            typeof(IntRange),
            typeof(EntityRef)
        );

        _entityManager = World.Active.GetOrCreateManager<EntityManager>();
        _instance = this;
    }

    public HashSet<Entity> processedMeshEntities = new HashSet<Entity>();
    public ListDict<Entity, MeshRef> managedMeshReferences = new ListDict<Entity, MeshRef>();

    public struct MeshRef
    {
        public Mesh mesh;
        public MeshFilter meshFilter;
        public List<UnityEngine.Vector3> vertices;
        public List<int> triangles;
    }

    public struct MeshBucketGroup
    {
        public BufferArray<VertexData> vertexBufferLookup;
        public BufferArray<EntityData> lineEntitiesLookup;
        public EntityArray meshEntities;
    }
    [Inject]
    MeshBucketGroup meshBucketGroup;

    public NativeList<JobHandle> externalDependencies;
    public JobHandle lineUpdateJobs = new JobHandle();
    public JobHandle meshManagementJobs = new JobHandle();
    public JobHandle meshUpdateJob = new JobHandle();

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // resolve scheduled work
        meshManagementJobs.Complete();

        // dump native buffers into managed meshes 
        UpdateMeshBuckets();

        UpdateLineJob job = new UpdateLineJob
        {
            vertexBufferLookup = GetBufferFromEntity<VertexData>(),
            pointsLookup = GetBufferFromEntity<PointData>(true),
            facingLookup = GetBufferFromEntity<FacingData>(true),
            widthLookup  = GetBufferFromEntity<WidthData>(true),
        };
        meshUpdateJob = job.Schedule(this, dependsOn: JobHandle.CombineDependencies(inputDeps, lineUpdateJobs));

        lineUpdateJobs.Complete();
        return meshUpdateJob;
    }

    

    public void AddUpdateDependency (JobHandle dependency)
    {
        if (lineUpdateJobs.IsCompleted) {
            lineUpdateJobs = dependency;
        } else {
            JobHandle.CombineDependencies(dependency, lineUpdateJobs);
        }
    }

    public static LineMeshSystem GetSystem ()
    {
        return _instance;
    }

    private void UpdateMeshBuckets ()
    {
        EntityArray meshBucketEntities = meshBucketGroup.meshEntities;
        processedMeshEntities.Clear();

        // just update all meshes for now
        for (int i = 0; i < meshBucketEntities.Length; i++)
        {
            Entity entity = meshBucketEntities[i];
            if (processedMeshEntities.Contains(entity)) continue;
            processedMeshEntities.Add(entity);

            // get the managed data associated with this entity
            MeshRef managedData;
            if (managedMeshReferences.ContainsKey(entity))
            {
                managedData = managedMeshReferences[entity];
            } 
            else 
            {
                Debug.Log("no mesh ref maintained for this entity");
                continue;
            }
            
            // we memcpy the native dynamic buffer straight into the managed vertices list, using the list as a wrapper
            DynamicBuffer<Vector3> nativeVertexBuffer = meshBucketGroup.vertexBufferLookup[i].Reinterpret<Vector3>();
            managedData.vertices.AddRange(nativeVertexBuffer);
            managedData.mesh.SetVertices(managedData.vertices);
            managedData.mesh.SetTriangles(managedData.triangles, 0);
            managedData.vertices.Clear();
        }
    }

    [BurstCompile]
    [RequireComponentTag(typeof(PointData), typeof(FacingData), typeof(WidthData))]
    public struct UpdateLineJob : IJobProcessComponentDataWithEntity<IntRange, EntityRef>
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<VertexData> vertexBufferLookup;
        [ReadOnly]
        public BufferFromEntity<PointData> pointsLookup;
        [ReadOnly]
        public BufferFromEntity<FacingData> facingLookup;
        [ReadOnly]
        public BufferFromEntity<WidthData> widthLookup;

        public void Execute(Entity entity, int index, ref IntRange vertexBufferBounds, ref EntityRef meshEntityRef)
        {
            // grab vertex buffer from supplied vertex buffer lookup using the index of the mesh entity associated with this line
            DynamicBuffer<float3> vertexBuffer = vertexBufferLookup[meshEntityRef.entity].Reinterpret<float3>();

            // reinterpret other buffers so we can use math on them
            DynamicBuffer<float3> pointBuffer = pointsLookup[entity].Reinterpret<float3>();
            DynamicBuffer<float3> facingBuffer = facingLookup[entity].Reinterpret<float3>();
            DynamicBuffer<float> widthBuffer = widthLookup[entity].Reinterpret<float>();

            // set first point
            float3 curPt = pointBuffer[0];
            float3 nextPt = pointBuffer[1];
            float3 facing = facingBuffer[0];
            float3 dir = normalize(nextPt - curPt);
            float3 miter = normalize(cross(dir, facing)) * widthBuffer[0];
            int vIdx = vertexBufferBounds.lowerBound;
            vertexBuffer[vIdx    ] = curPt + miter;
            vertexBuffer[vIdx + 1] = curPt - miter;

            // set second point
            int endIdx = pointBuffer.Length - 1;
            float3 prevPt = pointBuffer[endIdx - 1];
            curPt = pointBuffer[endIdx];
            facing = facingBuffer[endIdx];
            dir = normalize(curPt - prevPt);
            miter = normalize(cross(dir, facing)) * widthBuffer[endIdx];
            vIdx = vertexBufferBounds.upperBound - 1;
            vertexBuffer[vIdx    ] = curPt + miter;
            vertexBuffer[vIdx + 1] = curPt - miter;

            for (int i = 1; i < pointBuffer.Length - 1; i++)
            {
                curPt = pointBuffer[i];
                nextPt = pointBuffer[i + 1];
                prevPt = pointBuffer[i - 1];
                float3 ab = normalize(curPt - prevPt);
                float3 bc = normalize(nextPt - curPt);
                miter = normalize(cross(ab + bc, facing)) * widthBuffer[i];
                
                vIdx = vertexBufferBounds.lowerBound + (i * 2);
                vertexBuffer[vIdx    ] = curPt + miter;
                vertexBuffer[vIdx + 1] = curPt - miter;
            }
        }
    }

    public LineData CreateLine (int pointCount, MeshFilter testMeshFilter)
    {
        DynamicBuffer<float3> vertexBuffer = new DynamicBuffer<float3>();
        DynamicBuffer<Entity> lineEntitiesBuffer = new DynamicBuffer<Entity>();
        Entity meshEntity = new Entity();
        MeshRef managedMeshData = new MeshRef();

        // create our fresh line entity
        Entity lineEntity = EntityManager.CreateEntity(lineArchetype);

        // get the mesh bucket that we will place this line into
        bool makeNew = true;
        for (int i = 0; i < meshBucketGroup.meshEntities.Length; i++)
        {
            var buffer = meshBucketGroup.vertexBufferLookup[i];
            if (buffer.Length + pointCount < meshBucketVertexCap)
            {
                vertexBuffer = buffer.Reinterpret<float3>();
                lineEntitiesBuffer = meshBucketGroup.lineEntitiesLookup[i].Reinterpret<Entity>();
                meshEntity = meshBucketGroup.meshEntities[i];
                managedMeshData = managedMeshReferences[meshEntity];
                makeNew = false;
                break;
            }
        }
        if (makeNew)
        {
            meshEntity = EntityManager.CreateEntity(meshBucketArchetype);
            vertexBuffer = EntityManager.GetBuffer<VertexData>(meshEntity).Reinterpret<float3>();
            lineEntitiesBuffer = EntityManager.GetBuffer<EntityData>(meshEntity).Reinterpret<Entity>();
            managedMeshData = new MeshRef
            {
                mesh = new Mesh(),
                meshFilter = testMeshFilter,
                vertices = new List<Vector3>(),
                triangles = new List<int>()
            };
            managedMeshData.mesh.MarkDynamic();
            managedMeshData.meshFilter.mesh = managedMeshData.mesh;
            managedMeshReferences.Add(meshEntity, managedMeshData);
        }

        // set up the line's vertex buffer slice bounds so it can index into the mesh bucket vertices correctly
        IntRange vertexBufferBounds = new IntRange
        {
            lowerBound = 0,
            upperBound = pointCount
        };

        // get the upper and lower bounds for this mesh
        for (int i = 0; i < lineEntitiesBuffer.Length; i++)
        {
            var bounds = EntityManager.GetComponentData<IntRange>(lineEntitiesBuffer[i]);
            if (bounds.lowerBound > vertexBufferBounds.lowerBound)
            {
                vertexBufferBounds.lowerBound = bounds.upperBound + 1;
                vertexBufferBounds.upperBound = vertexBufferBounds.lowerBound + pointCount;
            }
        }

        EntityManager.SetComponentData<IntRange>(lineEntity, vertexBufferBounds);

        // give the line a reference to the mesh bucket so that it can grab the vertex buffer from it when drawing
        EntityRef meshEntityRef = new EntityRef { entity = meshEntity };
        EntityManager.SetComponentData<EntityRef>(lineEntity, meshEntityRef);

        // register the line we just created to the mesh's line entities buffer so we can add/remove/alter its bounds later
        lineEntitiesBuffer.Add(lineEntity);

        // set up the line's buffers and populate them with initialization data.
        // this could probably be an IJobParallelFor with native parallel for restriction disabled to speed up creation of big lines
        DynamicBuffer<float3> pointBuffer = EntityManager.GetBuffer<PointData>(lineEntity).Reinterpret<float3>();
        DynamicBuffer<float3> facingBuffer = EntityManager.GetBuffer<FacingData>(lineEntity).Reinterpret<float3>();
        DynamicBuffer<float> widthBuffer = EntityManager.GetBuffer<WidthData>(lineEntity).Reinterpret<float>();

        var jobHandles = new NativeArray<JobHandle>(4, Allocator.Temp);
        
        // point append
        jobHandles[0] = new AppendJob<float3>
        {
            count = pointCount,
            buffer = pointBuffer,
            value = float3(0)
        }.Schedule(dependsOn: lineUpdateJobs);

        // vertex append
        jobHandles[1] = new AppendJob<float3>
        {
            count = pointCount * 2,
            buffer = vertexBuffer,
            value = float3(0)
        }.Schedule(dependsOn: lineUpdateJobs);

        // facing append
        jobHandles[2] = new AppendJob<float3>
        {
            count = pointCount,
            buffer = facingBuffer,
            value = float3(0, 0, 1)
        }.Schedule(dependsOn: lineUpdateJobs);

        // width append
        jobHandles[3] = new AppendJob<float>
        {
            count = pointCount,
            buffer = widthBuffer,
            value = 0f
        }.Schedule(dependsOn: lineUpdateJobs);

        var appendJobHandle = JobHandle.CombineDependencies(jobHandles);
        meshManagementJobs = JobHandle.CombineDependencies(appendJobHandle, meshManagementJobs);
        jobHandles.Dispose();

        // triangles are a managed collection at the moment, so we have to append to them manually on the main thread
        List<int> triangles = managedMeshData.triangles;
        for (int vert = vertexBufferBounds.lowerBound; vert <= vertexBufferBounds.upperBound; vert += 2)
        {            
            if (vert == (pointCount - 1) * 2) continue;
            triangles.Add(vert    );
            triangles.Add(vert + 1);
            triangles.Add(vert + 2);
            triangles.Add(vert + 2);
            triangles.Add(vert + 1);
            triangles.Add(vert + 3);
        }

        appendJobHandle.Complete();

        return new LineData
        {
            lineEntity = lineEntity,   
            pointBuffer = pointBuffer,
            facingBuffer = facingBuffer,
            widthBuffer = widthBuffer
        };
    }

    [BurstCompile]
    public struct AppendJob<T> : IJob where T : struct
    {
        public int count;
        public DynamicBuffer<T> buffer;
        public T value;
        public void Execute()
        {
            for (int i = 0; i < count; i++) buffer.Add(value);
        }
    }

    [BurstCompile]
    public struct RemoveJob<T> : IJob where T : struct
    {
        public int index;
        public int range;
        public DynamicBuffer<T> buffer;
        public T value;
        public void Execute()
        {
            buffer.RemoveRange(index, range);
        }
    }
    [BurstCompile]
    public struct AppendTrianglesJob : IJob
    {
        public DynamicBuffer<int> triangles;
        public int count;
        public int startVertex;
        public int endVertex;
        public void Execute()
        {
            for (int vert = startVertex; vert <= endVertex; vert += 2)
            {            
                triangles.Add(vert    );
                triangles.Add(vert + 1);
                triangles.Add(vert + 2);
                triangles.Add(vert + 2);
                triangles.Add(vert + 1);
                triangles.Add(vert + 3);
            }
        }
    }
    [BurstCompile]
    public struct ShiftTrianglesJob : IJob
    {
        public DynamicBuffer<int> triangleBuffer;
        public int startIndex;
        public int endIndex;
        public int shift;

        public void Execute()
        {
            for (int i = startIndex; i <= endIndex; i++)
            {
                triangleBuffer[i] += shift;
            }
        }
    }
}


public struct LineData
{
    public Entity lineEntity;
    public DynamicBuffer<float3> pointBuffer;
    public DynamicBuffer<float3> facingBuffer;
    public DynamicBuffer<float> widthBuffer;
}