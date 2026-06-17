using UnityEngine;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// MVP 主菜单态：占位大厅 / 标题界面。
    /// <para>
    /// 扩展指引（详见 GameFlowApi.md §5）：
    /// 1. Enter 打开 UIMgr 主界面、播放 BGM（从 context.Services Get 并缓存）；
    /// 2. UI 按钮 → <c>context.Flow.ChangeState(..., userData)</c>；
    /// 3. Exit 关闭 UI、停止音频，与 Enter 对称；
    /// 4. 暂停全局时间请改 <see cref="IGameTimeClock.TimeScale"/>，勿另起 Update 链。
    /// </para>
    /// </summary>
    public sealed class MainMenuFlowState : IGameFlowState
    {
        /// <inheritdoc />
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
            // 正式版优先 UI 事件驱动；以下为输入示例（需 InputModule）：
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
