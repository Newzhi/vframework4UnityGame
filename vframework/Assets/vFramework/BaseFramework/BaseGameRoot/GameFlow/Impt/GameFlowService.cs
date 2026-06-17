using System;
using System.Collections.Generic;
using BaseFramework.BaseEventSys;
using UnityEngine;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 游戏宏观流程调度器：维护状态注册表、当前态、切换顺序与每帧 Tick。
    /// 对外暴露 <see cref="IGameFlowService"/>；配置阶段同时实现 <see cref="IGameFlowRegistry"/>。
    /// </summary>
    internal sealed class GameFlowService : IGameFlowService, IGameFlowRegistry
    {
        /// <summary>已注册的 Id → 状态实例；仅在 Init 回调中写入。</summary>
        private readonly Dictionary<string, IGameFlowState> _states =
            new Dictionary<string, IGameFlowState>(StringComparer.Ordinal);

        /// <summary>传给各状态 Enter/Update/Exit 的共享上下文（含 Services、Flow、UserData）。</summary>
        private readonly GameFlowContext _context;

        /// <summary>当前活跃状态；null 表示尚未 ChangeState 或已 Shutdown。</summary>
        private IGameFlowState _current;

        /// <summary>进入当前态时的 Time.realtimeSinceStartup，用于 Elapsed 计算。</summary>
        private float _stateEnterRealtime;

        /// <summary>
        /// 构造流程服务并绑定 IOC；此时尚未 Register 任何状态。
        /// </summary>
        public GameFlowService(IServiceRegistry services)
        {
            _context = new GameFlowContext(services, this);
        }

        /// <inheritdoc />
        public string CurrentStateId => _current?.Id;

        /// <inheritdoc />
        public string PreviousStateId { get; private set; }

        /// <inheritdoc />
        public float CurrentStateElapsedSeconds =>
            _current == null ? 0f : Time.realtimeSinceStartup - _stateEnterRealtime;

        /// <inheritdoc />
        public bool IsInState(string stateId) =>
            !string.IsNullOrEmpty(stateId) &&
            _current != null &&
            string.Equals(_current.Id, stateId, StringComparison.Ordinal);

        /// <inheritdoc />
        public void Register(IGameFlowState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (string.IsNullOrWhiteSpace(state.Id))
                throw new ArgumentException("State Id cannot be empty.", nameof(state));

            if (_states.ContainsKey(state.Id))
                throw new ArgumentException($"Duplicate flow state Id: {state.Id}", nameof(state));

            _states[state.Id] = state;
        }

        /// <inheritdoc />
        public bool Contains(string stateId) =>
            !string.IsNullOrEmpty(stateId) && _states.ContainsKey(stateId);

        /// <inheritdoc />
        public void ChangeState(string stateId, object userData = null)
        {
            if (string.IsNullOrWhiteSpace(stateId))
                throw new ArgumentException("State Id cannot be empty.", nameof(stateId));

            if (!_states.TryGetValue(stateId, out IGameFlowState next))
            {
                Debug.LogError($"[GameFlow] Unknown state '{stateId}'. Registered: {string.Join(", ", _states.Keys)}");
                return;
            }

            // 同 Id 不重复 Enter，避免 UI / 订阅叠层。
            if (_current != null && string.Equals(_current.Id, stateId, StringComparison.Ordinal))
                return;

            string fromId = _current?.Id;
            _current?.Exit(_context);

            PreviousStateId = fromId;
            _current = next;
            _context.UserData = userData;
            _stateEnterRealtime = Time.realtimeSinceStartup;

            _current.Enter(_context);

            GameEventBus.SentEvent(new GameFlowChangedEvent(fromId, stateId, userData));
            Debug.Log($"[GameFlow] {fromId ?? "<none>"} -> {stateId}");
        }

        /// <summary>由 <see cref="GameFlowModule.Update"/> 每帧调用，驱动当前态 Update。</summary>
        public void Tick(float deltaTime)
        {
            _current?.Update(deltaTime, _context);
        }

        /// <summary>模块 Dispose 时退出当前态并清空指针，避免订阅泄漏。</summary>
        public void Shutdown()
        {
            if (_current == null)
                return;

            _current.Exit(_context);
            _current = null;
            PreviousStateId = null;
        }
    }
}
