using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

public class BatchedLineSystem : ComponentSystem
{
    
    private EntityManager _entityManager;
    public static EntityArchetype lineArchetype;
    public static EntityArchetype sharedMeshArchetype;
    
    protected override void OnCreateManager()
    {
        _entityManager = World.Active.GetOrCreateManager<EntityManager>();
        lineArchetype = _entityManager.CreateArchetype(
            typeof(Line),
            typeof(VertexData),
            typeof(TriangleData),
            typeof(PointData),
            typeof(FacingData),
            typeof(WidthData)
        );
        sharedMeshArchetype = _entityManager.CreateArchetype(
            typeof(VertexData),
            typeof(TriangleData),
            typeof(MeshDirty)
        );
    }
}
