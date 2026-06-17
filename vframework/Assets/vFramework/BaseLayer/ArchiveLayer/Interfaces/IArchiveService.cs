using System.Collections.Generic;

namespace BaseLayer.Archive
{
    /// <summary>
    /// 存档服务：槽位 CRUD + 手动/自动存档。payload 内容由 <see cref="ISaveDataCollector"/> 提供。
    /// </summary>
    public interface IArchiveService
    {
        /// <summary>手动槽数量上限（manual_0 .. manual_{N-1}）。</summary>
        int ManualSlotCount { get; }

        IReadOnlyList<ArchiveSlotInfo> ListSlots();

        bool Exists(ArchiveSlotId slotId);

        /// <summary>手动存档到指定槽；已存在则覆盖（Update）。</summary>
        bool SaveManual(int manualIndex, string displayName = null);

        /// <summary>自动存档到固定槽 <see cref="ArchiveSlotId.Auto"/>。</summary>
        bool SaveAuto(string displayName = null);

        /// <summary>读档并调用 Applier；成功返回 true。</summary>
        bool Load(ArchiveSlotId slotId);

        /// <summary>删除槽位元数据与 payload。</summary>
        bool Delete(ArchiveSlotId slotId);

        /// <summary>删除全部槽位。</summary>
        void DeleteAll();
    }
}
