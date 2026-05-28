using System.Linq;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using Xunit;

namespace SabberStoneCoreTest.CardSets.Standard
{
	public class GalakrondsAwakeningDataPatch39954Test
	{
		private static readonly string[] CollectibleCardIds =
		{
			"YOD_001", "YOD_003", "YOD_004", "YOD_005", "YOD_006",
			"YOD_007", "YOD_008", "YOD_009", "YOD_010", "YOD_012",
			"YOD_013", "YOD_014", "YOD_015", "YOD_016", "YOD_017",
			"YOD_018", "YOD_020", "YOD_022", "YOD_023", "YOD_024",
			"YOD_025", "YOD_026", "YOD_027", "YOD_028", "YOD_029",
			"YOD_030", "YOD_032", "YOD_033", "YOD_035", "YOD_036",
			"YOD_038", "YOD_040", "YOD_041", "YOD_042", "YOD_043"
		};

		[Fact]
		public void GalakrondsAwakening_ShouldAddExpectedCollectibleCards()
		{
			Assert.Equal(35, CollectibleCardIds.Length);

			foreach (string cardId in CollectibleCardIds)
			{
				Card card = Cards.FromId(cardId);

				Assert.True(card.Collectible);
				Assert.Equal(CardSet.GALAKRONDS_AWAKENING, card.Set);
			}
		}

		[Fact]
		public void GalakrondsAwakening_KeyCardDataShouldMatchBuild39954()
		{
			Assert.Equal(10, Cards.FromId("YOD_009").Cost);
			Assert.Equal(CardType.HERO, Cards.FromId("YOD_009").Type);
			Assert.Equal(4, Cards.FromId("YOD_042").Cost);
			Assert.Equal(CardType.WEAPON, Cards.FromId("YOD_042").Type);
			Assert.Equal(10, Cards.FromId("YOD_041").Cost);
			Assert.Equal(5, Cards.FromId("YOD_043").Cost);
		}

		[Fact]
		public void GalakrondsAwakening_ShouldNotRemoveExistingCollectibleCards()
		{
			Assert.True(Cards.FromId("DRG_019").Collectible);
			Assert.True(Cards.FromId("DRG_089").Collectible);
			Assert.Equal(35, CollectibleCardIds.Select(Cards.FromId).Count(p => p.Collectible));
		}
	}
}
