namespace UTMO.Text.FileGenerator.Models;

using Abstract.Contracts;
using CommandLine;

public class GeneratorCliOptions : IGeneratorCliOptions
{
    [Option('o', "output-path", Required = true, HelpText = "The path to the output directory.")]
    public string OutputPath { get; set; } = null!;

    [Option('m', "generate-manifest", Required = false, HelpText = "Generate a manifest file.")]
    public bool GenerateManifest { get; set; } = false;
    
    [Option('t', "template-path", Required = true, HelpText = "The path to the template directory.")]
    public string TemplatePath { get; set; } = null!;
}