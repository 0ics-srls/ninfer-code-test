using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using MyApp.Server.Options;
using Xunit;

namespace MyApp.Server.Tests.Options;

public class AppOptionsTests
{
    [Fact]
    public void AppOptions_DefaultInstance_ValidatesSuccessfully()
    {
        var options = new AppOptions();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, true);

        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Fact]
    public void AppOptions_DefaultCorsOrigins_IncludesAngularDev()
    {
        var options = new AppOptions();

        options.CorsOrigins.Should().Contain("http://localhost:4200");
    }
}
