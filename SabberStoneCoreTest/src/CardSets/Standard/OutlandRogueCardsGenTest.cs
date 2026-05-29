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
	public class OutlandRogueCardsGenTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null)
		{
			var deck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.ROGUE,
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

		private static void SetDeck(Game game, params string[] cardNames)
		{
			EmptyZone(game.CurrentPlayer.DeckZone.GetAll());
			foreach (string cardName in cardNames)
				game.CurrentPlayer.DeckZone.Add(Entity.FromCard(game.CurrentPlayer, Cards.FromName(cardName)));
		}

		[Fact]
		public void Spymistress_BT_701_ShouldHaveStealth()
		{
			Game game = CreateGame();

			Minion spymistress = game.ProcessCard<Minion>("Spymistress", asZeroCost: true);

			Assert.Equal(1, spymistress[GameTag.STEALTH]);
		}

		[Fact]
		public void AshtongueSlayer_BT_702_ShouldBuffStealthedMinionAndGiveImmuneThisTurn()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Spymistress", asZeroCost: true);

			game.ProcessCard("Ashtongue Slayer", target, asZeroCost: true);

			Assert.Equal(6, target.AttackDamage);
			Assert.Equal(1, target[GameTag.IMMUNE]);
			game.EndTurn();
			Assert.Equal(0, game.CurrentOpponent.BoardZone[0][GameTag.IMMUNE]);
		}

		[Fact]
		public void CursedVagrant_BT_703_ShouldSummonStealthedShadowOnDeathrattle()
		{
			Game game = CreateGame();
			Minion vagrant = game.ProcessCard<Minion>("Cursed Vagrant", asZeroCost: true);

			game.ProcessCard("Fireball", vagrant, asZeroCost: true);
			game.ProcessCard("Moonfire", vagrant, asZeroCost: true);

			Minion shadow = Assert.Single(game.CurrentPlayer.BoardZone.Where(p => p.Card.Id == "BT_703t"));
			Assert.Equal(7, shadow.AttackDamage);
			Assert.Equal(5, shadow.Health);
			Assert.Equal(1, shadow[GameTag.STEALTH]);
		}

		[Fact]
		public void GreyheartSage_BT_710_ShouldDrawTwoIfYouControlStealthedMinion()
		{
			Game game = CreateGame();
			SetDeck(game, "Wisp", "River Crocolisk");
			game.ProcessCard("Spymistress", asZeroCost: true);
			int handCount = game.CurrentPlayer.HandZone.Count;

			game.ProcessCard("Greyheart Sage", asZeroCost: true);

			Assert.Equal(handCount + 2, game.CurrentPlayer.HandZone.Count);
		}

		[Fact]
		public void Akama_BT_713_ShouldShufflePrimeOnDeathrattle()
		{
			Game game = CreateGame();
			Minion akama = game.ProcessCard<Minion>("Akama", asZeroCost: true);
			int deckCount = game.CurrentPlayer.DeckZone.Count;

			game.ProcessCard("Fireball", akama, asZeroCost: true);

			Assert.Equal(deckCount + 1, game.CurrentPlayer.DeckZone.Count);
			Assert.Contains(game.CurrentPlayer.DeckZone, p => p.Card.Id == "BT_713t");
		}
	}
}
