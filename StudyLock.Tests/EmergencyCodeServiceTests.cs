using StudyLock.Services;
using Xunit;

namespace StudyLock.Tests;

public sealed class EmergencyCodeServiceTests
{
    [Fact]
    public void GenerateCode_ProducesExact55CharMixedEntropyString()
    {
        string code1 = EmergencyCodeService.GenerateCode();
        string code2 = EmergencyCodeService.GenerateCode();

        Assert.Equal(55, code1.Length);
        Assert.Equal(55, code2.Length);
        Assert.NotEqual(code1, code2);

        Assert.Contains(code1, char.IsUpper);
        Assert.Contains(code1, char.IsLower);
        Assert.Contains(code1, char.IsDigit);
        Assert.Contains(code1, c => !char.IsLetterOrDigit(c));
    }

    [Fact]
    public void IsExactMatch_IsCaseSensitiveAndStrict()
    {
        string code = EmergencyCodeService.GenerateCode();
        Assert.True(EmergencyCodeService.IsExactMatch(code, code));
        Assert.False(EmergencyCodeService.IsExactMatch(code, code + " "));
        Assert.False(EmergencyCodeService.IsExactMatch(code, code[..^1]));
        Assert.False(EmergencyCodeService.IsExactMatch(code, ""));
    }
}
