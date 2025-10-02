using System;
using System.Linq;
using System.IO;
using System.IO.Compression;

foreach(var devpkg in args.Where(Directory.Exists).Where(devpkg => Version.TryParse(devpkg.Split('-')[^1], out var _))) {
    using (var archive = new ZipArchive(File.OpenWrite($"{devpkg}.stp"), ZipArchiveMode.Create)) {
        foreach(var path in new DirectoryInfo(devpkg).GetFiles("*", SearchOption.AllDirectories)) {
             archive.CreateEntryFromFile(path.FullName,
                string.Join(Path.AltDirectorySeparatorChar, Path.GetRelativePath(devpkg, path.FullName).Split(Path.DirectorySeparatorChar)),
                    path.Extension.Equals(".unity3d", StringComparison.OrdinalIgnoreCase) ? CompressionLevel.NoCompression : CompressionLevel.Optimal);
        }
    }
}
return 0;