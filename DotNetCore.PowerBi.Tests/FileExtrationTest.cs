using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Reflection;
using DotNetCore.PowerBi.Converters;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace DotNetCore.PowerBi.Tests
{
    public class FileExtrationTest
    {
        private MockFileSystem _fileSystem;
        private PowerBiExtractor _extractor;

        [Fact]
        public void CanExtractResourcesFromAPBitFile()
        {
            this.Given(s => s.ANewPowerBiExtractor())
                    .And(s => s.AFileThatExists("Template.pbit"))
                .When(s => s.TheExtractProcessIsRun("Template.pbit", "Output"))
                .Then(s => s.AllTheFilesAreCreated())
                .BDDfy();
        }

        [Fact]
        public void CanExtractAndRecompressAFile()
        {
            this.Given(s => s.ANewPowerBiExtractor())
                .And(s => s.AFileThatExists("Template.pbit"))
                .When(s => s.TheExtractProcessIsRun("Template.pbit", "Output"))
                    .And(s => s.TheCompressionProcessIsRun("Output", "Template2.pbit"))
                .Then(s => s.TheFileIsCreated("Template2.pbit"))
                    .And(s => s.TheFileMatchesTheOriginal())
                .BDDfy();
        }

        private void TheFileMatchesTheOriginal()
        {
            var originalFile = _fileSystem.FileInfo.FromFileName("Template.pbit");
            var newFile = _fileSystem.FileInfo.FromFileName("Template2.pbit");
            //GetPbitLength(newFile).ShouldBe(GetPbitLength(originalFile));
            //Math.Abs((newFile.Length - originalFile.Length) / originalFile.Length * 100).ShouldBeLessThan(1);

            var originalFileHash = string.Empty;

            //using (var stream = originalFile.Open(FileMode.Open))
            //{
            //    originalFileHash = stream.HashFile();
            //}
            using (var stream = originalFile.Open(FileMode.Open))
            {
                using (FileStream fs = File.Create(Environment.CurrentDirectory+ "\\Template.zip"))
                {
                    stream.CopyTo(fs);
                }
            }

            using (var stream = newFile.Open(FileMode.Open))
            {
                using (FileStream fs = File.Create(Environment.CurrentDirectory + "\\Template2.zip"))
                {
                    stream.CopyTo(fs);
                }
            }

            originalFileHash = GetPbitHash(originalFile);

            var newFileHash = string.Empty;
            using (var stream = newFile.Open(FileMode.Open))
            {
                newFileHash = stream.HashFile();
            }
            newFileHash.ShouldBe(originalFileHash);
        }

        private void AllTheFilesAreCreated()
        {
            var files = _fileSystem.AllFiles;
            files.ShouldNotBeEmpty();
        }

        private void TheFileIsCreated(string filename)
        {
            _fileSystem.AllFiles.ShouldContain(Path.GetTempPath() + filename);
        }

        private void TheExtractProcessIsRun(string input, string output)
        {
            _extractor.ExtractPbit(input, output, true);
        }

        private void TheCompressionProcessIsRun(string input, string output)
        {
            _extractor.CompressPbit(input, output, true);
        }

        private void AFileThatExists(string templatePbit)
        {
            _fileSystem.AddFileFromEmbeddedResource("Template.pbit", Assembly.GetExecutingAssembly(), "DotNetCore.PowerBi.Tests.Files.Template.pbit");
        }

        private void ANewPowerBiExtractor()
        {
            _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), Path.GetTempPath());
            _extractor = new PowerBiExtractor(_fileSystem);
        }

        private long GetPbitLength(IFileInfo fileInfo)
        {
            using (var stream = fileInfo.Open(FileMode.Open))
            {
                using (var zip = new ZipArchive(stream))
                {
                    string tempZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    string outputZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
                    zip.ExtractToDirectory(tempZipPath);
                    ZipFile.CreateFromDirectory(tempZipPath, outputZipPath, CompressionLevel.Optimal, false);
                    var outputFileInfo = new FileInfo(outputZipPath);
                    using (Stream file = File.Open(outputZipPath, FileMode.Open))
                    {
                        return file.Length;
                    }
                }
            }
        }

        private string GetPbitHash(IFileInfo fileInfo)
        {
            using (var stream = fileInfo.Open(FileMode.Open))
            {
                using (var zip = new ZipArchive(stream))
                {
                    string tempZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    string outputZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
                    zip.ExtractToDirectory(tempZipPath);
                    ZipFile.CreateFromDirectory(tempZipPath, outputZipPath, CompressionLevel.Optimal, false);
                    var outputFileInfo = new FileInfo(outputZipPath);
                    using (Stream file = File.Open(outputZipPath, FileMode.Open))
                    {
                        return file.HashFile();
                    }
                }
            }
        }
    }
}
