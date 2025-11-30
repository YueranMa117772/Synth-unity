using UnityEngine;

namespace KnobsAsset
{
    /// <summary>
    /// 射线驱动的滑块：
    /// - 完全去掉鼠标逻辑
    /// - 使用 BeginGrab / EndGrab / UpdateGrabPosition 由外部控制
    /// - 支持 stepCount 离散档位（明显卡位，用来选波形刚好）
    /// </summary>
    public class SliderKnob : Knob
    {
        [Header("Slider configuration")]
        [Tooltip("最小位置（相对本地 Z 轴方向，负数往后）")]
        [SerializeField] private float MinPosition = -1f;

        [Tooltip("从最小位置起，滑块可以移动的总范围")]
        [SerializeField] private float MovementRange = 2f;

        [Tooltip("已经移动了多少（0 ~ MovementRange），Inspector 中是初始值")]
        [SerializeField] private float AmountMoved = 0f;

        [Header("Step snapping（卡位设置）")]
        [Tooltip("档位数量：\n- 0 或 1 = 不卡位，连续滑动\n- >1 = 有明确卡位，比如 3 档切换 3 种波形")]
        public int stepCount = 3;

        private Vector3 handleInitialPosition;

        /// <summary>
        /// 让外部脚本（RaycastInteractor）能读到当前是否被抓住
        /// </summary>
        public bool IsGrabbed => grabbed;

        protected override void Start()
        {
            base.Start();

            handleInitialPosition = handle.localPosition;

            if (MovementRange < 0f)
            {
                Debug.LogWarning("Movement range should be positive", this);
            }

            if (AmountMoved < 0f || AmountMoved > MovementRange)
            {
                Debug.LogWarning("Amount moved should be within the movement range", this);
            }

            // 初始化时，按当前 AmountMoved 做一次卡位 + 同步位置 + 通知监听
            ApplySnappingAndNotify();
        }

        private void Update()
        {
            // 所有输入逻辑交给 RaycastInteractor + InteractableGeneral
        }

        /// <summary>
        /// 开始抓取：在 InteractableGeneral.onPrimaryInteract 里调用
        /// </summary>
        public void BeginGrab()
        {
            grabbed = true;
        }

        /// <summary>
        /// 结束抓取：在 InteractableGeneral.onPrimaryInteractLift 里调用
        /// </summary>
        public void EndGrab()
        {
            grabbed = false;
        }

        /// <summary>
        /// 由 RaycastInteractor 每帧传入的射线命中点（世界坐标）
        /// 仅在 grabbed == true 时会更新滑块位置
        /// </summary>
        public void UpdateGrabPosition(Vector3 worldPoint)
        {
            if (!grabbed)
                return;

            // 把命中点投影到滑块轴线（transform.forward）上
            Vector3 pointOnAxis = PositionOnAxisClosestToPoint(worldPoint);

            float distance = Vector3.Distance(pointOnAxis, handle.position);
            float dot = Vector3.Dot(transform.forward, pointOnAxis - handle.position); // dot<0 表示反方向

            AmountMoved += distance * (dot < 0f ? -1f : 1f);

            // 限制在 [0, MovementRange]
            AmountMoved = Mathf.Clamp(AmountMoved, 0f, MovementRange);

            // 卡位 + 更新位置 + 通知监听者（OnValueChanged）
            ApplySnappingAndNotify();
        }

        /// <summary>
        /// 外部如果直接设置 0~1 的百分比，也会走卡位逻辑
        /// </summary>
        protected override void SetKnobPosition(float percentValue)
        {
            percentValue = Mathf.Clamp01(percentValue);
            AmountMoved = Mathf.Lerp(0f, MovementRange, percentValue);

            ApplySnappingAndNotify();
        }

        /// <summary>
        /// 把当前 AmountMoved 转换成 0~1，再按 stepCount 卡位，
        /// 然后更新 handle 位置，并通过 OnValueChanged(normalized) 通知外部。
        /// </summary>
        private void ApplySnappingAndNotify()
        {
            float normalized = MovementRange > 0f ? AmountMoved / MovementRange : 0f;

            // 有档位才做卡位
            if (stepCount > 1)
            {
                float stepIndex = Mathf.Round(normalized * (stepCount - 1)); // 最近的档
                normalized = stepIndex / (stepCount - 1);
                AmountMoved = normalized * MovementRange;
            }

            SetPositionBasedOnAmountMoved();

            // 向监听者发出卡位后的 0~1 数值（比如接到 Oscilloscope.SetWaveformFromSlider）
            OnValueChanged(normalized);
        }

        /// <summary>
        /// 根据 AmountMoved 更新 handle 的 localPosition
        /// </summary>
        private void SetPositionBasedOnAmountMoved()
        {
            Vector3 minPosition = Vector3.forward * MinPosition;
            handle.localPosition = minPosition + Vector3.forward * AmountMoved;
        }

        /// <summary>
        /// 计算 point 在滑块轴（transform.forward）上的投影点
        /// </summary>
        private Vector3 PositionOnAxisClosestToPoint(Vector3 point)
        {
            Ray axis = new Ray(transform.position, transform.forward);
            return axis.origin + axis.direction * Vector3.Dot(axis.direction, point - axis.origin);
        }
    }
}

