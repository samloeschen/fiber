﻿using UnityEngine;
using System.Collections;

public static class Vector3Extensions {

    public static Vector2 to2(this Vector3 vector) {
        return vector;
    }

    public static Vector3 withX(this Vector3 vector, float x) {
        return new Vector3(x, vector.y, vector.z);
    }

    public static Vector3 withY(this Vector3 vector, float y) {
        return new Vector3(vector.x, y, vector.z);
    }

    public static Vector3 withZ(this Vector3 vector, float z) {
        return new Vector3(vector.x, vector.y, z);
    }

    public static Vector3 plusX(this Vector3 vector, float plusX) {
        return new Vector3(vector.x + plusX, vector.y, vector.z);
    }

    public static Vector3 plusY(this Vector3 vector, float plusY) {
        return new Vector3(vector.x, vector.y + plusY, vector.z);
    }

    public static Vector3 plusZ(this Vector3 vector, float plusZ) {
        return new Vector3(vector.x, vector.y, vector.z + plusZ);
    }

    public static Vector3 timesX(this Vector3 vector, float timesX) {
        return new Vector3(vector.x * timesX, vector.y, vector.z);
    }

    public static Vector3 timesY(this Vector3 vector, float timesY) {
        return new Vector3(vector.x, vector.y * timesY, vector.z);
    }

    public static Vector3 timesZ(this Vector3 vector, float timesZ) {
        return new Vector3(vector.x, vector.y, vector.z * timesZ);
    }

    public static Vector3 FromXZCircle(float angle, float radius, Vector3 origin) {
        return origin + (new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
    }

    public static Vector3 toViewportCoords (this Vector3 screenspaceCoords) {
        return new Vector3(screenspaceCoords.x / Screen.width, screenspaceCoords.y / Screen.height, screenspaceCoords.z);
    }
}
