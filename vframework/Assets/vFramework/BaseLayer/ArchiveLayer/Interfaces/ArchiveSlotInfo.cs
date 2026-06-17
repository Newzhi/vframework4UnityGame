namespace BaseLayer.Archive
{
    /// <summary>
    /// 槽位列表用元数据；不含 payload，便于 UI 展示与排序。
    /// </summary>
    public sealed class ArchiveSlotInfo
    {
        /// <summary>槽位 id。</summary>
        public ArchiveSlotId SlotId;

        /// <summary>手动 / 自动。</summary>
        public ArchiveSaveKind Kind;

        /// <summary>存档 UTC 时间 ticks。</summary>
        public long SavedAtUtcTicks;

        /// <summary>存档时累计游戏时间（秒）；未接入 GameTime 时为 0。</summary>
        public float PlayTime;

        /// <summary>存档时章节 id；未接入时为 0。</summary>
        public int ChapterId;

        /// <summary>UI 展示名。</summary>
        public string DisplayName;

        /// <summary>payload 字节长度。</summary>
        public int PayloadLength;
    }
}
