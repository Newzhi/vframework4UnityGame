using System;
using System.Collections.Generic;
using BaseFramework.BaseGameRoot;
using UnityEngine;

namespace BaseLayer.Archive
{
    /// <summary>
    /// <see cref="IArchiveService"/> 默认实现。
    /// </summary>
    public sealed class ArchiveService : IArchiveService
    {
        private readonly ISaveDataCollector _collector;
        private readonly ISaveDataApplier _applier;
        private readonly IArchiveStorage _storage;
        private IGameTimeClock _clock;
        private ISessionTimeline _timeline;

        public ArchiveService(
            ISaveDataCollector collector,
            ISaveDataApplier applier,
            IArchiveStorage storage = null,
            int manualSlotCount = 3)
        {
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
            _applier = applier ?? throw new ArgumentNullException(nameof(applier));
            _storage = storage ?? new FileArchiveStorage();
            ManualSlotCount = manualSlotCount > 0 ? manualSlotCount : 3;
        }

        /// <inheritdoc />
        public int ManualSlotCount { get; }

        /// <summary>由 <see cref="ArchiveModule"/> 在 Init 时注入，用于写入元数据。</summary>
        internal void BindGameTime(IGameTimeClock clock, ISessionTimeline timeline)
        {
            _clock = clock;
            _timeline = timeline;
        }

        /// <inheritdoc />
        public IReadOnlyList<ArchiveSlotInfo> ListSlots() => _storage.LoadIndex();

        /// <inheritdoc />
        public bool Exists(ArchiveSlotId slotId)
        {
            foreach (ArchiveSlotInfo info in _storage.LoadIndex())
            {
                if (info.SlotId == slotId)
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool SaveManual(int manualIndex, string displayName = null)
        {
            if (manualIndex < 0 || manualIndex >= ManualSlotCount)
            {
                Debug.LogError($"[{nameof(ArchiveService)}] Manual index out of range: {manualIndex}");
                return false;
            }

            return Save(ArchiveSlotId.Manual(manualIndex), ArchiveSaveKind.Manual, displayName);
        }

        /// <inheritdoc />
        public bool SaveAuto(string displayName = null) =>
            Save(ArchiveSlotId.Auto, ArchiveSaveKind.Auto, displayName);

        /// <inheritdoc />
        public bool Load(ArchiveSlotId slotId)
        {
            if (!Exists(slotId))
                return false;

            byte[] payload = _storage.ReadPayload(slotId);
            if (payload == null || payload.Length == 0)
            {
                Debug.LogError($"[{nameof(ArchiveService)}] Payload missing: {slotId}");
                return false;
            }

            try
            {
                _applier.Apply(payload);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(ArchiveService)}] Apply failed: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool Delete(ArchiveSlotId slotId)
        {
            List<ArchiveSlotInfo> list = new List<ArchiveSlotInfo>(_storage.LoadIndex());
            int removed = list.RemoveAll(x => x.SlotId == slotId);
            if (removed == 0)
                return false;

            _storage.SaveIndex(list);
            _storage.DeletePayload(slotId);
            return true;
        }

        /// <inheritdoc />
        public void DeleteAll()
        {
            foreach (ArchiveSlotInfo info in _storage.LoadIndex())
                _storage.DeletePayload(info.SlotId);

            _storage.SaveIndex(Array.Empty<ArchiveSlotInfo>());
        }

        private bool Save(ArchiveSlotId slotId, ArchiveSaveKind kind, string displayName)
        {
            byte[] payload;
            try
            {
                payload = _collector.Collect();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(ArchiveService)}] Collect failed: {ex.Message}");
                return false;
            }

            if (payload == null || payload.Length == 0)
            {
                Debug.LogWarning($"[{nameof(ArchiveService)}] Collect returned empty payload, save cancelled.");
                return false;
            }

            var info = new ArchiveSlotInfo
            {
                SlotId = slotId,
                Kind = kind,
                SavedAtUtcTicks = DateTime.UtcNow.Ticks,
                PlayTime = _clock != null ? _clock.GameTime : 0f,
                ChapterId = _timeline != null ? _timeline.ChapterId : 0,
                DisplayName = string.IsNullOrEmpty(displayName) ? slotId.Value : displayName,
                PayloadLength = payload.Length
            };

            List<ArchiveSlotInfo> list = new List<ArchiveSlotInfo>(_storage.LoadIndex());
            int index = list.FindIndex(x => x.SlotId == slotId);
            if (index >= 0)
                list[index] = info;
            else
                list.Add(info);

            _storage.WritePayload(slotId, payload);
            _storage.SaveIndex(list);
            return true;
        }
    }
}
