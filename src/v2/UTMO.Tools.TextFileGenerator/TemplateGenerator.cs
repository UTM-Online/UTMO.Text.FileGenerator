namespace UTMO.Tools.TextFileGenerator
{
    using System.Reflection;
    using DotLiquid;
    using UTMO.Tools.TextFileGenerator.Interfaces.Core;

    public class TemplateGenerator
    {
        public static void GenerateTemplates(string outputPath, bool overwrite = false, IFileWriter? fileWriter = null)
        {
            // Using Reflection find all instances of IEnvironment in the calling assembly and place them into a list
            var environments = Assembly.GetCallingAssembly().GetTypes()
                .Where(x => x.GetInterfaces().Contains(typeof(IEnvironment)))
                .Select(x => (IEnvironment)Activator.CreateInstance(x)!)
                .ToList();
            
            var writer = fileWriter ?? new DefaultFileWriter();
            
            // For each environment in the list, generate the templates
            foreach (var env in environments)
            {
                if (!env.IsEnabled)
                {
                    continue;
                }
                
                foreach (var template in env.GenerateTemplates())
                {
                    var templateContent = File.ReadAllText(template.TemplatePath);
                    var parsedTemplate = Template.Parse(templateContent);
                    writer.WriteFile(template.ProduceOutputPath(outputPath), parsedTemplate.Render(Hash.FromAnonymousObject(template.ToDrop())), overwrite);
                }
            }
        }
    }
}