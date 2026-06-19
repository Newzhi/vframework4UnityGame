using UnityEngine;

namespace BaseFramework.BaseGameRoot.HotUpdateBootStrap
{
    /// <summary>
    /// 启动态占位：Patch 完成后、TryStart 之后的宏观 Boot Procedure（非 HybridCLR 加载阶段）。
    /// <para>
    /// 热更前资源/DLL 加载在 AOT <c>GameLaunch/</c> 完成；本态只负责 TryStart 后的首屏编排。
    /// 扩展见 <c>GameFlow/GameFlowApi.md</c> §5。
    /// </para>
    /// </summary>
    public sealed class BootFlowState : IGameFlowState
    {
        bool _transitionRequested;

        /// <inheritdoc />
        public string Id => GameFlowIds.Boot;

        /// <inheritdoc />
        public void Enter(IGameFlowContext context)
        {
            Debug.Log("[GameFlow] Boot.Enter — replace with post-TryStart bootstrap logic.");
        }

        /// <inheritdoc />
        public void Update(float deltaTime, IGameFlowContext context)
        {
            if (_transitionRequested)
                return;

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
