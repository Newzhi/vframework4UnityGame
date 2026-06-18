using UnityEngine;

namespace BaseFramework.BaseGameRoot.HotUpdateBootStrap
{
    /// <summary>
    /// 热更程序集加载完成后的统一入口（对标 TEngine GameApp.Entrance）。
    /// <para>
    /// 在 Bootstrap Scene 已存在 <see cref="GameRoot"/> 的前提下调用 <see cref="OnHotfixLoaded"/>。
    /// </para>
    /// </summary>
    public static class HotUpdateGameEntry
    {
        /// <summary>
        /// 热更 / 逻辑 DLL 就绪后调用一次。
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
