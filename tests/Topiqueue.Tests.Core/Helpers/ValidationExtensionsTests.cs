using System;
using AwesomeAssertions;
using Topiqueue.Core.Helpers;

namespace Topiqueue.Tests.Core.Helpers;

public class ValidationExtensionsTests
{
    [Fact]
    public void EnsureGreaterThan_Int_WithValueGreaterThanMin_ReturnsValue()
    {
        // Arrange
        int value = 10;
        int min = 5;
        string argumentName = "testArg";

        // Act
        int result = value.EnsureGreaterThan(min, argumentName);

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(3, 5)]
    public void EnsureGreaterThan_Int_Invalid_ThrowsArgumentException(int value, int min)
    {
        // Arrange
        string argumentName = "testArg";

        // Act
        Action act = () => value.EnsureGreaterThan(min, argumentName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Value of {argumentName} must be greater than {min}");
    }

    [Fact]
    public void EnsureGreaterThan_TimeSpan_WithValueGreaterThanMin_ReturnsValue()
    {
        // Arrange
        TimeSpan value = TimeSpan.FromMinutes(10);
        TimeSpan min = TimeSpan.FromMinutes(5);
        string argumentName = "timeArg";

        // Act
        TimeSpan result = value.EnsureGreaterThan(min, argumentName);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void EnsureGreaterThan_TimeSpan_WithValueEqualToMin_ThrowsArgumentException()
    {
        // Arrange
        TimeSpan value = TimeSpan.FromMinutes(5);
        TimeSpan min = TimeSpan.FromMinutes(5);
        string argumentName = "timeArg";

        // Act
        Action act = () => value.EnsureGreaterThan(min, argumentName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Value of {argumentName} must be greater than {min}");
    }

    [Fact]
    public void EnsureGreaterThan_TimeSpan_WithValueLessThanMin_ThrowsArgumentException()
    {
        // Arrange
        TimeSpan value = TimeSpan.FromMinutes(3);
        TimeSpan min = TimeSpan.FromMinutes(5);
        string argumentName = "timeArg";

        // Act
        Action act = () => value.EnsureGreaterThan(min, argumentName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Value of {argumentName} must be greater than {min}");
    }

    [Fact]
    public void EnsureNotEmpty_WithValidString_ReturnsValue()
    {
        // Arrange
        string value = "Hello";
        string argumentName = "stringArg";

        // Act
        string result = value.EnsureNotEmpty(argumentName);

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EnsureNotEmpty_invalid_ThrowsArgumentException(string? value)
    {
        // Arrange
        string argumentName = "stringArg";

        // Act
        Action act = () => value.EnsureNotEmpty(argumentName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Value of {argumentName} cannot be empty");
    }
}