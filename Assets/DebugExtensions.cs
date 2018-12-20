using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class DebugExtensions : MonoBehaviour {
	public static void DrawRect(Rect r, Color color, float duration = 1f){
		Debug.DrawLine(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), color, duration);
		Debug.DrawLine(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), color, duration);
		Debug.DrawLine(new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), color, duration);
		Debug.DrawLine(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMin), color, duration);
	}
}
