namespace BaseLayer.GameUtils
{
    /// <summary>XTBL v1 表头（Magic/Version 之后的数据区描述）。</summary>
    public readonly struct XtblHeader
    {
        public ushort Version { get; }
        public int RowCount { get; }
        public byte ColumnCount { get; }
        public byte[] ColumnTypes { get; }

        public XtblHeader(ushort version, int rowCount, byte columnCount, byte[] columnTypes)
        {
            Version = version;
            RowCount = rowCount;
            ColumnCount = columnCount;
            ColumnTypes = columnTypes;
        }
    }
}
