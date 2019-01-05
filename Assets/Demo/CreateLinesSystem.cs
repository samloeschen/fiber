using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;

using Unity.Burst;  

using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;

public class CreateLinesSystem : ComponentSystem
{
    private Entity _meshEntity;
    private Camera _camera;
    private float _cameraDist;
    private float _spawnRate = 20f;
    private float _spawnTimer;
    private bool _initialized = false;

    public void Initialize (MeshFilter meshFilter, Camera camera, float cameraDist)
    {
        _meshEntity = World.Active.GetOrCreateManager<BatchedLineSystem>().CreateBatchedMesh(meshFilter);
        _camera = camera;
        _cameraDist = cameraDist;
        _spawnTimer = 1f;
        _initialized = true;
    }
    protected override void OnUpdate()
    {
        if (!_initialized) return;

        if (_spawnTimer < 1f)
        {
            _spawnTimer += Time.deltaTime * _spawnRate;
        }
        if (_spawnTimer >= 1f && Input.GetMouseButton(0))
        {
            Vector2 mousePos = Input.mousePosition;
            float3 screenPoint = new float3(mousePos.x, mousePos.y, _cameraDist);
            float3 worldPoint = _camera.ScreenToWorldPoint(screenPoint);
            int spawnCount = UnityEngine.Random.Range(3, 6);
            
            var commandBuffer = PostUpdateCommands;
            for (int i = 0; i < spawnCount; i++)
            {
                var random = new Unity.Mathematics.Random();
                random.InitState();
                float3 position = worldPoint + (random.NextFloat3Direction() * UnityEngine.Random.Range(1f, 2f));
                
                commandBuffer.CreateEntity();
                var batchedLine = new BatchedLine
                {
                batchEntity = _meshEntity,
                };
                var line = new Line
                {
                    isActive = (byte)1,
                };
                var demoLine = new DemoLine
                {
                    speed = 1f,
                    time  = 0f,
                };
                commandBuffer.AddComponent(batchedLine);
                commandBuffer.AddComponent(line);
                commandBuffer.AddComponent(demoLine);

                var vertexBuffer = commandBuffer.AddBuffer<VertexBuffer>();
                var pointsBuffer = commandBuffer.AddBuffer<PointBuffer>().Reinterpret<float3>();
                var facingBuffer = commandBuffer.AddBuffer<FacingBuffer>().Reinterpret<float3>();
                var widthBuffer  = commandBuffer.AddBuffer<WidthBuffer>().Reinterpret<float>();

                pointsBuffer.Add(position);
                pointsBuffer.Add(position + float3(0.0001f)); // that's a hack to prevent nans from creeping into the mesh
                facingBuffer.Add(float3(0f, 0f, 1f));
                widthBuffer.Add(0.02f);
            }
            
            _spawnTimer -= 1f;

        }
    }
}