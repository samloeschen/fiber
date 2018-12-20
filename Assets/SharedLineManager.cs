using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SharedLineManager : MonoBehaviour {
    public List<Vector3> vertices;
    public List<int> triangles;
    public List<Vector3> points;
    public List<Vector4> colors;
    public List<SharedLine> activeLineList;
    public HashSet<SharedLine> activeLineSet;

    public bool autoUpdate = true;
    public bool autoAllocate = true;
    public int initialMeshSize = 512;

    [HideInInspector]
    public MeshFilter meshFilter;
    [ReadOnly]
    public Mesh mesh;

    // test bullshit
    SharedLine testLine0;
    SharedLine testLine1;
    public float testLineLength;
    public float testLineWidth;
    public int testLinePoints;
    public Color testLineColor;
    public AnimationCurve fadeCurve;


    void Awake () {
        meshFilter = GetComponent<MeshFilter>();
    }

    void OnEnable () {
        if (autoAllocate) {
            AllocateMesh();
        }

        testLinePool = new List<SharedLine>();
        timers = new List<float>();
        testLine0 = new SharedLine(testLinePoints, 1f);
        testLine1 = new SharedLine(testLinePoints, 1f);
    }

    public void AllocateMesh () {
        points = new List<Vector3>(initialMeshSize);
        activeLineList = new List<SharedLine>(5);
        activeLineSet = new HashSet<SharedLine>();
        vertices = new List<Vector3>(initialMeshSize * 2);
        colors = new List<Vector4>(initialMeshSize * 2);
        triangles = new List<int>(initialMeshSize * 6);
        mesh = new Mesh();
    }

    List<SharedLine> testLinePool;
    List<float> timers;

    public float spawnRate;
    float spawnTimer;
    public float lifeTime;
    int lastVertCount = 0;
    int lastTriCount = 0;

    void Update () {
        spawnTimer += Time.deltaTime / spawnRate;
        if (spawnTimer >= 1f) {
            spawnTimer = 0f;

            // try to get a line that is inactive
            SharedLine line = null;
            for (int i = 0; i < testLinePool.Count; i++) {
                if (testLinePool[i].index == -1) {
                    line = testLinePool[i];
                    break;
                }
            }
            if (line == null) { 
                line = new SharedLine(testLinePoints, testLineWidth); 
                testLinePool.Add(line);
                timers.Add(0f);
            }
            AddLine(line);
            Vector3 dir = UnityEngine.Random.onUnitSphere;
            Vector3 startPoint = UnityEngine.Random.onUnitSphere * testLineLength;
            float t = 0f;
            for (int i = 0; i < testLinePoints; i++) {
                t = (float) i / (testLinePoints - 1f);
                line.points[i] = startPoint + dir * t;
            }
            line.Draw(Vector3.forward);
        }

        for (int i = 0; i < testLinePool.Count; i++) {
            if (testLinePool[i].index == -1) { continue; }    
            timers[i] += Time.deltaTime / lifeTime;
            testLinePool[i].SetColor(testLineColor.withAlpha(fadeCurve.Evaluate(timers[i])));
            if (timers[i] >= 1f) {
                RemoveLine(testLinePool[i]);
                timers[i] = 0f;
            }
        }

        if (autoUpdate) {
            UpdateMesh();
        }
    }

    public void UpdateMesh () {
        // if we have removed a line we need to assign vertices in triangles in opposite order,
        // otherwise Unity will get mad about it
        if (lastVertCount < vertices.Count) {
            mesh.SetVertices(this.vertices);
            mesh.SetTriangles(this.triangles, 0);            
        } else {
            mesh.SetTriangles(this.triangles, 0);
            mesh.SetVertices(this.vertices);
        }
        lastVertCount = vertices.Count;
        mesh.SetUVs(1, colors);
        mesh.RecalculateBounds();
        meshFilter.mesh = mesh;
    }
    public void AddLine (SharedLine line) {
        if (activeLineSet.Contains(line)) {
            UnityEngine.Debug.LogError("This line is already managed by this SharedLineRenderer!");
            return;
        }
        if (line.index > 0) {
            UnityEngine.Debug.LogError("This line is already managed by a different SharedLineRenderer!");
            return; 
        }
        if (line.pointCount < 2) {
            UnityEngine.Debug.LogError("Cannot draw a line with less than two points!");
            return;
        }

        int count = activeLineList.Count;
        int pointLowerBound = 0;
        int vertexLowerBound = 0;
        int triLowerBound = 0;
        int pointUpperBound = line.pointCount - 1;
        int vertexUpperBound = (line.pointCount * 2) - 1;
        int triUpperBound = ((line.pointCount - 1) * 6) - 1;
        if (count > 0) {
            SharedLine lastLine = activeLineList[count - 1]; 

            pointLowerBound = lastLine.points.upperBound + 1;
            vertexLowerBound = lastLine.vertices.upperBound + 1;
            triLowerBound = lastLine.triangles.upperBound + 1;

            pointUpperBound += pointLowerBound;
            vertexUpperBound += vertexLowerBound;  
            triUpperBound += triLowerBound;
        }
        for (int i = pointLowerBound, set = vertexLowerBound; i <= pointUpperBound; i++, set += 2) {            
            points.Add(Vector3.zero);
            
            vertices.Add(Vector3.zero);
            vertices.Add(Vector3.zero);

            colors.Add(Color.magenta); // haha
            colors.Add(Color.magenta);

            if (i == pointUpperBound) { break; }
            triangles.Add(set    );
            triangles.Add(set + 1);
            triangles.Add(set + 2);
            triangles.Add(set + 2);
            triangles.Add(set + 1);
            triangles.Add(set + 3);
        }
        line.points = new ListSlice<Vector3>(this.points, pointLowerBound, pointUpperBound);
        line.vertices = new ListSlice<Vector3>(this.vertices, vertexLowerBound, vertexUpperBound);
        line.colors = new ListSlice<Vector4>(this.colors, vertexLowerBound, vertexUpperBound);
        line.triangles = new ListSlice<int>(this.triangles, triLowerBound, triUpperBound);
        line.index = activeLineList.Count;
        activeLineList.Add(line);
        activeLineSet.Add(line);
    }

    public void RemoveLine (SharedLine line) {
        if (line.index == -1) {
            UnityEngine.Debug.LogError("This line is not currently managed by a SharedLineRenderer!");
            return; 
        }
        if (!activeLineSet.Contains(line)) {
            UnityEngine.Debug.LogError("This line is managed by a different SharedLineRenderer!");
            return;
        }
        for (int i = line.index + 1; i < activeLineList.Count; i++) {

            SharedLine otherLine = activeLineList[i];
            otherLine.points.lowerBound -= line.points.length;
            otherLine.points.upperBound -= line.points.length;


            otherLine.vertices.lowerBound -= line.vertices.length;
            otherLine.vertices.upperBound -= line.vertices.length;
            
            otherLine.colors.lowerBound -= line.colors.length;
            otherLine.colors.upperBound -= line.colors.length;

            otherLine.triangles.lowerBound -= line.triangles.length;
            otherLine.triangles.upperBound -= line.triangles.length;
            otherLine.index--;
        }

        points.RemoveRange(line.points.lowerBound, line.points.length);
        vertices.RemoveRange(line.vertices.lowerBound, line.vertices.length);
        colors.RemoveRange(line.colors.lowerBound, line.colors.length);
        int triangleShift = line.pointCount * 2;
        for (int i = line.triangles.upperBound + 1; i < triangles.Count; i++) {
            triangles[i] -= triangleShift;
        }
        triangles.RemoveRange(line.triangles.lowerBound, line.triangles.length);
        activeLineList.RemoveAt(line.index);
        activeLineSet.Remove(line);
        line.index = -1;
    }

    public void AddPoints (SharedLine line, int pointCount) {
        if (line.index == -1) {
            UnityEngine.Debug.LogError("This line is not currently managed by a SharedLineRenderer!");
            return; 
        }
        if (!activeLineSet.Contains(line)) {
            UnityEngine.Debug.LogError("This line is managed by a different SharedLineRenderer!");
            return;
        }
        int vertCount = pointCount * 2;
        int triCount = (pointCount - 1) * 6;
        for (int i = line.index + 1; i < activeLineList.Count; i++) {
            SharedLine otherLine = activeLineList[i];
            otherLine.points.lowerBound += pointCount;
            otherLine.points.upperBound += pointCount;
                
            otherLine.vertices.lowerBound += vertCount;
            otherLine.vertices.upperBound += vertCount;

            otherLine.colors.lowerBound += vertCount;
            otherLine.colors.upperBound += vertCount;

            otherLine.triangles.lowerBound += triCount;
            otherLine.triangles.upperBound += triCount;
        }

        // shift all preexisting triangles
        for (int i = line.triangles.upperBound + 1; i < triangles.Count; i++) {
            triangles[i] += vertCount;
        }

        int count = line.points.upperBound + pointCount;
        for (int pt = line.points.upperBound, vert = line.vertices.upperBound, tri = line.triangles.upperBound; pt <= count; pt++, vert += 2, tri += 6) {            
            points.Insert(pt, Vector3.zero);
            
            vertices.Insert(vert, Vector3.zero);
            vertices.Insert(vert + 1, Vector3.zero);

            colors.Insert(vert, Color.magenta);
            colors.Insert(vert + 1, Color.magenta);

            if (pt == count) break;
            triangles.Insert(tri,     vert    );
            triangles.Insert(tri + 1, vert + 1);
            triangles.Insert(tri + 2, vert + 2);
            triangles.Insert(tri + 3, vert + 2);
            triangles.Insert(tri + 4, vert + 1);
            triangles.Insert(tri + 5, vert + 3);
        }
        line.points.upperBound += pointCount;
        line.vertices.upperBound += vertCount;
        line.triangles.upperBound += triCount;
    }

    public void RemovePoints(SharedLine line, int pointCount) {
        if (line.index == -1) {
            UnityEngine.Debug.LogError("This line is not currently managed by a SharedLineRenderer!");
            return; 
        }
        if (!activeLineSet.Contains(line)) {
            UnityEngine.Debug.LogError("This line is managed by a different SharedLineRenderer!");
            return;
        }
        int vertCount = pointCount * 2;
        int triCount = (pointCount - 1) * 6;
        for (int i = line.index + 1; i < activeLineList.Count; i++) {
            SharedLine otherLine = activeLineList[i];
            otherLine.points.lowerBound -= pointCount;
            otherLine.points.upperBound -= pointCount;
                
            otherLine.vertices.lowerBound -= vertCount;
            otherLine.vertices.upperBound -= vertCount;

            otherLine.colors.lowerBound -= vertCount;
            otherLine.colors.upperBound -= vertCount;

            otherLine.triangles.lowerBound -= triCount;
            otherLine.triangles.upperBound -= triCount;
        }
        // shift all preexisting triangles
        for (int i = line.triangles.upperBound + 1; i < triangles.Count; i++) {
            triangles[i] -= vertCount;
        }

        int ptIdx = line.points.upperBound - pointCount;
        int vertIdx = line.vertices.upperBound - vertCount;
        int triIdx = line.triangles.upperBound - triCount;
        for (int i = line.points.upperBound - pointCount; i <= line.points.upperBound; i++) {            
            points.RemoveAt(ptIdx);
            vertices.RemoveRange(vertIdx, 2);
            colors.RemoveRange(vertIdx, 2);

            if (i == line.points.upperBound) break;
            triangles.RemoveRange(triIdx, 6);
        }
        line.points.upperBound -= pointCount;
        line.vertices.upperBound -= vertCount;
        line.triangles.upperBound -= triCount;
    }
}


