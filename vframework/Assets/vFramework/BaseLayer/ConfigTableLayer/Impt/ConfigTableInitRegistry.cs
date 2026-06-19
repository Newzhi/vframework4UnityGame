using System;
using System.Collections.Generic;

namespace BaseLayer.ConfigTable
{
    /// <summary>
    /// 配置表解析回调注册表：由 HotUpdateScripts 在启动前注册 <c>GameConfigTables.Initialize</c>，
    /// BaseLayer 的 <see cref="ConfigTableModule"/> 在 Init 时取用，避免程序集循环引用。
    /// </summary>
    public static class ConfigTableInitRegistry
    {
        static Action<IReadOnlyDictionary<string, byte[]>> _parseCallback;

        /// <summary>热更层注册 bytes → 解析入口（通常为 GameConfigTables.Initialize）。</summary>
        public static void Register(Action<IReadOnlyDictionary<string, byte[]>> parseCallback)
        {
            _parseCallback = parseCallback ?? throw new ArgumentNullException(nameof(parseCallback));
        }

        /// <summary>已注册的解析回调；未注册时为 null。</summary>
        public static Action<IReadOnlyDictionary<string, byte[]>> GetParseCallback() => _parseCallback;

        /// <summary>测试或域重载时清空。</summary>
        public static void Clear() => _parseCallback = null;
    }
}
