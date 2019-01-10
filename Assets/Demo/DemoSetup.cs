using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Fiber;

public class DemoSetup : MonoBehaviour
{
    public float cameraDist;
    public new Camera camera;
    public MeshFilter meshFilter;

    public Rect bounds;

    private MoveLinesSystem _moveLinesSystem;
    void OnEnable() 
    {
        if (meshFilter == null)
        {
            Debug.LogError("No mesh filter attached!");
            return;
        }
        var createLinesSystem = World.Active.GetOrCreateManager<CreateLinesSystem>();
        createLinesSystem.Initialize(meshFilter, camera, cameraDist);

        _moveLinesSystem = World.Active.GetOrCreateManager<MoveLinesSystem>();
    }

    void Update()
    {
        _moveLinesSystem.bounds = new float4
        (
            bounds.xMin,
            bounds.yMin,
            bounds.xMax,
            bounds.yMax
        );

        Debug.DrawLine(new Vector3(bounds.xMin, bounds.yMin), new Vector3(bounds.xMax, bounds.yMax), Color.green);
    }
}