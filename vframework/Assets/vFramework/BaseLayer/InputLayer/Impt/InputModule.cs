using BaseFramework.BaseGameRoot;

namespace BaseLayer.Input
{
    /// <summary>
    /// 输入模块：<see cref="ModulePriority.Input"/>，每帧最先采集并写入 <see cref="IInputService"/>。
    /// </summary>
    public sealed class InputModule : IGameModule
    {
        private readonly InputService _service;
        private IInputDeviceProvider _provider;
        private int _frame;

        public int Priority => ModulePriority.Input;

        public InputModule(InputService service = null)
        {
            _service = service ?? new InputService();
        }

        public InputService Service => _service;

        public void Init(IServiceRegistry services)
        {
            services.Register<IInputService>(_service);
            _provider = CreateProvider();
        }

        public void Update(float deltaTime)
        {
            _frame++;
            _service.CollectFrame(_provider, _frame);
        }

        public void Dispose()
        {
            _provider = null;
        }

        private static IInputDeviceProvider CreateProvider()
        {
#if UNITY_ANDROID || UNITY_IOS
            return new TouchInputProvider();
#else
            if (UnityEngine.Application.isMobilePlatform)
                return new TouchInputProvider();
            return new KeyboardMouseInputProvider();
#endif
        }
    }
}
