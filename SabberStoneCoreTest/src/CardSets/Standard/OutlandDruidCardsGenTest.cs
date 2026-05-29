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
	public class OutlandDruidCardsGenTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null)
		{
			var deck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.DRUID,
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

		private static IPlayable AddHandCard(Game game, string cardName)
		{
			return Generic.DrawCard(game.CurrentPlayer, Cards.FromName(cardName));
		}

		private static void SetDeck(Game game, params string[] cardNames)
		{
			EmptyZone(game.CurrentPlayer.DeckZone.GetAll());
			foreach (string cardName in cardNames)
				game.CurrentPlayer.DeckZone.Add(Entity.FromCard(game.CurrentPlayer, Cards.FromName(cardName)));
		}

		private static void AdvanceTwoOwnerTurns(Game game)
		{
			game.EndTurn();
			game.EndTurn();
			game.EndTurn();
			game.EndTurn();
		}

		[Fact]
		public void FungalFortunes_BT_128_ShouldDrawThreeAndDiscardDrawnMinions()
		{
			Game game = CreateGame();
			SetDeck(game, "Wisp", "Moonfire", "Fireball");

			game.ProcessCard("Fungal Fortunes", asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Moonfire");
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Fireball");
			Assert.DoesNotContain(game.CurrentPlayer.HandZone, p => p.Card.Name == "Wisp");
			Assert.Contains(game.CurrentPlayer.DiscardedEntities, id => game.IdEntityDic[id].Card.Name == "Wisp");
		}

		[Fact]
		public void Germination_BT_129_ShouldSummonTauntCopyOfFriendlyMinion()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);

			game.ProcessCard("Germination", target, asZeroCost: true);

			Assert.Equal(2, game.CurrentPlayer.BoardZone.Count(p => p.Card.Name == "Chillwind Yeti"));
			Minion copy = game.CurrentPlayer.BoardZone.Last(p => p.Card.Name == "Chillwind Yeti");
			Assert.Equal(1, copy[GameTag.TAUNT]);
		}

		[Fact]
		public void Overgrowth_BT_130_ShouldGainTwoEmptyManaCrystals()
		{
			Game game = CreateGame();
			game.CurrentPlayer.BaseMana = 5;

			game.ProcessCard("Overgrowth", asZeroCost: true);

			Assert.Equal(7, game.CurrentPlayer.BaseMana);
		}

		[Fact]
		public void ImprisonedSatyr_BT_127_ShouldReduceRandomMinionInHandWhenAwakening()
		{
			Game game = CreateGame();
			IPlayable minion = AddHandCard(game, "Boulderfist Ogre");

			Minion satyr = game.ProcessCard<Minion>("Imprisoned Satyr", asZeroCost: true);

			Assert.True(satyr.Untouchable);
			AdvanceTwoOwnerTurns(game);

			Assert.False(satyr.Untouchable);
			Assert.Equal(1, minion.Cost);
		}

		[Fact]
		public void YsielWindsinger_BT_131_ShouldSetSpellsInHandToOneCost()
		{
			Game game = CreateGame();
			IPlayable spell = AddHandCard(game, "Overflow");
			IPlayable minion = AddHandCard(game, "Wisp");

			game.ProcessCard("Ysiel Windsinger", asZeroCost: true);

			Assert.Equal(1, spell.Cost);
			Assert.Equal(0, minion.Cost);
		}

		[Fact]
		public void Ironbark_BT_132_ShouldCostZeroAtSevenManaAndBuffTaunt()
		{
			Game game = CreateGame();
			game.CurrentPlayer.BaseMana = 7;
			IPlayable ironbark = AddHandCard(game, "Ironbark");
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);

			Assert.Equal(0, ironbark.Cost);
			game.ProcessCard(ironbark, target, asZeroCost: true);

			Assert.Equal(5, target.AttackDamage);
			Assert.Equal(8, target.Health);
			Assert.Equal(1, target[GameTag.TAUNT]);
		}

		[Fact]
		public void MarshHydra_BT_133_ShouldAddRandomEightCostMinionAfterAttack()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();
			Minion hydra = game.ProcessCard<Minion>("Marsh Hydra", asZeroCost: true);
			int handCount = game.CurrentPlayer.HandZone.Count;

			game.Process(MinionAttackTask.Any(game.CurrentPlayer, hydra, target));

			Assert.Equal(handCount + 1, game.CurrentPlayer.HandZone.Count);
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Type == CardType.MINION && p.Cost == 8);
		}

		[Fact]
		public void Bogbeam_BT_134_ShouldCostZeroAtSevenManaAndDamageMinion()
		{
			Game game = CreateGame();
			game.CurrentPlayer.BaseMana = 7;
			IPlayable bogbeam = AddHandCard(game, "Bogbeam");
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);

			Assert.Equal(0, bogbeam.Cost);
			game.ProcessCard(bogbeam, target, asZeroCost: true);

			Assert.Equal(3, target.Damage);
		}

		[Fact]
		public void GlowflySwarm_BT_135_ShouldSummonGlowflyForEachSpellInHand()
		{
			Game game = CreateGame();
			AddHandCard(game, "Moonfire");
			AddHandCard(game, "Fireball");
			AddHandCard(game, "Wisp");

			game.ProcessCard("Glowfly Swarm", asZeroCost: true);

			Assert.Equal(2, game.CurrentPlayer.BoardZone.Count(p => p.Card.Id == "BT_135t"));
		}

		[Fact]
		public void ArchsporeMsshifn_BT_136_ShouldShufflePrimeOnDeathrattle()
		{
			Game game = CreateGame();
			Minion archspore = game.ProcessCard<Minion>("Archspore Msshi'fn", asZeroCost: true);
			int deckCount = game.CurrentPlayer.DeckZone.Count;

			game.ProcessCard("Fireball", archspore, asZeroCost: true);

			Assert.Equal(deckCount + 1, game.CurrentPlayer.DeckZone.Count);
			Assert.Contains(game.CurrentPlayer.DeckZone, p => p.Card.Id == "BT_136t");
		}
	}
}
