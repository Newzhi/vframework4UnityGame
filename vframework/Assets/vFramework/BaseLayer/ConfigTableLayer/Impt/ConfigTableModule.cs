using System;
using System.Collections.Generic;
using BaseFramework.BaseGameRoot;
using UnityEngine;

namespace BaseLayer.ConfigTable
{
    /// <summary>
    /// 配置表模块：AB 预加载 bytes → 回调解析（通常为 <c>GameConfigTables.Initialize</c>）→ 注册 <see cref="IConfigTableService"/>。
    /// Priority 120，早于 <see cref="GameFlowModule"/>，保证 Boot 前表已就绪。
    /// </summary>
    public sealed class ConfigTableModule : IGameModule
    {
        readonly Action<IReadOnlyDictionary<string, byte[]>> _onBytesLoaded;
        bool _ready;

        public int Priority => ModulePriority.ConfigTable;

        /// <param name="onBytesLoaded">阶段二入口：将预加载 bytes 字典交给生成类解析（如 GameConfigTables.Initialize）。</param>
        public ConfigTableModule(Action<IReadOnlyDictionary<string, byte[]>> onBytesLoaded)
        {
            _onBytesLoaded = onBytesLoaded ?? throw new ArgumentNullException(nameof(onBytesLoaded));
        }

        /// <inheritdoc />
        public void Init(IServiceRegistry services)
        {
            IReadOnlyDictionary<string, byte[]> bytes = ConfigTableBytesLoader.Load();
            if (bytes.Count == 0)
            {
                services.Register<IConfigTableService>(new ConfigTableService(false));
                Debug.LogError("[ConfigTableModule] Init aborted: no bytes loaded.");
                return;
            }

            try
            {
                _onBytesLoaded(bytes);
                _ready = true;
            }
            catch (Exception ex)
            {
                _ready = false;
                Debug.LogException(ex);
            }

            services.Register<IConfigTableService>(new ConfigTableService(_ready));
        }

        /// <inheritdoc />
        public void Update(float deltaTime)
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _ready = false;
        }
    }
}
