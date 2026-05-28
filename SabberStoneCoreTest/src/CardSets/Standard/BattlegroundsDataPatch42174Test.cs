using System.Linq;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using Xunit;

namespace SabberStoneCoreTest.CardSets.Standard
{
	public class BattlegroundsDataPatch42174Test
	{
		[Fact]
		public void BattlegroundsDragonCards_ShouldLoadFromBuild42174()
		{
			Card redWhelp = Cards.FromId("BGS_019");
			Card kalecgos = Cards.FromId("BGS_041");
			Card galakrondHero = Cards.FromId("TB_BaconShop_HERO_02");
			Card galakrondHeroPower = Cards.FromId("TB_BaconShop_HP_011");

			Assert.Equal(CardSet.BATTLEGROUNDS, redWhelp.Set);
			Assert.Equal(Race.DRAGON, redWhelp.GetRawRace());
			Assert.Equal(1, redWhelp.Cost);
			Assert.Equal(1, redWhelp[GameTag.ATK]);
			Assert.Equal(2, redWhelp[GameTag.HEALTH]);

			Assert.Equal(CardSet.BATTLEGROUNDS, kalecgos.Set);
			Assert.Equal(Race.DRAGON, kalecgos.GetRawRace());
			Assert.Equal(CardType.HERO, galakrondHero.Type);
			Assert.Equal(CardType.HERO_POWER, galakrondHeroPower.Type);
			Assert.Equal(1, galakrondHeroPower.Cost);
		}

		[Fact]
		public void BattlegroundsBalanceChanges_ShouldMatchBuild42174()
		{
			Card mamaBear = Cards.FromId("BGS_021");
			Card goldenMamaBear = Cards.FromId("TB_BaconUps_090");

			Assert.Equal(5, mamaBear[GameTag.ATK]);
			Assert.Equal(5, mamaBear[GameTag.HEALTH]);
			Assert.Contains("+5/+5", mamaBear.Text);

			Assert.Equal(10, goldenMamaBear[GameTag.ATK]);
			Assert.Equal(10, goldenMamaBear[GameTag.HEALTH]);
			Assert.Contains("+10/+10", goldenMamaBear.Text);

			Assert.Equal(3, Cards.FromId("TB_BaconShop_HP_010").Cost);
			Assert.Equal(4, Cards.FromId("TB_BaconShop_HP_046").Cost);
			Assert.Equal(10, Cards.FromId("TB_BaconShopTechUp06_Button").Cost);
		}

		[Fact]
		public void RemovedBattlegroundsHeroPowers_ShouldNotExistInBuild42174()
		{
			Assert.DoesNotContain(Cards.All, card => card.Id == "TB_BaconShop_HP_038");
			Assert.DoesNotContain(Cards.All, card => card.Id == "TB_BaconShop_HP_045");
		}
	}
}
