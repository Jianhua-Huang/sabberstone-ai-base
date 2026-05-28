using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using Xunit;

namespace SabberStoneCoreTest.CardSets.Standard
{
	public class GalakrondsAwakeningDataPatch40734Test
	{
		[Fact]
		public void RiskySkipper_ShouldBePirateInBuild40734()
		{
			Card card = Cards.FromId("YOD_022");

			Assert.True(card.Collectible);
			Assert.True(card.IsRace(Race.PIRATE));
		}

		[Fact]
		public void NonCollectibleDataFixes_ShouldMatchBuild40734()
		{
			Assert.Equal(1, Cards.FromId("DRG_311a").Cost);
			Assert.Equal(1, Cards.FromId("DRG_311b").Cost);
			Assert.Equal(Rarity.RARE, Cards.FromId("YOD_012ts").Rarity);
			Assert.Equal(Rarity.INVALID, Cards.FromId("YOD_038t").Rarity);
		}
	}
}
