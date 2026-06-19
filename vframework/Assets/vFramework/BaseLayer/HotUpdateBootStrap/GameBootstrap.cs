using BaseLayer.ConfigTable;
using UnityEngine;

namespace BaseFramework.BaseGameRoot.HotUpdateBootStrap
{
    /// <summary>
    /// 热更 / 逻辑层启动装配模板：集中注册 Service 与 Module。
    /// <para>
    /// <strong>热更为可选能力</strong>：本类位于过渡目录 <c>HotUpdateBootStrap/</c>，目标迁入
    /// <c>HotUpdateScripts/</c> 热更程序集。无 HybridCLR 的项目可改用 <see cref="AotMinimalBootstrap"/>
    /// 或自建 <see cref="IGameBootstrap"/>，不必引用本类。
    /// </para>
    /// <para>
    /// 接入方式（启用热更时）：DLL 加载完成后
    /// <see cref="HotUpdateGameEntry.OnHotfixLoaded"/> → <see cref="GameRoot.TryStart"/> → 本类
    /// <see cref="Configure"/> → 各 Module.Init。
    /// </para>
    /// <para>
    /// GameTime / GameFlow 均为<strong>可选 Module</strong>，由本方法按需 <c>AddModule</c>；
    /// 框架层 <see cref="GameFlowModule"/> 仅提供调度内核，具体 <see cref="IGameFlowState"/> 在此 Register。
    /// </para>
    /// </summary>
    public sealed class GameBootstrap : IGameBootstrap
    {
        /// <inheritdoc />
        public void Configure(IServiceRegistry services, IModuleRegistry modules)
        {
            // [100] 游戏时间 / Timer / UpdatePipeline（可选；无则 GameRoot 回退 Unity deltaTime）
            modules.AddModule(new GameTimeModule(new GameTimeOptions
            {
                CalendarSettings = new GameCalendarSettings { SecondsPerDay = 120f },
                InitialTimeScale = 1f
            }));

            // [120] 配置表：AB 预加载 bytes → GameConfigTables.Initialize（导表后由生成类 RegisterToFramework 注册）
            var parseCallback = ConfigTableInitRegistry.GetParseCallback();
            if (parseCallback == null)
            {
                Debug.LogWarning("[GameBootstrap] ConfigTableInitRegistry not registered. " +
                                 "Run ExcelTool export.bat to generate GameConfigTables, or skip config tables.");
            }
            else
            {
                modules.AddModule(new ConfigTableModule(parseCallback));
            }

            // [150] 宏观流程：内核在 AOT，Procedure 实现与注册在 Bootstrap / 热更层
            modules.AddModule(new GameFlowModule(
                registerStates: reg =>
                {
                    reg.Register(new BootFlowState());
                    reg.Register(new MainMenuFlowState());
                    // reg.Register(new BattleFlowState());
                },
                initialStateId: GameFlowIds.Boot));

            // [0] 输入
            // modules.AddModule(new BaseLayer.Input.InputModule());

            // [500] 存档
            // modules.AddModule(new BaseLayer.Archive.ArchiveModule(...));

            // [600] 核心玩法
            // modules.AddModule(new MyGameLogicModule());

            // [1000] UI
            // modules.AddModule(new MyUiModule());
        }
    }
}
