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

public class SharedLineManagedMeshSystem : ComponentSystem
{
    public Dictionary<Entity, ManagedMeshData> managedMeshDataDict;
    public HashSet<Entity> processedMeshEntities;
    public ComponentGroup meshDirtyQuery;
    protected override void OnCreateManager()
    {
        managedMeshDataDict = new Dictionary<Entity, ManagedMeshData>();
        processedMeshEntities = new HashSet<Entity>();

        this.meshDirtyQuery = this.GetComponentGroup(new EntityArchetypeQuery
        {
            All = new[] { ComponentType.Create<MeshDirty>(), }
        });
    }

    protected override void OnUpdate()
    {
        processedMeshEntities.Clear();
        var meshDirtyType = GetArchetypeChunkComponentType<MeshDirty>(true);
        var chunks = meshDirtyQuery.CreateArchetypeChunkArray(Allocator.TempJob);
        for (int chunkIdx = 0; chunkIdx < chunks.Length; chunkIdx++)
        {
            var chunk = chunks[chunkIdx];
            var meshDirtyBuffer = chunk.GetNativeArray(meshDirtyType);
            for (int i = 0; i < chunk.Count; i++)
            {
                var meshEntity = meshDirtyBuffer[i].entity;
                if (processedMeshEntities.Contains(meshEntity)) return;
                processedMeshEntities.Add(meshEntity);
                
                // make sure the mesh exists in our dictionary, if not, create it
                ManagedMeshData managedMeshData = new ManagedMeshData();
                if (!managedMeshDataDict.ContainsKey(meshEntity))
                {
                    // create game object with mesh filter that this mesh will live in
                    GameObject gameObject = new GameObject("SharedLineMesh");
                    var meshFilter = gameObject.AddComponent<MeshFilter>();
                    gameObject.AddComponent<MeshRenderer>();

                    var newMesh = new Mesh();
                    newMesh.MarkDynamic();
                    meshFilter.mesh = newMesh;
                    managedMeshData = new ManagedMeshData
                    {
                        mesh = newMesh,
                        meshFilter = meshFilter,
                        vertices = new List<UnityEngine.Vector3>(),
                        triangles = new List<int>()
                    };
                    managedMeshDataDict.Add(meshEntity, managedMeshData);

                } else {
                    managedMeshData = managedMeshDataDict[meshEntity];
                }

                // update mesh arrays
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
            }
        }
        chunks.Dispose();
    }
}

public struct ManagedMeshData
{
    public Mesh mesh;
    public MeshFilter meshFilter;
    public List<UnityEngine.Vector3> vertices;
    public List<int> triangles;
}
