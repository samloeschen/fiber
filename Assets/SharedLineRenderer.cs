using System;
using System.Runtime.InteropServices;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;

public class SharedLineRenderer : MonoBehaviour {
    public NativeArray<Vector3> vertices;
    public NativeArray<int> triangles;
    public NativeArray<Vector4> colors;

    public List<SharedLine> activeLineList;
    public HashSet<SharedLine> activeLineSet;

    public bool autoUpdate = true;
    public bool autoAllocate = true;
    public int initialMeshSize = 512;

    [ReadOnly]
    public Mesh mesh;

    // test bullshit
    SharedLine testLine0;
    public float testLineLength;
    public float testLineWidth;
    public int testLinePoints;
    public Color testLineColor;
    public AnimationCurve fadeCurve;


    void OnEnable () {
        if (autoAllocate) {
        }
        AllocMesh();

        testLinePool = new List<SharedLine>();
        timers = new List<float>();
        testLine0 = new SharedLine();
        testLine0.points = new NativeArray<Vector3>(testLinePoints, Allocator.Persistent);
        testLine0.widths = new NativeArray<float>(testLinePoints, Allocator.Persistent);
        testLine0.facings = new NativeArray<Vector3>(testLinePoints, Allocator.Persistent);
        AddLine(testLine0);
    }

    void OnDisable () {
        DeallocMesh();
        testLine0.Dispose();
    }

    public void AllocMesh () {
        activeLineList = new List<SharedLine>(5);
        activeLineSet = new HashSet<SharedLine>();
        vertices = new NativeArray<Vector3>(0, Allocator.Persistent);
        colors = new NativeArray<Vector4>(0, Allocator.Persistent);
        triangles = new NativeArray<int>(0, Allocator.Persistent);
        meshVertices = new List<Vector3>();
        meshColors = new List<Vector4>();
        meshTriangles = new List<int>();
        mesh = new Mesh();
        mesh.MarkDynamic();
        mesh.name = "SharedLineMesh";
    }

    public void DeallocMesh () {
        activeLineList = null;
        activeLineSet = null;
        vertices.Dispose();
        colors.Dispose();
        triangles.Dispose();
        meshVertices = null;
        meshTriangles = null;
        meshColors = null;
        mesh.Clear();
    }

    List<SharedLine> testLinePool;
    List<float> timers;

    public float spawnRate;
    float spawnTimer;
    public float lifeTime;
    int lastVertCount = 0;

    [Range(0f, 1f)]
    public float bendControl = 0f;
    [Range(0f, 1f)]
    public float twistControl = 0f;

    int vertexCount = 0;

