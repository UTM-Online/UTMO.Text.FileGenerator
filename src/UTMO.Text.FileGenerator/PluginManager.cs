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
                        Console.WriteLine($"Plugin {plugin.Name} exceeded the maximum runtime of {instance.MaxRuntime.TotalMinutes} minutes.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"An error occurred while invoking plugin {plugin.Name} during {nameof(this.InvokeBeforeRenderPlugins)}.\r\nException Message: {e.Message}");
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
                        Console.WriteLine($"Plugin {plugin.Name} exceeded the maximum runtime of {instance.MaxRuntime.TotalMinutes} minutes.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"An error occurred while invoking plugin {plugin.Name} during {nameof(this.InvokeAfterRenderPlugins)}.\r\nException Message: {e.Message}");
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
                        Console.WriteLine($"Plugin {plugin.Name} exceeded the maximum runtime of {instance.MaxRuntime.TotalMinutes} minutes.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"An error occurred while invoking plugin {plugin.Name} during {nameof(this.InvokeBeforePipelinePlugins)}.\r\nException Message: {e.Message}");
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
                        Console.WriteLine($"Plugin {plugin.Name} exceeded the maximum runtime of {instance.MaxRuntime.TotalMinutes} minutes.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"An error occurred while invoking plugin {plugin.Name} during {nameof(this.InvokeAfterPipelinePlugins)}.\r\nException Message: {e.Message}");
                }
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