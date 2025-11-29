namespace ExLumina.Assets.Oscilloscope
{
    using System.Collections;
    using System.Threading;
    using Unity.Collections;
    using UnityEngine;

    public class Oscilloscope : MonoBehaviour
    {
        // Below this frame time, draw a solid trace, not animated.

        const float flickerTime = 1f / 25f;

        // "Off" intensity.

        const byte noGlow = 0x22;

        // Grid dimensions.

        const int numberVerticalDivs = 8;
        const int numberHorizontalDivs = 10;

        // These are the parameters most likely to be something the
        // user or client code will want to change.

        [Space(10, order = 0)]
        [Header("Change these parameters to suit your project.", order = 1)]
        [Space(10, order = 2)]

        [Tooltip("Power on or off as start")]
        public bool isOnAtStart = true;

        [Tooltip("Glowing phosphor color")]
        public Color traceColor = new Color(0, 1, 0, 1);
        [Tooltip("Dark phosphor color")]
        public Color screenColor = new Color(0, noGlow / 255f, 0, 1);

        // Time in seconds for the screen fade out when going off.

        [Tooltip("Seconds to fade out on power off")]
        public float fadeOutTime = 0.5f;

        [Tooltip("Size of the bitmap that holds the trace image")]
        public int textureSize = 512;
        [Tooltip("Number of signal samples in one trace")]
        public int samples = 512;

        [Tooltip("Pixel width of the trace center")]
        public float beamWidth = 1.0f;
        [Tooltip("Pixel width of the trace edges")]
        public float fringeWidth = 1.5f;

        [Tooltip("Sweeps per second")]
        public float sweepFrequency = 40;

        [Tooltip("Volts per grid division")]
        public float voltsPerDiv = .25f;

        // A ScriptableObject subclassed from FunctionGenerator.

        [Tooltip("A subsclass of FunctionGenerator")]
        public FunctionGenerator generator;

        [Tooltip("Frequency of the input signal")]
        public float generatorFrequency = 1;
        [Tooltip("Multiplies the signal level by this value")]
        public float generatorAmplitude = 1;

        // "Locked" means always starting at the same point in the
        // signal.

        [Tooltip("True = trace does not drift")]
        public bool alwaysPhaseLock = false;
        [Tooltip("Where in one cycle to start")]
        public float phaseAngle = 0;

        [Space(10, order = 0)]
        [Header("Change these if necessary (unlikely).", order = 1)]
        [Space(10, order = 2)]

        // Phosphor fade rate, as an exponent.

        [Tooltip("Phosphor persistence")]
        public float rollOff = 2;

        // Sweep time above this always phase locks.

        [Tooltip("Longer sweeps than this will not drift")]
        public float lockSweepSeconds = 60;

        // These are just for making it easy to find the prefabs
        // internal GameObjects.

        [Space(10, order = 0)]
        [Header("Do not change these settings.", order = 1)]
        [Space(10, order = 2)]

        public GameObject outerGO;
        public GameObject fixedGO;
        public GameObject movingGO;
        public GameObject faderGO;
        public GameObject curtainGO;
        public GameObject gridGO;

        public bool isPhaseLocked = false;

        float lockSweepRate;

        Transform movingQuadTransform;
        Vector3 movingQuadScale;

        Material movingQuadMaterial;
        Vector2 movingQuadOffset;
        Vector2 movingQuadTiling;

        Material fixedQuadMaterial;
        Vector2 fixedQuadOffset;

        Material fadeQuadMaterial;
        Vector2 fadeQuadOffset;

        Material curtainQuadMaterial;

        float phiMoving;
        float phiFixed;

        float generatorFrequencyCopy;
        float secondsPerSweep;

        float phaseAngleCopy;

        // 本地时间：用它代替 Time.time，这样暂停时不会“跳帧”
        float scopeTime;

        bool useLastTrace;
        bool thereIsALastTrace = false;

        bool noTraceInTheWorks = true;
        bool tracePending = false;

        int sweepCount; // number of sweeps

        Texture2D workingBuffer;
        Texture2D blankScreen;
        Texture2D phosphorFadeTexture;

        Material gridMaterial;

        string warning =
            "Sweep frequency higher than generator frequency.\n" +
            "This will probably not create a successful fading\n" +
            "or moving trace. Set the sweep frequency at or below\n" +
            "the generator frequency, or be sure the sweep frequency\n " +
            string.Format(
                "is above {0}Hz and the trace is phase locked.",
                1f / flickerTime);

        Coroutine fadingTrace;
        Coroutine phaseDriftQuad;
        Coroutine lowerTheCurtain;

        bool scopeIsOn;

        // 暂停标志：为 true 时动画停在当前帧
        [Tooltip("Pause the oscilloscope animation without turning it off")]
        public bool pauseAnimation = false;

        // Sweep time in seconds per division an sweep frequency are
        // linked. A perfect place to use properties instead of fields.

        public float SecondsPerDiv
        {
            get => 1f / (numberHorizontalDivs * sweepFrequency);

            set
            {
                sweepFrequency = 1f / (numberHorizontalDivs * value);
            }
        }

        public float SweepFrequency
        {
            get => sweepFrequency;

            set
            {
                sweepFrequency = value;
            }
        }

        void Start()
        {
            // Keep a pointer to the grid quad's own material so we can
            // turn the scope on and off.

            gridMaterial = gridGO.GetComponent<Renderer>().material;

            // Pull out pointers to the transforms and offsets we will
            // use for animation, as well as the Vector3 and Vector2
            // structures we need to use to update those transforms.

            movingQuadTransform = movingGO.transform;
            movingQuadScale = movingQuadTransform.localScale;

            movingQuadMaterial = movingGO.GetComponent<Renderer>().material;
            movingQuadOffset = movingQuadMaterial.mainTextureOffset;
            movingQuadTiling = movingQuadMaterial.mainTextureScale;

            fixedQuadMaterial = fixedGO.GetComponent<Renderer>().material;
            fixedQuadOffset = fixedQuadMaterial.mainTextureOffset;

            fadeQuadMaterial = faderGO.GetComponent<Renderer>().material;
            fadeQuadOffset = fadeQuadMaterial.mainTextureOffset;

            // Make the outer edge and curtain meshes use the screen color.

            outerGO.GetComponent<Renderer>().material.color = screenColor;
            curtainQuadMaterial = curtainGO.GetComponent<Renderer>().material;
            curtainQuadMaterial.color = screenColor; // Color.black;
                                                     //curtainQuadMaterial.SetColor("_EmissionColor", screenColor);

            // Swap in a texture we create now so changes will not appear
            // to be persistent if we are in edit mode.

            fixedQuadMaterial.mainTexture = new Texture2D(
                textureSize * 2,
                textureSize,
                TextureFormat.RGBA32,
                true);

            movingQuadMaterial.mainTexture = fixedQuadMaterial.mainTexture;

            // Make a texture to use as a working buffer so we can clear
            // it and draw the new trace, taking as long as necessary, while
            // leaving the existing trace in place. When we're done drawing
            // the new one, we'll copy it in with a single API call.

            workingBuffer = new Texture2D(
                textureSize * 2,
                textureSize,
                TextureFormat.RGBA32,
                true);

            // Make a blank Texture2D in the background color so we can
            // clear the working buffer with a single API call.

            blankScreen = new Texture2D(
                textureSize * 2,
                textureSize,
                TextureFormat.RGBA32,
                true);

            NativeArray<uint> pixels = blankScreen.GetRawTextureData<uint>();

            uint fillColor =
                (uint)((Mathf.RoundToInt(screenColor.a * 255) << 24) |
                       (Mathf.RoundToInt(screenColor.b * 255) << 16) |
                       (Mathf.RoundToInt(screenColor.g * 255) << 8) |
                        Mathf.RoundToInt(screenColor.r * 255));

            for (int pixelIndex = 0; pixelIndex < pixels.Length; ++pixelIndex)
            {
                pixels[pixelIndex] = fillColor;
            }

            blankScreen.Apply(); // This does not appear to be needed. How come?

            // Create the fade texture.

            phosphorFadeTexture = new Texture2D(
                textureSize,
                textureSize,
                TextureFormat.RGBA32,
                true);

            fadeQuadMaterial.mainTexture = phosphorFadeTexture;

            // Set the fade texture to the screen color now too.

            pixels = phosphorFadeTexture.GetRawTextureData<uint>();

            for (int col = 0; col < phosphorFadeTexture.width; ++col)
            {
                float alpha = 1f - (float)col / (phosphorFadeTexture.width - 1);

                alpha = Mathf.Pow(alpha, rollOff);

                uint alphaUint = (uint)Mathf.RoundToInt(255 * alpha) << 24;

                for (int row = 0; row < phosphorFadeTexture.height; ++row)
                {
                    pixels[row * phosphorFadeTexture.width + col] =
                           (fillColor & 0x00FFFFFF) | alphaUint;
                }
            }

            phosphorFadeTexture.Apply();

            // Let the user pick slow-enough-to-lock in seconds, convert here to
            // a rate.

            lockSweepRate = 1f / lockSweepSeconds;

            // As Trace is a static class, there is no instance to construct and no
            // constructor. Perhaps there should be an instance and we should be
            // passing pixels, width, and height to a constructor?

            Trace.width = textureSize * 2;
            Trace.height = textureSize;

            Off();

            new Thread(Trace.Run).Start();

            if (isOnAtStart)
            {
                On();
            }
        }

        int traceNum = 1;

        public void On(bool useLastTrace = false)
        {
            this.useLastTrace = useLastTrace;

            if (noTraceInTheWorks)
            {
                noTraceInTheWorks = false;

                if (useLastTrace && thereIsALastTrace)
                {
                    Trace.UseLastTrace();
                }
                else
                {

                    thereIsALastTrace = true;

                    secondsPerSweep = 1f / sweepFrequency;

                    // Make a defensive copy so the trace animators can use
                    // it without being affected by changes in the public
                    // generator frequency.

                    generatorFrequencyCopy = generatorFrequency;
                    phaseAngleCopy = phaseAngle;
                    generator.Frequency = generatorFrequency;
                    generator.Amplitude = generatorAmplitude * 2 / (numberVerticalDivs * voltsPerDiv);

                    // Wipe the working buffer clean. (Must call this from main thread.)

                    Graphics.CopyTexture(blankScreen, workingBuffer);

                    // Send a parameter object to the blocked drawing thread, so
                    // it can start creating the trace.

                    Trace.traceInput.Add(new TraceParams
                    {
                        traceNum = traceNum,
                        f = generator,

                        // Must call this from main thread.

                        pixels = workingBuffer.GetRawTextureData<byte>(),
                        secondsPerSweep = secondsPerSweep,
                        beamWidth = beamWidth,
                        fringeWidth = fringeWidth,
                        samples = samples,
                        traceColor = traceColor,
                        screenColor = screenColor
                    });
                }

                // Start coroutine that will use the finished trace after
                // it has been drawn by another thread.

                StartCoroutine(WaitForTrace(traceNum));

                traceNum = traceNum + 1;
            }
            else
            {
                tracePending = true;
            }
        }

        public void Off()
        {
            if (scopeIsOn == false)
            {
                return;
            }

            scopeIsOn = false;

            // 关机时顺便取消暂停状态
            pauseAnimation = false;

            StopCoroutines();

            lowerTheCurtain = StartCoroutine(LowerTheCurtain());

            isPhaseLocked = false;
        }

        public void LoadNew()
        {
            if (scopeIsOn)
            {
                On();
            }
        }

        IEnumerator WaitForTrace(int traceNum)
        {
            IntBox traceReady;

            // TODO: use a "yield return until" here.

            while (!Trace.traceOutput.TryTake(out traceReady))
            {
                yield return null;
            }

            workingBuffer.Apply();
            Graphics.CopyTexture(workingBuffer, fixedQuadMaterial.mainTexture);

            // Raise the curtain.

            curtainQuadMaterial.color = Color.clear;

            // Figure the phase drift, if any.

            phiMoving = 0;
            phiFixed = 0.5f * (secondsPerSweep % (1.0f / generatorFrequencyCopy))
                            / secondsPerSweep; // (1.0f / generatorFrequency);

            fixedQuadOffset.x = phiFixed;
            fixedQuadMaterial.mainTextureOffset = fixedQuadOffset;

            sweepCount = 1;

            StopCoroutines();

            isPhaseLocked = false;

            // 每次开始新的 trace 时，重置本地时间
            scopeTime = 0f;

            if (secondsPerSweep < flickerTime)
            {
                // Is it drifting slow enough to regard as locked?

                float cyclesPerSweep = secondsPerSweep * generatorFrequencyCopy;

                float partialCyclesPerSweep = Mathf.Abs(Mathf.Round(cyclesPerSweep) - cyclesPerSweep);

                float partialSweepsPerSweep = partialCyclesPerSweep / cyclesPerSweep;

                float driftSweepsPerSecond = partialSweepsPerSweep / secondsPerSweep;

                if (alwaysPhaseLock || driftSweepsPerSecond <= lockSweepRate)
                {
                    isPhaseLocked = true;

                    PhaseLockQuad();
                }
                else
                {
                    phaseDriftQuad = StartCoroutine(PhaseDriftQuad());
                }
            }
            else
            {
                fadingTrace = StartCoroutine(FadingTrace(alwaysPhaseLock));
            }

            scopeIsOn = true;
            noTraceInTheWorks = true;

            if (tracePending)
            {
                On(useLastTrace);
                tracePending = false;
            }
        }

        // Draw the trace with fading phosphor.

        IEnumerator FadingTrace(bool forcePhaseLock)
        {
            faderGO.SetActive(true);

            if (forcePhaseLock)
            {
                isPhaseLocked = true;

                phiMoving = phaseAngleCopy / 2f;
                phiFixed = phiMoving;

                fixedQuadOffset.x = phiFixed;
                fixedQuadMaterial.mainTextureOffset = fixedQuadOffset;
            }

            while (true)
            {
                // 暂停：不更新时间、不更新画面
                if (pauseAnimation)
                {
                    yield return null;
                    continue;
                }

                // 只有没暂停时才推进本地时间
                scopeTime += Time.deltaTime;

                if (!forcePhaseLock)
                {
                    int sNew = 1 + (int)(scopeTime / secondsPerSweep);

                    if (sweepCount != sNew)
                    {
                        sweepCount = sNew;

                        phiMoving = phiFixed;
                        phiFixed = 0.5f * ((sweepCount * secondsPerSweep) % (1.0f / generatorFrequencyCopy))
                                        / secondsPerSweep;

                        fixedQuadOffset.x = phiFixed;
                        fixedQuadMaterial.mainTextureOffset = fixedQuadOffset;
                    }
                }

                // Figure horzontal beam location on [0, 1).

                float xSweep = (scopeTime % secondsPerSweep) / secondsPerSweep;

                movingQuadScale.x = 1f - xSweep;
                movingQuadTransform.localScale = movingQuadScale;

                movingQuadOffset.x = phiMoving + xSweep / 2f;
                movingQuadMaterial.mainTextureOffset = movingQuadOffset;

                movingQuadTiling.x = (1f - xSweep) / 2f;
                movingQuadMaterial.mainTextureScale = movingQuadTiling;

                fadeQuadOffset.x = 1f - xSweep + .001f; // a bit leaks out
                fadeQuadMaterial.mainTextureOffset = fadeQuadOffset;

                yield return null;
            }
        }

        // Trace too fast to show fade, but animate phase drift.

        IEnumerator PhaseDriftQuad()
        {
            PhaseLockQuad();

            while (true)
            {
                if (pauseAnimation)
                {
                    yield return null;
                    continue;
                }

                // 同样用本地时间推进
                scopeTime += Time.deltaTime;

                // 去掉 sweep 内的小数部分，相当于取到“当前 sweep 的起点”
                float tWithoutFraction = scopeTime - (scopeTime % secondsPerSweep);

                phiMoving = (tWithoutFraction % (1f / generatorFrequencyCopy)) / secondsPerSweep;

                movingQuadOffset.x = 0.5f * phiMoving;
                movingQuadMaterial.mainTextureOffset = movingQuadOffset;

                yield return null;
            }
        }

        // Fade out the image on power off.

        IEnumerator LowerTheCurtain()
        {
            float alpha = 0;

            float t0 = Time.time;

            Color color = screenColor;

            while (alpha < 1)
            {
                yield return null;

                float dt = Time.time - t0;

                alpha = dt / fadeOutTime;

                color.a = alpha > 1 ? 1 : alpha;

                curtainQuadMaterial.color = color;
            }

            lowerTheCurtain = null;
        }

        // No fade or drift, so just draw the trace once.

        void PhaseLockQuad()
        {
            faderGO.SetActive(false);

            fadeQuadOffset.x = 0;
            fadeQuadMaterial.mainTextureOffset = fadeQuadOffset;

            movingQuadScale.x = 1;
            movingQuadTransform.localScale = movingQuadScale;

            movingQuadOffset.x = phaseAngleCopy / 2f;
            movingQuadMaterial.mainTextureOffset = movingQuadOffset;

            movingQuadTiling.x = 0.5f;
            movingQuadMaterial.mainTextureScale = movingQuadTiling;
        }

        // ===== 外部控制接口：给 InteractableGeneral / UI 按钮调用 =====

        // 切换暂停/恢复
        public void TogglePause()
        {
            pauseAnimation = !pauseAnimation;
        }

        // 强制暂停
        public void Pause()
        {
            pauseAnimation = true;
        }

        // 强制恢复
        public void Resume()
        {
            pauseAnimation = false;
        }

        // 直接设置暂停状态（适合 UI Toggle）
        public void SetPause(bool value)
        {
            pauseAnimation = value;
        }

        // Coroutines are not separate threads. Stopping them here just
        // means they are removed from the running list. This happens
        // when they have all yielded.

        void StopCoroutines()
        {
            if (phaseDriftQuad != null)
            {
                StopCoroutine(phaseDriftQuad);
                phaseDriftQuad = null;
            }

            if (fadingTrace != null)
            {
                StopCoroutine(fadingTrace);
                fadingTrace = null;
            }

            if (lowerTheCurtain != null)
            {
                StopCoroutine(lowerTheCurtain);
                lowerTheCurtain = null;
            }
        }
    }
}
