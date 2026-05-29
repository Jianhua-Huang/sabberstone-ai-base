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
	public class OutlandHunterCardsGenTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null)
		{
			var deck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.HUNTER,
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

		[Fact]
		public void NagrandSlam_BT_163_ShouldSummonClefthoofsAndAttackEnemies()
		{
			Game game = CreateGame();

			game.ProcessCard("Nagrand Slam", asZeroCost: true);

			Assert.Equal(4, game.CurrentPlayer.BoardZone.Count(p => p.Card.Id == "BT_163t"));
			Assert.Equal(12, game.CurrentOpponent.Hero.Damage);
		}

		[Fact]
		public void AugmentedPorcupine_BT_201_ShouldSplitAttackDamageOnDeathrattle()
		{
			Game game = CreateGame();
			Minion porcupine = game.ProcessCard<Minion>("Augmented Porcupine", asZeroCost: true);

			game.ProcessCard("Fireball", porcupine, asZeroCost: true);

			Assert.Equal(2, game.CurrentOpponent.Hero.Damage);
		}

		[Fact]
		public void Helboar_BT_202_ShouldBuffRandomBeastInHandOnDeathrattle()
		{
			Game game = CreateGame();
			Minion helboar = game.ProcessCard<Minion>("Helboar", asZeroCost: true);
			IPlayable beast = AddHandCard(game, "River Crocolisk");

			game.ProcessCard("Moonfire", helboar, asZeroCost: true);

			Assert.Equal(3, ((Minion)beast).AttackDamage);
			Assert.Equal(4, ((Minion)beast).Health);
		}

		[Fact]
		public void PackTactics_BT_203_ShouldSummonThreeThreeCopyWhenFriendlyMinionIsAttacked()
		{
			Game game = CreateGame();
			game.ProcessCard("Pack Tactics", asZeroCost: true);
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();
			Minion attacker = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			attacker.IsExhausted = false;

			game.Process(MinionAttackTask.Any(game.CurrentPlayer, attacker, target));

			Assert.Empty(game.CurrentOpponent.SecretZone);
			Minion copy = Assert.Single(game.CurrentOpponent.BoardZone.Where(p => p.Card.Name == "Chillwind Yeti" && p != target));
			Assert.Equal(3, copy.AttackDamage);
			Assert.Equal(3, copy.Health);
		}

		[Fact]
		public void ScrapShot_BT_205_ShouldDamageTargetAndBuffBeastInHand()
		{
			Game game = CreateGame();
			IPlayable beast = AddHandCard(game, "River Crocolisk");

			game.ProcessCard("Scrap Shot", game.CurrentOpponent.Hero, asZeroCost: true);

			Assert.Equal(3, game.CurrentOpponent.Hero.Damage);
			Assert.Equal(5, ((Minion)beast).AttackDamage);
			Assert.Equal(6, ((Minion)beast).Health);
		}

		[Fact]
		public void Zixor_BT_210_ShouldShufflePrimeAndPrimeSummonsCopies()
		{
			Game game = CreateGame();
			Minion zixor = game.ProcessCard<Minion>("Zixor, Apex Predator", asZeroCost: true);

			game.ProcessCard("Fireball", zixor, asZeroCost: true);
			Assert.Contains(game.CurrentPlayer.DeckZone, p => p.Card.Id == "BT_210t");

			IPlayable primeCard = Generic.DrawCard(game.CurrentPlayer, Cards.FromId("BT_210t"));
			game.ProcessCard((Minion)primeCard, asZeroCost: true);

			Assert.Equal(4, game.CurrentPlayer.BoardZone.Count(p => p.Card.Id == "BT_210t"));
		}

		[Fact]
		public void ScavengersIngenuity_BT_213_ShouldDrawAndBuffBeast()
		{
			Game game = CreateGame();
			SetDeck(game, "Wisp", "River Crocolisk");

			game.ProcessCard("Scavenger's Ingenuity", asZeroCost: true);

			Minion beast = Assert.IsType<Minion>(game.CurrentPlayer.HandZone.Single(p => p.Card.Name == "River Crocolisk"));
			Assert.Equal(5, beast.AttackDamage);
			Assert.Equal(6, beast.Health);
		}

		[Fact]
		public void BeastmasterLeoroxx_BT_214_ShouldSummonThreeBeastsFromHand()
		{
			Game game = CreateGame();
			AddHandCard(game, "River Crocolisk");
			AddHandCard(game, "Stonetusk Boar");
			AddHandCard(game, "Bloodfen Raptor");
			AddHandCard(game, "Wisp");

			game.ProcessCard("Beastmaster Leoroxx", asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.BoardZone, p => p.Card.Name == "River Crocolisk");
			Assert.Contains(game.CurrentPlayer.BoardZone, p => p.Card.Name == "Stonetusk Boar");
			Assert.Contains(game.CurrentPlayer.BoardZone, p => p.Card.Name == "Bloodfen Raptor");
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Wisp");
		}
	}
}
