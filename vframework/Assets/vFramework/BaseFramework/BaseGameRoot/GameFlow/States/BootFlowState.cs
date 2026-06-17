using UnityEngine;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// MVP 启动态：占位 Patch / 热更 / 首包加载。
    /// <para>
    /// 扩展指引（详见 GameFlowApi.md §5）：
    /// 1. Enter 启动 UniTask 异步（资源校验、DLL 加载）；
    /// 2. Update 轮询完成标志，完成后 <c>context.Flow.ChangeState(...)</c>；
    /// 3. Exit 取消进行中的异步、退订；
    /// 4. 失败时切到错误态，勿阻塞主线程。
    /// </para>
    /// </summary>
    public sealed class BootFlowState : IGameFlowState
    {
        /// <summary>防止 MVP 在单帧内重复发起 ChangeState。</summary>
        private bool _transitionRequested;

        /// <inheritdoc />
        public string Id => GameFlowIds.Boot;

        /// <inheritdoc />
        public void Enter(IGameFlowContext context)
        {
            // TODO(MainRoadmap): Patch、VersionCheck、ProcedureLoadAssembly — 见 GameFlowApi.md
            Debug.Log("[GameFlow] Boot.Enter — replace with real bootstrap logic.");
        }

        /// <inheritdoc />
        public void Update(float deltaTime, IGameFlowContext context)
        {
            if (_transitionRequested)
                return;

            // MVP：首帧切主菜单。正式版改为异步就绪后再 ChangeState。
            _transitionRequested = true;
            context.Flow.ChangeState(GameFlowIds.MainMenu);
        }

        /// <inheritdoc />
        public void Exit(IGameFlowContext context)
        {
            _transitionRequested = false;
            Debug.Log("[GameFlow] Boot.Exit");
        }
    }
}
