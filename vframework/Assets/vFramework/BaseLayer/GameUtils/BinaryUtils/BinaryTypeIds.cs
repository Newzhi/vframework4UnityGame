namespace BaseLayer.GameUtils
{
    /// <summary>
    /// ExcelTool 配置表 XTBL v1 列类型 ID（与导表 BinaryTypeCodec 一致）。
    /// 见 ConfigTableLayer/配置表方案报告.md §3.6。
    /// </summary>
    public static class BinaryTypeIds
    {
        public const byte UInt = 1;
        public const byte String = 2;
        public const byte Float = 3;
        public const byte Int = 4;
        public const byte Long = 5;
        public const byte Double = 6;
        public const byte Bool = 7;

        public const byte IntArray = 11;
        public const byte UIntArray = 12;
        public const byte FloatArray = 13;
        public const byte StringArray = 14;
        public const byte LongArray = 15;
        public const byte BoolArray = 16;
    }
}
