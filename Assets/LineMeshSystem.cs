using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using static Unity.Mathematics.math;
// public class LineMeshSystem : JobComponentSystem
// {
//     public EntityArchetype lineArchetype;
//     public EntityArchetype meshBucketArchetype;


//     EntityManager _entityManager;
//     private readonly HashSet<Entity> entitySet = new HashSet<Entity>();

//     private static LineMeshSystem _instance;

//     public int meshBucketVertexCap = 4000; // idk

//     protected override void OnCreateManager()
//     {

//         meshBucketArchetype = EntityManager.CreateArchetype(
//             typeof(VertexData),
//             typeof(TriangleData),
//             typeof(EntityData)
//         );

//         lineArchetype = EntityManager.CreateArchetype(
//             typeof(PointData), 
//             typeof(FacingData), 
//             typeof(WidthData), 
//             typeof(IntRange),
//             typeof(EntityRef)
//         );

//         _entityManager = World.Active.GetOrCreateManager<EntityManager>();
//         _instance = this;
//     }

//     public HashSet<Entity> processedMeshEntities = new HashSet<Entity>();
//     public ListDict<Entity, MeshRef> managedMeshReferences = new ListDict<Entity, MeshRef>();

//     public struct MeshRef
//     {
//         public Mesh mesh;
//         public MeshFilter meshFilter;
//         public List<UnityEngine.Vector3> vertices;
//         public List<int> triangles;
//     }

//     public struct MeshBucketGroup
//     {
//         public BufferArray<VertexData> vertexBufferLookup;
//         public BufferArray<TriangleData> triangleBufferLookup; 
//         public BufferArray<EntityData> lineEntitiesLookup;
//         public EntityArray meshEntities;
//     }
//     [Inject]
//     MeshBucketGroup meshBucketGroup;

//     public NativeList<JobHandle> externalDependencies;
//     public JobHandle lineUpdateJobs = new JobHandle();
//     public JobHandle meshManagementJobs = new JobHandle();
//     public JobHandle meshUpdateJob = new JobHandle();

//     protected override JobHandle OnUpdate(JobHandle inputDeps)
//     {
//         // resolve scheduled work
//         meshManagementJobs.Complete();

//         // dump native buffers into managed meshes 
//         UpdateMeshBuckets();

//         UpdateLineJob job = new UpdateLineJob
//         {
//             vertexBufferLookup = GetBufferFromEntity<VertexData>(),
//             pointsLookup = GetBufferFromEntity<PointData>(true),
//             facingLookup = GetBufferFromEntity<FacingData>(true),
//             widthLookup  = GetBufferFromEntity<WidthData>(true),
//         };
//         meshUpdateJob = job.Schedule(this, dependsOn: JobHandle.CombineDependencies(inputDeps, lineUpdateJobs));

//         lineUpdateJobs.Complete();
//         return meshUpdateJob;
//     }

    

//     public void AddUpdateDependency (JobHandle dependency)
//     {
//         if (lineUpdateJobs.IsCompleted) {
//             lineUpdateJobs = dependency;
//         } else {
//             JobHandle.CombineDependencies(dependency, lineUpdateJobs);
//         }
//     }

//     public static LineMeshSystem GetSystem ()
//     {
//         return _instance;
//     }

//     private void UpdateMeshBuckets ()
//     {
//         EntityArray meshBucketEntities = meshBucketGroup.meshEntities;
//         processedMeshEntities.Clear();

//         // just update all meshes for now
//         for (int i = 0; i < meshBucketEntities.Length; i++)
//         {
//             Entity entity = meshBucketEntities[i];
//             if (processedMeshEntities.Contains(entity)) continue;
//             processedMeshEntities.Add(entity);

//             // get the managed data associated with this entity
//             MeshRef managedData;
//             if (managedMeshReferences.ContainsKey(entity))
//             {
//                 managedData = managedMeshReferences[entity];
//             } 
//             else 
//             {
//                 Debug.Log("no mesh ref maintained for this entity");
//                 continue;
//             }
            
//             // we memcpy the native dynamic buffer straight into the managed vertices list, using the list as a wrapper
//             managedData.mesh.Clear();
//             DynamicBuffer<Vector3> nativeVertexBuffer = meshBucketGroup.vertexBufferLookup[i].Reinterpret<Vector3>();
//             managedData.vertices.AddRange(nativeVertexBuffer);
//             managedData.mesh.SetVertices(managedData.vertices);

//             DynamicBuffer<int> nativeTriangleBuffer = meshBucketGroup.triangleBufferLookup[i].Reinterpret<int>();
//             managedData.triangles.AddRange(nativeTriangleBuffer);
//             managedData.mesh.SetTriangles(managedData.triangles, 0);

