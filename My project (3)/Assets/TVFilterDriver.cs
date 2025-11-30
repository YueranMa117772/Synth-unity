using UnityEngine;

public class TVFilterDriver : MonoBehaviour
{
    public AudioLowPassFilter filter;

    public float minCutoff = 500f;
    public float maxCutoff = 22000f;

    private Renderer rend;
    private MaterialPropertyBlock mpb;

    void Start()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (!filter) return;

        // 只读取当前滤波器频率（谁改都行：别的脚本、Timeline、你手拖）
        float f = Mathf.Clamp(filter.cutoffFrequency, minCutoff, maxCutoff);

        float t = Mathf.InverseLerp(
            Mathf.Log10(minCutoff),
            Mathf.Log10(maxCutoff),
            Mathf.Log10(f)
        );

        rend.GetPropertyBlock(mpb);
        mpb.SetFloat("_Cutoff", t);
        rend.SetPropertyBlock(mpb);
    }
}
