using AwesomeAssertions;
using DevExtreme.AspNet.Data;
using MyApp.Server.Binding;
using Xunit;

namespace MyApp.Server.Tests.Binding;

public class DataSourceLoadOptionsTests
{
    private sealed record Item(string title, string status);

    [Fact]
    public void FromValues_Parses_Paging()
    {
        var values = new Dictionary<string, string?>
        {
            ["skip"] = "20",
            ["take"] = "10",
            ["requireTotalCount"] = "true",
        };

        var options = DataSourceLoadOptions.FromValues(key => values.GetValueOrDefault(key));

        options.Skip.Should().Be(20);
        options.Take.Should().Be(10);
        options.RequireTotalCount.Should().BeTrue();
    }

    [Fact]
    public void FromValues_Parses_SortJson()
    {
        var values = new Dictionary<string, string?>
        {
            ["sort"] = "[{\"selector\":\"title\",\"desc\":true}]",
        };

        var options = DataSourceLoadOptions.FromValues(key => values.GetValueOrDefault(key));

        options.Sort.Should().ContainSingle();
        options.Sort[0].Selector.Should().Be("title");
        options.Sort[0].Desc.Should().BeTrue();
    }

    [Fact]
    public void FromValues_Parses_NestedFilterJson()
    {
        var values = new Dictionary<string, string?>
        {
            ["filter"] = "[[\"title\",\"contains\",\"ABC\"],\"and\",[\"status\",\"=\",\"Pending\"]]",
        };

        var options = DataSourceLoadOptions.FromValues(key => values.GetValueOrDefault(key));

        options.Filter.Should().NotBeNull();

        Item[] source =
        [
            new("abc todo", "Pending"),
            new("zzz", "Pending"),
        ];

        var result = DataSourceLoader.Load(source.AsQueryable(), options);

        var items = result.data.Cast<Item>().ToList();
        items.Should().ContainSingle().Which.title.Should().Be("abc todo");
    }

    [Fact]
    public void FromValues_MissingKeys_LeavesDefaults()
    {
        var values = new Dictionary<string, string?>();

        var options = DataSourceLoadOptions.FromValues(key => values.GetValueOrDefault(key));

        options.Skip.Should().Be(0);
        options.Take.Should().Be(0);
        options.RequireTotalCount.Should().BeFalse();
        options.Filter.Should().BeNull();
        options.Sort.Should().BeNull();
    }
}
