using MapOverhaul.Game;
using Xunit;

public class ItemNamesTests : GameTestBase
{
    public ItemNamesTests()
    {
        L.Texts.Clear();
    }

    [Fact]
    public void The_ore_names_come_from_the_name_keys_in_the_order_of_the_metals()
    {
        L.Texts["item.tin_ore.name"] = "Zinnerz";
        L.Texts["item.copper_ore.name"] = "Kupfererz";
        L.Texts["item.iron_ore.name"] = "Eisenerz";
        L.Texts["item.silver_ore.name"] = "Silbererz";

        Assert.Equal(new[] { "Zinnerz", "Kupfererz", "Eisenerz", "Silbererz" }, ItemNames.Ores());
    }

    [Fact]
    public void A_key_with_no_text_falls_back_to_the_english_name()
    {
        L.Texts["item.iron_ore.name"] = "";

        Assert.Equal(new[] { "Tin Ore", "Copper Ore", "Iron Ore", "Silver Ore" }, ItemNames.Ores());
    }
}
