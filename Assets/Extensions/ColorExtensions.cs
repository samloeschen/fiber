using UnityEngine;
using System.Collections;

public static class ColorExtensions 
{
    public static Color withAlpha(this Color color, float alpha) {
        color.a = alpha;
        return color;
    }
    public static Color32 withAlpha(this Color32 color, byte alpha) {
        color.a = alpha;
        return color;
    }

    public static Color toVector4(this Color color) {
        return new Vector4(color.r, color.g, color.b, color.a);
    }
}
