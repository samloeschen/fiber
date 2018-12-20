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
        testLine0 = new SharedLine();
        testLine1 = new SharedLine();
        testLine0.points = new Vector3[testLinePoints];
        testLine0.widths = new float[testLinePoints];
        testLine0.facings = new Vector3[testLinePoints];
        AddLine(testLine0);
        // AddLine(testLine1);
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
        //         line.points = new Vector3[testLinePoints];
        //         testLinePool.Add(line);
        //         timers.Add(0f);
        //     }
        //     AddLine(line);
        //     Vector3 dir = UnityEngine.Random.onUnitSphere;
        //     Vector3 startPoint = UnityEngine.Random.onUnitSphere * testLineLength;
        //     float t = 0f;
        //     for (int i = 0; i < testLinePoints; i++) {
        //         t = (float) i / (testLinePoints - 1f);
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
                0f,//Mathf.Sin(UnityEngine.Time.time + (t * 2f)),
                0f
            );
            float angle = Mathf.Sin(t * 4f);
            testLine0.facings[i] = Quaternion.AngleAxis(angle * 20f, Vector3.right) * Vector3.forward;
            UnityEngine.Debug.DrawRay(testLine0.points[i], testLine0.facings[i] * 5f, Color.red);
            testLine0.widths[i] = testLineWidth;//Mathf.Lerp(0.1f, testLineWidth, (Mathf.Sin((Time.time * 2f) + (t * 4f)) + 1f) * 0.5f);
        }

        float a = Mathf.Sin(Time.time * 2f);
        Vector3 facing = Quaternion.AngleAxis(a * 20f, Vector3.right) * Vector3.forward;

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        testLine0.Draw(lineWidth: testLineWidth);
        stopwatch.Stop();
        UnityEngine.Debug.Log("draw ms: " + stopwatch.ElapsedTicks / 10000f);

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

            vertexLowerBound = lastLine.vertices.upperBound + 1;
            triLowerBound = lastLine.triangles.upperBound + 1;

            vertexUpperBound += vertexLowerBound;  
            triUpperBound += triLowerBound;
        }
        for (int pt = 0, vert = vertexLowerBound; pt < line.points.Length; pt++, vert += 2) {            
            points.Add(Vector3.zero);
            
            vertices.Add(Vector3.zero);
            vertices.Add(Vector3.zero);

            colors.Add(Color.magenta); // haha
            colors.Add(Color.magenta);

            if (pt == line.points.Length - 1) { break; }
            triangles.Add(vert    );
            triangles.Add(vert + 1);
            triangles.Add(vert + 2);
            triangles.Add(vert + 2);
            triangles.Add(vert + 1);
            triangles.Add(vert + 3);
        }
        
        line.vertices = new ListSlice<Vector3>(this.vertices, vertexLowerBound, vertexUpperBound);
        line.colors = new ListSlice<Vector4>(this.colors, vertexLowerBound, vertexUpperBound);
        line.triangles = new ListSlice<int>(this.triangles, triLowerBound, triUpperBound);
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
        for (int i = line.rendererIdx + 1; i < activeLineList.Count; i++) {

            SharedLine otherLine = activeLineList[i];

            otherLine.vertices.lowerBound -= line.vertices.length;
            otherLine.vertices.upperBound -= line.vertices.length;
            
            otherLine.colors.lowerBound -= line.colors.length;
            otherLine.colors.upperBound -= line.colors.length;

            otherLine.triangles.lowerBound -= line.triangles.length;
            otherLine.triangles.upperBound -= line.triangles.length;
            otherLine.rendererIdx--;
        }

        vertices.RemoveRange(line.vertices.lowerBound, line.vertices.length);
        colors.RemoveRange(line.colors.lowerBound, line.colors.length);
        int triangleShift = line.points.Length * 2;
        for (int i = line.triangles.upperBound + 1; i < triangles.Count; i++) {
            triangles[i] -= triangleShift;
        }
        triangles.RemoveRange(line.triangles.lowerBound, line.triangles.length);
        activeLineList.RemoveAt(line.rendererIdx);
        activeLineSet.Remove(line);
        line.rendererIdx = -1;
    }

    [Serializable]
    public class SharedLine {
    public Vector3[] points;
    public Vector3[] facings;
    public float[] widths;
    public ListSlice<Vector3> vertices;
    public ListSlice<int> triangles;
    public ListSlice<Vector4> colors;
    public int rendererIdx = -1;
    public bool isActive {
        get { return rendererIdx > 0; }
    }
    public void SetPointColor (int pointIdx, Color c) {
        int colorIdx = pointIdx * 2;
        colors[colorIdx    ] = c;
        colors[colorIdx + 1] = c;
    }
    public void SetColor (Color c) {
        if (rendererIdx == -1) { return; }
        for (int i = 0; i < colors.length; i++) {
            colors[i] = c;
        }
    }

    Vector3 curPt;
    Vector3 nextPt;
    Vector3 prevPt;
    Vector3 facing;
    float miterX, miterY, miterZ;
    float dirX, dirY, dirZ;
    float abX, abY, abZ;
    float bcX, bcY, bcZ;
    float lineWidth;
    int vIdx;
    int vertexCount;
    int pointCount;

    public void Draw () {
        if (rendererIdx == -1) {
            UnityEngine.Debug.LogError("This line is not currently managed by a SharedLineRenderer, call AddLine() on a SharedLineRenderer to add it.");
            return; 
        }
        if (points == null) {
            UnityEngine.Debug.LogError("The points array is null! You need to set up a points array to draw this line.");
            return;
        }
        if (facings == null) {
            UnityEngine.Debug.LogError("The facings array is null! You need to set up a facings array to draw this line without an explicit facing direction.");
            return;
        }
        if (widths == null) {
            UnityEngine.Debug.LogError("The widths array is null! You need to set up a widths array to draw this line without an explicit width.");
        }

        int vertexCount = vertices.length;
        int pointCount = vertexCount / 2;
        
        // do start point
        facing = facings[0];
        curPt = points[0];
        nextPt = points[1];
        lineWidth = widths[0];

        dirX = (nextPt.x - curPt.x);
        dirY = (nextPt.y - curPt.y);
        dirZ = (nextPt.z - curPt.z);
        FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
        FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
        miterX *= lineWidth;
        miterY *= lineWidth;
        miterZ *= lineWidth;

        vertices[0] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
        vertices[1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);

        // do end point
        facing = facings[pointCount - 1];
        curPt = points[pointCount - 1];
        prevPt = points[pointCount - 2];
        lineWidth = widths[pointCount - 1];

        dirX = (curPt.x - prevPt.x);
        dirY = (curPt.y - prevPt.y);
        dirZ = (curPt.y - prevPt.z);
        FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
        FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
        miterX *= lineWidth;
        miterY *= lineWidth;
        miterZ *= lineWidth;

        vIdx = vertexCount - 2;
        vertices[vIdx] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
        vertices[vIdx + 1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);
        
        // do all other points
        for (int i = 1; i < pointCount - 1; i++) {
            facing = facings[i];
            curPt = points[i];
            nextPt = points[i + 1];
            prevPt = points[i - 1];
            lineWidth = widths[i];
            
            abX = (curPt.x - prevPt.x);
            abY = (curPt.y - prevPt.y);
            abZ = (curPt.z - prevPt.z);

            bcX = (nextPt.x - curPt.x);
            bcY = (nextPt.y - curPt.y);
            bcZ = (nextPt.z - curPt.z);
            
            FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
            FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
            miterX *= lineWidth;
            miterY *= lineWidth;
            miterZ *= lineWidth;

            vIdx = i * 2;
            vertices[vIdx    ] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
            vertices[vIdx + 1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);
        }
    }

    public void Draw (Vector3 facing) {
        if (rendererIdx == -1) {
            UnityEngine.Debug.LogError("This line is not currently managed by a SharedLineRenderer, call AddLine() on a SharedLineRenderer to add it.");
            return; 
        }
        if (points == null) {
            UnityEngine.Debug.LogError("The points array is null! You need to set up a points array to draw this line.");
            return;
        }
        if (widths == null) {
            UnityEngine.Debug.LogError("The facings array is null! You need to set up a widths array to draw this line without an explicit line width.");
            return;
        }

        int vertexCount = vertices.length;
        int pointCount = vertexCount / 2;

        // do start point
        curPt = points[0];
        nextPt = points[1];
        lineWidth = widths[0];

        dirX = (nextPt.x - curPt.x);
        dirY = (nextPt.y - curPt.y);
        dirZ = (nextPt.z - curPt.z);
        FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
        FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
        miterX *= lineWidth;
        miterY *= lineWidth;
        miterZ *= lineWidth;

        vertices[0] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
        vertices[1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);

        // do end point
        curPt = points[pointCount - 1];
        prevPt = points[pointCount - 2];
        lineWidth = widths[pointCount - 1];
        dirX = (curPt.x - prevPt.x);
        dirY = (curPt.y - prevPt.y);
        dirZ = (curPt.y - prevPt.z);
        FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
        FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
        miterX *= lineWidth;
        miterY *= lineWidth;
        miterZ *= lineWidth;

        vIdx = (pointCount - 1) * 2;
        vertices[vIdx    ] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
        vertices[vIdx + 1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);
        
        // do all other points
        for (int i = 1; i < pointCount - 1; i++) {
            vIdx = i * 2;
            curPt = points[i];
            nextPt = points[i + 1];
            prevPt = points[i - 1];
            lineWidth = widths[i];
            
            abX = (curPt.x - prevPt.x);
            abY = (curPt.y - prevPt.y);
            abZ = (curPt.z - prevPt.z);

            bcX = (nextPt.x - curPt.x);
            bcY = (nextPt.y - curPt.y);
            bcZ = (nextPt.z - curPt.z);

            FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
            FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
            miterX *= lineWidth;
            miterY *= lineWidth;
            miterZ *= lineWidth;

            vertices[vIdx    ] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
            vertices[vIdx + 1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);
        }
    }

    public void Draw (float lineWidth) {
        if (rendererIdx == -1) {
            UnityEngine.Debug.LogError("This line is not currently managed by a SharedLineRenderer, call AddLine() on a SharedLineRenderer to add it.");
            return; 
        }
        if (points == null) {
            UnityEngine.Debug.LogError("The points array is null! You need to set up a points array to draw this line.");
            return;
        }
        if (facings == null) {
            UnityEngine.Debug.LogError("The facings array is null! You need to set up a facings array to draw this line without an explicit facing direction.");
            return;
        }

        int vertexCount = vertices.length;
        int pointCount = vertexCount / 2;

        // do start point
        curPt = points[0];
        nextPt = points[1];
        facing = facings[0];
        dirX = (nextPt.x - curPt.x);
        dirY = (nextPt.y - curPt.y);
        dirZ = (nextPt.z - curPt.z);
        FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
        FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
        miterX *= lineWidth;
        miterY *= lineWidth;
        miterZ *= lineWidth;

        vertices[0] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
        vertices[1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);

        // do end point
        curPt = points[pointCount - 1];
        prevPt = points[pointCount - 2];
        facing = facings[pointCount - 1];
        dirX = (curPt.x - prevPt.x);
        dirY = (curPt.y - prevPt.y);
        dirZ = (curPt.y - prevPt.z);
        FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
        FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
        miterX *= lineWidth;
        miterY *= lineWidth;
        miterZ *= lineWidth;

        vIdx = (pointCount - 1) * 2;
        vertices[vIdx    ] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
        vertices[vIdx + 1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);
        
        // do all other points
        for (int i = 1; i < pointCount - 1; i++) {
            vIdx = i * 2;
            curPt = points[i];
            nextPt = points[i + 1];
            prevPt = points[i - 1];
            facing = facings[i];
            
            abX = (curPt.x - prevPt.x);
            abY = (curPt.y - prevPt.y);
            abZ = (curPt.z - prevPt.z);

            bcX = (nextPt.x - curPt.x);
            bcY = (nextPt.y - curPt.y);
            bcZ = (nextPt.z - curPt.z);

            FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
            FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
            miterX *= lineWidth;
            miterY *= lineWidth;
            miterZ *= lineWidth;

            vertices[vIdx    ] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
            vertices[vIdx + 1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);
        }

        vertices[0] = vertices[2];
        vertices[1] = vertices[3];
        vertices[vertices.length - 1] = vertices[vertices.length - 3];
        vertices[vertices.length - 2] = vertices[vertices.length - 4];
    }

    public void Draw (Vector3 facing, float lineWidth) {
        if (rendererIdx == -1) {
            UnityEngine.Debug.LogError("This line is not currently managed by a SharedLineRenderer, call AddLine() on a SharedLineRenderer to add it!");
            return; 
        }
        if (points == null) {
            UnityEngine.Debug.LogError("The points array is null! You need to set up a points array to draw this line!");
            return;
        }
        int vertexCount = vertices.length;
        int pointCount = vertexCount / 2;

        // do start point
        curPt = points[0];
        nextPt = points[1];

        dirX = (nextPt.x - curPt.x);
        dirY = (nextPt.y - curPt.y);
        dirZ = (nextPt.z - curPt.z);
        FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
        FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
        miterX *= lineWidth;
        miterY *= lineWidth;
        miterZ *= lineWidth;

        vertices[0] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
        vertices[1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);

        // do end point
        curPt = points[pointCount - 1];
        prevPt = points[pointCount - 2];
        dirX = (curPt.x - prevPt.x);
        dirY = (curPt.y - prevPt.y);
        dirZ = (curPt.y - prevPt.z);
        FastCross(facing.x, facing.y, facing.z, dirX, dirY, dirZ, out miterX, out miterY, out miterZ);
        FastNormalize(miterX, miterY, miterZ, out miterX, out miterY, out miterZ);
        miterX *= lineWidth;
        miterY *= lineWidth;
        miterZ *= lineWidth;

        vIdx = (pointCount - 1) * 2;
        vertices[vIdx    ] = new Vector3(curPt.x - miterX, curPt.y - miterY, curPt.z - miterZ);
        vertices[vIdx + 1] = new Vector3(curPt.x + miterX, curPt.y + miterY, curPt.z + miterZ);
        
        // do all other points
        for (int i = 1; i < pointCount - 1; i++) {
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
            miterX *= lineWidth;
            miterY *= lineWidth;
            miterZ *= lineWidth;

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
}



