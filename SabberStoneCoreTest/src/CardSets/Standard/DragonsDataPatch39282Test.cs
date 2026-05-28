using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using Xunit;

namespace SabberStoneCoreTest.CardSets.Standard
{
	public class DragonsDataPatch39282Test
	{
		[Fact]
		public void CardData_ShouldMatchPatch39282BalanceChanges()
		{
			Assert.Equal(4, Cards.FromId("DRG_019").Cost);
			Assert.Equal(2, Cards.FromId("DRG_025")[GameTag.DURABILITY]);
			Assert.Equal(5, Cards.FromId("DRG_031").Cost);
			Assert.Equal(2, Cards.FromId("DRG_248").Cost);
			Assert.Equal(4, Cards.FromId("DRG_250").Cost);
		}

		[Fact]
		public void CardText_ShouldMatchPatch39282EffectTextChanges()
		{
			Assert.Contains("other", Cards.FromId("DRG_089").Text);
			Assert.Contains("+2/+2", Cards.FromId("DRG_217").Text);
			Assert.Contains("+2/+2", Cards.FromId("DRG_217e").Text);
		}
	}
}
