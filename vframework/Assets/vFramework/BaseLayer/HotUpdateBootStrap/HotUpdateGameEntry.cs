using UnityEngine;

namespace BaseFramework.BaseGameRoot.HotUpdateBootStrap
{
    /// <summary>
    /// <strong>可选</strong>热更程序集入口（对标 TEngine GameApp.Entrance）。
    /// <para>
    /// 无 HybridCLR 的项目不需要本类；请使用 <see cref="AotMinimalBootstrap"/> 或
    /// <see cref="GameLaunchRunner"/> 的 <see cref="GameLaunchMode.AotBootstrap"/>。
    /// </para>
    /// <para>
    /// 类型全名须与 <see cref="HotfixLaunchCoordinator.HotfixEntryTypeName"/> 一致，供 AOT 反射调用
    ///（仅解析一次并缓存 MethodInfo）。迁入 HotUpdate 程序集后本文件随 DLL 热更，AOT 侧仅保留
    /// <see cref="HotfixLaunchCoordinator"/>。
    /// </para>
    /// </summary>
    public static class HotUpdateGameEntry
    {
        /// <summary>
        /// 热更 / 逻辑 DLL 就绪后调用一次；内部执行 <see cref="GameRoot.TryStart"/>（幂等）。
        /// </summary>
        /// <returns>是否成功启动 GameRoot 管道（Configure + InitAll）。</returns>
        public static bool OnHotfixLoaded()
        {
            if (!GameRoot.TryStart(new GameBootstrap()))
            {
                Debug.LogError("[HotUpdateGameEntry] GameRoot.TryStart failed. " +
                               "Ensure Bootstrap Scene has a GameRoot and TryStart was not called twice.");
                return false;
            }

            Debug.Log("[HotUpdateGameEntry] GameRoot pipeline started.");
            return true;
        }
    }
}