    NativeArray<JobHandle> handles;
    void Update () {
        // spawnTimer += Time.deltaTime / spawnRate;
        // if (spawnTimer >= 1f) {
        //     spawnTimer = 0f;

        //     // try to get a line that is inactive
        //     SharedLine line = null;
        //     for (int i = 0; i < testLinePool.Count; i++) {
        //         if (testLinePool[i].rendererIdx == -1) {
        //             line = testLinePool[i];
        //             break;
        //         }
        //     }
        //     if (line == null) { 
        //         line = new SharedLine(); 
        //         line.points = new Vector3[2];
        //         testLinePool.Add(line);
        //         timers.Add(0f);
        //     }
        //     AddLine(line);
        //     Vector3 dir = UnityEngine.Random.onUnitSphere;
        //     Vector3 startPoint = UnityEngine.Random.onUnitSphere * testLineLength;
        //     float t = 0f;
        //     for (int i = 0; i < 2; i++) {
        //         t = (float) i / (2 - 1f);
        //         line.points[i] = startPoint + dir * t;
        //     }
        //     line.Draw(Vector3.forward, testLineWidth);
        // }

        // for (int i = 0; i < testLinePool.Count; i++) {
        //     if (testLinePool[i].rendererIdx == -1) { continue; }    
        //     timers[i] += Time.deltaTime / lifeTime;
        //     testLinePool[i].SetColor(testLineColor.withAlpha(fadeCurve.Evaluate(timers[i])));
        //     if (timers[i] >= 1f) {
        //         RemoveLine(testLinePool[i]);
        //         timers[i] = 0f;
        //     }
        // }

        for (int i = 0; i < testLine0.points.Length; i++) {
            float t = ((float)i / (testLine0.points.Length - 1f)) - 0.5f;
            testLine0.points[i] = new Vector3(
                testLineLength * t,
                Mathf.Sin(Time.time + (t * 4f)) * 2f * bendControl,
                0f
            );
            float angle = Mathf.Sin((Time.time * 2f) + (t * 4f)) * twistControl;
            testLine0.facings[i] = Quaternion.AngleAxis(angle * 40f, Vector3.right) * Vector3.forward;
            testLine0.widths[i] = Mathf.Lerp(0.1f, testLineWidth, (Mathf.Sin((Time.time * 2f) + (t * 4f)) + 1f) * 0.5f);
        }
        

        // TODO calculate line slices here
        handles = new NativeArray<JobHandle>(activeLineList.Count, Allocator.TempJob);
        for (int i = 0; i < activeLineList.Count; i++) {
            var line = activeLineList[i];
            int length = line.vertexUpperBound - line.vertexLowerBound + 1;
            if (!line.drawFlag) { continue; }
            var drawLineJob = new DrawLineJob {
                points = activeLineList[i].points,
                facings = activeLineList[i].facings,
                widths = activeLineList[i].widths,

                vertices = new NativeSlice<Vector3>(this.vertices, line.vertexLowerBound, length),
                colors = new NativeSlice<Vector4>(this.colors, line.vertexLowerBound, length),
            };
            handles[i] = drawLineJob.Schedule(drawLineJob.points.Length, Mathf.NextPowerOfTwo(line.points.Length / 4));
        }

        if (autoUpdate) {
            
        }
    }


    void LateUpdate () {
        JobHandle.CompleteAll(handles);
        handles.Dispose();
        UpdateMesh();
    }

    List<Vector3> meshVertices;
    List<int> meshTriangles;
    List<Vector4> meshColors;

    public void UpdateMesh () {
        mesh.Clear();
        if (vertices.Length == 0) { return; }
 
        // meshVertices.AddRange(vertices);
        // meshColors.AddRange(colors);
        // meshTriangles.AddRange(triangles);
 
        mesh.SetVertices(meshVertices);
        mesh.SetTriangles(meshTriangles, 0);
        mesh.SetUVs(1, meshColors);
 
        // meshVertices.Clear();
        // meshTriangles.Clear();
        // meshColors.Clear();
    }