//             managedData.vertices.Clear();
//             managedData.triangles.Clear();
//         }
//     }

//     [BurstCompile]
//     [RequireComponentTag(typeof(PointData), typeof(FacingData), typeof(WidthData))]
//     public struct UpdateLineJob : IJobProcessComponentDataWithEntity<IntRange, EntityRef>
//     {
//         [NativeDisableParallelForRestriction]
//         public BufferFromEntity<VertexData> vertexBufferLookup;
//         [ReadOnly]
//         public BufferFromEntity<PointData> pointsLookup;
//         [ReadOnly]
//         public BufferFromEntity<FacingData> facingLookup;
//         [ReadOnly]
//         public BufferFromEntity<WidthData> widthLookup;

//         public void Execute(Entity entity, int index, ref IntRange vertexBufferBounds, ref EntityRef meshEntityRef)
//         {
//             // grab vertex buffer from supplied vertex buffer lookup using the index of the mesh entity associated with this line
//             DynamicBuffer<float3> vertexBuffer = vertexBufferLookup[meshEntityRef.entity].Reinterpret<float3>();

//             // reinterpret other buffers so we can use math on them
//             DynamicBuffer<float3> pointBuffer = pointsLookup[entity].Reinterpret<float3>();
//             DynamicBuffer<float3> facingBuffer = facingLookup[entity].Reinterpret<float3>();
//             DynamicBuffer<float> widthBuffer = widthLookup[entity].Reinterpret<float>();

//             // set first point
//             float3 curPt = pointBuffer[0];
//             float3 nextPt = pointBuffer[1];
//             float3 facing = facingBuffer[0];
//             float3 dir = normalize(nextPt - curPt);
//             float3 miter = normalize(cross(dir, facing)) * widthBuffer[0];
//             int vIdx = vertexBufferBounds.lowerBound;
//             vertexBuffer[vIdx    ] = curPt + miter;
//             vertexBuffer[vIdx + 1] = curPt - miter;

//             // set second point
//             int endIdx = pointBuffer.Length - 1;
//             float3 prevPt = pointBuffer[endIdx - 1];
//             curPt = pointBuffer[endIdx];
//             facing = facingBuffer[endIdx];
//             dir = normalize(curPt - prevPt);
//             miter = normalize(cross(dir, facing)) * widthBuffer[endIdx];
//             vIdx = vertexBufferBounds.upperBound - 1;
//             vertexBuffer[vIdx    ] = curPt + miter;
//             vertexBuffer[vIdx + 1] = curPt - miter;

//             for (int i = 1; i < pointBuffer.Length - 1; i++)
//             {
//                 curPt = pointBuffer[i];
//                 nextPt = pointBuffer[i + 1];
//                 prevPt = pointBuffer[i - 1];
//                 float3 ab = normalize(curPt - prevPt);
//                 float3 bc = normalize(nextPt - curPt);
//                 miter = normalize(cross(ab + bc, facing)) * widthBuffer[i];
                
//                 vIdx = vertexBufferBounds.lowerBound + (i * 2);
//                 vertexBuffer[vIdx    ] = curPt + miter;
//                 vertexBuffer[vIdx + 1] = curPt - miter;
//             }
//         }
//     }

//     public LineData CreateLine (int pointCount, MeshFilter testMeshFilter)
//     {
//         // create our fresh line entity
//         Entity lineEntity = EntityManager.CreateEntity(lineArchetype);
//         Entity meshEntity = new Entity();

//         // get the mesh bucket that we will place this line into
//         bool makeNew = true;
//         for (int i = 0; i < meshBucketGroup.meshEntities.Length; i++)
//         {
//             var buffer = meshBucketGroup.vertexBufferLookup[i];
//             if (buffer.Length + pointCount < meshBucketVertexCap)
//             {
//                 meshEntity = meshBucketGroup.meshEntities[i];
//                 makeNew = false;
//                 break;
//             }
//         }
//         if (makeNew)
//         {
//             meshEntity = EntityManager.CreateEntity(meshBucketArchetype);
//             MeshRef managedMeshData = new MeshRef
//             {
//                 mesh = new Mesh(),
//                 meshFilter = testMeshFilter,
//                 vertices = new List<Vector3>(),
//                 triangles = new List<int>()
//             };
//             managedMeshData.mesh.MarkDynamic();
//             managedMeshData.meshFilter.mesh = managedMeshData.mesh;
//             managedMeshReferences.Add(meshEntity, managedMeshData);
//         }

//         // set up the line's vertex buffer slice bounds so it can index into the mesh bucket vertices correctly
//         IntRange vertexBufferBounds = new IntRange
//         {
//             lowerBound = 0,
//             upperBound = (pointCount * 2) - 1
//         };

