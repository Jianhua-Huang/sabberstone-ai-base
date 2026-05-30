using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using Xunit;

namespace SabberStoneCoreTest.CardSets.Standard
{
	public class OutlandDataPatch44582Test
	{
		private const GameTag GalakrondRelatedTag = (GameTag)676;

		[Fact]
		public void Build44582_ShouldApplyTraditionalDataChanges()
		{
			Assert.Equal(Rarity.COMMON, Cards.FromId("AT_037").Rarity);
			Assert.Equal(Rarity.FREE, Cards.FromId("EX1_193").Rarity);
			Assert.Equal(Rarity.FREE, Cards.FromId("EX1_194").Rarity);
			Assert.False(Cards.FromId("LOOT_526e").Collectible);
		}

		[Theory]
		[InlineData("DRG_030")]
		[InlineData("DRG_246")]
		[InlineData("DRG_247")]
		public void GalakrondHeroCards_ShouldKeepGalakrondRelatedTag(string cardId)
		{
			Assert.Equal(1, Cards.FromId(cardId)[GalakrondRelatedTag]);
		}
	}
}
