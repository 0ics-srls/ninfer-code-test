using AwesomeAssertions;
using MyApp.Core.Dtos;
using MyApp.Core.Tests.Factories;
using Xunit;

namespace MyApp.Core.Tests.Dtos;

public class WeekPlannerTests
{
    private static readonly DateOnly Monday = new(2026, 9, 7);

    [Fact]
    public void GetWeekStart_OnMonday_ReturnsSameDate()
    {
        WeekPlanner.GetWeekStart(Monday).Should().Be(Monday);
    }

    [Fact]
    public void GetWeekStart_OnSunday_ReturnsPreviousMonday()
    {
        var sunday = new DateOnly(2026, 9, 6);

        WeekPlanner.GetWeekStart(sunday).Should().Be(new DateOnly(2026, 8, 31));
    }

    [Fact]
    public void GetWeekStart_CrossesMonthBoundary()
    {
        var saturday = new DateOnly(2026, 8, 1);

        WeekPlanner.GetWeekStart(saturday).Should().Be(new DateOnly(2026, 7, 27));
    }

    [Fact]
    public void GetWeekStart_CrossesYearBoundary()
    {
        var newYearFriday = new DateOnly(2027, 1, 1);

        WeekPlanner.GetWeekStart(newYearFriday).Should().Be(new DateOnly(2026, 12, 28));
    }

    [Fact]
    public void BuildWeek_ReturnsSevenOrderedDays()
    {
        var week = WeekPlanner.BuildWeek(Monday, []);

        week.Days.Should().HaveCount(7);
        for (var i = 0; i < 7; i++)
            week.Days[i].Date.Should().Be(Monday.AddDays(i));
        week.Undated.Should().BeEmpty();
    }

    [Fact]
    public void BuildWeek_BucketsByDueDate()
    {
        var todo = TodoFactory.Create("wedding", dueDate: Monday.AddDays(2));

        var week = WeekPlanner.BuildWeek(Monday, [todo]);

        week.Days[2].Todos.Select(t => t.Title).Should().ContainSingle().Which.Should().Be("wedding");
    }

    [Fact]
    public void BuildWeek_NullDueDateGoesToUndated()
    {
        var todo = TodoFactory.Create("no date");

        var week = WeekPlanner.BuildWeek(Monday, [todo]);

        week.Undated.Select(t => t.Title).Should().ContainSingle().Which.Should().Be("no date");
        week.Days.Sum(d => d.Todos.Count).Should().Be(0);
    }

    [Fact]
    public void BuildWeek_OutOfRangeDueDateExcludedEverywhere()
    {
        var before = TodoFactory.Create("before", dueDate: Monday.AddDays(-1));
        var after = TodoFactory.Create("after", dueDate: Monday.AddDays(7));

        var week = WeekPlanner.BuildWeek(Monday, [before, after]);

        week.Days.Sum(d => d.Todos.Count).Should().Be(0);
        week.Undated.Should().BeEmpty();
    }

    [Fact]
    public void BuildWeek_OrdersTodosByIdWithinDay()
    {
        var first = TodoFactory.Create("first");
        var second = TodoFactory.Create("second");
        var third = TodoFactory.Create("third");

        var week = WeekPlanner.BuildWeek(Monday, [third, first, second]);

        week.Undated.Select(t => t.Title).Should().ContainInOrder("first", "second", "third");
    }
}
