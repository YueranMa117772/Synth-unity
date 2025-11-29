namespace ExLumina.Assets.Oscilloscope
{
    using UnityEngine;
    using Unity.Collections;

    public class TraceParams
    {
        public int traceNum;
        public FunctionGenerator f;
        public NativeArray<byte> pixels;
        public float secondsPerSweep;
        public float beamWidth;
        public float fringeWidth;
        public int samples;
        public Color traceColor;
        public Color screenColor;
    }
}