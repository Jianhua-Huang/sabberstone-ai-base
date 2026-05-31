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
	public class Year2021BarrensFrenzyCardsTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null,
			CardClass player1HeroClass = CardClass.WARRIOR, CardClass player2HeroClass = CardClass.WARLOCK)
		{
			var defaultDeck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = player1HeroClass,
				Player2HeroClass = player2HeroClass,
				Player1Deck = (playerDeck ?? defaultDeck).ToList(),
				Player2Deck = defaultDeck.ToList(),
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

		private static void Damage(Minion minion, int amount)
		{
			Generic.DamageCharFunc.Invoke(minion.Controller.Hero, minion, amount, false);
		}

		[Fact]
		public void OasisThrasher_FrenzyDealsThreeToEnemyHeroOnceOnlyAfterSurvivingDamage()
		{
			Game game = CreateGame();
			Minion thrasher = game.ProcessCard<Minion>("Oasis Thrasher", asZeroCost: true);

			Damage(thrasher, 1);

			Assert.Equal(27, game.Player2.Hero.Health);

			Damage(thrasher, 1);

			Assert.Equal(27, game.Player2.Hero.Health);
		}

		[Fact]
		public void OasisThrasher_FrenzyDoesNotTriggerWhenDamageKillsTheMinion()
		{
			Game game = CreateGame();
			Minion thrasher = game.ProcessCard<Minion>("Oasis Thrasher", asZeroCost: true);

			Damage(thrasher, thrasher.Health);

			Assert.Equal(30, game.Player2.Hero.Health);
		}

		[Fact]
		public void EfficientOctobot_FrenzyReducesCardsInHandByOne()
		{
			Game game = CreateGame(player1HeroClass: CardClass.ROGUE);
			IPlayable first = Generic.DrawCard(game.Player1, Cards.FromName("River Crocolisk"));
			IPlayable second = Generic.DrawCard(game.Player1, Cards.FromName("Chillwind Yeti"));
			Minion octobot = game.ProcessCard<Minion>("Efficient Octo-bot", asZeroCost: true);

			Damage(octobot, 1);

			Assert.Equal(1, first.Cost);
			Assert.Equal(3, second.Cost);
		}

		[Fact]
		public void WarsongEnvoy_FrenzyGainsAttackForEachDamagedCharacter()
		{
			Game game = CreateGame();
			Minion envoy = game.ProcessCard<Minion>("Warsong Envoy", asZeroCost: true);
			Minion friendly = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			game.Process(EndTurnTask.Any(game.CurrentPlayer));
			Minion enemy = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			game.Process(EndTurnTask.Any(game.CurrentPlayer));
			Generic.DamageCharFunc.Invoke(game.Player1.Hero, friendly, 1, false);
			Generic.DamageCharFunc.Invoke(game.Player2.Hero, enemy, 1, false);
			Generic.DamageCharFunc.Invoke(game.Player1.Hero, game.Player1.Hero, 1, false);

			Damage(envoy, 1);

			Assert.Equal(5, envoy.AttackDamage);
		}

		[Fact]
		public void StonemaulAnchorman_FrenzyDrawsOneCard()
		{
			Game game = CreateGame(Enumerable.Repeat(Cards.FromName("River Crocolisk"), 5));
			Minion anchorman = game.ProcessCard<Minion>("Stonemaul Anchorman", asZeroCost: true);

			Damage(anchorman, 1);

			IPlayable drawn = Assert.Single(game.Player1.HandZone);
			Assert.Equal("River Crocolisk", drawn.Card.Name);
		}
	}
}
