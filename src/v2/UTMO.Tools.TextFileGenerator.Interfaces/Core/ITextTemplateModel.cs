namespace UTMO.Tools.TextFileGenerator.Interfaces.Core
{
    using DotLiquid;

    public interface ITextTemplateModel
    {
        /// <summary>The name of the resource type.</summary>
        string ResourceTypeName { get; }

        /// <summary>A path to the Template that is relative to the root of the template search directory.</summary>
        /// <value>
        ///     <para>(A template in the Template Root directory)</para>
        ///     <blockquote style="margin-right: 0px;" dir="ltr">
        ///         <para style="margin-right: 0px;" dir="ltr">ServiceModel</para>
        ///     </blockquote>
        ///     <para>(a template in a sub directory of the Templates root directory)</para>
        ///     <blockquote style="margin-right: 0px;" dir="ltr">
        ///         <para>Parameters/KeyVault</para>
        ///     </blockquote>
        /// </value>
        /// <remarks>The template path MUST include the template file name (without the ".liquid" file extension).</remarks>
        string TemplatePath { get; }

        /// <summary>The file extension for the files this resource generates.</summary>
        /// <value>json</value>
        string OutputExtension { get; }

        /// <summary>The name of the resource the template is generating.</summary>
        string ResourceName { get; }
        
        bool GenerateFile { get; }
        
        bool GenerateManifest { get; }
        
        /// <summary>FOR INTERNAL USE ONLY</summary>
        /// <param name="basePath">The base output path.</param>
        string ProduceOutputPath(string basePath);
        
        Drop ToDrop();
    }
}