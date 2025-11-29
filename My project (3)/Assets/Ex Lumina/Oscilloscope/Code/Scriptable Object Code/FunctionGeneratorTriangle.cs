using System;
using UnityEngine;

// Generator that produces a triangle wave.

[CreateAssetMenu(fileName = "FGTriangle", menuName = "ExLumina/Generators/Triangle", order = 30)]
public class FunctionGeneratorTriangle : FunctionGenerator
{
    Func<float, float> signalAt = t =>
    {
        // First quarter, rise 0 to 1

        if (t < .25f)
        {
            return t * 4;
        }

        // Fourth quarter, rise -1 to 0

        if (t < .75f)
        {
            return 1 - 4 * (t - .25f);
        }

        // Second and third quarters,
        // fall 1 to -1

        return 4 * (t - .75f) - 1;
    };

    public override float SampleAt(float t)
    {
        return Amplitude * signalAt(mod1(Frequency * t));
    }
}
