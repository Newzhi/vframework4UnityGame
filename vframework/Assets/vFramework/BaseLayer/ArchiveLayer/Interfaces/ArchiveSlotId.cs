using System;

namespace BaseLayer.Archive
{
    /// <summary>
    /// 槽位标识。手动槽：<see cref="Manual"/>；自动槽：<see cref="Auto"/>。
    /// </summary>
    public readonly struct ArchiveSlotId : IEquatable<ArchiveSlotId>
    {
        public static readonly ArchiveSlotId Auto = new ArchiveSlotId("auto_0");

        /// <summary>槽位唯一字符串，用作文件名前缀。</summary>
        public string Value { get; }

        public ArchiveSlotId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Slot id cannot be empty.", nameof(value));
            Value = value;
        }

        /// <summary>创建手动槽位 id，例如 manual_0、manual_1。</summary>
        public static ArchiveSlotId Manual(int index) => new ArchiveSlotId($"manual_{index}");

        public bool Equals(ArchiveSlotId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is ArchiveSlotId other && Equals(other);

        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;

        public override string ToString() => Value;

        public static bool operator ==(ArchiveSlotId left, ArchiveSlotId right) => left.Equals(right);

        public static bool operator !=(ArchiveSlotId left, ArchiveSlotId right) => !left.Equals(right);
    }
}