//         // get the upper and lower bounds for this mesh
//         DynamicBuffer<Entity> lineEntitiesBuffer = EntityManager.GetBuffer<EntityData>(meshEntity).Reinterpret<Entity>();
//         for (int i = 0; i < lineEntitiesBuffer.Length; i++)
//         {
//             var bounds = EntityManager.GetComponentData<IntRange>(lineEntitiesBuffer[i]);
//             if (bounds.lowerBound > vertexBufferBounds.lowerBound)
//             {
//                 vertexBufferBounds.lowerBound = bounds.upperBound + 1;
//                 vertexBufferBounds.upperBound = vertexBufferBounds.lowerBound + (pointCount * 2);
//             }
//         }

//         EntityManager.SetComponentData<IntRange>(lineEntity, vertexBufferBounds);

//         // give the line a reference to the mesh bucket so that it can grab the vertex buffer from it when drawing
//         EntityRef meshEntityRef = new EntityRef { entity = meshEntity };
//         EntityManager.SetComponentData<EntityRef>(lineEntity, meshEntityRef);

//         // register the line we just created to the mesh's line entities buffer so we can add/remove/alter its bounds later
//         lineEntitiesBuffer.Add(lineEntity);

//         // set up the line's buffers and populate them with initialization data.
//         // this could probably be an IJobParallelFor with native parallel for restriction disabled to speed up creation of big lines
//         DynamicBuffer<float3> vertexBuffer   = EntityManager.GetBuffer<VertexData>(meshEntity).Reinterpret<float3>();
//         DynamicBuffer<int>    triangleBuffer = EntityManager.GetBuffer<TriangleData>(meshEntity).Reinterpret<int>();
//         DynamicBuffer<float3> pointBuffer    = EntityManager.GetBuffer<PointData>(lineEntity).Reinterpret<float3>();
//         DynamicBuffer<float3> facingBuffer   = EntityManager.GetBuffer<FacingData>(lineEntity).Reinterpret<float3>();
//         DynamicBuffer<float>  widthBuffer    = EntityManager.GetBuffer<WidthData>(lineEntity).Reinterpret<float>();

//         var jobHandles = new NativeArray<JobHandle>(5, Allocator.Temp);
        
//         // point append
//         jobHandles[0] = new AppendJob<float3>
//         {
//             count = pointCount,
//             buffer = pointBuffer,
//             value = float3(0)
//         }.Schedule(dependsOn: lineUpdateJobs);

//         // vertex append
//         jobHandles[1] = new AppendJob<float3>
//         {
//             count = pointCount * 2,
//             buffer = vertexBuffer,
//             value = float3(0)
//         }.Schedule(dependsOn: lineUpdateJobs);

//         // facing append
//         jobHandles[2] = new AppendJob<float3>
//         {
//             count = pointCount,
//             buffer = facingBuffer,
//             value = float3(0, 0, 1)
//         }.Schedule(dependsOn: lineUpdateJobs);

//         // width append
//         jobHandles[3] = new AppendJob<float>
//         {
//             count = pointCount,
//             buffer = widthBuffer,
//             value = 0f
//         }.Schedule(dependsOn: lineUpdateJobs);

//         // triangles append
//         jobHandles[4] = new AppendTrianglesJob
//         {
//             startVertex = vertexBufferBounds.lowerBound,
//             endVertex = vertexBufferBounds.upperBound - 2,
//             triangleBuffer = triangleBuffer
//         }.Schedule(dependsOn: lineUpdateJobs);

//         var appendJobHandle = JobHandle.CombineDependencies(jobHandles);
//         meshManagementJobs = JobHandle.CombineDependencies(appendJobHandle, meshManagementJobs);
//         jobHandles.Dispose();

//         return new LineData
//         {
//             lineEntity = lineEntity,   
//             pointBuffer = pointBuffer,
//             facingBuffer = facingBuffer,
//             widthBuffer = widthBuffer
//         };
//     }

//     public void RemoveLine (Entity lineEntity)
//     {
//         // make sure the mesh entity reference held by the line contains the line in its references
//         if (!EntityManager.HasComponent<EntityRef>(lineEntity)) return;
//         var lineMeshEntityRef = EntityManager.GetComponentData<EntityRef>(lineEntity).entity;

//         if (!EntityManager.HasComponent<EntityData>(lineMeshEntityRef)) return;
//         var entityBuffer = EntityManager.GetBuffer<EntityData>(lineMeshEntityRef).Reinterpret<Entity>();

//         // just using a byte out of laziness here because bool isn't blittable yet
//         NativeArray<byte> containsArr = new NativeArray<byte>(1, Allocator.TempJob);
//         var containsJob = new ContainsJob<Entity>
//         {
//             containsArr = containsArr,
//             value = lineEntity,
//             buffer = entityBuffer
//         };
//         JobHandle containsHandle = containsJob.Schedule();
//         containsHandle.Complete();

