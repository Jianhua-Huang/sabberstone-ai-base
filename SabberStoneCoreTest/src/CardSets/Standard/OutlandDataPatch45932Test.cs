using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using Xunit;

namespace SabberStoneCoreTest.CardSets.Standard
{
	public class OutlandDataPatch45932Test
	{
		[Fact]
		public void Build45932_ShouldApplyTraditionalBalanceChanges()
		{
			Assert.Equal(5, Cards.FromId("BT_011").Cost);
			Assert.True(Cards.FromId("BT_230").IsRace(Race.BEAST));
			Assert.Equal(7, Cards.FromId("BT_255").Cost);
			Assert.Equal(1, Cards.FromId("BT_351")[GameTag.ATK]);
			Assert.Equal(6, Cards.FromId("BT_495")[GameTag.ATK]);
			Assert.Equal(4, Cards.FromId("BT_937")[GameTag.ATK]);
			Assert.Equal(4, Cards.FromId("BT_937").Cost);
			Assert.Equal(4, Cards.FromId("DRG_071").Cost);
			Assert.Contains("friendly Demon", Cards.FromId("NEW1_003").Text);
			Assert.Equal(8, Cards.FromId("UNG_028")[GameTag.QUEST_PROGRESS_TOTAL]);
			Assert.Equal(4, Cards.FromId("UNG_832").Cost);
			Assert.Equal(2, Cards.FromId("YOD_032")[GameTag.HEALTH]);
		}

		[Fact]
		public void Build45932_ShouldLoadPuzzleOnlySpellEntities()
		{
			Card holySmitePuzzle = Cards.FromId("CS1_130_Puzzle");
			Card powerWordShieldPuzzle = Cards.FromId("CS2_004_Puzzle");

			Assert.Equal("Holy Smite", holySmitePuzzle.Name);
			Assert.Equal(CardType.SPELL, holySmitePuzzle.Type);
			Assert.False(holySmitePuzzle.Collectible);
			Assert.Equal("Power Word: Shield", powerWordShieldPuzzle.Name);
			Assert.Equal(CardType.SPELL, powerWordShieldPuzzle.Type);
			Assert.False(powerWordShieldPuzzle.Collectible);
		}
	}
}
