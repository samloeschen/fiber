using UnityEngine;
using Unity.Entities;
using Unity.Jobs;

public class DemoSetup : MonoBehaviour
{
    public float cameraDist;
    public new Camera camera;
    public MeshFilter meshFilter;
    void OnEnable() 
    {
        if (meshFilter == null)
        {
            Debug.LogError("No mesh filter attached!");
            return;
        }
        var createLinesSystem = World.Active.GetOrCreateManager<CreateLinesSystem>();
        createLinesSystem.Initialize(meshFilter, camera, cameraDist);
    }

    void Update()
    {
    }
}