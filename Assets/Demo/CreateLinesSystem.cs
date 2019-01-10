using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;  

using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;

public class CreateLinesSystem : JobComponentSystem
{
    private Entity _meshEntity;
    private Camera _camera;
    private float _cameraDist;
    private float _spawnRate = 50f;
    private float _spawnTimer;
    private bool _initialized = false;
    private EntityArchetype _lineArchetype;
    private AssignToMeshSystem _assignToMeshSystem;
    public void Initialize (MeshFilter meshFilter, Camera camera, float cameraDist)
    {
        _meshEntity = World.Active.GetOrCreateManager<BatchedLineSystem>().CreateBatchedMesh(meshFilter);
        _assignToMeshSystem = World.Active.GetOrCreateManager<AssignToMeshSystem>();
        _camera = camera;
        _cameraDist = cameraDist;
        _spawnTimer = 1f;
        _initialized = true;
    }

    protected override void OnCreateManager()
    {
        var entityManager = World.Active.GetOrCreateManager<EntityManager>();
        _lineArchetype = entityManager.CreateArchetype(
            typeof(IsActive),
            typeof(DemoLine),
            typeof(MeshAssigner),
            typeof(VertexBuffer)
        );
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int spawnCount  = 0;
        float3 spawnPos = float3(0);
        if (_spawnTimer < 1f)
        {
            _spawnTimer += Time.deltaTime * _spawnRate;
        }
        var JobHandle = new JobHandle();
        if (_spawnTimer >= 1f && Input.GetMouseButton(0))
        {
            _spawnTimer -= 1f;
            Vector2 mousePos = Input.mousePosition;
            float3 screenPoint = new float3(mousePos.x, mousePos.y, _cameraDist);
            spawnPos = _camera.ScreenToWorldPoint(screenPoint);
            spawnCount = UnityEngine.Random.Range(1, 3);
        }
        var job = new CreateLinesJob
        {
            spawnCount      = spawnCount,
            spawnPos        = spawnPos,
            lineArchetype   = _lineArchetype,
            meshEntity      = _meshEntity,
            commandBuffer   = barrier.CreateCommandBuffer(),
        };
        return job.Schedule(inputDeps);
    }

    [Inject] public LineModificationBarrier barrier;

    public struct CreateLinesJob : IJob
    {
        public int spawnCount;
        public float3 spawnPos;
        public EntityCommandBuffer commandBuffer;
        public EntityArchetype lineArchetype;
        public Entity meshEntity;
        public void Execute()
        {
            for (int i = 0; i < spawnCount; i++)
            {
                commandBuffer.CreateEntity(lineArchetype);
                commandBuffer.SetComponent<IsActive>(new IsActive
                {
                    value = true
                });
                commandBuffer.SetComponent<DemoLine>(new DemoLine
                {
                    speed = 3f,
                    time = 0f
                });
                commandBuffer.SetComponent<MeshAssigner>(new MeshAssigner
                {
                    targetMesh = meshEntity
                });
                
                var pointBuffer     = commandBuffer.AddBuffer<PointBuffer>().Reinterpret<float3>();
                var facingBuffer    = commandBuffer.AddBuffer<FacingBuffer>().Reinterpret<float3>();
                var widthBuffer     = commandBuffer.AddBuffer<WidthBuffer>().Reinterpret<float>();

                pointBuffer.Add(spawnPos);
                pointBuffer.Add(spawnPos);
                facingBuffer.Add(float3(0, 0, 1));
                widthBuffer.Add(0.01f);
            }
        }
    }
}