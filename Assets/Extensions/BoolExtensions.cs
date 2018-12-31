using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoolExtensions {

	public static float toSignedFloat (this bool b) {
		return b ? 1 : -1f;
	}
	public static float toFloat (this bool b) {
		return b ? 1f : 0f;
	}
}
