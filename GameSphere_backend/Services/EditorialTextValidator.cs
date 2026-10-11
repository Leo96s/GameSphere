using System.Globalization;

namespace GameSphere_backend.Services;

public interface IEditorialTextValidator
{
    string? Validate(string? text, bool allowLineBreaks);
}

public sealed class EditorialTextValidator : IEditorialTextValidator
{
    public const string UnsupportedCharactersMessage =
        "The text contains unsupported control or invisible characters.";

    public string? Validate(string? text, bool allowLineBreaks)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        foreach (var character in text)
        {
            if (IsForbidden(character, allowLineBreaks))
            {
                return UnsupportedCharactersMessage;
            }
        }

        return null;
    }

    private static bool IsForbidden(char character, bool allowLineBreaks)
    {
        if (character is '\t' or '\n' or '\r')
        {
            return !allowLineBreaks;
        }

        if (char.GetUnicodeCategory(character) == UnicodeCategory.Control)
        {
            return true;
        }

        return character is '​' or '‌' or '‎' or '‏' or '﻿'
            or (>= '‪' and <= '‮')
            or (>= '⁠' and <= '⁤')
            or (>= '⁦' and <= '⁩');
    }
}
