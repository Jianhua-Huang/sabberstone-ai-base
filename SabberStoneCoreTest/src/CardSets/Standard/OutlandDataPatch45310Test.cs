using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using Xunit;

namespace SabberStoneCoreTest.CardSets.Standard
{
	public class OutlandDataPatch45310Test
	{
		[Fact]
		public void Build45310_ShouldApplyDemonHunterBalanceChanges()
		{
			Assert.Equal(6, Cards.FromId("BT_601").Cost);
			Assert.Equal(2, Cards.FromId("BT_921")[GameTag.DURABILITY]);
			Assert.Equal(6, Cards.FromId("BT_934").Cost);
			Assert.Contains("This costs (1)", Cards.FromId("BT_801").Text);
		}
	}
}
