using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using Xunit;

namespace SabberStoneCoreTest.CardSets.Standard
{
	public class OutlandDataPatch48313Test
	{
		[Fact]
		public void Build48313_ShouldApplyTraditionalBalanceChanges()
		{
			Assert.Equal(1, Cards.FromId("BT_020").Cost);
			Assert.Equal(1, Cards.FromId("BT_020")[GameTag.ATK]);
			Assert.Equal(4, Cards.FromId("BT_110").Cost);
			Assert.Equal(5, Cards.FromId("BT_114")[GameTag.ATK]);
			Assert.Equal(5, Cards.FromId("BT_138")[GameTag.ATK]);
			Assert.Equal(4, Cards.FromId("BT_188")[GameTag.HEALTH]);
			Assert.Equal(5, Cards.FromId("BT_230")[GameTag.HEALTH]);
			Assert.Equal(1, Cards.FromId("BT_480")[GameTag.ATK]);
			Assert.Equal(5, Cards.FromId("BT_493")[GameTag.HEALTH]);
			Assert.Equal(2, Cards.FromId("ULD_720")[GameTag.ATK]);
			Assert.Equal(2, Cards.FromId("ULD_720")[GameTag.HEALTH]);
		}

		[Fact]
		public void Build48313_ShouldApplyBalanceTextUpdates()
		{
			Assert.Contains("+2/+2", Cards.FromId("BT_213").Text);
			Assert.Contains("+2/+2", Cards.FromId("BT_213e").Text);
			Assert.Contains("+2/+1", Cards.FromId("BT_305").Text);
			Assert.Contains("+2/+1", Cards.FromId("BT_305e").Text);
			Assert.Contains("(1) more", Cards.FromId("BT_711").Text);
			Assert.Contains("(1) more", Cards.FromId("BT_711e").Text);
		}
	}
}
