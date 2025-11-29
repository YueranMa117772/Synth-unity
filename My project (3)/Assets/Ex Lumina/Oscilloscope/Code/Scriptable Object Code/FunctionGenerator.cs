using System;
using UnityEngine;

// Subclass this abstract class to create a FunctionGenerator
// the Oscilliscope will use as its input signal.

public abstract class FunctionGenerator : ScriptableObject
{
    public float Frequency = 1;
    public float Amplitude = 1;

    // Convenience lambda to provide a real modulus operation,
    // not C#'s remainder % opertor.

    readonly protected Func<float, float> mod1 = t => ((t % 1) + 1) % 1;

    // Return a sample of the signal, where t=1 -> one full cycle.

    public abstract float SampleAt(float t);
}
