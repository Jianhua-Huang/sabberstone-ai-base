using System.Collections.Generic;
using System.Linq;
using SabberStoneCore.Actions;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks.PlayerTasks;
using Xunit;

namespace SabberStoneCoreTest.CardSets.Standard
{
	public class OutlandNeutralCardsGenTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null)
		{
			var deck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.DEMONHUNTER,
				Player2HeroClass = CardClass.MAGE,
				Player1Deck = (playerDeck ?? deck).ToList(),
				Player2Deck = (opponentDeck ?? deck).ToList(),
				Shuffle = false,
				FillDecks = false,
				SkipMulligan = true,
				RandomSeed = 1
			});
			game.StartGame();
			EmptyZone(game.Player1.HandZone.GetAll());
			EmptyZone(game.Player2.HandZone.GetAll());
			game.Player1.BaseMana = 10;
			game.Player2.BaseMana = 10;
			return game;
		}

		private static void EmptyZone(IEnumerable<IPlayable> cards)
		{
			foreach (IPlayable card in cards.ToArray())
				Generic.RemoveFromZone(card.Controller, card);
		}

		[Fact]
		public void RustswornInitiate_BT_008_ShouldSummonSpellDamageImpcasterOnDeathrattle()
		{
			Game game = CreateGame();
			Minion initiate = game.ProcessCard<Minion>("Rustsworn Initiate", asZeroCost: true);

			game.ProcessCard("Fireball", initiate, asZeroCost: true);

			Minion impcaster = Assert.Single(game.CurrentPlayer.BoardZone.Where(p => p.Card.Id == "BT_008t"));
			Assert.Equal(1, impcaster.AttackDamage);
			Assert.Equal(1, impcaster.Health);
			Assert.Equal(1, impcaster[GameTag.SPELLPOWER]);
		}

		[Fact]
		public void FelfinNavigator_BT_010_ShouldBuffOtherFriendlyMurlocs()
		{
			Game game = CreateGame();
			Minion murloc1 = game.ProcessCard<Minion>("Murloc Raider", asZeroCost: true);
			Minion murloc2 = game.ProcessCard<Minion>("Murloc Raider", asZeroCost: true);

			Minion navigator = game.ProcessCard<Minion>("Felfin Navigator", asZeroCost: true);

			Assert.Equal(3, murloc1.AttackDamage);
			Assert.Equal(2, murloc1.Health);
			Assert.Equal(3, murloc2.AttackDamage);
			Assert.Equal(2, murloc2.Health);
			Assert.Equal(4, navigator.AttackDamage);
			Assert.Equal(4, navigator.Health);
		}

		[Fact]
		public void FrozenShadoweaver_BT_714_ShouldFreezeEnemyTarget()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Frozen Shadoweaver", target, asZeroCost: true);

			Assert.True(target.IsFrozen);
		}

		[Theory]
		[InlineData("Bonechewer Brawler", 4)]
		[InlineData("Bonechewer Vanguard", 6)]
		public void BonechewerMinions_ShouldGainAttackWhenDamaged(string cardName, int expectedAttack)
		{
			Game game = CreateGame();
			Minion minion = game.ProcessCard<Minion>(cardName, asZeroCost: true);

			game.ProcessCard("Moonfire", minion, asZeroCost: true);

			Assert.Equal(1, minion[GameTag.TAUNT]);
			Assert.Equal(expectedAttack, minion.AttackDamage);
		}

		[Fact]
		public void RuststeedRaider_BT_720_ShouldGainTemporaryAttack()
		{
			Game game = CreateGame();

			Minion raider = game.ProcessCard<Minion>("Ruststeed Raider", asZeroCost: true);

			Assert.Equal(1, raider[GameTag.TAUNT]);
			Assert.Equal(1, raider[GameTag.RUSH]);
			Assert.Equal(5, raider.AttackDamage);
			game.EndTurn();
			Assert.Equal(1, game.CurrentOpponent.BoardZone[0].AttackDamage);
		}

		[Fact]
		public void GuardianAugmerchant_BT_722_ShouldDamageMinionAndGiveDivineShield()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);

			game.ProcessCard("Guardian Augmerchant", target, asZeroCost: true);

			Assert.Equal(1, target.Damage);
			Assert.Equal(1, target[GameTag.DIVINE_SHIELD]);
		}

		[Fact]
		public void RocketAugmerchant_BT_723_ShouldDamageMinionAndGiveRush()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);

			game.ProcessCard("Rocket Augmerchant", target, asZeroCost: true);

			Assert.Equal(1, target.Damage);
			Assert.Equal(1, target[GameTag.RUSH]);
			Assert.False(target.IsExhausted);
		}

		[Fact]
		public void EtherealAugmerchant_BT_724_ShouldDamageMinionAndGiveSpellDamage()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);

			game.ProcessCard("Ethereal Augmerchant", target, asZeroCost: true);

			Assert.Equal(1, target.Damage);
			Assert.Equal(1, target[GameTag.SPELLPOWER]);
		}

		[Fact]
		public void SoulboundAshtongue_BT_727_ShouldDamageControllerHeroWhenDamaged()
		{
			Game game = CreateGame();
			Minion ashtongue = game.ProcessCard<Minion>("Soulbound Ashtongue", asZeroCost: true);

			game.ProcessCard("Moonfire", ashtongue, asZeroCost: true);

			Assert.Equal(1, ashtongue.Damage);
			Assert.Equal(1, game.CurrentPlayer.Hero.Damage);
		}

		[Fact]
		public void OverconfidentOrc_BT_730_ShouldLoseAttackWhenDamaged()
		{
			Game game = CreateGame();
			Minion orc = game.ProcessCard<Minion>("Overconfident Orc", asZeroCost: true);

			Assert.Equal(1, orc[GameTag.TAUNT]);
			Assert.Equal(3, orc.AttackDamage);
			game.ProcessCard("Moonfire", orc, asZeroCost: true);
			Assert.Equal(1, orc.AttackDamage);
		}
	}
}
