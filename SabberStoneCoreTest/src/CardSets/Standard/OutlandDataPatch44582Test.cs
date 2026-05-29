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
		[InlineData("DRG_019")]
		[InlineData("DRG_021")]
		[InlineData("DRG_027")]
		[InlineData("DRG_030")]
		[InlineData("DRG_050")]
		[InlineData("DRG_202")]
		[InlineData("DRG_203")]
		[InlineData("DRG_217")]
		[InlineData("DRG_218")]
		[InlineData("DRG_242")]
		[InlineData("DRG_246")]
		[InlineData("DRG_247")]
		[InlineData("DRG_248")]
		[InlineData("DRG_249")]
		[InlineData("DRG_250")]
		[InlineData("DRG_300")]
		[InlineData("DRG_303")]
		public void Build44582_ShouldMarkGalakrondRelatedCards(string cardId)
		{
			Assert.Equal(1, Cards.FromId(cardId)[GalakrondRelatedTag]);
		}
	}
}
