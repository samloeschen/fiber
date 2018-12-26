using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using static Unity.Mathematics.math;


public class LineMeshSystem : ComponentSystem
{
    public static EntityArchetype LineArchetype;
    public static EntityArchetype LineMeshArchetype;


    EntityManager _entityManager;
    private readonly HashSet<Entity> entitySet = new HashSet<Entity>();

    public static LineMeshSystem instance;

    public int meshBucketVertexCap = 4000; // idk

    protected override void OnCreateManager()
    {

        LineMeshArchetype = EntityManager.CreateArchetype(
            typeof(VertexData), typeof(PointData)
        );

        _entityManager = World.Active.GetOrCreateManager<EntityManager>();
        instance = this;
        jobHandles = new NativeList<JobHandle>(Allocator.Persistent);
    }

    protected override void OnDestroyManager()
    {
        jobHandles.Dispose();
    }

    public List<ActiveMeshJob> activeJobs = new List<ActiveMeshJob>();
    public NativeList<JobHandle> jobHandles;
    public HashSet<Entity> processedEntities = new HashSet<Entity>();
    int handleCount = 0;


    public Dictionary<Entity, MeshRef> managedMeshReferences = new Dictionary<Entity, MeshRef>();

    public struct ActiveMeshJob
    {
        // native stuff
        public DynamicBuffer<float3> nativeVertices;
        public DynamicBuffer<float3> nativePoints;
        
        // managed stuff
        public Mesh managedMesh;
        public List<Vector3> managedVertices;
        public List<int> managedTriangles;
    }
    

    public struct MeshRef
    {
        public Mesh mesh;
        public MeshFilter meshFilter;
        public List<UnityEngine.Vector3> vertices;
        public List<int> triangles;
    }

    public struct MeshBucketGroup
    {
        public BufferArray<VertexData> vertexBufferArray;
        public BufferArray<PointData> pointBufferArray;
        public EntityArray entities;
    }
    [Inject] MeshBucketGroup meshBucketGroup;

    protected override void OnUpdate()
    {
        if (handleCount > 0)
        {
            JobHandle.CompleteAll(jobHandles);
            jobHandles.Clear();
            handleCount = 0;
        }
        
        // complete any handles that need to be finished and apply work from previous frame
        if (activeJobs.Count > 0) {
            for (int i = 0; i < activeJobs.Count; i++)
            {
                ActiveMeshJob activeJob = activeJobs[i];
                // activeJobs[i].jobHandle.Complete();
                
                DynamicBuffer<Vector3> nativeVertexBuffer = activeJob.nativeVertices.Reinterpret<Vector3>();
                Mesh mesh = activeJob.managedMesh;
                
                // this is using memcpy internally, the list is essentially used as a momentary wrapper for the dynamic buffer
                activeJob.managedVertices.AddRange(nativeVertexBuffer); 

                // put the wrapped buffer into the mesh
                activeJob.managedMesh.SetVertices(activeJob.managedVertices);
                activeJob.managedMesh.SetTriangles(activeJob.managedTriangles, 0);

                // we don't need any of the data in the vertex list
                activeJob.managedVertices.Clear();
            }
            activeJobs.Clear();
        }

        processedEntities.Clear();
        EntityArray entities = meshBucketGroup.entities;
        for (int i = 0; i < entities.Length; i++)
        {
            // grab the entity referenced by this component
            Entity entity = entities[i];
            

            // make sure we have not already processed this entity this frame
            if (processedEntities.Contains(entity)) continue;
            processedEntities.Add(entity);

            MeshRef meshRef;
            if (managedMeshReferences.ContainsKey(entity))
            {
                meshRef = managedMeshReferences[entity];
            } else {
                Debug.Log("no mesh ref maintained for this entity");
                continue;
            }
            DynamicBuffer<float3> vertexData = meshBucketGroup.vertexBufferArray[i].Reinterpret<float3>();
            DynamicBuffer<float3> pointData = meshBucketGroup.pointBufferArray[i].Reinterpret<float3>();

            // kick off processing for this mesh
            int indexesPerWorker = Mathf.NextPowerOfTwo(pointData.Length);
            UpdateMeshJob job = new UpdateMeshJob
            {
                vertexData = vertexData,
                pointData = pointData,
            };

            JobHandle handle = job.Schedule(pointData.Length, indexesPerWorker);
            ActiveMeshJob activeJob = new ActiveMeshJob
            {
                nativeVertices = vertexData,
                nativePoints = pointData,
                managedMesh = meshRef.mesh,
                managedVertices = meshRef.vertices,
                managedTriangles = meshRef.triangles
            };
            activeJobs.Add(activeJob);
            jobHandles.Add(handle);

            // just complete immediately for now
            handle.Complete();
            // handleCount++;
        }
    }

