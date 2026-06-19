using UnityEngine;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// Bootstrap 场景可选组件：在 <see cref="GameRoot"/> Awake 之后、Start 之前触发启动管道。
    /// <para>默认 <see cref="GameLaunchMode.AotBootstrap"/>，无 HybridCLR 依赖。</para>
    /// <para>启用热更时将 <see cref="launchMode"/> 改为 HotfixReflection，或由 Launcher 直接调用
    /// <see cref="HotfixLaunchCoordinator.TryLaunchGame"/>（可关闭 <see cref="autoLaunchOnAwake"/>）。</para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9999)]
    public sealed class GameLaunchRunner : MonoBehaviour
    {
        [Header("Launch")]
        [Tooltip("Awake 时自动启动；若外部 Launcher 已调用 TryStart / TryLaunchGame，请关闭避免重复。")]
        public bool autoLaunchOnAwake = true;

        [Tooltip("默认 AotBootstrap；启用 HybridCLR 时改为 HotfixReflection。")]
        public GameLaunchMode launchMode = GameLaunchMode.AotBootstrap;

        void Awake()
        {
            if (!autoLaunchOnAwake)
                return;

            switch (launchMode)
            {
                case GameLaunchMode.HotfixReflection:
                    HotfixLaunchCoordinator.TryLaunchGame();
                    break;

                case GameLaunchMode.AotBootstrap:
                default:
                    GameRoot.TryStart(new AotMinimalBootstrap());
                    break;
            }
        }
    }
}
