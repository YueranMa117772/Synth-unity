using UnityEngine;
using KnobsAsset;
using System.Reflection;

public class SliderStepSound : MonoBehaviour
{
    public SliderKnob slider;       // 你的滑块
    public AudioSource audioSource; // 用来发声
    public AudioClip[] clips;       // 三个声音，对应三档

    private FieldInfo movementRangeField;
    private FieldInfo amountMovedField;

    private int lastStep = -1;

    private void Awake()
    {
        if (slider != null)
        {
            var type = typeof(SliderKnob);
            movementRangeField = type.GetField("MovementRange", BindingFlags.NonPublic | BindingFlags.Instance);
            amountMovedField = type.GetField("AmountMoved", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        // 如果没在 Inspector 手动拖 AudioSource，就尝试在自己身上找一个
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (slider == null || audioSource == null || clips == null || clips.Length == 0)
            return;

        if (movementRangeField == null || amountMovedField == null)
            return;

        // 从 SliderKnob 里读当前移动量
        float movementRange = (float)movementRangeField.GetValue(slider);
        float amountMoved = (float)amountMovedField.GetValue(slider);

        // 0~1 的归一化值
        float normalized = movementRange > 0f ? amountMoved / movementRange : 0f;

        // 换算成档位 index（0,1,2）
        int steps = Mathf.Max(1, slider.stepCount);
        int stepIndex;
        if (steps == 1)
            stepIndex = 0;
        else
            stepIndex = Mathf.RoundToInt(normalized * (steps - 1)); // 3 档：0/0.5/1 -> 0/1/2

        // 防止数组越界
        stepIndex = Mathf.Clamp(stepIndex, 0, clips.Length - 1);

        // 档位没变就不重复播放
        if (stepIndex == lastStep)
            return;

        lastStep = stepIndex;

        var clip = clips[stepIndex];
        if (clip == null)
            return;

        audioSource.clip = clip;
        audioSource.Play();
    }
}


