using UnityEngine;

public class OscilloscopeScrollToggle : MonoBehaviour
{
    // 在 Inspector 里【拖那个带 Oscilloscope (Script) 的屏幕物体】
    [Tooltip("拖有 Oscilloscope (Script) 的那个屏幕物体")]
    public GameObject oscObject;

    private MonoBehaviour osc;   // 运行时自动找到真正的 Oscilloscope 组件
    private bool running = true;

    private void Awake()
    {
        if (oscObject == null)
        {
            Debug.LogError("[OscToggle] 没有给 oscObject 赋值（示波器物体）");
            return;
        }

        // 在这个物体上找到名字叫 "Oscilloscope" 的脚本
        foreach (var c in oscObject.GetComponents<MonoBehaviour>())
        {
            if (c.GetType().Name == "Oscilloscope")
            {
                osc = c;
                Debug.Log("[OscToggle] 找到 Oscilloscope 组件，挂在：" + osc.gameObject.name);
                break;
            }
        }

        if (osc == null)
        {
            Debug.LogError("[OscToggle] 在 " + oscObject.name + " 上找不到名为 Oscilloscope 的脚本！");
        }
    }

    // 给 Interactable 调用的函数
    public void OnInteract()
    {
        if (osc == null)
        {
            Debug.LogError("[OscToggle] 没有 Oscilloscope，OnInteract 不起作用");
            return;
        }

        running = !running;
        osc.enabled = running;  // 关/开脚本本身

        Debug.Log("[OscToggle] osc.enabled = " + running);
    }
}
