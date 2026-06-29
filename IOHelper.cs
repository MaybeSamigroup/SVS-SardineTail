using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Buffers.Binary;
using CoastalSmell;
using Storage = (uint Uncompressed, uint Compressed, ushort Flags);
using Node = (long Offset, long Size, uint Flags, string Path);

namespace SardineTail
{
    delegate T ReadSpan<T>(Span<byte> input, out Span<byte> output);
    static class IOExtension
    {
        static Func<Stream, byte[]> ReadBytes(int length) =>
            stream => new BinaryReader(stream).ReadBytes(length);

        internal static byte[] ReadBytes(this Stream stream, int length) =>
            ReadBytes(length).ApplyDisposable(stream)
                .Try(Plugin.Instance.Log.LogMessage, out var bytes) ? bytes : [];

        internal static string ReadCString(this Stream stream) => stream.ReadCString([]);

        static string ReadCString(this Stream stream, IEnumerable<byte> buffer) =>
            stream.ReadByte() switch
            {
                0 => Encoding.UTF8.GetString(buffer.ToArray()),
                var value => stream.ReadCString(buffer.Append((byte)value))
            };
        internal static (string, Storage[], Node[]) ReadStorages(this Span<byte> input) =>
            (ReadHash128(input, out var output), ReadStorages(output, out var _), []);
        internal static (string, Storage[], Node[]) ReadStorageAndNodes(this Span<byte> input) =>
            (ReadHash128(input, out var output), ReadStorages(output, out output), ReadNodes(output, out var _));
        static string ReadHash128(Span<byte> input, out Span<byte> output) =>
            (output = input.Slice(16)) switch { _ => Encoding.UTF8.GetString(input[0..16]) };
        static Storage[] ReadStorages(Span<byte> input, out Span<byte> output) =>
            ReadArray(ReadStorage, input, out output);
        static Node[] ReadNodes(Span<byte> input, out Span<byte> output) =>
            ReadArray(ReadNode, input, out output);
        static ReadSpan<IEnumerable<T>> Identity<T>() =>
            (Span<byte> input, out Span<byte> output) => (output = input) switch { _ => [] };
        static ReadSpan<IEnumerable<T>> Lift<T>(ReadSpan<T> f) =>
            (Span<byte> input, out Span<byte> output) => [f(input, out output)];
        static ReadSpan<IEnumerable<T>> Plus<T>(ReadSpan<IEnumerable<T>> f, ReadSpan<IEnumerable<T>> g) =>
            (Span<byte> input, out Span<byte> output) => [.. f(input, out output), .. g(output, out output)];
        static T[] ReadArray<T>(ReadSpan<T> accumulate, Span<byte> input, out Span<byte> output) =>
            Enumerable.Repeat(Lift(accumulate), ReadInt(input, out output))
                .Aggregate(Identity<T>(), Plus).Invoke(output, out output).ToArray();
        static Storage ReadStorage(Span<byte> input, out Span<byte> output) =>
            (ReadUint(input, out output), ReadUint(output, out output), ReadUshort(output, out output));
        static Node ReadNode(Span<byte> input, out Span<byte> output) =>
            (ReadLong(input, out output), ReadLong(output, out output), ReadUint(output, out output), ReadCString(output, out output));
        static ushort ReadUshort(Span<byte> input, out Span<byte> output) =>
            (output = input.Slice(2)) switch { _ => BinaryPrimitives.ReadUInt16BigEndian(input[0..2]) };
        static uint ReadUint(Span<byte> input, out Span<byte> output) =>
            (output = input.Slice(4)) switch { _ => BinaryPrimitives.ReadUInt32BigEndian(input[0..4]) };
        static int ReadInt(Span<byte> input, out Span<byte> output) =>
            (output = input.Slice(4)) switch { _ => BinaryPrimitives.ReadInt32BigEndian(input[0..4]) };
        static long ReadLong(Span<byte> input, out Span<byte> output) =>
            (output = input.Slice(8)) switch { _ => BinaryPrimitives.ReadInt64BigEndian(input[0..8]) };
        static string ReadCString(Span<byte> input, out Span<byte> output) =>
            ReadCString(input, out output, input.IndexOf((byte)0));
        static string ReadCString(Span<byte> input, out Span<byte> output, int index) =>
            (output = input.Slice(index + 1)) switch { _ => Encoding.UTF8.GetString(input[0..index]) };
    }
}