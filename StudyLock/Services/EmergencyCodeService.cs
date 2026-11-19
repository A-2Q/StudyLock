using System.Security.Cryptography;

namespace StudyLock.Services;

public sealed class EmergencyCodeService
{
    public const int RequiredLength = 55;

    private const string UpperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijkmnopqrstuvwxyz";
    private const string DigitChars = "23456789";
    private const string SymbolChars = "!@#$%&*?-+=^";
    private const string AllChars = UpperChars + LowerChars + DigitChars + SymbolChars;

    public static string GenerateCode(int length = RequiredLength)
    {
        if (length < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Code length must be at least 4.");
        }

        char[] buffer = new char[length];

        // Guarantee at least one character from each category
        buffer[0] = UpperChars[RandomNumberGenerator.GetInt32(UpperChars.Length)];
        buffer[1] = LowerChars[RandomNumberGenerator.GetInt32(LowerChars.Length)];
        buffer[2] = DigitChars[RandomNumberGenerator.GetInt32(DigitChars.Length)];
        buffer[3] = SymbolChars[RandomNumberGenerator.GetInt32(SymbolChars.Length)];

        for (int i = 4; i < length; i++)
        {
            buffer[i] = AllChars[RandomNumberGenerator.GetInt32(AllChars.Length)];
        }

        // Fisher-Yates shuffle using CSPRNG
        for (int i = buffer.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
        }

        return new string(buffer);
    }

    public static bool IsExactMatch(string expectedCode, string? typedInput)
    {
        if (string.IsNullOrEmpty(expectedCode) || string.IsNullOrEmpty(typedInput))
        {
            return false;
        }

        return string.Equals(expectedCode, typedInput, StringComparison.Ordinal);
    }
}