[Serializable]
public class SharedLine {
    public int pointCount;
    public ListSlice<Vector3> points;
    public ListSlice<Vector3> vertices;
    public ListSlice<int> triangles;
    public ListSlice<Vector4> colors;
    public int index = -1;
    public float lineThickness;
    public bool isActive {
        get { return index > 0; }
    }
    public SharedLine (int pointCount, float lineThickness) {
        this.pointCount = pointCount;
        this.lineThickness = lineThickness;
    }
    public void SetPointColor (int pointIdx, Color c) {
        int colorIdx = pointIdx * 2;
        colors[colorIdx    ] = c;
        colors[colorIdx + 1] = c;
    }
    public void SetColor (Color c) {
        if (index == -1) { return; }
        for (int i = 0; i < colors.length; i++) {
            colors[i] = c;
        }
    }

    Vector3 curPt;
    Vector3 nextPt;
    Vector3 prevPt;
    int vIdx;
    int length;
    float miterX, miterY, miterZ;
    float dirX, dirY, dirZ;
    float abX, abY, abZ;
    float bcX, bcY, bcZ;
    public void Draw (Vector3 facing) {
        if (index == -1) {
            UnityEngine.Debug.LogError("This line is not currently managed by a SharedLineRenderer, call AddLine() on a SharedLineRenderer to add it!");
            return; 
        }
        // do start point
        curPt = points[0];
        nextPt = points[1];
        dirX = (nextPt.x - curPt.x);
        dirY = (nextPt.y - curPt.y);
        dirZ = (nextPt.z - curPt.z);
        FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
        FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
        miterX *= lineThickness;
        miterY *= lineThickness;
        miterZ *= lineThickness;

        vertices[0] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
        vertices[1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);

        // do end point
        length = points.length;
        curPt = points[length - 1];
        prevPt = points[length - 2];
        dirX = (curPt.x - prevPt.x);
        dirY = (curPt.y - prevPt.y);
        dirZ = (curPt.y - prevPt.z);
        FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
        FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
        miterX *= lineThickness;
        miterY *= lineThickness;
        miterZ *= lineThickness;

        vIdx = (length - 1) * 2;
        vertices[vIdx    ] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
        vertices[vIdx + 1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);
        
        // do all other points
        for (int i = 1; i < length - 1; i++) {
            vIdx = i * 2;
            curPt = points[i];
            nextPt = points[i + 1];
            prevPt = points[i - 1];
            
            abX = (curPt.x - prevPt.x);
            abY = (curPt.y - prevPt.y);
            abZ = (curPt.z - prevPt.z);

            bcX = (nextPt.x - curPt.x);
            bcY = (nextPt.y - curPt.y);
            bcZ = (nextPt.z - curPt.z);

            FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
            FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
            miterX *= lineThickness;
            miterY *= lineThickness;
            miterZ *= lineThickness;

            vertices[vIdx    ] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
            vertices[vIdx + 1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);
        }
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
