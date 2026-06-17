using System.Collections.Generic;

namespace BaseLayer.Archive
{
    /// <summary>
    /// 存档文件读写抽象；默认实现为 persistentDataPath 下文件。
    /// </summary>
    public interface IArchiveStorage
    {
        IReadOnlyList<ArchiveSlotInfo> LoadIndex();

        void SaveIndex(IReadOnlyList<ArchiveSlotInfo> index);

        bool PayloadExists(ArchiveSlotId slotId);

        void WritePayload(ArchiveSlotId slotId, byte[] payload);

        byte[] ReadPayload(ArchiveSlotId slotId);

        void DeletePayload(ArchiveSlotId slotId);
    }
}
