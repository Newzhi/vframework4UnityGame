using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BaseLayer.Archive
{
    /// <summary>
    /// 默认存储：<see cref="Application.persistentDataPath"/>/Archives/index.json + {slotId}.bin。
    /// </summary>
    public sealed class FileArchiveStorage : IArchiveStorage
    {
        private readonly string _rootDirectory;

        public FileArchiveStorage(string rootDirectory = null)
        {
            _rootDirectory = rootDirectory ?? Path.Combine(Application.persistentDataPath, "Archives");
        }

        /// <inheritdoc />
        public IReadOnlyList<ArchiveSlotInfo> LoadIndex()
        {
            string path = GetIndexPath();
            if (!File.Exists(path))
                return Array.Empty<ArchiveSlotInfo>();

            try
            {
                string json = File.ReadAllText(path);
                ArchiveIndexDto dto = JsonUtility.FromJson<ArchiveIndexDto>(json);
                if (dto?.Slots == null || dto.Slots.Length == 0)
                    return Array.Empty<ArchiveSlotInfo>();

                var list = new List<ArchiveSlotInfo>(dto.Slots.Length);
                for (int i = 0; i < dto.Slots.Length; i++)
                    list.Add(dto.Slots[i].ToInfo());
                return list;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(FileArchiveStorage)}] LoadIndex failed: {ex.Message}");
                return Array.Empty<ArchiveSlotInfo>();
            }
        }

        /// <inheritdoc />
        public void SaveIndex(IReadOnlyList<ArchiveSlotInfo> index)
        {
            Directory.CreateDirectory(_rootDirectory);

            var dto = new ArchiveIndexDto
            {
                Slots = new ArchiveSlotInfoDto[index.Count]
            };

            for (int i = 0; i < index.Count; i++)
                dto.Slots[i] = ArchiveSlotInfoDto.FromInfo(index[i]);

            string json = JsonUtility.ToJson(dto, prettyPrint: true);
            File.WriteAllText(GetIndexPath(), json);
        }

        /// <inheritdoc />
        public bool PayloadExists(ArchiveSlotId slotId) => File.Exists(GetPayloadPath(slotId));

        /// <inheritdoc />
        public void WritePayload(ArchiveSlotId slotId, byte[] payload)
        {
            Directory.CreateDirectory(_rootDirectory);
            File.WriteAllBytes(GetPayloadPath(slotId), payload ?? Array.Empty<byte>());
        }

        /// <inheritdoc />
        public byte[] ReadPayload(ArchiveSlotId slotId)
        {
            string path = GetPayloadPath(slotId);
            if (!File.Exists(path))
                return null;
            return File.ReadAllBytes(path);
        }

        /// <inheritdoc />
        public void DeletePayload(ArchiveSlotId slotId)
        {
            string path = GetPayloadPath(slotId);
            if (File.Exists(path))
                File.Delete(path);
        }

        private string GetIndexPath() => Path.Combine(_rootDirectory, "index.json");

        private string GetPayloadPath(ArchiveSlotId slotId) =>
            Path.Combine(_rootDirectory, slotId.Value + ".bin");

        [Serializable]
        private sealed class ArchiveIndexDto
        {
            public ArchiveSlotInfoDto[] Slots;
        }

        [Serializable]
        private sealed class ArchiveSlotInfoDto
        {
            public string SlotId;
            public int Kind;
            public long SavedAtUtcTicks;
            public float PlayTime;
            public int ChapterId;
            public string DisplayName;
            public int PayloadLength;

            public static ArchiveSlotInfoDto FromInfo(ArchiveSlotInfo info)
            {
                return new ArchiveSlotInfoDto
                {
                    SlotId = info.SlotId.Value,
                    Kind = (int)info.Kind,
                    SavedAtUtcTicks = info.SavedAtUtcTicks,
                    PlayTime = info.PlayTime,
                    ChapterId = info.ChapterId,
                    DisplayName = info.DisplayName,
                    PayloadLength = info.PayloadLength
                };
            }

            public ArchiveSlotInfo ToInfo()
            {
                return new ArchiveSlotInfo
                {
                    SlotId = new ArchiveSlotId(SlotId),
                    Kind = (ArchiveSaveKind)Kind,
                    SavedAtUtcTicks = SavedAtUtcTicks,
                    PlayTime = PlayTime,
                    ChapterId = ChapterId,
                    DisplayName = DisplayName,
                    PayloadLength = PayloadLength
                };
            }
        }
    }
}
