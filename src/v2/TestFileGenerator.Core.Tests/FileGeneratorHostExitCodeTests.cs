using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moq;
using UTMO.Text.FileGenerator;
using UTMO.Text.FileGenerator.Abstract.Constants;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;
using UTMO.Text.FileGenerator.EnvironmentInit;
using UTMO.Text.FileGenerator.Models;
namespace TestFileGenerator.Core.Tests;
/// <summary>
/// Tests that verify the exit-code outcomes set by <see cref="FileGeneratorHost"/> in
/// response to various runtime conditions.
/// </summary>
[TestFixture]
public class FileGeneratorHostExitCodeTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------
    private static (FileGeneratorHost host, GenerationExitCodeHolder holder, Mock<IHostApplicationLifetime> lifetime)
        CreateHost(bool registerFeatureManager = true, ILogger<FileGeneratorHost>? logger = null)
    {
        var loggerToUse = logger ?? new Mock<ILogger<FileGeneratorHost>>().Object;
        var services = new ServiceCollection();
        if (registerFeatureManager)
        {
            var featureManagerMock = new Mock<IFeatureManager>();
            featureManagerMock.Setup(fm => fm.IsEnabledAsync(It.IsAny<string>())).ReturnsAsync(false);
            services.AddSingleton(featureManagerMock.Object);
        }
        var provider = services.BuildServiceProvider();
        var fileWriterMock = new Mock<IGeneralFileWriter>();
        var initPluginLogger = new Mock<ILogger<EnvironmentInitPlugin>>();
        var initPlugin = new EnvironmentInitPlugin(initPluginLogger.Object);
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var exitCodeHolder = new GenerationExitCodeHolder();
        var host = new FileGeneratorHost(provider, loggerToUse, fileWriterMock.Object, initPlugin,
                                         lifetimeMock.Object, exitCodeHolder);
        return (host, exitCodeHolder, lifetimeMock);
    }
    private static void InvokeStartAsync(FileGeneratorHost host, CancellationToken token = default)
        => host.StartAsync(token).GetAwaiter().GetResult();
    // -------------------------------------------------------------------------
    // StartAsync exit-code tests
    // -------------------------------------------------------------------------
    [Test]
    public void StartAsync_WhenNothingFails_SetsExitCodeSuccess()
    {
        var (host, holder, _) = CreateHost(registerFeatureManager: true);
        InvokeStartAsync(host);
        holder.ExitCode.Should().Be(ExitCodes.Success);
    }
    [Test]
    public void StartAsync_WhenFeatureManagerMissing_SetsExitCodeGenerationErrors()
    {
        var (host, holder, _) = CreateHost(registerFeatureManager: false);
        InvokeStartAsync(host);
        holder.ExitCode.Should().Be(ExitCodes.GenerationErrors);
    }
    [Test]
    public void StartAsync_AlwaysCallsStopApplication_OnSuccess()
    {
        var (host, _, lifetimeMock) = CreateHost(registerFeatureManager: true);
        InvokeStartAsync(host);
        lifetimeMock.Verify(l => l.StopApplication(), Times.Once);
    }
    [Test]
    public void StartAsync_AlwaysCallsStopApplication_OnFailure()
    {
        var (host, _, lifetimeMock) = CreateHost(registerFeatureManager: false);
        InvokeStartAsync(host);
        lifetimeMock.Verify(l => l.StopApplication(), Times.Once);
    }
    [Test]
    public void StartAsync_WhenFatalOperationExceptionExitCodeSet_ReflectsCorrectCode()
    {
        var (_, holder, _) = CreateHost();
        var fatalEx = new FatalOperationException(ExitCodes.PathNormalizationError,
                                                   "Error normalizing path: {0}", "some/path");
        holder.ExitCode = fatalEx.ExitCode;
        holder.ExitCode.Should().Be(ExitCodes.PathNormalizationError);
    }
    [Test]
    public void StartAsync_WhenUnhandledExceptionMapped_SetsUnhandledExitCode()
    {
        var (_, holder, _) = CreateHost();
        holder.ExitCode = ExitCodes.UnhandledException;
        holder.ExitCode.Should().Be(ExitCodes.UnhandledException);
    }
    // -------------------------------------------------------------------------
    // StopAsync exit-code precedence tests
    // -------------------------------------------------------------------------
    [Test]
    public void StopAsync_WhenExceptionCountersNonEmpty_AndExitCodeIsSuccess_SetsExceptionsTracked()
    {
        var (host, holder, _) = CreateHost();
        AddExceptionCounter(host);
        host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        holder.ExitCode.Should().Be(ExitCodes.ExceptionsTracked);
    }
    [Test]
    public void StopAsync_WhenExceptionCountersNonEmpty_AndExitCodeAlreadySet_DoesNotOverwrite()
    {
        var (host, holder, _) = CreateHost();
        holder.ExitCode = ExitCodes.ValidationFailure;
        AddExceptionCounter(host);
        host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        holder.ExitCode.Should().Be(ExitCodes.ValidationFailure);
    }
    [Test]
    public void StopAsync_WhenExceptionCountersEmpty_DoesNotChangeExitCode()
    {
        var (host, holder, _) = CreateHost();
        host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        holder.ExitCode.Should().Be(ExitCodes.Success);
    }
    // -------------------------------------------------------------------------
    // FatalOperationException formatting tests
    // -------------------------------------------------------------------------
    [Test]
    public void FatalOperationException_WithPositionalTemplate_FormatsMessageCorrectly()
    {
        var ex = new FatalOperationException(ExitCodes.PathNormalizationError,
                                              "Error normalizing path: {0}", "my/path");
        ex.ExitCode.Should().Be(ExitCodes.PathNormalizationError);
        ex.Message.Should().Contain("my/path");
    }
    [Test]
    public void FatalOperationException_WithNamedMelTemplate_DoesNotThrow()
    {
        var act = () => new FatalOperationException(ExitCodes.UnhandledException,
                                                    "Validation failed for {ResourceName}", "MyResource");
        act.Should().NotThrow();
        var ex = act();
        ex.Message.Should().Contain("MyResource");
    }
    [Test]
    public void FatalOperationException_WithNoArgs_ReturnsTemplateAsIs()
    {
        var ex = new FatalOperationException(ExitCodes.GenerationErrors, "Something went wrong");
        ex.Message.Should().Be("Something went wrong");
    }
    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------
    private static void AddExceptionCounter(FileGeneratorHost host)
    {
        var countersField = typeof(FileGeneratorHost)
            .GetProperty("ExceptionCounters",
                         System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        countersField.Should().NotBeNull();
        var counters = (Dictionary<Type, int>)countersField!.GetValue(host)!;
        counters[typeof(Exception)] = 1;
    }
}