    public void AddLine (SharedLine line) {
        if (activeLineSet.Contains(line)) {
            UnityEngine.Debug.LogError("This line is already managed by this SharedLineRenderer!");
            return;
        }
        if (line.rendererIdx > 0) {
            UnityEngine.Debug.LogError("This line is already managed by a different SharedLineRenderer!");
            return; 
        }
        if (line.points.Length < 2) {
            UnityEngine.Debug.LogError("Cannot draw a line with less than two points!");
            return;
        }

        int activeLineCount = activeLineList.Count;
        int vertexLowerBound = 0;
        int triLowerBound = 0;
        int pointCount = line.points.Length;
        int vertexUpperBound = ((line.points.Length) * 2) - 1;
        int triUpperBound = ((line.points.Length - 1) * 6) - 1;

        if (activeLineCount > 0) {
            SharedLine lastLine = activeLineList[activeLineCount - 1]; 

            vertexLowerBound = (lastLine.points.Length * 2);
            triLowerBound = ((lastLine.points.Length - 1) * 6);

            vertexUpperBound += vertexLowerBound;  
            triUpperBound += triLowerBound;
        }

        vertexCount = Mathf.Max(vertexCount, vertexUpperBound + 1);
        if (vertexCount > vertices.Length) {
            var newVertices = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
            var newColors = new NativeArray<Vector4>(vertexCount, Allocator.Persistent);
            var newTriangles = new NativeArray<int>((vertexCount - 2) * 3, Allocator.Persistent);
            if (vertices != null && triangles != null) {
                for (int i = 0; i < vertices.Length; i++) {
                    newVertices[i] = vertices[i];
                    newColors[i] = colors[i];
                    
                    if (i == vertices.Length - 2) { continue;}
                    newTriangles[i    ] = triangles[i    ];
                    newTriangles[i + 1] = triangles[i + 1];
                    newTriangles[i + 2] = triangles[i + 2];
                }
                triangles.Dispose();
                vertices.Dispose();
                colors.Dispose();
            }
            triangles = newTriangles;
            vertices = newVertices;
            colors = newColors;
        }

        for (int vert = vertexLowerBound, tri = triLowerBound; vert <= vertexUpperBound; vert += 2, tri += 6) {            

            vertices[vert] = Vector3.zero;
            vertices[vert + 1] = Vector3.zero;

            colors[vert] = Color.white;
            colors[vert + 1] = Color.white;

            if (vert >= vertexUpperBound - 1) { continue; }
            triangles[tri    ] = vert    ;
            triangles[tri + 1] = vert + 1;
            triangles[tri + 2] = vert + 2;
            triangles[tri + 3] = vert + 2;
            triangles[tri + 4] = vert + 1;
            triangles[tri + 5] = vert + 3;
        }
        
        line.vertexLowerBound = vertexLowerBound;
        line.vertexUpperBound = vertexUpperBound;
        line.rendererIdx = activeLineList.Count;
        activeLineList.Add(line);
        activeLineSet.Add(line);
    }
    

    public void RemoveLine (SharedLine line) {
        if (line.rendererIdx == -1) {
            UnityEngine.Debug.LogError("This line is not currently managed by a SharedLineRenderer!");
            return; 
        }
        if (!activeLineSet.Contains(line)) {
            UnityEngine.Debug.LogError("This line is managed by a different SharedLineRenderer!");
            return;
        }
        int length = line.vertexUpperBound - line.vertexLowerBound + 1;
        for (int i = line.rendererIdx + 1; i < activeLineList.Count; i++) {
            SharedLine otherLine = activeLineList[i];
            otherLine.vertexLowerBound -= length;
            otherLine.vertexLowerBound -= length;
            otherLine.rendererIdx--;
        }

        //todo: perform removing of line in a job
        for (int i = line.vertexUpperBound; i < vertices.Length; i++) {
            vertices[line.vertexLowerBound + i] = vertices[i];
            colors[line.vertexLowerBound + i] = colors[i];
        }

        int triangleLowerBound = ((line.vertexLowerBound / 2) - 1) * 6;
        int triangleShift = (line.points.Length) * 2;
        for (int i = triangleLowerBound + 1; i < triangles.Length; i++) {
            int value = triangles[i];
            value -= triangleShift;
            triangles[i] = value;
        }
        vertexCount -= line.points.Length * 2;
        activeLineList.RemoveAt(line.rendererIdx);
        activeLineSet.Remove(line);
        line.rendererIdx = -1;
    }

    public class SharedLine {
        public NativeArray<Vector3> points;
        public NativeArray<Vector3> facings;
        public NativeArray<float> widths;
        public int rendererIdx = -1;
        
        public int vertexLowerBound;
        public int vertexUpperBound;


        public bool isActive {
            get { return rendererIdx > 0; }
        }


        public bool drawFlag = true;

        public void Draw () {
            drawFlag = true;
        }

