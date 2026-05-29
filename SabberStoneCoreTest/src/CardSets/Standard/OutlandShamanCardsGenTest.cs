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
	public class OutlandShamanCardsGenTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null)
		{
			var deck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.SHAMAN,
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
		public void SerpentshrinePortal_BT_100_ShouldDamageSummonAndOverload()
		{
			Game game = CreateGame();

			game.ProcessCard("Serpentshrine Portal", game.CurrentOpponent.Hero, asZeroCost: true);

			Assert.Equal(3, game.CurrentOpponent.Hero.Damage);
			Assert.Single(game.CurrentPlayer.BoardZone, p => p.Cost == 3);
			Assert.Equal(1, game.CurrentPlayer.OverloadOwed);
		}

		[Fact]
		public void VividSpores_BT_101_ShouldResummonMinionOnDeathrattle()
		{
			Game game = CreateGame();
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard("Vivid Spores", asZeroCost: true);
			game.ProcessCard("Moonfire", wisp, asZeroCost: true);

			Assert.Single(game.CurrentPlayer.BoardZone, p => p.Card.Name == "Wisp" && p != wisp);
		}

		[Fact]
		public void BoggspineKnuckles_BT_102_ShouldTransformFriendlyMinionsAfterHeroAttack()
		{
			Game game = CreateGame();
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			game.ProcessCard("Boggspine Knuckles", asZeroCost: true);

			game.Process(HeroAttackTask.Any(game.CurrentPlayer, game.CurrentOpponent.Hero));

			Assert.Contains(wisp, game.CurrentPlayer.BoardZone);
			Assert.Equal(1, wisp.Cost);
			Assert.NotEqual("Wisp", wisp.Card.Name);
		}

		[Fact]
		public void BogstrokClacker_BT_106_ShouldTransformAdjacentMinions()
		{
			Game game = CreateGame();
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard("Bogstrok Clacker", asZeroCost: true);

			Assert.Contains(wisp, game.CurrentPlayer.BoardZone);
			Assert.Equal(1, wisp.Cost);
			Assert.NotEqual("Wisp", wisp.Card.Name);
		}

		[Fact]
		public void LadyVashj_BT_109_ShouldShufflePrimeOnDeathrattle()
		{
			Game game = CreateGame();
			Minion vashj = game.ProcessCard<Minion>("Lady Vashj", asZeroCost: true);
			int deckCount = game.CurrentPlayer.DeckZone.Count;

			game.ProcessCard("Fireball", vashj, asZeroCost: true);

			Assert.Equal(deckCount + 1, game.CurrentPlayer.DeckZone.Count);
			Assert.Contains(game.CurrentPlayer.DeckZone, p => p.Card.Id == "BT_109t");
		}

		[Fact]
		public void TotemicReflection_BT_113_ShouldBuffAndCopyTotem()
		{
			Game game = CreateGame();
			var totem = (Minion)Entity.FromCard(game.CurrentPlayer, Cards.FromId("CS2_050"));
			Generic.SummonBlock.Invoke(game, totem, -1, null);

			game.ProcessCard("Totemic Reflection", totem, asZeroCost: true);

			Assert.Equal(3, totem.AttackDamage);
			Assert.Equal(3, totem.Health);
			Assert.Equal(2, game.CurrentPlayer.BoardZone.Count(p => p.Card.Id == "CS2_050"));
		}

		[Fact]
		public void LurkerBelow_BT_230_ShouldRepeatOnNeighborIfTargetDies()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion left = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion right = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("The Lurker Below", left, asZeroCost: true);

			Assert.True(left.ToBeDestroyed);
			Assert.True(right.ToBeDestroyed);
		}
	}
}
