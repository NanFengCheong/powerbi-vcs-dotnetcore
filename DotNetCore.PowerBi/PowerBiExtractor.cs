﻿using DotNetCore.PowerBi.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetCore.PowerBi.Converters
{
    public class PowerBiExtractor
    {
        private readonly IFileSystem _fileSystem;
        private readonly Dictionary<string, Converter> _converters;

        public PowerBiExtractor(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _converters = new Dictionary<string, Converter>()
            {
                {"DataModelSchema", new JsonConverter(new UnicodeEncoding(false, false), fileSystem, true)  },
                {"DiagramState", new  JsonConverter(new UnicodeEncoding(false, false), fileSystem, false)},
                {"Report/Layout", new JsonConverter(new UnicodeEncoding(false, false), fileSystem, false)},
                {"Report/LinguisticSchema", new  XMLConverter(new UnicodeEncoding(false, false), fileSystem, true)},
                {"[Content_Types].xml", new  XMLConverter(Encoding.UTF8, fileSystem, false)},
                {"SecurityBindings", new  NoopConverter(fileSystem)},
                {"Settings", new  NoopConverter(fileSystem)},
                {"Version", new  NoopConverter(fileSystem)},
                {"Report/StaticResources/", new  NoopConverter(fileSystem)},
                {"DataMashup", new DataMashupConverter(fileSystem)},
                {"Metadata", new MetadataConverter(fileSystem)},
                {"*.json", new  JsonConverter(Encoding.UTF8, fileSystem, false)},
            };
        }

        public Converter FindConverter(string path)
        {

            Regex.Escape(path).Replace(@"\*", ".*").Replace(@"\?", ".");
            foreach (var converter in _converters)
            {
                if (path.MatchesGlob(converter.Key))
                {
                    return converter.Value;
                }
            }
            return new NoopConverter(_fileSystem);
        }

        public void CompressPbit(string extractedPath, string compressedPath, bool overwrite)
        {
            if (_fileSystem.File.Exists(compressedPath))
            {
                if (overwrite)
                {
                    _fileSystem.File.Delete(compressedPath);
                }
                else
                {
                    throw new Exception($"Output path {extractedPath} already exists.");
                }
            }

            // Get order
            var order = new List<string>();
            using (var file = _fileSystem.File.Open(Path.Combine(extractedPath, ".zo"), FileMode.Open))
            using (var reader = new StreamReader(file))
            {
                while (!reader.EndOfStream)
                {
                    order.Add(reader.ReadLine());
                }
            }

            using (var zipStream = _fileSystem.File.Create(compressedPath))
            {
                using (var zipFile = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    foreach (var name in order)
                    {
                        var vcsPath = Path.Combine(extractedPath, name.Replace('/', '\\'));
                        var zipPath = name.Replace('/', '\\');
                        var converter = FindConverter(name);

                        if (_fileSystem.File.Exists(vcsPath) || _fileSystem.Directory.Exists(vcsPath))
                        {
                            var zipEntry = zipFile.CreateEntry(zipPath, CompressionLevel.Fastest);
                            using (var zipEntryStream = zipEntry.Open())
                            {
                                converter.WriteVcsToRaw(Path.Combine(extractedPath, name.Replace('/', '\\')), zipEntryStream);
                            }
                        }
                        else
                        {
                            throw new Exception($"File {vcsPath} does not exist");
                        }
                    }
                }
            }
        }

        public void WritePbitToScreen(string path)
        {
            var stringBuilder = new StringBuilder();
            using (var fileStream = _fileSystem.File.OpenRead(path))
            {
                using (var zip = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    foreach (var zipArchiveEntry in zip.Entries)
                    {

                        var converter = FindConverter(zipArchiveEntry.FullName);

                        using (var zipStream = zipArchiveEntry.Open())
                        {
                            var fileText = converter.WriteRawToConsoleText(zipStream);
                            stringBuilder.AppendLine("Filename: " + zipArchiveEntry.FullName);
                            stringBuilder.AppendLine(fileText);
                        }
                    }
                }
            }

            Console.WriteLine(stringBuilder.ToString());
        }

        public void ExtractPbit(string path, string outdir, bool overwrite)
        {
            //if (string.Compare(Path.GetExtension(path), ".pbit", StringComparison.OrdinalIgnoreCase) != 0)
            //{
            //    throw new ArgumentException("File must be of type *.pbit", nameof(path));
            //}

            EnsureDestinationFolderExists(outdir, overwrite);

            var order = new List<string>();

            using (var fileStream = _fileSystem.File.Open(path, FileMode.Open))
            {
                using (var zip = new ZipArchive(fileStream))
                {
                    foreach (var zipArchiveEntry in zip.Entries)
                    {
                        order.Add(zipArchiveEntry.FullName);
                        var outpath = Path.Combine(outdir, zipArchiveEntry.FullName.Replace('/', '\\'));
                        var converter = FindConverter(zipArchiveEntry.FullName);

                        converter.WriteRawToVcs(zipArchiveEntry.Open(), outpath);
                    }
                }
            }

            using (var file = _fileSystem.File.Create(Path.Combine(outdir, ".zo")))
            using (var writer = new StreamWriter(file))
            {
                writer.Write(string.Join("\n", order));
                writer.Flush();
            }
        }

        private void EnsureDestinationFolderExists(string outdir, bool overwrite)
        {
            if (_fileSystem.Directory.Exists(outdir))
            {
                if (overwrite)
                {
                    Directory.Delete(outdir, true);
                    //var existingFiles = Directory.EnumerateFiles(outdir);
                    //foreach (var file in existingFiles)
                    //{
                    //    _fileSystem.DeleteFile(file);
                    //}
                }
                else
                {
                    throw new Exception($"Output path \"{outdir}\" already exists");
                }
            }
            else
            {
                _fileSystem.Directory.CreateDirectory(outdir);
            }
        }
    }
}