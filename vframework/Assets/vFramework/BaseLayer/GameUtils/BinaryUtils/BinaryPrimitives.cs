using System;
using System.IO;
using System.Text;

namespace BaseLayer.GameUtils
{
    /// <summary>
    /// 通用二进制读写原语（UTF-8 长度前缀字符串、定长数组等）。
    /// 与 AssetCatalog 字符串编码一致：长度 int32，-1 表示 null，0 表示空串。
    /// 供业务自定义二进制协议或配置表解析复用。
    /// </summary>
    public static class BinaryPrimitives
    {
        public const int NullStringLength = -1;

        public static void WriteInt32(BinaryWriter writer, int value)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            writer.Write(value);
        }

        public static int ReadInt32(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            return reader.ReadInt32();
        }

        public static void WriteUInt32(BinaryWriter writer, uint value)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            writer.Write(value);
        }

        public static uint ReadUInt32(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            return reader.ReadUInt32();
        }

        public static void WriteInt64(BinaryWriter writer, long value)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            writer.Write(value);
        }

        public static long ReadInt64(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            return reader.ReadInt64();
        }

        public static void WriteFloat(BinaryWriter writer, float value)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            writer.Write(value);
        }

        public static float ReadFloat(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            return reader.ReadSingle();
        }

        public static void WriteDouble(BinaryWriter writer, double value)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            writer.Write(value);
        }

        public static double ReadDouble(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            return reader.ReadDouble();
        }

        public static void WriteBool(BinaryWriter writer, bool value)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            writer.Write(value ? (byte)1 : (byte)0);
        }

        public static bool ReadBool(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            return reader.ReadByte() != 0;
        }

        /// <summary>int32 字节长度 + UTF-8；-1 写 null。</summary>
        public static void WriteUtf8String(BinaryWriter writer, string value)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (value == null)
            {
                writer.Write(NullStringLength);
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length);
            if (bytes.Length > 0)
                writer.Write(bytes);
        }

        /// <summary>int32 字节长度 + UTF-8；-1 读 null。</summary>
        public static string ReadUtf8String(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            if (length == 0)
                return string.Empty;

            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length)
                throw new EndOfStreamException("Unexpected end of UTF-8 string.");

            return Encoding.UTF8.GetString(bytes);
        }

        public static byte[] ReadBytesExact(BinaryReader reader, int count)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0)
                return Array.Empty<byte>();

            byte[] bytes = reader.ReadBytes(count);
            if (bytes.Length != count)
                throw new EndOfStreamException("Unexpected end of binary stream.");

            return bytes;
        }

        public static bool TryReadMagic(BinaryReader reader, string expectedMagic)
        {
            if (reader == null || string.IsNullOrEmpty(expectedMagic))
                return false;

            byte[] magicBytes = Encoding.ASCII.GetBytes(expectedMagic);
            byte[] read = reader.ReadBytes(magicBytes.Length);
            if (read.Length != magicBytes.Length)
                return false;

            for (int i = 0; i < magicBytes.Length; i++)
            {
                if (read[i] != magicBytes[i])
                    return false;
            }

            return true;
        }

        public static void WriteInt32Array(BinaryWriter writer, int[] values)
        {
            WriteArrayHeader(writer, values);
            if (values == null)
                return;

            for (int i = 0; i < values.Length; i++)
                writer.Write(values[i]);
        }

        public static int[] ReadInt32Array(BinaryReader reader)
        {
            int count = ReadArrayHeader(reader);
            if (count < 0)
                return null;

            if (count == 0)
                return Array.Empty<int>();

            var result = new int[count];
            for (int i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        public static void WriteUInt32Array(BinaryWriter writer, uint[] values)
        {
            WriteArrayHeader(writer, values);
            if (values == null)
                return;

            for (int i = 0; i < values.Length; i++)
                writer.Write(values[i]);
        }

        public static uint[] ReadUInt32Array(BinaryReader reader)
        {
            int count = ReadArrayHeader(reader);
            if (count < 0)
                return null;

            if (count == 0)
                return Array.Empty<uint>();

            var result = new uint[count];
            for (int i = 0; i < count; i++)
                result[i] = reader.ReadUInt32();

            return result;
        }

        public static void WriteInt64Array(BinaryWriter writer, long[] values)
        {
            WriteArrayHeader(writer, values);
            if (values == null)
                return;

            for (int i = 0; i < values.Length; i++)
                writer.Write(values[i]);
        }

        public static long[] ReadInt64Array(BinaryReader reader)
        {
            int count = ReadArrayHeader(reader);
            if (count < 0)
                return null;

            if (count == 0)
                return Array.Empty<long>();

            var result = new long[count];
            for (int i = 0; i < count; i++)
                result[i] = reader.ReadInt64();

            return result;
        }

        public static void WriteFloatArray(BinaryWriter writer, float[] values)
        {
            WriteArrayHeader(writer, values);
            if (values == null)
                return;

            for (int i = 0; i < values.Length; i++)
                writer.Write(values[i]);
        }

        public static float[] ReadFloatArray(BinaryReader reader)
        {
            int count = ReadArrayHeader(reader);
            if (count < 0)
                return null;

            if (count == 0)
                return Array.Empty<float>();

            var result = new float[count];
            for (int i = 0; i < count; i++)
                result[i] = reader.ReadSingle();

            return result;
        }

        public static void WriteDoubleArray(BinaryWriter writer, double[] values)
        {
            WriteArrayHeader(writer, values);
            if (values == null)
                return;

            for (int i = 0; i < values.Length; i++)
                writer.Write(values[i]);
        }

        public static double[] ReadDoubleArray(BinaryReader reader)
        {
            int count = ReadArrayHeader(reader);
            if (count < 0)
                return null;

            if (count == 0)
                return Array.Empty<double>();

            var result = new double[count];
            for (int i = 0; i < count; i++)
                result[i] = reader.ReadDouble();

            return result;
        }

        public static void WriteBoolArray(BinaryWriter writer, bool[] values)
        {
            WriteArrayHeader(writer, values);
            if (values == null)
                return;

            for (int i = 0; i < values.Length; i++)
                writer.Write(values[i] ? (byte)1 : (byte)0);
        }

        public static bool[] ReadBoolArray(BinaryReader reader)
        {
            int count = ReadArrayHeader(reader);
            if (count < 0)
                return null;

            if (count == 0)
                return Array.Empty<bool>();

            var result = new bool[count];
            for (int i = 0; i < count; i++)
                result[i] = reader.ReadByte() != 0;

            return result;
        }

        public static void WriteUtf8StringArray(BinaryWriter writer, string[] values)
        {
            WriteArrayHeader(writer, values);
            if (values == null)
                return;

            for (int i = 0; i < values.Length; i++)
                WriteUtf8String(writer, values[i]);
        }

        public static string[] ReadUtf8StringArray(BinaryReader reader)
        {
            int count = ReadArrayHeader(reader);
            if (count < 0)
                return null;

            if (count == 0)
                return Array.Empty<string>();

            var result = new string[count];
            for (int i = 0; i < count; i++)
                result[i] = ReadUtf8String(reader);

            return result;
        }

        static void WriteArrayHeader<T>(BinaryWriter writer, T[] values)
        {
            if (values == null)
            {
                writer.Write(NullStringLength);
                return;
            }

            writer.Write(values.Length);
        }

        static int ReadArrayHeader(BinaryReader reader)
        {
            return reader.ReadInt32();
        }
    }
}
