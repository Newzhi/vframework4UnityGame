using System;
using System.Collections.Generic;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 三相位 Facade 共用的订阅列表；Tick 期间 Add / Remove 延迟到本帧结束。
    /// </summary>
    internal sealed class UpdatablePhaseFacade<TUpdatable> where TUpdatable : class
    {
        private readonly Action<TUpdatable, float> _tick;
        private readonly List<TUpdatable> _items = new List<TUpdatable>(16);
        private readonly List<TUpdatable> _pendingAdd = new List<TUpdatable>(4);
        private readonly List<TUpdatable> _pendingRemove = new List<TUpdatable>(4);
        private bool _ticking;

        public UpdatablePhaseFacade(Action<TUpdatable, float> tick) => _tick = tick;

        public void Add(TUpdatable updatable)
        {
            if (updatable == null)
                return;

            if (_ticking)
            {
                _pendingAdd.Add(updatable);
                return;
            }

            if (!_items.Contains(updatable))
                _items.Add(updatable);
        }

        public void Remove(TUpdatable updatable)
        {
            if (updatable == null)
                return;

            if (_ticking)
            {
                _pendingRemove.Add(updatable);
                return;
            }

            _items.Remove(updatable);
        }

        public void Clear()
        {
            _items.Clear();
            _pendingAdd.Clear();
            _pendingRemove.Clear();
        }

        public void Tick(float deltaTime)
        {
            if (_items.Count == 0)
                return;

            _ticking = true;
            try
            {
                for (int i = 0; i < _items.Count; i++)
                    _tick(_items[i], deltaTime);
            }
            finally
            {
                _ticking = false;
                ApplyPendingChanges();
            }
        }

        private void ApplyPendingChanges()
        {
            for (int i = 0; i < _pendingRemove.Count; i++)
                _items.Remove(_pendingRemove[i]);
            _pendingRemove.Clear();

            for (int i = 0; i < _pendingAdd.Count; i++)
            {
                TUpdatable updatable = _pendingAdd[i];
                if (!_items.Contains(updatable))
                    _items.Add(updatable);
            }

            _pendingAdd.Clear();
        }
    }
}
