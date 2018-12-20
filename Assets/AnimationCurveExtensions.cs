using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AnimationCurveExtensions {
    public static float RejectionSampleCurve(this AnimationCurve curve, int tries = 1000) {
        int count = 0;
        while (true && count < tries){
            float sample = Random.value;
            if (curve.Evaluate(sample) >= Random.value) {
                return sample;
            }
            count++;
        }
        return 0f;
    }
}
