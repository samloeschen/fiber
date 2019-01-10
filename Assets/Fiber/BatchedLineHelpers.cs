using UnityEngine;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Fiber
{
    public static class BatchedLineHelpers
    {
        public static EntityArchetype DynamicLineArchetype;
        public static EntityArchetype DynamicMeshArchetype;

        // system
        private static EntityManager _entityManager;
        private static ManagedMeshSystem _managedMeshSystem;
        private static AssignToMeshSystem _assignToMeshSystem;
        private static RemoveFromMeshSystem _removeFromMeshSystem;
        public static Entity CreateLine(bool isActive = true)
        {
            if (_entityManager == null)
            {
                Initialize();
            }

            var entity = _entityManager.CreateEntity(DynamicLineArchetype);
            _entityManager.SetComponentData(entity, new IsActive
            {
                value = isActive
            });
            return entity;
        }

        public static Entity CreateLine(int pointCount, int facingCount = 1, int widthCount = 1, bool isActive = true)
        {
            if (_entityManager == null)
            {
                Initialize();
            }
            var lineEntity      = _entityManager.CreateEntity(DynamicLineArchetype);
            var pointBuffer     = _entityManager.GetBuffer<PointBuffer>(lineEntity).Reinterpret<float3>();
            var facingBuffer    = _entityManager.GetBuffer<FacingBuffer>(lineEntity).Reinterpret<float3>();
            var widthBuffer     = _entityManager.GetBuffer<WidthBuffer>(lineEntity).Reinterpret<float>();

            for (int i = 0; i < max(max(pointCount, facingCount), widthCount); i++)
            {
                if (i < pointCount)
                    pointBuffer.Add(float3(0));

                if (i < facingCount)
                    facingBuffer.Add(float3(0));
                    
                if (i < widthCount)
                    widthBuffer.Add(0f);
            }
            _entityManager.SetComponentData(lineEntity, new IsActive
            {
                value = isActive
            });
            return lineEntity;
        }

        public static LineMeshData CreateLineMesh(bool isActive = true, int vertexAllocation = 0)
        {
            if (_entityManager == null)
            {
                Initialize();
            }
            var meshEntity = _entityManager.CreateEntity(DynamicMeshArchetype);
            _entityManager.SetComponentData(meshEntity, new IsActive
            {
                value = isActive
            });
            var mesh = new Mesh();
            mesh.MarkDynamic();
            _managedMeshSystem.AssignManagedMeshToEntity(mesh, meshEntity, vertexAllocation);   

            return new LineMeshData
            {
                entity          = meshEntity,
                mesh            = mesh,
            };
        }

        public static void AssignLineToMesh(Entity lineEntity, LineMeshData lineMeshData)
        {
            if (_entityManager == null)
            {
                Initialize();
            }
            var entityBuffer = _entityManager.GetBuffer<EntityBuffer>(lineMeshData.entity).Reinterpret<Entity>();
            entityBuffer.Add(lineEntity);
        }

        public static void RemoveLineFromMesh(Entity lineEntity, LineMeshData lineMeshData)
        {
            if (_entityManager == null)
            {
                Initialize();
            }
            var entityBuffer = _entityManager.GetBuffer<EntityBuffer>(lineMeshData.entity).Reinterpret<Entity>();

            // slow, O(n) removal for now
            for (int i = 0; i < entityBuffer.Length; i++)
            {
                if (entityBuffer[i] == lineEntity)
                {
                    entityBuffer.RemoveAt(i);
                    break;
                }
            }
        }
        
        public static void SetEntityActive(Entity lineEntity, bool active)
        {
            if (_entityManager == null)
            {
                Initialize();
            }
            _entityManager.SetComponentData(lineEntity, new IsActive
            {
                value = active,
            });
        }

        public static DynamicBuffer<T> GetBufferForEntity<T>(Entity entity) where T : struct, IBufferElementData
        {
            return _entityManager.GetBuffer<T>(entity);
        }

        private static void Initialize()
        {
            _entityManager          = World.Active.GetOrCreateManager<EntityManager>();
            _managedMeshSystem      = World.Active.GetOrCreateManager<ManagedMeshSystem>();
            _assignToMeshSystem     = World.Active.GetOrCreateManager<AssignToMeshSystem>();
            _removeFromMeshSystem   = World.Active.GetOrCreateManager<RemoveFromMeshSystem>();

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
        }
    }
    public struct LineMeshData
    {
        public Entity entity;
        public Mesh mesh;
    }
}