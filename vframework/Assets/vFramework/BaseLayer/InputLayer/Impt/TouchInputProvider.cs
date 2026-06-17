using UnityEngine;

namespace BaseLayer.Input
{
    /// <summary>
    /// 移动端：首指触摸 + 简易屏幕虚拟摇杆（左半屏拖动为 Move）。
    /// </summary>
    public sealed class TouchInputProvider : IInputDeviceProvider
    {
        private bool _pointerWasHeld;
        private bool _attackWasHeld;
        private int _moveFingerId = -1;
        private Vector2 _moveOrigin;

        public void Collect(ref InputSnapshot snapshot)
        {
            bool pointerHeld = UnityEngine.Input.touchCount > 0;
            if (pointerHeld)
            {
                Touch t = UnityEngine.Input.GetTouch(0);
                snapshot.PointerPosition = t.position;
                snapshot.PointerDelta = t.deltaPosition;
                snapshot.PointerHeld = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
            }
            else
            {
                snapshot.PointerHeld = false;
                snapshot.PointerDelta = default;
            }

            snapshot.PointerPressedThisFrame = snapshot.PointerHeld && !_pointerWasHeld;
            snapshot.PointerReleasedThisFrame = !snapshot.PointerHeld && _pointerWasHeld;
            _pointerWasHeld = snapshot.PointerHeld;

            snapshot.Move = ReadMoveStick();
            snapshot.Look = default;

            snapshot.Confirm = default;
            snapshot.Cancel = default;
            snapshot.Jump = default;

            snapshot.Attack = InputButton.FromState(
                pointerHeld && UnityEngine.Input.touchCount > 0 && UnityEngine.Input.GetTouch(0).phase == TouchPhase.Began,
                _attackWasHeld);
            _attackWasHeld = snapshot.Attack.Held;
        }

        private Vector2 ReadMoveStick()
        {
            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                Touch t = UnityEngine.Input.touchCount > i ? UnityEngine.Input.GetTouch(i) : default;
                if (t.position.x > Screen.width * 0.5f)
                    continue;

                if (t.phase == TouchPhase.Began)
                {
                    _moveFingerId = t.fingerId;
                    _moveOrigin = t.position;
                }

                if (t.fingerId != _moveFingerId)
                    continue;

                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    _moveFingerId = -1;
                    return default;
                }

                Vector2 delta = t.position - _moveOrigin;
                const float radius = 120f;
                return Vector2.ClampMagnitude(delta / radius, 1f);
            }

            return default;
        }
    }
}
