namespace UTMO.Text.FileGenerator
{
    using Abstract;
    using Unity;

    public class PluginManager : IPluginManager
    {
        public IRegisterPluginManager RegisterDependency(Type type)
        {
            this.Container.RegisterType(type);
            return this;
        }

        public IRegisterPluginManager RegisterDependency<T>(T instance)
        {
            this.Container.RegisterInstance(instance);
            return this;
        }

        public IRegisterPluginManager RegisterDependency<TInterface, TImplementation>() where TImplementation : TInterface
        {
            this.Container.RegisterType<TInterface, TImplementation>();
            return this;
        }

        public IRegisterPluginManager RegisterBeforeRenderPlugin(Type plugin)
        {
            if (!plugin.IsAssignableTo(typeof(IRenderingPipelinePlugin)))
            {
                throw new ApplicationException($"The plugin {plugin.Name} does not implement the {nameof(IRenderingPipelinePlugin)} interface.");
            }

            this.BeforeRenderPlugins.Add(plugin);
            this.RegisterDependency(plugin);
            return this;
        }

        public IRegisterPluginManager RegisterBeforeRenderPlugin<T>(T plugin) where T : IRenderingPipelinePlugin
        {
            this.BeforeRenderPlugins.Add(plugin.GetType());
            this.RegisterDependency(plugin);
            return this;
        }

        public IRegisterPluginManager RegisterAfterRenderPlugin(Type plugin)
        {
            if (!plugin.IsAssignableTo(typeof(IRenderingPipelinePlugin)))
            {
                throw new ApplicationException($"The plugin {plugin.Name} does not implement the {nameof(IRenderingPipelinePlugin)} interface.");
            }

            this.AfterRenderPlugins.Add(plugin);
            this.RegisterDependency(plugin);
            return this;
        }

        public IRegisterPluginManager RegisterAfterRenderPlugin<T>(T plugin) where T : IRenderingPipelinePlugin
        {
            this.AfterRenderPlugins.Add(plugin.GetType());
            this.RegisterDependency(plugin);
            return this;
        }

        public IRegisterPluginManager RegisterBeforePipelinePlugin(Type plugin)
        {
            if (!plugin.IsAssignableTo(typeof(IPipelinePlugin)))
            {
                throw new ApplicationException($"The plugin {plugin.Name} does not implement the {nameof(IPipelinePlugin)} interface.");
            }

            this.BeforePipelinePlugins.Add(plugin);
            this.RegisterDependency(plugin);
            return this;
        }

        public IRegisterPluginManager RegisterBeforePipelinePlugin<T>(T plugin) where T : IPipelinePlugin
        {
            this.BeforePipelinePlugins.Add(plugin.GetType());
            this.RegisterDependency(plugin);
            return this;
        }

        public IRegisterPluginManager RegisterAfterPipelinePlugin(Type plugin)
        {
            if (!plugin.IsAssignableTo(typeof(IPipelinePlugin)))
            {
                throw new ApplicationException($"The plugin {plugin.Name} does not implement the {nameof(IPipelinePlugin)} interface.");
            }

            this.AfterPipelinePlugins.Add(plugin);
            this.RegisterDependency(plugin);
            return this;
        }

        public IRegisterPluginManager RegisterAfterPipelinePlugin<T>(T plugin) where T : IPipelinePlugin
        {
            this.AfterPipelinePlugins.Add(plugin.GetType());
            this.RegisterDependency(plugin);
            return this;
        }

        public void InvokeBeforeRenderPlugins(ITemplateModel resource)
        {
            if (!this.BeforeRenderPlugins.Any())
            {
                return;
            }

            foreach (var plugin in this.BeforeRenderPlugins)
            {
                var instance = this.Container.Resolve(plugin) as IRenderingPipelinePlugin;
                instance?.HandleTemplate(resource);
            }
        }

        public void InvokeAfterRenderPlugins(ITemplateModel resource)
        {
            if (!this.AfterRenderPlugins.Any())
            {
                return;
            }

            foreach (var plugin in this.AfterRenderPlugins)
            {
                var instance = this.Container.Resolve(plugin) as IRenderingPipelinePlugin;
                instance?.HandleTemplate(resource);
            }
        }

        public void InvokeBeforePipelinePlugins(ITemplateGenerationEnvironment environment)
        {
            if (!this.BeforePipelinePlugins.Any())
            {
                return;
            }

            foreach (var plugin in this.BeforePipelinePlugins)
            {
                var instance = this.Container.Resolve(plugin) as IPipelinePlugin;
                instance?.ProcessPlugin(environment);
            }
        }

        public void InvokeAfterPipelinePlugins(ITemplateGenerationEnvironment environment)
        {
            if (!this.AfterPipelinePlugins.Any())
            {
                return;
            }

            foreach (var plugin in this.AfterPipelinePlugins)
            {
                var instance = this.Container.Resolve(plugin) as IPipelinePlugin;
                instance?.ProcessPlugin(environment);
            }
        }

        public T Resolve<T>()
        {
            return this.Container.Resolve<T>();
        }

        private IUnityContainer Container { get; } = new UnityContainer();

        private List<Type> BeforeRenderPlugins { get; } = new();

        private List<Type> AfterRenderPlugins { get; } = new();

        private List<Type> BeforePipelinePlugins { get; } = new();

        private List<Type> AfterPipelinePlugins { get; } = new();
    }
}