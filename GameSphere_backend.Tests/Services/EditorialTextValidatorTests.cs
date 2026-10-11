#nullable enable

using GameSphere_backend.Services;
using Xunit;

namespace GameSphere_backend.Tests.Services;

public sealed class EditorialTextValidatorTests
{
    private readonly EditorialTextValidator _validator = new();

    [Theory]
    [InlineData("\u0000")]
    [InlineData("\u0007")]
    [InlineData("\u007F")]
    [InlineData("\u0085")]
    [InlineData("​")]
    [InlineData("‌")]
    [InlineData("‎")]
    [InlineData("‏")]
    [InlineData("‪")]
    [InlineData("‫")]
    [InlineData("‬")]
    [InlineData("‭")]
    [InlineData("‮")]
    [InlineData("⁠")]
    [InlineData("⁢")]
    [InlineData("⁤")]
    [InlineData("⁦")]
    [InlineData("⁧")]
    [InlineData("⁨")]
    [InlineData("⁩")]
    [InlineData("﻿")]
    public void Validator_rejects_forbidden_characters_in_every_context(string forbidden)
    {
        var text = $"Hello{forbidden}World";

        Assert.Equal(EditorialTextValidator.UnsupportedCharactersMessage, _validator.Validate(text, allowLineBreaks: false));
        Assert.Equal(EditorialTextValidator.UnsupportedCharactersMessage, _validator.Validate(text, allowLineBreaks: true));
    }

    [Fact]
    public void Validator_allows_zero_width_joiner_for_composite_emoji()
    {
        Assert.Null(_validator.Validate("\U0001F468‍\U0001F469‍\U0001F467", allowLineBreaks: false));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\t")]
    public void Validator_allows_line_breaks_and_tabs_only_when_requested(string whitespace)
    {
        var text = $"Line one{whitespace}line two";

        Assert.Null(_validator.Validate(text, allowLineBreaks: true));
        Assert.Equal(EditorialTextValidator.UnsupportedCharactersMessage, _validator.Validate(text, allowLineBreaks: false));
    }

    [Theory]
    [InlineData("Capital of Portugal?")]
    [InlineData("<b>Olá</b> & ção — 日本語")]
    [InlineData("")]
    [InlineData(null)]
    public void Validator_accepts_regular_text(string? text)
    {
        Assert.Null(_validator.Validate(text, allowLineBreaks: false));
    }

    [Theory]
    [InlineData("⁡")]
    [InlineData("⁣")]
    public void Validator_rejects_the_remaining_invisible_operators_in_the_forbidden_range(string forbidden)
    {
        Assert.Equal(EditorialTextValidator.UnsupportedCharactersMessage, _validator.Validate($"a{forbidden}b", allowLineBreaks: true));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("‍")]
    [InlineData("‐")]
    [InlineData(" ")]
    [InlineData(" ")]
    [InlineData("⁥")]
    [InlineData("⁪")]
    [InlineData(" ")]
    [InlineData("️")]
    public void Validator_accepts_characters_just_outside_the_forbidden_ranges(string allowed)
    {
        Assert.Null(_validator.Validate($"a{allowed}b", allowLineBreaks: false));
    }
}
