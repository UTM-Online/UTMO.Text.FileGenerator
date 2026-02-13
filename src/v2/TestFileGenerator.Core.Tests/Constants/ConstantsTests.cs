using FluentAssertions;
using UTMO.Text.FileGenerator.Constants;

namespace TestFileGenerator.Core.Tests.Constants;

/// <summary>
/// Tests for constants classes to ensure values are correct and documented.
/// </summary>
[TestFixture]
public class ConstantsTests
{
    [Test]
    public void ExitCodes_Success_ShouldBeZero()
    {
        ExitCodes.Success.Should().Be(0);
    }

    [Test]
    public void ExitCodes_UnhandledException_ShouldBeOne()
    {
        ExitCodes.UnhandledException.Should().Be(1);
    }

    [Test]
    public void ExitCodes_GenerationErrors_ShouldBeThree()
    {
        ExitCodes.GenerationErrors.Should().Be(3);
    }

    [Test]
    public void ExitCodes_Cancelled_ShouldBeFive()
    {
        ExitCodes.Cancelled.Should().Be(5);
    }

    [Test]
    public void ExitCodes_ValidationFailure_ShouldBeFortyFive()
    {
        ExitCodes.ValidationFailure.Should().Be(45);
    }

    [Test]
    public void ExitCodes_ExceptionsTracked_ShouldBeNegativeThreeFifteen()
    {
        ExitCodes.ExceptionsTracked.Should().Be(-315);
    }

    [Test]
    public void ExitCodes_PathNormalizationError_ShouldBeNegativeThreeEightTwoEight()
    {
        ExitCodes.PathNormalizationError.Should().Be(-3828);
    }

    [Test]
    public void GenerationConstants_ValidationRetryDelayMs_ShouldBeOneHundred()
    {
        GenerationConstants.ValidationRetryDelayMs.Should().Be(100);
    }

    [Test]
    public void GenerationConstants_MaxValidationRetryAttempts_ShouldBeOne()
    {
        GenerationConstants.MaxValidationRetryAttempts.Should().Be(1);
    }

    [Test]
    public void GenerationConstants_LiquidTemplateExtension_ShouldBeDotLiquid()
    {
        GenerationConstants.LiquidTemplateExtension.Should().Be(".liquid");
    }

    [Test]
    public void FeatureFlags_EnableParallelResourceRendering_ShouldBeConsistent()
    {
        FeatureFlags.EnableParallelResourceRendering.Should().Be("ParallelResourceRendering");
    }

    [Test]
    public void FeatureFlags_EnableParallelPropertyRendering_ShouldBeConsistent()
    {
        FeatureFlags.EnableParallelPropertyRendering.Should().Be("ParallelPropertyRendering");
    }
}
