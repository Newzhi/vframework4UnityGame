using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BaseLayer.GameUtils
{
    /// <summary>
    /// ExcelTool 配置表 XTBL v1 读取器。表体按列类型顺序序列化，生成代码按 Header.ColumnTypes 逐列读取。
    /// </summary>
    public sealed class XtblBinaryReader : IDisposable
    {
        public const string Magic = "XTBL";
        public const ushort FormatVersion = 1;

        readonly BinaryReader reader;
        readonly Stream stream;
        readonly bool ownsStream;

        public XtblHeader Header { get; private set; }

        XtblBinaryReader(Stream inputStream, bool ownsStream)
        {
            stream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
            this.ownsStream = ownsStream;
            reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: !ownsStream);
        }

        /// <summary>从完整表 bytes 打开；校验 Magic 并读取表头。</summary>
        public static bool TryOpen(byte[] data, out XtblBinaryReader tableReader)
        {
            tableReader = null;
            if (data == null || data.Length == 0)
                return false;

            try
            {
                var stream = new MemoryStream(data, writable: false);
                var instance = new XtblBinaryReader(stream, ownsStream: true);
                if (!instance.ReadHeader())
                {
                    instance.Dispose();
                    return false;
                }

                tableReader = instance;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[XtblBinaryReader] TryOpen failed: " + ex.Message);
                tableReader = null;
                return false;
            }
        }

        /// <summary>从 Stream 打开（不接管所有权）。</summary>
        public static XtblBinaryReader Open(Stream stream)
        {
            var instance = new XtblBinaryReader(stream, ownsStream: false);
            if (!instance.ReadHeader())
                throw new InvalidDataException("Invalid XTBL header.");

            return instance;
        }

        bool ReadHeader()
        {
            if (!BinaryPrimitives.TryReadMagic(reader, Magic))
                return false;

            ushort version = reader.ReadUInt16();
            if (version != FormatVersion)
            {
                Debug.LogWarning("[XtblBinaryReader] Unsupported version: " + version);
                return false;
            }

            int rowCount = reader.ReadInt32();
            byte columnCount = reader.ReadByte();
            if (columnCount == 0)
            {
                Header = new XtblHeader(version, rowCount, 0, Array.Empty<byte>());
                return true;
            }

            byte[] columnTypes = BinaryPrimitives.ReadBytesExact(reader, columnCount);
            Header = new XtblHeader(version, rowCount, columnCount, columnTypes);
            return true;
        }

        /// <summary>按 XTBL 类型 ID 读取一个字段（供生成表代码或调试）。</summary>
        public object ReadField(byte typeId)
        {
            switch (typeId)
            {
                case BinaryTypeIds.UInt:
                    return ReadUInt32();
                case BinaryTypeIds.String:
                    return ReadUtf8String();
                case BinaryTypeIds.Float:
                    return ReadFloat();
                case BinaryTypeIds.Int:
                    return ReadInt32();
                case BinaryTypeIds.Long:
                    return ReadInt64();
                case BinaryTypeIds.Double:
                    return ReadDouble();
                case BinaryTypeIds.Bool:
                    return ReadBool();
                case BinaryTypeIds.IntArray:
                    return ReadInt32Array();
                case BinaryTypeIds.UIntArray:
                    return ReadUInt32Array();
                case BinaryTypeIds.FloatArray:
                    return ReadFloatArray();
                case BinaryTypeIds.StringArray:
                    return ReadUtf8StringArray();
                case BinaryTypeIds.LongArray:
                    return ReadInt64Array();
                case BinaryTypeIds.BoolArray:
                    return ReadBoolArray();
                default:
                    throw new InvalidDataException("Unknown XTBL type id: " + typeId);
            }
        }

        /// <summary>按当前表头 ColumnTypes 顺序读取一整行（返回 object[]）。</summary>
        public object[] ReadRow()
        {
            byte[] types = Header.ColumnTypes;
            if (types == null || types.Length == 0)
                return Array.Empty<object>();

            var row = new object[types.Length];
            for (int i = 0; i < types.Length; i++)
                row[i] = ReadField(types[i]);

            return row;
        }

        public uint ReadUInt32() => BinaryPrimitives.ReadUInt32(reader);
        public int ReadInt32() => BinaryPrimitives.ReadInt32(reader);
        public long ReadInt64() => BinaryPrimitives.ReadInt64(reader);
        public float ReadFloat() => BinaryPrimitives.ReadFloat(reader);
        public double ReadDouble() => BinaryPrimitives.ReadDouble(reader);
        public bool ReadBool() => BinaryPrimitives.ReadBool(reader);
        public string ReadUtf8String() => BinaryPrimitives.ReadUtf8String(reader);
        public int[] ReadInt32Array() => BinaryPrimitives.ReadInt32Array(reader);
        public uint[] ReadUInt32Array() => BinaryPrimitives.ReadUInt32Array(reader);
        public long[] ReadInt64Array() => BinaryPrimitives.ReadInt64Array(reader);
        public float[] ReadFloatArray() => BinaryPrimitives.ReadFloatArray(reader);
        public double[] ReadDoubleArray() => BinaryPrimitives.ReadDoubleArray(reader);
        public bool[] ReadBoolArray() => BinaryPrimitives.ReadBoolArray(reader);
        public string[] ReadUtf8StringArray() => BinaryPrimitives.ReadUtf8StringArray(reader);

        public void Dispose()
        {
            reader.Dispose();
            if (ownsStream)
                stream.Dispose();
        }
    }
}
