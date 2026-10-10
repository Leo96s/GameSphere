using GameSphere_backend.Enums;

namespace GameSphere_backend.Services.QuizAdministration;

public static class QuizStatusTransitions
{
    private static readonly HashSet<(QuizStatus From, QuizStatus To)> AllowedTransitions =
    [
        (QuizStatus.Draft, QuizStatus.Published),
        (QuizStatus.Unpublished, QuizStatus.Published),
        (QuizStatus.Published, QuizStatus.Unpublished),
        (QuizStatus.Draft, QuizStatus.Archived),
        (QuizStatus.Unpublished, QuizStatus.Archived),
        (QuizStatus.Archived, QuizStatus.Unpublished)
    ];

    public static bool IsAllowed(QuizStatus from, QuizStatus to) =>
        AllowedTransitions.Contains((from, to));
}
