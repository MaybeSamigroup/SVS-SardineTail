using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CoastalSmell;
using System.Buffers.Binary;
using Storage = (uint Uncompressed, uint Compressed, ushort Flags);
using Node = (long Offset, long Size, uint Flags, string Path);

namespace SardineTail
{
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