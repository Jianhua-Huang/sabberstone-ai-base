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
		public void NetherwindPortal_BT_003_ShouldSummonRandomFourCostMinionAfterOpponentCastsSpell()
		{
			Game game = CreateGame();
			game.ProcessCard("Netherwind Portal", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Moonfire", game.CurrentOpponent.Hero, asZeroCost: true);

			Assert.Empty(game.CurrentOpponent.SecretZone);
			Assert.Contains(game.CurrentOpponent.BoardZone, p => p.Cost == 4);
		}

		[Fact]
		public void FontOfPower_BT_021_ShouldKeepAllThreeMageMinionsWhenDeckHasNoMinions()
		{
			Game game = CreateGame();
			SetDeck(game, "Fireball", "Moonfire");
			int handCount = game.CurrentPlayer.HandZone.Count;

			game.ProcessCard("Font of Power", asZeroCost: true);

			Assert.Null(game.CurrentPlayer.Choice);
			Assert.Equal(handCount + 3, game.CurrentPlayer.HandZone.Count);
			Assert.All(game.CurrentPlayer.HandZone, card =>
			{
				Assert.Equal(CardType.MINION, card.Card.Type);
				Assert.Equal(CardClass.MAGE, card.Card.Class);
			});
		}

		[Fact]
		public void FontOfPower_BT_021_ShouldDiscoverOneMageMinionWhenDeckHasMinion()
		{
			Game game = CreateGame();
			SetDeck(game, "Wisp", "Fireball");

			game.ProcessCard("Font of Power", asZeroCost: true);

			Assert.NotNull(game.CurrentPlayer.Choice);
			Assert.All(game.CurrentPlayer.Choice.Choices, choice =>
			{
				Card card = game.IdEntityDic[choice].Card;
				Assert.Equal(CardType.MINION, card.Type);
				Assert.Equal(CardClass.MAGE, card.Class);
			});
			int handCount = game.CurrentPlayer.HandZone.Count;
			game.Process(ChooseTask.Pick(game.CurrentPlayer, game.CurrentPlayer.Choice.Choices[0]));
			Assert.Equal(handCount + 1, game.CurrentPlayer.HandZone.Count);
		}

		[Fact]
		public void ApexisSmuggler_BT_022_ShouldOfferSpellAfterPlayingSecret()
		{
			Game game = CreateGame();
			game.ProcessCard("Apexis Smuggler", asZeroCost: true);

			game.ProcessCard("Netherwind Portal", asZeroCost: true);

			Assert.NotNull(game.CurrentPlayer.Choice);
			Assert.All(game.CurrentPlayer.Choice.Choices, choice =>
				Assert.Equal(CardType.SPELL, game.IdEntityDic[choice].Card.Type));
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
