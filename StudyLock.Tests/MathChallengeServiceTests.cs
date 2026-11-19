using StudyLock.Services;
using Xunit;

namespace StudyLock.Tests;

public sealed class MathChallengeServiceTests
{
    [Fact]
    public void GenerateNext_AlwaysProducesTensAndFivesWithValidNonNegativeAnswer()
    {
        MathChallengeService service = new();
        string? previous = null;

        for (int i = 0; i < 60; i++)
        {
            MathChallenge challenge = service.GenerateNext();

            Assert.NotNull(challenge);
            Assert.NotEmpty(challenge.Expression);
            Assert.NotEqual(previous, challenge.Expression);
            Assert.True(challenge.ExpectedAnswer >= 0);
            Assert.Equal(0, challenge.ExpectedAnswer % 5);

            // Independently evaluate the generated expression to verify correctness
            int evaluated = EvaluateSimpleExpression(challenge.Expression);
            Assert.Equal(evaluated, challenge.ExpectedAnswer);

            previous = challenge.Expression;
        }
    }

    [Fact]
    public void VerifyAnswer_AcceptsAsciiAndArabicIndicDigits()
    {
        MathChallenge challenge = new("20 - 10 - 5 = ?", 5);

        Assert.True(MathChallengeService.VerifyAnswer(challenge, "5"));
        Assert.True(MathChallengeService.VerifyAnswer(challenge, " 5 "));
        Assert.True(MathChallengeService.VerifyAnswer(challenge, "٥"));
        Assert.False(MathChallengeService.VerifyAnswer(challenge, "10"));
        Assert.False(MathChallengeService.VerifyAnswer(challenge, "abc"));
        Assert.False(MathChallengeService.VerifyAnswer(challenge, ""));
    }

    private static int EvaluateSimpleExpression(string expr)
    {
        string lhs = expr.Split('=')[0].Trim();
        string[] tokens = lhs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int result = int.Parse(tokens[0]);
        Assert.Equal(0, result % 5);

        for (int i = 1; i < tokens.Length; i += 2)
        {
            string op = tokens[i];
            int operand = int.Parse(tokens[i + 1]);
            Assert.Equal(0, operand % 5);
            result = op == "+" ? result + operand : result - operand;
        }

        return result;
    }
}
