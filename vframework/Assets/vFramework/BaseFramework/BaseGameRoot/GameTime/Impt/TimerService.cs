using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// <see cref="ITimerService"/> 默认实现；链表存储，Phase 1 不做堆优化。
    /// </summary>
    public sealed class TimerService : ITimerService
    {
        private sealed class TimerEntry
        {
            public int Id;
            public float Remaining;
            /// <summary>Repeat 间隔；0 表示一次性 Delay。</summary>
            public float Interval;
            public Action Callback;
            public bool Cancelled;
        }

        private readonly List<TimerEntry> _entries = new List<TimerEntry>(8);
        private int _nextId = 1;

        /// <inheritdoc />
        public TimerHandle Delay(float seconds, Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (seconds < 0f)
                seconds = 0f;

            return AddEntry(seconds, 0f, callback);
        }

        /// <inheritdoc />
        public TimerHandle Repeat(float intervalSeconds, Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (intervalSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds), "Repeat interval must be positive.");

            return AddEntry(intervalSeconds, intervalSeconds, callback);
        }

        /// <inheritdoc />
        public void Cancel(TimerHandle handle)
        {
            if (!handle.IsValid)
                return;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id != handle.Id)
                    continue;

                _entries[i].Cancelled = true;
                _entries.RemoveAt(i);
                return;
            }
        }

        /// <inheritdoc />
        public void CancelAll() => _entries.Clear();

        /// <summary>由 <see cref="GameUpdatePipeline"/> 每帧调用。</summary>
        internal void Tick(float gameDelta)
        {
            if (gameDelta <= 0f || _entries.Count == 0)
                return;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                TimerEntry entry = _entries[i];
                if (entry.Cancelled)
                {
                    _entries.RemoveAt(i);
                    continue;
                }

                entry.Remaining -= gameDelta;
                if (entry.Remaining > 0f)
                    continue;

                try
                {
                    entry.Callback?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                if (entry.Interval > 0f && !entry.Cancelled)
                {
                    entry.Remaining = entry.Interval;
                    continue;
                }

                _entries.RemoveAt(i);
            }
        }

        private TimerHandle AddEntry(float remaining, float interval, Action callback)
        {
            int id = _nextId++;
            _entries.Add(new TimerEntry
            {
                Id = id,
                Remaining = remaining,
                Interval = interval,
                Callback = callback
            });
            return new TimerHandle(id);
        }
    }
}
