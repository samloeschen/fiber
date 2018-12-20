using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SharedLineRendererMeshFilter : MonoBehaviour {

    public MeshFilter meshFilter;
    public SharedLineRenderer sharedLineRenderer;
    void Start () {
        if (meshFilter == null) {
            meshFilter = GetComponent<MeshFilter>();
        }
        if (meshFilter != null) {
            meshFilter.mesh = sharedLineRenderer.mesh;
        }
    }
}
