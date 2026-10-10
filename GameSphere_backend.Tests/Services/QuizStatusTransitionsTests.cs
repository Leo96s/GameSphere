#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using GameSphere_backend.Enums;
using GameSphere_backend.Services.QuizAdministration;
using Xunit;

namespace GameSphere_backend.Tests.Services;

public sealed class QuizStatusTransitionsTests
{
    private static readonly HashSet<(QuizStatus From, QuizStatus To)> ExpectedAllowed =
    [
        (QuizStatus.Draft, QuizStatus.Published),
        (QuizStatus.Unpublished, QuizStatus.Published),
        (QuizStatus.Published, QuizStatus.Unpublished),
        (QuizStatus.Draft, QuizStatus.Archived),
        (QuizStatus.Unpublished, QuizStatus.Archived),
        (QuizStatus.Archived, QuizStatus.Unpublished)
    ];

    public static IEnumerable<object[]> AllPairs() =>
        from source in Enum.GetValues<QuizStatus>()
        from target in Enum.GetValues<QuizStatus>()
        select new object[] { source, target };

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void Only_the_documented_transitions_are_allowed(QuizStatus from, QuizStatus to)
    {
        Assert.Equal(ExpectedAllowed.Contains((from, to)), QuizStatusTransitions.IsAllowed(from, to));
    }

    [Fact]
    public void The_matrix_covers_all_four_states_and_exactly_six_allowed_transitions()
    {
        Assert.Equal(4, Enum.GetValues<QuizStatus>().Length);
        Assert.Equal(0, (int)QuizStatus.Draft);
        Assert.Equal(1, (int)QuizStatus.Published);
        Assert.Equal(2, (int)QuizStatus.Unpublished);
        Assert.Equal(3, (int)QuizStatus.Archived);

        var allowed = AllPairs()
            .Select(pair => ((QuizStatus)pair[0], (QuizStatus)pair[1]))
            .Count(pair => QuizStatusTransitions.IsAllowed(pair.Item1, pair.Item2));

        Assert.Equal(6, allowed);
    }

    [Fact]
    public void A_published_quiz_must_be_unpublished_before_being_archived()
    {
        Assert.False(QuizStatusTransitions.IsAllowed(QuizStatus.Published, QuizStatus.Archived));
        Assert.True(QuizStatusTransitions.IsAllowed(QuizStatus.Published, QuizStatus.Unpublished));
        Assert.True(QuizStatusTransitions.IsAllowed(QuizStatus.Unpublished, QuizStatus.Archived));
    }

    [Theory]
    [InlineData(QuizStatus.Draft, (QuizStatus)99)]
    [InlineData((QuizStatus)99, QuizStatus.Published)]
    [InlineData((QuizStatus)(-1), QuizStatus.Archived)]
    public void Undefined_states_are_never_allowed(QuizStatus from, QuizStatus to)
    {
        Assert.False(QuizStatusTransitions.IsAllowed(from, to));
    }
}
