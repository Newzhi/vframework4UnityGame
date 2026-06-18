using System;
using System.IO;
using System.Text;

namespace BaseLayer.GameUtils
{
    /// <summary>
    /// ExcelTool 配置表 XTBL v1 写入器（Editor/工具侧或运行时调试导出可用）。
    /// </summary>
    public sealed class XtblBinaryWriter : IDisposable
    {
        readonly BinaryWriter writer;
        readonly Stream stream;
        readonly bool ownsStream;
        bool headerWritten;

        XtblBinaryWriter(Stream outputStream, bool ownsStream)
        {
            stream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
            this.ownsStream = ownsStream;
            writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: !ownsStream);
        }

        public static XtblBinaryWriter Create(Stream stream)
        {
            return new XtblBinaryWriter(stream, ownsStream: false);
        }

        public static byte[] WriteTable(int rowCount, byte[] columnTypes, Action<XtblBinaryWriter> writeRows)
        {
            if (columnTypes == null)
                columnTypes = Array.Empty<byte>();

            using (var memory = new MemoryStream())
            using (var tableWriter = new XtblBinaryWriter(memory, ownsStream: false))
            {
                tableWriter.WriteHeader(rowCount, columnTypes);
                writeRows?.Invoke(tableWriter);
                tableWriter.Flush();
                return memory.ToArray();
            }
        }

        public void WriteHeader(int rowCount, byte[] columnTypes)
        {
            if (headerWritten)
                throw new InvalidOperationException("XTBL header already written.");

            if (columnTypes == null)
                columnTypes = Array.Empty<byte>();

            if (columnTypes.Length > byte.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(columnTypes), "Column count exceeds byte.MaxValue.");

            byte[] magic = Encoding.ASCII.GetBytes(XtblBinaryReader.Magic);
            writer.Write(magic);
            writer.Write(XtblBinaryReader.FormatVersion);
            writer.Write(rowCount);
            writer.Write((byte)columnTypes.Length);
            if (columnTypes.Length > 0)
                writer.Write(columnTypes);

            headerWritten = true;
        }

        public void WriteField(byte typeId, object value)
        {
            switch (typeId)
            {
                case BinaryTypeIds.UInt:
                    WriteUInt32(value == null ? 0u : Convert.ToUInt32(value));
                    break;
                case BinaryTypeIds.String:
                    WriteUtf8String(value as string);
                    break;
                case BinaryTypeIds.Float:
                    WriteFloat(value == null ? 0f : Convert.ToSingle(value));
                    break;
                case BinaryTypeIds.Int:
                    WriteInt32(value == null ? 0 : Convert.ToInt32(value));
                    break;
                case BinaryTypeIds.Long:
                    WriteInt64(value == null ? 0L : Convert.ToInt64(value));
                    break;
                case BinaryTypeIds.Double:
                    WriteDouble(value == null ? 0d : Convert.ToDouble(value));
                    break;
                case BinaryTypeIds.Bool:
                    WriteBool(value != null && Convert.ToBoolean(value));
                    break;
                case BinaryTypeIds.IntArray:
                    WriteInt32Array(value as int[]);
                    break;
                case BinaryTypeIds.UIntArray:
                    WriteUInt32Array(value as uint[]);
                    break;
                case BinaryTypeIds.FloatArray:
                    WriteFloatArray(value as float[]);
                    break;
                case BinaryTypeIds.StringArray:
                    WriteUtf8StringArray(value as string[]);
                    break;
                case BinaryTypeIds.LongArray:
                    WriteInt64Array(value as long[]);
                    break;
                case BinaryTypeIds.BoolArray:
                    WriteBoolArray(value as bool[]);
                    break;
                default:
                    throw new InvalidDataException("Unknown XTBL type id: " + typeId);
            }
        }

        public void WriteRow(byte[] columnTypes, object[] values)
        {
            if (columnTypes == null || values == null)
                return;

            int count = Math.Min(columnTypes.Length, values.Length);
            for (int i = 0; i < count; i++)
                WriteField(columnTypes[i], values[i]);
        }

        public void WriteUInt32(uint value) => BinaryPrimitives.WriteUInt32(writer, value);
        public void WriteInt32(int value) => BinaryPrimitives.WriteInt32(writer, value);
        public void WriteInt64(long value) => BinaryPrimitives.WriteInt64(writer, value);
        public void WriteFloat(float value) => BinaryPrimitives.WriteFloat(writer, value);
        public void WriteDouble(double value) => BinaryPrimitives.WriteDouble(writer, value);
        public void WriteBool(bool value) => BinaryPrimitives.WriteBool(writer, value);
        public void WriteUtf8String(string value) => BinaryPrimitives.WriteUtf8String(writer, value);
        public void WriteInt32Array(int[] values) => BinaryPrimitives.WriteInt32Array(writer, values);
        public void WriteUInt32Array(uint[] values) => BinaryPrimitives.WriteUInt32Array(writer, values);
        public void WriteInt64Array(long[] values) => BinaryPrimitives.WriteInt64Array(writer, values);
        public void WriteFloatArray(float[] values) => BinaryPrimitives.WriteFloatArray(writer, values);
        public void WriteDoubleArray(double[] values) => BinaryPrimitives.WriteDoubleArray(writer, values);
        public void WriteBoolArray(bool[] values) => BinaryPrimitives.WriteBoolArray(writer, values);
        public void WriteUtf8StringArray(string[] values) => BinaryPrimitives.WriteUtf8StringArray(writer, values);

        public void Flush() => writer.Flush();

        public void Dispose()
        {
            writer.Dispose();
            if (ownsStream)
                stream.Dispose();
        }
    }
}
