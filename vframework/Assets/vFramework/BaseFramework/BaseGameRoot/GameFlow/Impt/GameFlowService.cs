using System;
using System.Collections.Generic;
using BaseFramework.BaseEventSys;
using UnityEngine;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 游戏宏观流程调度器：维护状态表、当前态、切换与 Tick。
    /// </summary>
    internal sealed class GameFlowService : IGameFlowService, IGameFlowRegistry
    {
        private readonly Dictionary<string, IGameFlowState> _states =
            new Dictionary<string, IGameFlowState>(StringComparer.Ordinal);

        private readonly GameFlowContext _context;
        private IGameFlowState _current;
        private float _stateEnterRealtime;

        public GameFlowService(IServiceRegistry services)
        {
            _context = new GameFlowContext(services, this);
        }

        public string CurrentStateId => _current?.Id;
        public string PreviousStateId { get; private set; }
        public float CurrentStateElapsedSeconds =>
            _current == null ? 0f : Time.realtimeSinceStartup - _stateEnterRealtime;

        public bool IsInState(string stateId) =>
            !string.IsNullOrEmpty(stateId) &&
            _current != null &&
            string.Equals(_current.Id, stateId, StringComparison.Ordinal);

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

        public bool Contains(string stateId) =>
            !string.IsNullOrEmpty(stateId) && _states.ContainsKey(stateId);

        public void ChangeState(string stateId, object userData = null)
        {
            if (string.IsNullOrWhiteSpace(stateId))
                throw new ArgumentException("State Id cannot be empty.", nameof(stateId));

            if (!_states.TryGetValue(stateId, out IGameFlowState next))
            {
                Debug.LogError($"[GameFlow] Unknown state '{stateId}'. Registered: {string.Join(", ", _states.Keys)}");
                return;
            }

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

        public void Tick(float deltaTime)
        {
            _current?.Update(deltaTime, _context);
        }

        /// <summary>模块 Dispose 时退出当前态，避免泄漏订阅。</summary>
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
