using System;
using UnityEngine;

// Generator that produces a square wave.

[CreateAssetMenu(fileName = "FGSquare", menuName = "ExLumina/Generators/Square", order = 31)]
public class FunctionGeneratorSquare : FunctionGenerator
{
    // Return square wave: +1 for first half, -1 for second half of cycle
    Func<float, float> signalAt = t => (t < 0.5f) ? 1f : -1f;

    public override float SampleAt(float t)
    {
        return Amplitude * signalAt(mod1(Frequency * t));
    }
}
