﻿using CommandLine;
using DotNetCore.PowerBi.Converters;
using System;
using System.IO.Abstractions;

namespace DotNetCore.PowerBiVcs
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileSystem = new FileSystem();
            var extractor = new PowerBiExtractor(fileSystem);
            extractor.ExtractPbit("Files\\Template.pbit", "TemplateVcs", true);
            extractor.CompressPbit("TemplateVcs", "Files\\Template2.pbit", true);
            //extractor.ExtractPbit("Files\\Template2.pbit", "Template2Vcs", true);

            var options = new CommandLineOptions();
            var result = Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(x => options = x);

            if (options.Input == options.Output)
            {
                Console.WriteLine("Error! Input and output paths cannot be same");
                return;
            }

            if ((options.ExtractToVcs || options.CompressFromVcs) && string.IsNullOrEmpty(options.Output))
            {
                Console.WriteLine("Error! Output is required for extraction and compression");
                return;
            }

            if (options.ExtractToVcs)
            {
                extractor.ExtractPbit(options.Input, options.Output, options.Overwrite);
            }

            if (options.CompressFromVcs)
            {
                extractor.CompressPbit(options.Input, options.Output, options.Overwrite);
            }

            if (options.WriteToScreen)
            {
                extractor.WritePbitToScreen(options.Input);
            }
        }
    }
}
