using UnityEngine;

namespace BaseLayer.Input
{
    /// <summary>
    /// 单帧输入快照：PC（键鼠）与移动端（触摸）统一字段，供各 Module 只读消费。
    /// </summary>
    public struct InputSnapshot
    {
        public int Frame;

        /// <summary>指针位置（屏幕像素）；鼠标或首指触摸。</summary>
        public Vector2 PointerPosition;

        /// <summary>指针本帧位移。</summary>
        public Vector2 PointerDelta;

        public bool PointerHeld;
        public bool PointerPressedThisFrame;
        public bool PointerReleasedThisFrame;

        /// <summary>移动意图（WASD / 摇杆 / 虚拟摇杆）。</summary>
        public Vector2 Move;

        /// <summary>视角/瞄准增量（鼠标 / 右摇杆）。</summary>
        public Vector2 Look;

        public InputButton Confirm;
        public InputButton Cancel;
        public InputButton Jump;
        public InputButton Attack;
    }
}
