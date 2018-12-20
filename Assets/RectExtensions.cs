using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RectExtensions {
	public static Vector2 RandomInRect(this Rect r) {
		float x = Random.Range(r.xMin, r.xMax);
		float y = Random.Range(r.yMin, r.yMax);
		return new Vector2(x,y);
	}

	public static Rect withCenter(this Rect r, Vector2 center) {
		Rect rect = new Rect(r);
		rect.center = center;
		return rect;
	}

	public static Rect FromPoints (List<Vector2> points) {
		if (points.Count == 0) return new Rect();

		Vector2 p = points[0];
		Rect r = new Rect(p.x, p.y, 0, 0);
		for (int i = 0; i < points.Count; i++) {
			p = points[i];

			r.xMin = Mathf.Min(r.xMin, p.x);
            r.yMin = Mathf.Min(r.yMin, p.y);

            r.xMax = Mathf.Max(r.xMax, p.x);
            r.yMax = Mathf.Max(r.yMax, p.y);	
		}
		return r;
	}

	public static Rect FromPoints (Vector2[] points, int pointCount) {
		pointCount = Mathf.Min(pointCount, points.Length);
		if (points.Length == 0) return new Rect();

		Vector2 p = points[0];
		Rect r = new Rect(p.x, p.y, 0, 0);
		for (int i = 0; i < pointCount; i++) {
			p = points[i];

			r.xMin = Mathf.Min(r.xMin, p.x);
            r.yMin = Mathf.Min(r.yMin, p.y);

            r.xMax = Mathf.Max(r.xMax, p.x);
            r.yMax = Mathf.Max(r.yMax, p.y);	
		}
		return r;
	}
}
