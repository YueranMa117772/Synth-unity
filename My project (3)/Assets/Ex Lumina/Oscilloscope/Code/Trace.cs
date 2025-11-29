namespace ExLumina.Assets.Oscilloscope
{
    using ExLumina.GraphicUtils.Lines;
    using ExLumina.GraphicUtils.Shared;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Unity.Collections;
    using UnityEngine;

    // THIS CLASS IS NOT THREAD SAFE.
    //
    // You can have an many oscilloscopes as you want, but no two of them can be
    // changing their trace on a background thread at the same time. If you want
    // to change traces on more than one, you must synchronize so that no other
    // oscilloscope adds a TraceParams object to the class's BlockingCollection
    // while it is in the middle of drawing a new trace.

    public static class Trace
    {
        // NOTE: An exact signal value of 1 would be on the line at the upper
        // limit. If the top margin is zero, only the lower half of the beam
        // and the entire lower fringe will be seen. It will clip the upper
        // half of the beam and all of the upper fringe. THIS IS CORRECT!
        // That's why we want to have some margin for this as well. It is not
        // right to think that when f(x) = 1 the center of the beam is still
        // inside the vertical drawing region. Without a margin, it would sit
        // at the upper boundary, which is NOT inside the pixel whose center
        // is just below it.

        public static int width;
        public static int height;
        public static BlockingCollection<TraceParams> traceInput = new BlockingCollection<TraceParams>();
        public static BlockingCollection<IntBox> traceOutput = new BlockingCollection<IntBox>();
        public static object bitmapLocker = new object();

        private static TraceParams useLastTrace = new TraceParams();

        public static void Run()
        {
            TraceParams tp;
            IntBox traceNum = new IntBox();

            // Block until we are signaled with a parameter object that it is time
            // to draw a new trace.

            while ((tp = traceInput.Take()) != null)
            {
                // Unique object indicates we should just use the last trace we drew.

                if (tp != useLastTrace)
                {
                    Draw(
                        tp.f,
                        tp.pixels,
                        tp.secondsPerSweep,
                        tp.beamWidth,
                        tp.fringeWidth,
                        tp.samples,
                        tp.traceColor,
                        tp.screenColor);
                }

                traceNum.num = tp.traceNum;
                traceOutput.Add(traceNum);
            }
        }

        public static void UseLastTrace()
        {
            traceInput.Add(useLastTrace);
        }

        internal static void Draw(
            FunctionGenerator f,
            NativeArray<byte> pixels,
            float secondsPerSweep,
            float beamWidth,
            float fringeWidth,
            int samples,
            Color traceColor,
            Color screenColor)
        {
            // Adjust the frequency to give us the number of cycles
            // in a sweep. (Times two for two screens' worth on a
            // a single double-width texture.)

            float frequency = f.Frequency;

            f.Frequency *= 2 * secondsPerSweep;

            // Fudge the amplitude to fit the grid.

            float amplitude = f.Amplitude;

            f.Amplitude *= 0.9296875f;

            List<Point2D> points = new List<Point2D>();

            int doubleSamples = samples * 2;

            float horizontalScale = (float)width / doubleSamples;

            // Fill the list with signal samples.

            for (int sample = 0; sample <= doubleSamples; sample = sample + 1)
            {
                float t = (float)sample / doubleSamples;

                float y = (height / 2.0f) * (1.0f + f.SampleAt(t));

                points.Add(new Point2D(horizontalScale * sample, y));
            }

            DrawLines.Soft(
                pixels,
                points,
                width,
                height,
                beamWidth,
                fringeWidth,
                JoinWith.Fillet,
                EndWith.Nothing,
                new RGB(traceColor.r, traceColor.g, traceColor.b),
                new RGB(screenColor.r, screenColor.g, screenColor.b));

            f.Frequency = frequency;
            f.Amplitude = amplitude;
        }
    }
}