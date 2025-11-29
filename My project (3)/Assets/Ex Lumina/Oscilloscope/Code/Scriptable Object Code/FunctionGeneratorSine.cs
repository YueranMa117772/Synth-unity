using System;
using UnityEngine;

// Generator that produces a sine wave.

[CreateAssetMenu(fileName = "FGSine", menuName = "ExLumina/Generators/Sine", order = 30)]
public class FunctionGeneratorSine : FunctionGenerator
{
    // Return sine, scaling [0, 1] by [0, 2Pi]

    Func<float, float> signalAt = t => Mathf.Sin(2 * Mathf.PI * t);

    public override float SampleAt(float t)
    {
        return Amplitude * signalAt(mod1(Frequency * t));
    }
}