//         byte containsLine = containsArr[0];
//         containsArr.Dispose();
//         if (containsLine == 0) return;

//         lineUpdateJobs.Complete();

//         IntRange vertexBufferBounds = EntityManager.GetComponentData<IntRange>(lineEntity);

//         // we got a valid line entity so time to grab our buffers
//         DynamicBuffer<float3> vertexBuffer   = EntityManager.GetBuffer<VertexData>(lineMeshEntityRef).Reinterpret<float3>();
//         DynamicBuffer<int>    triangleBuffer = EntityManager.GetBuffer<TriangleData>(lineMeshEntityRef).Reinterpret<int>();
//         DynamicBuffer<float3> pointsBuffer   = EntityManager.GetBuffer<PointData>(lineEntity).Reinterpret<float3>();
//         DynamicBuffer<float3> facingBuffer   = EntityManager.GetBuffer<PointData>(lineEntity).Reinterpret<float3>();
//         DynamicBuffer<float>  widthBuffer    = EntityManager.GetBuffer<WidthData>(lineEntity).Reinterpret<float> ();

//         var jobHandles = new NativeArray<JobHandle>(5, Allocator.Temp);
//         var dependency = JobHandle.CombineDependencies(meshUpdateJob, lineUpdateJobs);
        
//         int triangleLowerBound = (vertexBufferBounds.lowerBound / 2) * 6;
//         int triangleRange = ((vertexBufferBounds.upperBound - vertexBufferBounds.lowerBound / 2) - 1) + 1;

//         JobHandle vertexRemoveHandle = new RemoveJob<float3>
//         {
//             index = vertexBufferBounds.lowerBound,
//             range = vertexBufferBounds.upperBound - vertexBufferBounds.lowerBound + 1,
//             buffer = vertexBuffer
//         }.Schedule(dependsOn: dependency);

//         JobHandle triangleRemoveHandle = new RemoveJob<int>
//         {
//             index = triangleLowerBound,
//             range = triangleRange,
//             buffer = triangleBuffer

//         }.Schedule(dependsOn: dependency);
//         JobHandle removeJobs = JobHandle.CombineDependencies(vertexRemoveHandle, triangleRemoveHandle);
//         removeJobs.Complete();
//         JobHandle shiftTrianglesJob = new ShiftTrianglesJob
//         {
//             triangleBuffer = triangleBuffer,
//             startIndex = triangleLowerBound,
//             endIndex = triangleBuffer.Length,
//             shift = pointsBuffer.Length * 2,
//         }.Schedule(dependsOn: removeJobs);

        
//         shiftTrianglesJob.Complete();
//         EntityManager.DestroyEntity(lineEntity);
//     }

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

//     [BurstCompile]
//     public struct RemoveJob<T> : IJob where T : struct
//     {
//         public int index;
//         public int range;
//         public Entity entity;
//         public DynamicBuffer<T> buffer;
//         public T value;
//         public void Execute()
//         {
//             buffer.RemoveRange(index, range);
//             Debug.Log(typeof(T).ToString());
//         }
//     }

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

//     [BurstCompile]
//     public struct ShiftTrianglesJob : IJob
//     {
//         public DynamicBuffer<int> triangleBuffer;
//         public int startIndex;
//         public int endIndex;
//         public int shift;

//         public void Execute()
//         {
//             for (int i = startIndex; i <= endIndex; i++)
//             {
//                 triangleBuffer[i] += shift;
//             }
//         }
//     }

//     [BurstCompile]
//     public struct ContainsJob<T> : IJob where T : struct, IEquatable<T>
//     {
//         public NativeArray<byte> containsArr;
//         public T value;
//         public DynamicBuffer<T> buffer;
//         public void Execute()
//         {
//             containsArr[0] = 0;
//             for (int i = 0; i < buffer.Length; i++)
//             {
//                 if (buffer[i].Equals(value)) {
//                     containsArr[0] = 1;
//                     break;
//                 }
//             }
//         }
//     }
//     [BurstCompile]
//     public struct ContainsEntityJob : IJob
//     {
//         public NativeArray<byte> containsArr;
//         public Entity entity;
//         public DynamicBuffer<Entity> buffer;
//         public void Execute()
//         {
//             containsArr[0] = 0;
//             for (int i = 0; i < buffer.Length; i++)
//             {
//                 if (buffer[i] == entity) {
//                     containsArr[0] = 1;
//                     break;
//                 }
//             }
//         }
//     }
// }


// public struct LineData
// {
//     public Entity lineEntity;
//     public DynamicBuffer<float3> pointBuffer;
//     public DynamicBuffer<float3> facingBuffer;
//     public DynamicBuffer<float> widthBuffer;
// }