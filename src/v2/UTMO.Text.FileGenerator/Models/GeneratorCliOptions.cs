namespace UTMO.Text.FileGenerator.Models;

using Abstract.Contracts;
using CommandLine;

public class GeneratorCliOptions : IGeneratorCliOptions
{
    [Option('o', "output-path", Required = true, HelpText = "The path to the output directory.")]
    public string OutputPath { get; set; } = null!;

    [Option('m', "generate-manifest", Required = false, HelpText = "Generate a manifest file.")]
    public bool GenerateManifest { get; set; } = false;

    [Option('f', "force", Required = false, HelpText = "Force overwrite of existing files.")]
    public bool AllowOverwrite { get; set; } = false;

    [Option('t', "template-path", Required = true, HelpText = "The path to the template directory.")]
    public string TemplatePath { get; set; } = null!;
    
    [Option('g', "generate-manifests-only", Required = false, HelpText = "Generate only manifest files without generating the actual content files.")]
    public bool GenerateManifestsOnly { get; set; } = false;

    /// <summary>
    /// Normalizes CLI options to ensure consistency between GenerateManifestsOnly and GenerateManifest.
    /// When GenerateManifestsOnly is true, GenerateManifest is implicitly set to true.
    /// </summary>
    public void NormalizeOptions()
    {
        if (this.GenerateManifestsOnly)
        {
            this.GenerateManifest = true;
        }
    }
}