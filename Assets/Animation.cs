using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animation : MonoBehaviour {

    public List<SharedLineRenderer.SharedLine> linePool;
    public List<SharedLineRenderer.SharedLine> activeLines;
    public Rect spawnArea;
    public int lineSmoothness;
    public float noiseFrequency;
    public float moveSpeed;
    public float pointDist;
    public float lineWidth;
    

    public SharedLineRenderer lineRenderer;

    
    void Update() {
        
        // update lines
        // temps
        Vector3 startPoint;
        Vector3 direction = Vector3.zero;
        Vector3 pointOffset;
        Vector3[] points;
        NoiseSample noiseSample;
        float angle;
        float len;

        for (int i = 0; i < activeLines.Count; i++) {
            points = activeLines[i].points;
            startPoint = points[0];
            noiseSample = Noise.Perlin2D(startPoint, noiseFrequency);
            angle = noiseSample.value * Mathf.PI * 2f / noiseFrequency;
            direction.x = Mathf.Cos(angle);
            direction.y = Mathf.Sin(angle);
            startPoint += direction * moveSpeed * Time.deltaTime;
            points[0] = startPoint;
            for (int j = 1; j < points.Length; j++) {
                pointOffset = points[j - 1] - points[j];
                len = pointOffset.magnitude;
                if (len > pointDist) {
                    points[j] += pointOffset.normalized * (len - pointDist);
                }
            }
            activeLines[i].Draw(lineWidth: lineWidth, facing: Vector3.forward);
        }


        if (Input.GetKeyDown(KeyCode.Space)) {
            int count = 1;//Random.Range(5, 15);
            for (int i = 0; i < count; i++) {
                SpawnLine();
            }
        }
    }



    void SpawnLine () {
        Vector3 spawnPos = spawnArea.RandomInRect();
        SharedLineRenderer.SharedLine line = null;
        for (int i = 0; i < linePool.Count; i++){
            if (!linePool[i].isActive) {
                line = linePool[i];
                break;
            }
        }
        if (line == null) {
            line = new SharedLineRenderer.SharedLine();
            line.points = new Vector3[lineSmoothness];
        }
        for (int i = 0; i < line.points.Length; i++) {
            line.points[i] = spawnPos;
        }
        activeLines.Add(line);
        lineRenderer.AddLine(line);
    }
}
