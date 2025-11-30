namespace ExLumina.Assets.Oscilloscope
{
    using System.Collections;
    using System.Threading;
    using Unity.Collections;
    using UnityEngine;

    public class Oscilloscope : MonoBehaviour
    {
        const float flickerTime = 1f / 25f;
        const byte noGlow = 0x22;

        const int numberVerticalDivs = 8;
        const int numberHorizontalDivs = 10;

        [Space(10, order = 0)]
        [Header("Change these parameters to suit your project.", order = 1)]
        [Space(10, order = 2)]

        [Tooltip("Power on or off as start")]
        public bool isOnAtStart = true;

        [Tooltip("Glowing phosphor color")]
        public Color traceColor = new Color(0, 1, 0, 1);
        [Tooltip("Dark phosphor color")]
        public Color screenColor = new Color(0, noGlow / 255f, 0, 1);

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

        [Tooltip("A subsclass of FunctionGenerator")]
        public FunctionGenerator generator;

        // 滑块选择的波形列表
        [Tooltip("Waveforms to select via slider (0..1 maps across this array)")]
        public FunctionGenerator[] sliderWaveforms;

        [Tooltip("Frequency of the input signal")]
        public float generatorFrequency = 1;
        [Tooltip("Multiplies the signal level by this value")]
        public float generatorAmplitude = 1;

        [Tooltip("True = trace does not drift")]
        public bool alwaysPhaseLock = false;
        [Tooltip("Where in one cycle to start")]
        public float phaseAngle = 0;

        [Space(10, order = 0)]
        [Header("Change these if necessary (unlikely).", order = 1)]
        [Space(10, order = 2)]

        [Tooltip("Phosphor persistence")]
        public float rollOff = 2;

        [Tooltip("Longer sweeps than this will not drift")]
        public float lockSweepSeconds = 60;

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

        // ★ 新增：暂停状态 + 自己的时间轴
        bool isPaused = false;
        float sweepClock = 0f;

        public float SecondsPerDiv
        {
            get => 1f / (numberHorizontalDivs * sweepFrequency);
            set => sweepFrequency = 1f / (numberHorizontalDivs * value);
        }

        public float SweepFrequency
        {
            get => sweepFrequency;
            set => sweepFrequency = value;
        }

        void Start()
        {
            gridMaterial = gridGO.GetComponent<Renderer>().material;

            movingQuadTransform = movingGO.transform;
            movingQuadScale = movingQuadTransform.localScale;

            movingQuadMaterial = movingGO.GetComponent<Renderer>().material;
            movingQuadOffset = movingQuadMaterial.mainTextureOffset;
            movingQuadTiling = movingQuadMaterial.mainTextureScale;

            fixedQuadMaterial = fixedGO.GetComponent<Renderer>().material;
            fixedQuadOffset = fixedQuadMaterial.mainTextureOffset;

            fadeQuadMaterial = faderGO.GetComponent<Renderer>().material;
            fadeQuadOffset = fadeQuadMaterial.mainTextureOffset;

            outerGO.GetComponent<Renderer>().material.color = screenColor;
            curtainQuadMaterial = curtainGO.GetComponent<Renderer>().material;
            curtainQuadMaterial.color = screenColor;

            fixedQuadMaterial.mainTexture = new Texture2D(
                textureSize * 2,
                textureSize,
                TextureFormat.RGBA32,
                true);

            movingQuadMaterial.mainTexture = fixedQuadMaterial.mainTexture;

            workingBuffer = new Texture2D(
                textureSize * 2,
                textureSize,
                TextureFormat.RGBA32,
                true);

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

            blankScreen.Apply();

            phosphorFadeTexture = new Texture2D(
                textureSize,
                textureSize,
                TextureFormat.RGBA32,
                true);

            fadeQuadMaterial.mainTexture = phosphorFadeTexture;

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

            lockSweepRate = 1f / lockSweepSeconds;

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
                    sweepClock = 0f;              // 新 trace 从 0 开始

                    generatorFrequencyCopy = generatorFrequency;
                    phaseAngleCopy = phaseAngle;
                    generator.Frequency = generatorFrequency;
                    generator.Amplitude = generatorAmplitude * 2 / (numberVerticalDivs * voltsPerDiv);

                    Graphics.CopyTexture(blankScreen, workingBuffer);

                    Trace.traceInput.Add(new TraceParams
                    {
                        traceNum = traceNum,
                        f = generator,
                        pixels = workingBuffer.GetRawTextureData<byte>(),
                        secondsPerSweep = secondsPerSweep,
                        beamWidth = beamWidth,
                        fringeWidth = fringeWidth,
                        samples = samples,
                        traceColor = traceColor,
                        screenColor = screenColor
                    });
                }

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
            if (!scopeIsOn)
            {
                return;
            }

            scopeIsOn = false;
            isPaused = false;

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

        // 滑块切波形
        public void SetWaveformFromSlider(float t)
        {
            if (sliderWaveforms == null || sliderWaveforms.Length == 0)
            {
                return;
            }

            t = Mathf.Clamp01(t);

            int lastIndex = sliderWaveforms.Length - 1;
            int index = Mathf.RoundToInt(t * lastIndex);
            if (index < 0) index = 0;
            if (index > lastIndex) index = lastIndex;

            var newGen = sliderWaveforms[index];
            if (newGen == null || newGen == generator)
            {
                return;
            }

            generator = newGen;

            if (scopeIsOn)
            {
                On(false);
            }
        }

        // ★ 暂停 / 恢复：只控制 isPaused，不停协程
        public void TogglePause()
        {
            if (!scopeIsOn)
            {
                return;
            }

            isPaused = !isPaused;
        }

        IEnumerator WaitForTrace(int traceNum)
        {
            IntBox traceReady;

            while (!Trace.traceOutput.TryTake(out traceReady))
            {
                yield return null;
            }

            workingBuffer.Apply();
            Graphics.CopyTexture(workingBuffer, fixedQuadMaterial.mainTexture);

            curtainQuadMaterial.color = Color.clear;

            phiMoving = 0;
            phiFixed = 0.5f * (secondsPerSweep % (1.0f / generatorFrequencyCopy))
                            / secondsPerSweep;

            fixedQuadOffset.x = phiFixed;
            fixedQuadMaterial.mainTextureOffset = fixedQuadOffset;

            StopCoroutines();
            isPhaseLocked = false;

            if (!isPaused)
            {
                StartAnimationCoroutines();
            }

            scopeIsOn = true;
            noTraceInTheWorks = true;

            if (tracePending)
            {
                On(useLastTrace);
                tracePending = false;
            }
        }

        // ★ 抽出来的协程启动逻辑
        void StartAnimationCoroutines()
        {
            sweepCount = 1;

            if (secondsPerSweep < flickerTime)
            {
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
        }

        // 带余辉动画的 trace
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
                if (isPaused)
                {
                    yield return null;
                    continue;
                }

                if (!forcePhaseLock)
                {
                    int sNew = 1 + (int)(sweepClock / secondsPerSweep);

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

                float xSweep = (sweepClock % secondsPerSweep) / secondsPerSweep;

                movingQuadScale.x = 1f - xSweep;
                movingQuadTransform.localScale = movingQuadScale;

                movingQuadOffset.x = phiMoving + xSweep / 2f;
                movingQuadMaterial.mainTextureOffset = movingQuadOffset;

                movingQuadTiling.x = (1f - xSweep) / 2f;
                movingQuadMaterial.mainTextureScale = movingQuadTiling;

                fadeQuadOffset.x = 1f - xSweep + .001f;
                fadeQuadMaterial.mainTextureOffset = fadeQuadOffset;

                sweepClock += Time.deltaTime;

                yield return null;
            }
        }

        // 只做相位漂移的 trace（没有显著余辉）
        IEnumerator PhaseDriftQuad()
        {
            PhaseLockQuad();

            while (true)
            {
                if (isPaused)
                {
                    yield return null;
                    continue;
                }

                sweepClock += Time.deltaTime;

                phiMoving = ((sweepClock - (sweepClock % secondsPerSweep)) % (1f / generatorFrequencyCopy)) / secondsPerSweep;

                movingQuadOffset.x = 0.5f * phiMoving;
                movingQuadMaterial.mainTextureOffset = movingQuadOffset;

                yield return null;
            }
        }

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
