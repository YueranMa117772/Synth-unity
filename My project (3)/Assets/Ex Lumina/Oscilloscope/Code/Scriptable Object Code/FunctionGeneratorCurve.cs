using System;
using UnityEngine;

// Generator that uses an AnimationCurve.

[CreateAssetMenu(fileName = "FGCurve", menuName = "ExLumina/Generators/Curve", order = 30)]
public class FunctionGeneratorCurve : FunctionGenerator
{
    public AnimationCurve curve;

    public override float SampleAt(float t)
    {
        return Amplitude * curve.Evaluate(mod1(Frequency * t));
    }
}
