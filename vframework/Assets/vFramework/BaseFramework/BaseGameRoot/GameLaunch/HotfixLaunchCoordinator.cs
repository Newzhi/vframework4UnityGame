using System;
using System.Reflection;
using UnityEngine;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// <strong>可选</strong>热更启动协调：AOT 侧通过反射调用热更程序集入口，避免框架程序集硬引用热更 Bootstrap 类型。
    /// <para>
    /// 无 HybridCLR 的项目<strong>不需要</strong>本类；请用 <see cref="GameLaunchRunner"/> 的
    /// <see cref="GameLaunchMode.AotBootstrap"/> 或自行 <see cref="GameRoot.TryStart"/>。
    /// </para>
    /// <para>
    /// HybridCLR 加载 DLL 后由 Launcher 或 <see cref="GameLaunchRunner"/>（HotfixReflection 模式）调用
    /// <see cref="TryLaunchGame"/>。入口类型与方法名仅解析一次并缓存，避免重复反射带来的性能损耗。
    /// </para>
    /// </summary>
    public static class HotfixLaunchCoordinator
    {
        /// <summary>
        /// 热更入口类型全名。迁入 HotUpdate 程序集后保持约定，或修改此常量与热更侧入口类一致。
        /// </summary>
        public const string HotfixEntryTypeName =
            "BaseFramework.BaseGameRoot.HotUpdateBootStrap.HotUpdateGameEntry";

        /// <summary>热更入口静态方法（无参，返回 bool 表示 TryStart 是否成功）。</summary>
        public const string HotfixEntryMethodName = "OnHotfixLoaded";

        /// <summary>首次成功解析后缓存，后续 Launch 零反射查找开销。</summary>
        static MethodInfo cachedEntryMethod;

        /// <summary>
        /// 调用热更入口启动 GameRoot 管道。已启动时直接返回 true（幂等，可安全重复调用）。
        /// </summary>
        public static bool TryLaunchGame()
        {
            if (GameRoot.Instance != null && GameRoot.Instance.IsStarted)
                return true;

            if (!TryInvokeHotfixEntry(out bool success))
            {
                Debug.LogError(
                    "[HotfixLaunchCoordinator] Hotfix entry not found: " +
                    HotfixEntryTypeName + "." + HotfixEntryMethodName +
                    ". Ensure HybridCLR loaded the hotfix assembly, or switch to GameLaunchMode.AotBootstrap.");
                return false;
            }

            return success;
        }

        static bool TryInvokeHotfixEntry(out bool success)
        {
            success = false;
            MethodInfo method = ResolveHotfixEntryMethod();
            if (method == null)
                return false;

            try
            {
                object result = method.Invoke(null, null);
                success = result is bool started ? started : true;
                return true;
            }
            catch (TargetInvocationException ex)
            {
                Debug.LogException(ex.InnerException ?? ex);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return true;
            }
        }

        static MethodInfo ResolveHotfixEntryMethod()
        {
            if (cachedEntryMethod != null)
                return cachedEntryMethod;

            Type entryType = ResolveHotfixEntryType();
            if (entryType == null)
                return null;

            cachedEntryMethod = entryType.GetMethod(
                HotfixEntryMethodName,
                BindingFlags.Public | BindingFlags.Static);

            return cachedEntryMethod;
        }

        static Type ResolveHotfixEntryType()
        {
            Type direct = Type.GetType(HotfixEntryTypeName);
            if (direct != null)
                return direct;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(HotfixEntryTypeName, throwOnError: false);
                if (type != null)
                    return type;
            }

            return null;
        }

        internal static void ResetCacheForTests()
        {
            cachedEntryMethod = null;
        }
    }
}
