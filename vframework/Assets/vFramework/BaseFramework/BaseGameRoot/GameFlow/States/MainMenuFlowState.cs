using UnityEngine;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// MVP 主菜单态：占位大厅 / 标题界面。
    /// <para>
    /// 扩展指引：
    /// 1. Enter 打开 UIMgr 主界面、播放 BGM；
    /// 2. 按钮「开始游戏」→ <c>ChangeState(GameFlowIds.Loading, userData: sceneName)</c>；
    /// 3. Exit 关闭 UI、停止音频；
    /// 4. 若需暂停全局时间，改 <see cref="IGameTimeClock.TimeScale"/> 而非单独 Update 链。
    /// </para>
    /// </summary>
    public sealed class MainMenuFlowState : IGameFlowState
    {
        public string Id => GameFlowIds.MainMenu;

        /// <inheritdoc />
        public void Enter(IGameFlowContext context)
        {
            // TODO: UIMgr.OpenMainMenu(), AudioMgr.PlayMenuBgm()
            Debug.Log("[GameFlow] MainMenu.Enter — idle. Add UI / input handling here.");
        }

        /// <inheritdoc />
        public void Update(float deltaTime, IGameFlowContext context)
        {
            // 示例：读输入服务切战斗（正式版改 UI 事件驱动）
            // if (context.Services.TryGet(out IInputService input) && input.WasPressed(...))
            //     context.Flow.ChangeState(GameFlowIds.Battle);
        }

        /// <inheritdoc />
        public void Exit(IGameFlowContext context)
        {
            // TODO: UIMgr.CloseMainMenu()
            Debug.Log("[GameFlow] MainMenu.Exit");
        }
    }
}
