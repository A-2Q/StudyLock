using System.Security.Cryptography;

namespace StudyLock.Services;

public sealed record MathChallenge(string Expression, int ExpectedAnswer);

public sealed class MathChallengeService
{
    private static readonly MathChallenge[] CuratedTemplates =
    [
        new("10 - 5 - 5 = ?", 0),
        new("20 - 10 - 5 = ?", 5),
        new("15 - 5 - 5 = ?", 5),
        new("10 + 10 - 5 = ?", 15),
        new("20 - 5 - 5 = ?", 10),
        new("25 - 10 - 5 = ?", 10),
        new("30 - 10 - 5 = ?", 15),
        new("15 + 5 - 10 = ?", 10),
        new("10 + 5 + 5 = ?", 20),
        new("20 + 10 - 5 = ?", 25),
        new("30 - 15 - 5 = ?", 10),
        new("25 - 15 - 5 = ?", 5),
        new("15 + 10 - 5 = ?", 20),
        new("20 - 15 + 5 = ?", 10),
        new("30 - 20 + 5 = ?", 15),
        new("10 + 15 - 10 = ?", 15),
        new("25 - 5 - 10 = ?", 10),
        new("20 + 5 - 10 = ?", 15),
        new("15 + 15 - 10 = ?", 20),
        new("30 - 10 - 10 = ?", 10)
    ];

    private string? _lastExpression;

    public MathChallenge GenerateNext()
    {
        MathChallenge selected;
        int attempts = 0;

        do
        {
            int index = RandomNumberGenerator.GetInt32(CuratedTemplates.Length);
            selected = CuratedTemplates[index];
            attempts++;
        }
        while (selected.Expression == _lastExpression && attempts < 10);

        _lastExpression = selected.Expression;
        return selected;
    }

    public static bool VerifyAnswer(MathChallenge challenge, string? userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return false;
        }

        // Convert Arabic-Indic digits (٠-٩) to ASCII digits seamlessly
        string normalized = ConvertArabicDigitsToAscii(userInput.Trim());
        return int.TryParse(normalized, out int parsed) && parsed == challenge.ExpectedAnswer;
    }

    public static string ConvertArabicDigitsToAscii(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        char[] chars = input.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= '\u0660' and <= '\u0669')
            {
                chars[i] = (char)('0' + (chars[i] - '\u0660'));
            }
            else if (chars[i] is >= '\u06F0' and <= '\u06F9')
            {
                chars[i] = (char)('0' + (chars[i] - '\u06F0'));
            }
        }
        return new string(chars);
    }
}
