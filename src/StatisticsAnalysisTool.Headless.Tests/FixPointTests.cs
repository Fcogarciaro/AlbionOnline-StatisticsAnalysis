using StatisticsAnalysisTool.Common;

namespace StatisticsAnalysisTool.Headless.Tests;

public class FixPointTests
{
    [Theory]
    [InlineData(100000000L, 10000.0)]
    [InlineData(15000L, 1.5)]
    [InlineData(0L, 0.0)]
    public void InternalValue_DecodesWithFactor10000(long internalValue, double expected)
    {
        Assert.Equal(expected, FixPoint.FromInternalValue(internalValue).DoubleValue);
    }
}