    [BurstCompile]
    public struct UpdateMeshJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public DynamicBuffer<float3> vertexData;
        [ReadOnly]
        public DynamicBuffer<float3> pointData;
        public void Execute (int i)
        {
            if (i == 0 || i == pointData.Length - 1) { return; } // temp until i have beginning and end jobs written
            float3 curPt  = pointData[i];
            float3 nextPt = pointData[i + 1];
            float3 prevPt = pointData[i - 1];

            // Vector3 facing = facings[i];
            curPt = pointData[i];
            nextPt = pointData[i + 1];
            prevPt = pointData[i - 1];
            // float lineWidth = widths[i];

            float3 ab = curPt - prevPt;
            float3 bc = nextPt - curPt;

            float3 facing = float3(0f, 0f, 1f);
            float width = 0.3f;

            float3 miter = normalize(cross(ab + bc, facing)) * width;
            int vIdx = i * 2;
            vertexData[vIdx    ] = curPt + miter;
            vertexData[vIdx + 1] = curPt - miter;
        }
    }
    public DynamicBufferSlice<float3> CreateLine (int pointCount, MeshFilter testMeshFilter)
    {
        DynamicBuffer<float3> pointData = new DynamicBuffer<PointData>().Reinterpret<float3>();
        DynamicBuffer<float3> vertexData = new DynamicBuffer<float3>().Reinterpret<float3>();
        Entity entity = new Entity();
        bool makeNew = true;
        for (int i = 0; i < meshBucketGroup.entities.Length; i++)
        {
            pointData = meshBucketGroup.pointBufferArray[i].Reinterpret<float3>();
            vertexData = meshBucketGroup.vertexBufferArray[i].Reinterpret<float3>();
            entity = meshBucketGroup.entities[i];
            makeNew = false;
            break;
        }
        if (makeNew)
        {
            entity = EntityManager.CreateEntity(LineMeshArchetype);
            pointData = EntityManager.GetBuffer<PointData>(entity).Reinterpret<float3>();
            vertexData = EntityManager.GetBuffer<VertexData>(entity).Reinterpret<float3>();
        }

        MeshRef meshRef;
        List<int> triangleData;
        if (managedMeshReferences.ContainsKey(entity))
        {
            meshRef = managedMeshReferences[entity];
            triangleData = meshRef.triangles;
        } 
        else
        {
            triangleData = new List<int>();
            meshRef = new MeshRef {
                mesh = new Mesh(),
                meshFilter = testMeshFilter,
                vertices = new List<Vector3>(),
                triangles = triangleData
            };
            meshRef.mesh.MarkDynamic();
            meshRef.meshFilter.mesh = meshRef.mesh;
            managedMeshReferences.Add(entity, meshRef);
        }

        vertexData.Clear();
        pointData.Clear();
        triangleData.Clear();
        for (int pt = 0, vert = 0; pt < pointCount; pt++, vert += 2)
        {
            pointData.Add(float3(0));

            vertexData.Add(float3(0));
            vertexData.Add(float3(0));
            
            if (vert == (pointCount - 1) * 2) continue;
            triangleData.Add(vert    );
            triangleData.Add(vert + 1);
            triangleData.Add(vert + 2);
            triangleData.Add(vert + 2);
            triangleData.Add(vert + 1);
            triangleData.Add(vert + 3);
        }
        
        return new DynamicBufferSlice<float3>
        {
            lowerBound = 0,
            upperBound = pointCount - 1,
            buffer = pointData.Reinterpret<float3>(),
        };
    }
}


public struct DynamicBufferSlice<T> where T : struct
{
    public int lowerBound;
    public int upperBound;
    public DynamicBuffer<T> buffer;

    public int Length {
        get { return upperBound - lowerBound + 1; }
    }
    public T this [int subscript] {
        get {
            int idx = subscript + lowerBound;
            return buffer[idx];
        }
        set {
            int idx = subscript + lowerBound;
            buffer[idx] = value;
        }
    }
}