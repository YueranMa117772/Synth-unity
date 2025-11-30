using UnityEngine;
using KnobsAsset;

public class TVFilterSliderBinder : MonoBehaviour
{
    public SliderKnob slider;               // 拖你的 3D slider 上来
    public AudioLowPassFilter filter;       // 拖 AudioLowPassFilter 上来

    [Header("Cutoff 范围（Hz）")]
    public float minCutoff = 500f;
    public float maxCutoff = 22000f;

    [Header("分档数量")]
    public int steps = 6;   // 6 档

    // SliderKnob 在每次滑动时会给一个 0~1 的 normalized 值
    public void OnSliderValueChanged(float normalized)
    {
        normalized = Mathf.Clamp01(normalized);

        // ---------- (1) 先做 6 档卡位 ----------
        if (steps > 1)
        {
            float stepIndex = Mathf.Round(normalized * (steps - 1));
            normalized = stepIndex / (steps - 1);   // 0, 0.2, 0.4, 0.6, 0.8, 1
        }

        // ---------- (2) 对数方式映射频率 ----------
        float logMin = Mathf.Log10(minCutoff);
        float logMax = Mathf.Log10(maxCutoff);

        float logF = Mathf.Lerp(logMin, logMax, normalized);
        float cutoff = Mathf.Pow(10f, logF);

        // ---------- (3) 写入滤波器 ----------
        if (filter != null)
        {
            filter.cutoffFrequency = cutoff;
        }
    }
}