        public void Dispose () {
            points.Dispose();
            facings.Dispose();
            widths.Dispose();
        }
    }
}

    [BurstCompile]
    public struct DrawLineJob : IJobParallelFor {
        [ReadOnly]
        public NativeArray<Vector3> points;
        [ReadOnly]
        public NativeArray<Vector3> facings;
        [ReadOnly]
        public NativeArray<float> widths;

    [NativeDisableParallelForRestriction] 
        public NativeSlice<Vector3> vertices;
        [NativeDisableParallelForRestriction] 
        public NativeSlice<Vector4> colors;

        public void Execute (int index) {

            if (index < 1 || index == points.Length - 1) { return; }
            int vertexCount = vertices.Length;
            int pointCount = vertexCount / 2;
            
            // do start point
            // Vector3 facing = facings[0];
            // Vector3 curPt = points[0];
            // Vector3 nextPt = points[1];
            // float lineWidth = widths[0];

            // float dirX = (nextPt.x - curPt.x);
            // float dirY = (nextPt.y - curPt.y);
            // float dirZ = (nextPt.z - curPt.z);
            // float miterX, miterY, miterZ = 0;
            // FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
            // FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
            // miterX *= lineWidth;
            // miterY *= lineWidth;
            // miterZ *= lineWidth;

            // vertices[0] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
            // vertices[1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);

            // // do end point
            // facing = facings[pointCount - 1];
            // curPt = points[pointCount - 1];
            // Vector3 prevPt = points[pointCount - 2];
            // lineWidth = widths[pointCount - 1];

            // dirX = (curPt.x - prevPt.x);
            // dirY = (curPt.y - prevPt.y);
            // dirZ = (curPt.z - prevPt.z);
            // FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
            // FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
            // miterX *= lineWidth;
            // miterY *= lineWidth;
            // miterZ *= lineWidth;



            // int vIdx = vertexCount - 2;
            // vertices[vIdx] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
            // vertices[vIdx + 1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);
            
            // do all other point
            
            Vector3 curPt = points[index];
            Vector3 nextPt = points[index + 1];
            Vector3 prevPt = points[index - 1];

            Vector3 facing = facings[index];
            curPt = points[index];
            nextPt = points[index + 1];
            prevPt = points[index - 1];
            float lineWidth = widths[index];
            
            float abX = (curPt.x - prevPt.x);
            float abY = (curPt.y - prevPt.y);
            float abZ = (curPt.z - prevPt.z);

            float bcX = (nextPt.x - curPt.x);
            float bcY = (nextPt.y - curPt.y);
            float bcZ = (nextPt.z - curPt.z);

            float dirX = abX + bcX;
            float dirY = abY + bcY;
            float dirZ = abZ + bcZ;
            float miterX, miterY, miterZ = 0f;
            
            FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
            FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
            miterX *= lineWidth;
            miterY *= lineWidth;
            miterZ *= lineWidth;

            int vIdx = index * 2;
            vertices[vIdx    ] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
            vertices[vIdx + 1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);


            // for (int i = 1; i < pointCount - 1; i++) {
            //     facing = facings[i];
            //     curPt = points[i];
            //     nextPt = points[i + 1];
            //     prevPt = points[i - 1];
            //     lineWidth = widths[i];
                
            //     float abX = (curPt.x - prevPt.x);
            //     float abY = (curPt.y - prevPt.y);
            //     float abZ = (curPt.z - prevPt.z);

            //     float bcX = (nextPt.x - curPt.x);
            //     float bcY = (nextPt.y - curPt.y);
            //     float bcZ = (nextPt.z - curPt.z);

            //     dirX = abX + bcX;
            //     dirY = abY + bcY;
            //     dirZ = abZ + bcZ;
                
            //     FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
            //     FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
            //     miterX *= lineWidth;
            //     miterY *= lineWidth;
            //     miterZ *= lineWidth;

            //     vIdx = i * 2;
            //     vertices[vIdx    ] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
            //     vertices[vIdx + 1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);
            // }
        }
        void FastCross (float inX1, float inY1, float inZ1, float inX2, float inY2, float inZ2, out float outX, out float outY, out float outZ) {
            outX = (inY1 * inZ2) - (inZ1 * inY2);
            outY = (inZ1 * inX2) - (inX1 * inZ2);
            outZ = (inX1 * inY2) - (inY1 * inX2);
        }
        void FastNormalize (float inX, float inY, float inZ, out float outX, out float outY, out float outZ) {
            float w = Mathf.Sqrt(inX * inX + inY * inY + inZ * inZ);
            outX = inX / w;
            outY = inY / w;
            outZ = inZ / w;
        }
    }



