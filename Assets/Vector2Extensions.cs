using UnityEngine;
using System.Collections;

public static class Vector2Extensions {

    public static Vector3 to3(this Vector2 vector) {
        return vector;
    }

    public static Vector3 to3(this Vector2 vector, float z) {
        return new Vector3(vector.x, vector.y, z);
    }

    public static Vector2 withX(this Vector2 vector, float x) {
        return new Vector2(x, vector.y);
    }

    public static Vector2 withY(this Vector2 vector, float y) {
        return new Vector2(vector.x, y);
    }

    public static Vector2 plusX(this Vector2 vector, float plusX) {
        return new Vector2(vector.x + plusX, vector.y);
    }

    public static Vector2 plusY(this Vector2 vector, float plusY) {
        return new Vector2(vector.x, vector.y + plusY);
    }

    public static Vector2 timesX(this Vector2 vector, float timesX) {
        return new Vector2(vector.x * timesX, vector.y);
    }

    public static Vector2 timesY(this Vector2 vector, float timesY) {
        return new Vector2(vector.x, vector.y * timesY);
    }

    public static Vector2 orthogonal(this Vector2 vector) {
        return new Vector2(-vector.y, vector.x);
    }
    public static Vector2 flipX(this Vector2 vector){
        return new Vector2(-vector.x, vector.y);
    }
    public static Vector2 flipY(this Vector2 vector){
        return new Vector2(vector.x, -vector.y);
    }

    public static Vector2Int floorToInt (this Vector2 vector) {
        return new Vector2Int(Mathf.FloorToInt(vector.x), Mathf.FloorToInt(vector.y));
    }
    
    public static Vector2Int ceilToInt (this Vector2 vector) {
        return new Vector2Int(Mathf.CeilToInt(vector.x), Mathf.CeilToInt(vector.y));
    }

    public static Vector2Int roundToInt (this Vector2 vector) {
        return new Vector2Int(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y));
    }

    public static Vector2 toViewportCoords (this Vector2 screenspaceCoords) {
        return new Vector2(screenspaceCoords.x / Screen.width, screenspaceCoords.y / Screen.height);
    }

	public static Vector2 Rotate(this Vector2 vector, float degrees) {
		float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
		float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
		
		float tx = vector.x;
		float ty = vector.y;
		vector.x = (cos * tx) - (sin * ty);
		vector.y = (sin * tx) + (cos * ty);
		return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
	}
}
