namespace UTMO.Tools.TextFileGenerator.Bases
{
    using UTMO.Tools.TextFileGenerator.Interfaces.Core;

    public abstract class EnvironmentBase : IEnvironment
    {
        public virtual string Alias { get; } = "Default";

        public virtual bool IsEnabled { get; } = true;

        public abstract IEnumerable<ITextTemplateModel> GenerateTemplates();
    }
}