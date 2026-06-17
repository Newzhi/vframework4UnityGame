using System;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 统一帧推进入口，由 <see cref="GameRoot"/> 在三相位分别调用。
    /// Update：Clock → Modules → UpdateFacade → Calendar → Timer；
    /// Fixed / Late：Modules → 对应 Facade。
    /// </summary>
    public interface IGameUpdatePipeline
    {
        /// <summary>上一帧 Advance 后的游戏 delta（Update 相位内有效）。</summary>
        float GameDeltaTime { get; }

        /// <summary>Update 相位：推进 Clock 并驱动模块与 Timer / Calendar。</summary>
        void RunFrame(float rawDelta, Action<float> moduleUpdate);

        /// <summary>FixedUpdate 相位：驱动模块与 FixedUpdateFacade。</summary>
        void RunFixedFrame(float rawFixedDelta, Action<float> moduleFixedUpdate);

        /// <summary>LateUpdate 相位：驱动模块与 LateUpdateFacade。</summary>
        void RunLateFrame(float rawDelta, Action<float> moduleLateUpdate);
    }
}
