// // ***********************************************************************
// // Assembly         : MD.MIF.FileGenerator.Abstract
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/07/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/07/2023 4:53 PM
// // ***********************************************************************
// // <copyright file="IRegisterPluginManager.cs" company="Microsoft Corp">
// //     Copyright (c) Microsoft Corporation. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Contracts
{
    public interface IRegisterPluginManager
    {
        /// <summary>
        /// Registers a plugins dependency with the DI Container. 
        /// </summary>
        /// <param name="type">The type of the dependency being registered.</param>
        /// <returns>The Plugin Registration Manager.</returns>
        IRegisterPluginManager RegisterDependency(Type type);
        
        /// <summary>
        /// Registers a plugins dependency with the DI Container. 
        /// </summary>
        /// <param name="instance">The instance of the dependency being registered.</param>
        /// <typeparam name="T">The type of the dependency being registered.</typeparam>
        /// <returns>The Plugin Registration Manager.</returns>
        IRegisterPluginManager RegisterDependency<T>(T instance);
        
        /// <summary>
        /// Registers a plugins dependency with the DI Container via an interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface being registered.</typeparam>
        /// <typeparam name="TImplementation">The type to return with the interface is requested.</typeparam>
        /// <returns>The Plugin Registration Manager.</returns>
        IRegisterPluginManager RegisterDependency<TInterface, TImplementation>() where TImplementation : TInterface;
        
        /// <summary>
        /// Registers a Rendering Pipeline Plugin to be run before the rendering pipeline.
        /// </summary>
        /// <param name="plugin">The type of plugin being registered.</param>
        /// <returns>The Plugin Registration Manager.</returns>
        IRegisterPluginManager RegisterBeforeRenderPlugin(Type plugin);
        
        /// <summary>
        /// Registers a Rendering Pipeline Plugin to be run before the rendering pipeline.
        /// </summary>
        /// <param name="plugin">The instance of the plugin being registered.</param>
        /// <typeparam name="T">The type of plugin being registered.</typeparam>
        /// <returns>The Plugin Registration Manager.</returns>
        IRegisterPluginManager RegisterBeforeRenderPlugin<T>(T plugin) where T : IRenderingPipelinePlugin;
        
        /// <summary>
        /// Registers a Rendering Pipeline Plugin to be run after the rendering pipeline.
        /// </summary>
        /// <param name="plugin">The type of plugin being registered.</param>
        /// <returns>The Plugin Registration Manager.</returns>
        IRegisterPluginManager RegisterAfterRenderPlugin(Type plugin);
        
        /// <summary>
        /// Registers a Rendering Pipeline Plugin to be run after the rendering pipeline.
        /// </summary>
        /// <param name="plugin">The instance of the plugin being registered.</param>
        /// <typeparam name="T">The type of the plugin being registered.</typeparam>
        /// <returns>The Plugin Registration Manager.</returns>
        IRegisterPluginManager RegisterAfterRenderPlugin<T>(T plugin) where T : IRenderingPipelinePlugin;
        
        /// <summary>
        /// Registers a Pipeline Plugin to be run before the pipeline.
        /// </summary>
        /// <param name="plugin">The type of plugin being registered.</param>
        /// <returns>The Plugin Registration Manager.</returns>
        IRegisterPluginManager RegisterBeforePipelinePlugin(Type plugin);
        
        /// <summary>
        /// Registers a Pipeline Plugin to be run before the pipeline.
        /// </summary>
        /// <param name="plugin">The instance of the plugin being registered.</param>
        /// <typeparam name="T">The type of the plugin being registered.</typeparam>
        /// <returns>The Plugin Registration Manager.</returns>
        IRegisterPluginManager RegisterBeforePipelinePlugin<T>(T plugin) where T : IPipelinePlugin;
        
        /// <summary>
        /// Registers a Pipeline Plugin to be run after the pipeline.
        /// </summary>
        /// <param name="plugin">The type of plugin being registered.</param>
        /// <returns>The Plugin Registration Manager.</returns>
        IRegisterPluginManager RegisterAfterPipelinePlugin(Type plugin);

        /// <summary>
        /// Registers a Pipeline Plugin to be run after the pipeline.
        /// </summary>
        /// <param name="plugin">The instance of the plugin being registered.</param>
        /// <typeparam name="T">The type of plugin being registered.</typeparam>
        /// <returns>The Plugin Registration Manager.</returns>
        IRegisterPluginManager RegisterAfterPipelinePlugin<T>(T plugin) where T : IPipelinePlugin;
    }
}