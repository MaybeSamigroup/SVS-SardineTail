using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using CoastalSmell;
using System.Buffers.Binary;
using Storage = (uint Uncompressed, uint Compressed, ushort Flags);
using Node = (long Offset, long Size, uint Flags, string Path);

namespace SardineTail
{
    delegate T ReadSpan<T>(Span<byte> input, out Span<byte> output);
    static partial class IOExtension
    {
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
    public record UnityFS(uint Version, string Major, string Minor, long Size, int CompressedBlocksInfo, int UncompressedBlocksInfo, uint Flags)
    {
        public string Hash128 { get; init; }
        public Storage[] Storages { get; init; }
        public Node[] Nodes { get; init; }
        public IEnumerable<string> Identity =>
            Nodes.Where(node => (node.Flags & 4u) is 4u).Select(node => node.Path);
        public int HeaderSize =>
            8 + 4 + Major.Length + 1 + Minor.Length + 1 + 8 + 4 + 4 + 4;
        UnityFS(Stream stream) : this(
            BinaryPrimitives.ReadUInt32BigEndian([
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
            ]),
            stream.ReadCString(),
            stream.ReadCString(),
            BinaryPrimitives.ReadInt64BigEndian([
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
            ]),
            BinaryPrimitives.ReadInt32BigEndian([
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
            ]),
            BinaryPrimitives.ReadInt32BigEndian([
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
            ]),
            BinaryPrimitives.ReadUInt32BigEndian([
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
            ])
        ) => (Hash128, Storages, Nodes) = ReadMetaData(stream.With(SkipToBlocksInfo).ReadBytes(CompressedBlocksInfo));
        
        Action<Stream> SkipToBlocksInfo =>
            (Flags & 0x80, Version >= 7, HeaderSize % 16) switch
            {
                (0x80, _, _) => SkipBytes(Size - HeaderSize - CompressedBlocksInfo),
                (_, true, 1) => SkipBytes(15),
                (_, true, 2) => SkipBytes(14),
                (_, true, 3) => SkipBytes(13),
                (_, true, 4) => SkipBytes(12),
                (_, true, 5) => SkipBytes(11),
                (_, true, 6) => SkipBytes(10),
                (_, true, 7) => SkipBytes(9),
                (_, true, 8) => SkipBytes(8),
                (_, true, 9) => SkipBytes(7),
                (_, true, 10) => SkipBytes(6),
                (_, true, 11) => SkipBytes(5),
                (_, true, 12) => SkipBytes(4),
                (_, true, 13) => SkipBytes(3),
                (_, true, 14) => SkipBytes(2),
                (_, true, 15) => SkipBytes(1),
                (_, true, _) => SkipBytes(0),
                (_, false, _) => SkipBytes(0),
            };
        Action<Stream> SkipBytes(long length) =>
            stream => stream.Read(new byte[length]);

        (string, Storage[], Node[]) ReadMetaData(byte[] bytes) =>
            (Flags & 0x3F) switch
            {
                0 => ReadBlockInfos(bytes),
                1 => ReadBlockInfos(new byte[UncompressedBlocksInfo].With(DecodeLzma(bytes))),
                2 or 3 => ReadBlockInfos(new byte[UncompressedBlocksInfo].With(DecodeLz4(bytes))),
                _ => ("", [], [])
            };
        Action<byte[]> DecodeLz4(byte[] inputs) =>
            buffer => K4os.Compression.LZ4.LZ4Codec.Decode(inputs, buffer);
        Action<byte[]> DecodeLzma(byte[] inputs) =>
            buffer => new SevenZip.Compression.LZMA.Decoder()
                .Code(new MemoryStream(inputs), new MemoryStream(buffer),
                    CompressedBlocksInfo, UncompressedBlocksInfo, null);
        (string, Storage[], Node[]) ReadBlockInfos(byte[] bytes) =>
            (Flags & 0x40) switch
            {
                0x40 => bytes.AsSpan().ReadStorageAndNodes(),
                _ => bytes.AsSpan().ReadStorageAndNodes()
            };
        public static IEnumerable<UnityFS> Extract(Stream stream) =>
            stream.ReadCString() switch
            {
                "UnityFS" => [new UnityFS(stream)],
                _ => Array.Empty<UnityFS>()
            };
    }
}