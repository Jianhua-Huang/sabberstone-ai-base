using System.Collections.Generic;
using System.Linq;
using SabberStoneCore.Actions;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using Xunit;

namespace SabberStoneCoreTest.CardSets.Standard
{
	public class OutlandDataPatch44222Test
	{
		private static Game CreateGame(CardClass playerClass = CardClass.PRIEST, CardClass opponentClass = CardClass.MAGE)
		{
			var fillerDeck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = playerClass,
				Player2HeroClass = opponentClass,
				Player1Deck = fillerDeck,
				Player2Deck = fillerDeck.ToList(),
				Shuffle = false,
				FillDecks = false,
				SkipMulligan = true,
				RandomSeed = 1
			});
			game.StartGame();
			EmptyZone(game.Player1.HandZone.GetAll());
			EmptyZone(game.Player2.HandZone.GetAll());
			EmptyZone(game.Player1.DeckZone.GetAll());
			EmptyZone(game.Player2.DeckZone.GetAll());
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
		public void Build44222_ShouldLoadDemonHunterClassAndSets()
		{
			Assert.Equal("Illidan Stormrage", Cards.HeroCard(CardClass.DEMONHUNTER).Name);
			Assert.Equal(CardClass.DEMONHUNTER, Cards.FromId("HERO_10").Class);
			Assert.Equal(CardClass.DEMONHUNTER, Cards.FromId("BT_187").Class);
			Assert.Equal(CardSet.BLACK_TEMPLE, Cards.FromId("BT_187").Set);
			Assert.Equal(CardSet.DEMON_HUNTER_INITIATE, Cards.FromId("BT_173").Set);
			Assert.Contains(Cards.FromId("BT_187"), Cards.Standard[CardClass.DEMONHUNTER]);
			Assert.Contains(Cards.FromId("BT_173"), Cards.Standard[CardClass.DEMONHUNTER]);
		}

		[Fact]
		public void DemonClaws_HERO_10p_ShouldGiveHeroOneAttackThisTurn()
		{
			Game game = CreateGame(CardClass.DEMONHUNTER);

			game.PlayHeroPower(asZeroCost: true);

			Assert.Equal(1, game.CurrentPlayer.Hero.AttackDamage);
			game.EndTurn();
			Assert.Equal(0, game.CurrentOpponent.Hero.AttackDamage);
		}

		[Fact]
		public void Build44222_ShouldApplyTraditionalBalanceData()
		{
			Assert.Equal(7, Cards.FromId("BOT_238").Cost);
			Assert.Equal(6, Cards.FromId("BOT_270").Cost);
			Assert.Equal(4, Cards.FromId("CS1_112").Cost);
			Assert.Equal(0, Cards.FromId("CS2_004").Cost);
			Assert.Equal(3, Cards.FromId("EX1_334").Cost);
			Assert.Equal(2, Cards.FromId("EX1_339").Cost);
			Assert.Equal(2, Cards.FromId("EX1_622").Cost);
			Assert.Equal(5, Cards.FromId("EX1_623").Cost);
			Assert.Equal(5, Cards.FromId("EX1_623")[GameTag.ATK]);
			Assert.Equal(3, Cards.FromId("TRL_124").Cost);
		}

		[Fact]
		public void PriestCoreChanges_ShouldMatchBuild44222()
		{
			Game game = CreateGame();
			Minion friendly = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			friendly.Damage = 2;
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Bloodfen Raptor", asZeroCost: true);
			game.EndTurn();
			int opponentHealth = game.CurrentOpponent.Hero.Health;

			game.ProcessCard("Holy Nova", asZeroCost: true);

			Assert.Equal(opponentHealth, game.CurrentOpponent.Hero.Health);
			Assert.Equal(3, friendly.Health);
			Assert.Equal(Zone.GRAVEYARD, enemy.Zone.Type);

			int drawn = game.CurrentPlayer[GameTag.NUM_CARDS_DRAWN_THIS_TURN];
			game.ProcessCard("Power Word: Shield", friendly, asZeroCost: true);

			Assert.Equal(drawn, game.CurrentPlayer[GameTag.NUM_CARDS_DRAWN_THIS_TURN]);
			Assert.Equal(5, friendly.BaseHealth);

			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();
			game.ProcessCard("Holy Smite", target, asZeroCost: true);

			Assert.Equal(3, target.Damage);
		}
	}
}
