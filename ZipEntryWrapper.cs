using System;
using System.IO;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SardineTail
{
    public partial class Plugin
    {
        static Plugin()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ZipEntryWrapper>();
        }
    }

    public partial class ZipEntryWrapper : Il2CppSystem.IO.Stream
    {
        Stream Target;

        public long EntryOffset { get; init; }

        public long EntryLength { get; init; }

        public override long Length => EntryLength;

        public override long Position
        {
            get => Target.Position - EntryOffset;
            set => Target.Position = EntryOffset + Math.Min(value, EntryLength);
        }

        ZipEntryWrapper() : base(ClassInjector.DerivedConstructorPointer<ZipEntryWrapper>()) =>
            ClassInjector.DerivedConstructorBody(this);

        internal ZipEntryWrapper(Stream target, long offset, long length) : this() =>
            (Target, EntryOffset, EntryLength, target.Position) = (target, offset, length, offset);

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override int Read(Il2CppStructArray<byte> buffer, int offset, int count) =>
            Target.Read(buffer.AsSpan()
                .Slice(offset, Position >= EntryLength ? 0 : Position + count < EntryLength ? count : (int)(EntryLength - Position)));

        public override long Seek(long offset, Il2CppSystem.IO.SeekOrigin origin) =>
            Target.Seek(origin switch
            {
                Il2CppSystem.IO.SeekOrigin.End => EntryOffset + EntryLength + offset,
                Il2CppSystem.IO.SeekOrigin.Begin => EntryOffset + offset,
                Il2CppSystem.IO.SeekOrigin.Current => offset,
                _ => throw new NotImplementedException()
            }, SeekOrigin.Begin) - EntryOffset;

        public override void Write(Il2CppStructArray<byte> buffer, int offset, int count) =>
            throw new NotImplementedException();

        public override void Flush() => Target.Flush();

        public override void Dispose() => Target.Dispose();

#if SamabakeScramble
        public override void SetLength(long value) =>
            throw new NotImplementedException();
#endif

    }
}