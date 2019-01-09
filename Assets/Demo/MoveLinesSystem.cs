using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;  

using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;

public class MoveLinesSystem : JobComponentSystem {

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var pointBuffers = GetBufferFromEntity<PointBuffer>(isReadOnly: false);
        var moveLinesJob = new MoveLinesJob
        {
            pointBuffers = pointBuffers,
            deltaTime = Time.deltaTime
        };
        return moveLinesJob.Schedule(this, inputDeps);
    }

    [BurstCompile]
    public struct MoveLinesJob : IJobProcessComponentDataWithEntity<DemoLine>
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<PointBuffer> pointBuffers;
        public float deltaTime;
        public const float noiseScale = 0.1f;
        public const float newPointDist = 0.15f;
        public void Execute(Entity lineEntity, int jobIdx, [ReadOnly]ref DemoLine demoLine)
        {
            
            var pointBuffer = pointBuffers[lineEntity].Reinterpret<float3>();
            float3 linePos = pointBuffer[pointBuffer.Length - 1];
            if (length(linePos - pointBuffer[pointBuffer.Length - 2]) > newPointDist)
            {
                pointBuffer.Add(linePos);
            }
            float noise = snoise(linePos * noiseScale);
            float angle = noise * ((float)PI * 2f) / noiseScale; 
            linePos += float3(cos(angle), sin(angle), 0f) * deltaTime * demoLine.speed;
            pointBuffer[pointBuffer.Length - 1] = linePos;
        }
    }
}