namespace BaseLayer.Input
{
    public sealed class InputService : IInputService
    {
        private InputSnapshot _current;
        private InputSnapshot _previous;

        public InputSnapshot Current => _current;
        public InputSnapshot Previous => _previous;
        public InputContext Context { get; set; } = InputContext.Gameplay;

        internal void CollectFrame(IInputDeviceProvider provider, int frame)
        {
            _previous = _current;
            _current = default;
            _current.Frame = frame;

            if (Context == InputContext.Blocked)
                return;

            provider.Collect(ref _current);

            if (Context == InputContext.UI)
            {
                _current.Move = default;
                _current.Look = default;
                _current.Jump = default;
                _current.Attack = default;
            }
        }
    }
}
