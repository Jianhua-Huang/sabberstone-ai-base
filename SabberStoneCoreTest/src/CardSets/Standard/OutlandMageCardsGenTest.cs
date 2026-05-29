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
	public class OutlandMageCardsGenTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null)
		{
			var deck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.MAGE,
				Player2HeroClass = CardClass.WARRIOR,
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

		private static void SetDeck(Game game, params string[] cardNames)
		{
			EmptyZone(game.CurrentPlayer.DeckZone.GetAll());
			foreach (string cardName in cardNames)
				game.CurrentPlayer.DeckZone.Add(Entity.FromCard(game.CurrentPlayer, Cards.FromName(cardName)));
		}

		[Fact]
		public void IncantersFlow_BT_002_ShouldReduceSpellsInDeck()
		{
			Game game = CreateGame();
			SetDeck(game, "Fireball", "Wisp");

			game.ProcessCard("Incanter's Flow", asZeroCost: true);

			Assert.Equal(3, game.CurrentPlayer.DeckZone.Single(p => p.Card.Name == "Fireball").Cost);
			Assert.Equal(0, game.CurrentPlayer.DeckZone.Single(p => p.Card.Name == "Wisp").Cost);
		}

		[Fact]
		public void Starscryer_BT_014_ShouldDrawSpellOnDeathrattle()
		{
			Game game = CreateGame();
			SetDeck(game, "Wisp", "Fireball");
			Minion starscryer = game.ProcessCard<Minion>("Starscryer", asZeroCost: true);

			game.ProcessCard("Moonfire", starscryer, asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Fireball");
		}

		[Fact]
		public void AstromancerSolarian_BT_028_ShouldShufflePrimeOnDeathrattle()
		{
			Game game = CreateGame();
			Minion solarian = game.ProcessCard<Minion>("Astromancer Solarian", asZeroCost: true);
			int deckCount = game.CurrentPlayer.DeckZone.Count;

			game.ProcessCard("Fireball", solarian, asZeroCost: true);

			Assert.Equal(deckCount + 1, game.CurrentPlayer.DeckZone.Count);
			Assert.Contains(game.CurrentPlayer.DeckZone, p => p.Card.Id == "BT_028t");
		}

		[Fact]
		public void DeepFreeze_BT_072_ShouldFreezeEnemyAndSummonWaterElementals()
		{
			Game game = CreateGame();

			game.ProcessCard("Deep Freeze", game.CurrentOpponent.Hero, asZeroCost: true);

			Assert.True(game.CurrentOpponent.Hero.IsFrozen);
			Assert.Equal(2, game.CurrentPlayer.BoardZone.Count(p => p.Card.Name == "Water Elemental"));
		}

		[Fact]
		public void ApexisBlast_BT_291_ShouldDamageAndSummonWhenDeckHasNoMinions()
		{
			Game game = CreateGame();
			SetDeck(game, "Fireball", "Moonfire");

			game.ProcessCard("Apexis Blast", game.CurrentOpponent.Hero, asZeroCost: true);

			Assert.Equal(5, game.CurrentOpponent.Hero.Damage);
			Assert.Contains(game.CurrentPlayer.BoardZone, p => p.Cost == 5);
		}
	}
}
