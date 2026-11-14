using MapOverhaul;
using Xunit;

public class MapOpenRuleTests
{
    [Fact]
    public void The_first_call_is_an_open()
    {
        Assert.True(new MapOpenRule().IsOpen(100));
    }

    [Fact]
    public void A_call_one_frame_after_the_last_is_no_open()
    {
        var rule = new MapOpenRule();
        rule.IsOpen(100);

        Assert.False(rule.IsOpen(101));
        Assert.False(rule.IsOpen(102));
        Assert.False(rule.IsOpen(102));
    }

    [Fact]
    public void A_call_two_or_more_frames_after_the_last_is_an_open()
    {
        var rule = new MapOpenRule();
        rule.IsOpen(100);

        Assert.True(rule.IsOpen(102));
        Assert.True(rule.IsOpen(500));
    }

    [Fact]
    public void Reset_makes_the_next_call_an_open()
    {
        var rule = new MapOpenRule();
        rule.IsOpen(100);

        rule.Reset();

        Assert.True(rule.IsOpen(101));
    }
}
