using UnityEngine;

namespace BaseLayer.Input
{
    /// <summary>
    /// PC：键盘 WASD + 鼠标；使用 Unity 旧 Input API，无 Input System 依赖。
    /// </summary>
    public sealed class KeyboardMouseInputProvider : IInputDeviceProvider
    {
        private bool _pointerWasHeld;
        private bool _confirmWasHeld;
        private bool _cancelWasHeld;
        private bool _jumpWasHeld;
        private bool _attackWasHeld;

        public void Collect(ref InputSnapshot snapshot)
        {
            float h = UnityEngine.Input.GetAxisRaw("Horizontal");
            float v = UnityEngine.Input.GetAxisRaw("Vertical");
            snapshot.Move = new Vector2(h, v);

            snapshot.Look = new Vector2(UnityEngine.Input.GetAxisRaw("Mouse X"), UnityEngine.Input.GetAxisRaw("Mouse Y"));

            bool pointerHeld = UnityEngine.Input.GetMouseButton(0);
            snapshot.PointerPosition = UnityEngine.Input.mousePosition;
            snapshot.PointerDelta = new Vector2(UnityEngine.Input.GetAxisRaw("Mouse X"), UnityEngine.Input.GetAxisRaw("Mouse Y"));
            snapshot.PointerHeld = pointerHeld;
            snapshot.PointerPressedThisFrame = pointerHeld && !_pointerWasHeld;
            snapshot.PointerReleasedThisFrame = !pointerHeld && _pointerWasHeld;
            _pointerWasHeld = pointerHeld;

            snapshot.Confirm = InputButton.FromState(
                UnityEngine.Input.GetKey(KeyCode.Return) || UnityEngine.Input.GetKey(KeyCode.Space),
                _confirmWasHeld);
            _confirmWasHeld = snapshot.Confirm.Held;

            snapshot.Cancel = InputButton.FromState(UnityEngine.Input.GetKey(KeyCode.Escape), _cancelWasHeld);
            _cancelWasHeld = snapshot.Cancel.Held;

            snapshot.Jump = InputButton.FromState(UnityEngine.Input.GetKey(KeyCode.Space), _jumpWasHeld);
            _jumpWasHeld = snapshot.Jump.Held;

            snapshot.Attack = InputButton.FromState(
                UnityEngine.Input.GetMouseButton(0) || UnityEngine.Input.GetKey(KeyCode.J),
                _attackWasHeld);
            _attackWasHeld = snapshot.Attack.Held;
        }
    }
}
