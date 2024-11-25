namespace UTMO.Text.FileGenerator
{
    using Unity;
    using UTMO.Text.FileGenerator.Abstract;
    using UTMO.Text.FileGenerator.Logging;
    using UTMO.Text.FileGenerator.Messages;
    using UTMO.Text.FileGenerator.Messages;

    public class PluginManager : IPluginManager
    {
        private PluginManager()
        {
            this.Logger = new FileGeneratorLogger();
        }
        
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
                this.Logger.Fatal(LogExceptions.PluginDoesNotImplimentInterface, shouldExit: true, exitCode: 1, plugin.Name, nameof(IRenderingPipelinePlugin));
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
                this.Logger.Fatal(LogExceptions.PluginDoesNotImplimentInterface, shouldExit: true, exitCode: 1, plugin.Name, nameof(IRenderingPipelinePlugin));
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
                this.Logger.Fatal(LogExceptions.PluginDoesNotImplimentInterface, shouldExit: true, exitCode: 1, plugin.Name, nameof(IRenderingPipelinePlugin));
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
                this.Logger.Fatal(LogExceptions.PluginDoesNotImplimentInterface, shouldExit: true, exitCode: 1, plugin.Name, nameof(IRenderingPipelinePlugin));
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
                try
                {
                    var instance = this.Container.Resolve(plugin) as IRenderingPipelinePlugin;

                    if (instance == null)
                    {
                        continue;
                    }

                    var task = Task.Run(() => instance.HandleTemplate(resource));
                    if (!task.Wait(instance.MaxRuntime))
                    {
                        this.Logger.Error(LogMessage.PluginTimeOut, plugin.Name, instance.MaxRuntime.TotalMinutes);
                    }
                }
                catch (Exception e)
                {
                    this.Logger.Error(LogMessage.PluginException, plugin.Name, nameof(this.InvokeBeforeRenderPlugins), e.Message);
                }
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
                try
                {
                    var instance = this.Container.Resolve(plugin) as IRenderingPipelinePlugin;

                    if (instance == null)
                    {
                        continue;
                    }

                    var task = Task.Run(() => instance.HandleTemplate(resource));
                    if (!task.Wait(instance.MaxRuntime))
                    {
                        this.Logger.Warning(LogMessage.PluginTimeOut, plugin.Name, instance.MaxRuntime.TotalMinutes);
                    }
                }
                catch (Exception e)
                {
                    this.Logger.Error(LogMessage.PluginException, plugin.Name, nameof(this.InvokeAfterRenderPlugins), e.Message);
                }
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
                try
                {
                    var instance = this.Container.Resolve(plugin) as IPipelinePlugin;
                    
                    if (instance == null)
                    {
                        continue;
                    }
                    
                    var task = Task.Run(() => instance.ProcessPlugin(environment));
                    if (!task.Wait(instance.MaxRuntime))
                    {
                        this.Logger.Warning(LogMessage.PluginTimeOut, plugin.Name, instance.MaxRuntime.TotalMinutes);
                    }
                }
                catch (Exception e)
                {
                    this.Logger.Error(LogMessage.PluginException, plugin.Name, nameof(this.InvokeBeforePipelinePlugins), e.Message);
                }
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
                try
                {
                    var instance = this.Container.Resolve(plugin) as IPipelinePlugin;
                    
                    if (instance == null)
                    {
                        continue;
                    }
                    
                    var task = Task.Run(() => instance.ProcessPlugin(environment));
                    
                    if (!task.Wait(instance.MaxRuntime))
                    {
                        this.Logger.Warning(LogMessage.PluginTimeOut, plugin.Name, instance.MaxRuntime.TotalMinutes);
                    }
                }
                catch (Exception e)
                {
                    this.Logger.Error(LogMessage.PluginException, plugin.Name, nameof(this.InvokeAfterPipelinePlugins), e.Message);
                }
            }
        }

        public T Resolve<T>()
        {
            if (typeof(T).IsAssignableTo(typeof(IGeneratorLogger)))
            {
                return (T) this.Logger;
            }
            
            return this.Container.Resolve<T>();
        }

        public IGeneratorLogger ResolveLogger() => this.Logger;

        public static IPluginManager Instance => new PluginManager();

        private IGeneratorLogger Logger { get; }

        internal IUnityContainer Container { get; } = new UnityContainer();

        private List<Type> BeforeRenderPlugins { get; } = new();

        private List<Type> AfterRenderPlugins { get; } = new();

        private List<Type> BeforePipelinePlugins { get; } = new();

        private List<Type> AfterPipelinePlugins { get; } = new();
    }
}