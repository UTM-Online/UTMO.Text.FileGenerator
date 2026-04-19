using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moq;
using UTMO.Text.FileGenerator;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.EnvironmentInit;
using UTMO.Text.FileGenerator.Models;

namespace TestFileGenerator.Core.Tests;

[TestFixture]
public class FileGeneratorHostRuntimeServicesTests
{
    [Test]
    public void ApplyRuntimeServices_ShouldAssignFeatureManagerAndLoggerToTemplateResource()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FileGeneratorHost>>();
        var featureManagerMock = new Mock<IFeatureManager>();
        var host = CreateHost(loggerMock.Object);
        var resource = new HostRuntimeResource();

        // Act
        InvokeApplyRuntimeServices(host, resource, featureManagerMock.Object);

        // Assert
        resource.AssignedFeatureManager.Should().BeSameAs(featureManagerMock.Object);
        resource.AssignedLogger.Should().BeSameAs(loggerMock.Object);
    }

    [Test]
    public void ApplyRuntimeServices_WithNullFeatureManager_ShouldStillAssignLogger()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FileGeneratorHost>>();
        var host = CreateHost(loggerMock.Object);
        var resource = new HostRuntimeResource();

        // Act
        InvokeApplyRuntimeServices(host, resource, null);

        // Assert
        resource.AssignedFeatureManager.Should().BeNull();
        resource.AssignedLogger.Should().BeSameAs(loggerMock.Object);
    }

    private static FileGeneratorHost CreateHost(ILogger<FileGeneratorHost> logger)
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var fileWriterMock = new Mock<IGeneralFileWriter>();
        var initPluginLogger = new Mock<ILogger<EnvironmentInitPlugin>>();
        var initPlugin = new EnvironmentInitPlugin(initPluginLogger.Object);
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        return new FileGeneratorHost(provider, logger, fileWriterMock.Object, initPlugin, lifetimeMock.Object);
    }

    private static void InvokeApplyRuntimeServices(FileGeneratorHost host, TemplateResourceBase resource, IFeatureManager? featureManager)
    {
        var method = typeof(FileGeneratorHost).GetMethod(
            "ApplyRuntimeServices",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        method.Should().NotBeNull();
        method!.Invoke(host, [resource, featureManager]);
    }

    private class HostRuntimeResource : TemplateResourceBase
    {
        public IFeatureManager? AssignedFeatureManager => this.FeatureManager;

        public ILogger? AssignedLogger => this.Logger;

        public override string ResourceTypeName => "HostRuntimeResource";

        public override string TemplatePath => "runtime/test";

        public override string OutputExtension => ".txt";

        public override string ResourceName => "runtime";
    }
}


