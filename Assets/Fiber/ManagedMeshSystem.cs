using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace Fiber
{
    [UpdateAfter(typeof(GenerateMeshSystem))]
    public class ManagedMeshSystem : ComponentSystem
    {
        private ComponentGroup _meshQuery;
        private HashSet<Entity> _processedEntities;
        private Dictionary<Entity, ManagedMeshData> _managedMeshes;
        private EntityManager _entityManager;
        
        protected override void OnCreateManager()
        {
            _entityManager = World.Active.GetOrCreateManager<EntityManager>();
            _managedMeshes = new Dictionary<Entity, ManagedMeshData>();
            _processedEntities = new HashSet<Entity>();
            _meshQuery = this.GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[]
                { 
                    ComponentType.Create<IsActive>(),
                    ComponentType.Create<VertexBuffer>(),
                    ComponentType.Create<TriangleBuffer>() 
                }
            });
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

        public void AssignManagedMeshToEntity(UnityEngine.Mesh mesh, Entity meshEntity, int vertexAllocation = 0)
        {
            if (!_managedMeshes.ContainsKey(meshEntity))
            {
                _managedMeshes.Add(meshEntity, new ManagedMeshData
                {
                    mesh        = mesh,
                    vertices    = new List<Vector3>(vertexAllocation),
                    triangles   = new List<int>(vertexAllocation * 3)
                });
            }
            else
            {
                var data = _managedMeshes[meshEntity];
                if (data.vertices.Capacity < vertexAllocation)
                {
                    data.vertices.Resize(vertexAllocation);
                }
                int triangleAllocation = vertexAllocation * 3;
                if (data.triangles.Capacity < triangleAllocation)
                {
                    data.triangles.Resize(triangleAllocation);
                }
                data.mesh = mesh;
                _managedMeshes[meshEntity] = data;
            }
        }
    }

    public struct ManagedMeshData
    {
        public Mesh mesh;
        public List<Vector3> vertices;
        public List<int> triangles;
    }
}