namespace BaseLayer.Input
{
    /// <summary>
    /// 单键/单动作三态：按住、本帧按下、本帧抬起。
    /// </summary>
    public struct InputButton
    {
        public bool Held;
        public bool PressedThisFrame;
        public bool ReleasedThisFrame;

        public static InputButton FromState(bool held, bool wasHeld)
        {
            return new InputButton
            {
                Held = held,
                PressedThisFrame = held && !wasHeld,
                ReleasedThisFrame = !held && wasHeld
            };
        }
    }
}
