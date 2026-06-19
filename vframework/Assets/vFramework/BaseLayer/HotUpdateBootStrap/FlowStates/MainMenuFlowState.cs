using UnityEngine;

namespace BaseFramework.BaseGameRoot.HotUpdateBootStrap
{
    /// <summary>
    /// 主菜单占位 Procedure；业务 UI / 输入在热更层替换实现。
    /// </summary>
    public sealed class MainMenuFlowState : IGameFlowState
    {
        /// <inheritdoc />
        public string Id => GameFlowIds.MainMenu;

        /// <inheritdoc />
        public void Enter(IGameFlowContext context)
        {
            Debug.Log("[GameFlow] MainMenu.Enter — idle. Add UI / input handling here.");
        }

        /// <inheritdoc />
        public void Update(float deltaTime, IGameFlowContext context)
        {
        }

        /// <inheritdoc />
        public void Exit(IGameFlowContext context)
        {
            Debug.Log("[GameFlow] MainMenu.Exit");
        }
    }
}
