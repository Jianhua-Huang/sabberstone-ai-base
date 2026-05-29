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
	public class OutlandPaladinCardsGenTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null)
		{
			var deck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.PALADIN,
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
		public void LibramOfJustice_BT_011_ShouldEquipWeaponAndSetEnemyMinionsHealthToOne()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Libram of Justice", asZeroCost: true);

			Assert.NotNull(game.CurrentPlayer.Hero.Weapon);
			Assert.Equal(1, game.CurrentPlayer.Hero.Weapon.AttackDamage);
			Assert.Equal(4, game.CurrentPlayer.Hero.Weapon.Durability);
			Assert.Equal(1, enemy.Health);
		}

		[Fact]
		public void UnderlightAnglingRod_BT_018_ShouldAddRandomMurlocAfterHeroAttacks()
		{
			Game game = CreateGame();
			game.ProcessCard("Underlight Angling Rod", asZeroCost: true);
			int handCount = game.CurrentPlayer.HandZone.Count;

			game.Process(HeroAttackTask.Any(game.CurrentPlayer, game.CurrentOpponent.Hero));

			Assert.Equal(handCount + 1, game.CurrentPlayer.HandZone.Count);
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.IsRace(Race.MURLOC));
		}

		[Fact]
		public void MurgurMurgurgle_BT_019_ShouldShufflePrimeAndPrimeSummonsShieldedMurlocs()
		{
			Game game = CreateGame();
			Minion murgur = game.ProcessCard<Minion>("Murgur Murgurgle", asZeroCost: true);

			game.ProcessCard("Moonfire", murgur, asZeroCost: true);
			game.ProcessCard("Fireball", murgur, asZeroCost: true);
			Assert.Contains(game.CurrentPlayer.DeckZone, p => p.Card.Id == "BT_019t");

			IPlayable primeCard = Generic.DrawCard(game.CurrentPlayer, Cards.FromId("BT_019t"));
			game.ProcessCard((Minion)primeCard, asZeroCost: true);

			Assert.Equal(4, game.CurrentPlayer.BoardZone.Count(p => p.Card.IsRace(Race.MURLOC) && p.Card.Id != "BT_019t"));
			Assert.All(game.CurrentPlayer.BoardZone.Where(p => p.Card.IsRace(Race.MURLOC) && p.Card.Id != "BT_019t"),
				p => Assert.Equal(1, p[GameTag.DIVINE_SHIELD]));
		}

		[Fact]
		public void AldorAttendantAndTruthseeker_ShouldReduceLibramsInHandAndDeck()
		{
			Game game = CreateGame();
			IPlayable wisdom = AddHandCard(game, "Libram of Wisdom");
			SetDeck(game, "Libram of Hope", "Wisp");

			game.ProcessCard("Aldor Attendant", asZeroCost: true);

			Assert.Equal(1, wisdom.Cost);
			Assert.Equal(8, game.CurrentPlayer.DeckZone.Single(p => p.Card.Name == "Libram of Hope").Cost);

			game.ProcessCard("Aldor Truthseeker", asZeroCost: true);

			Assert.Equal(0, wisdom.Cost);
			Assert.Equal(6, game.CurrentPlayer.DeckZone.Single(p => p.Card.Name == "Libram of Hope").Cost);
		}

		[Fact]
		public void LibramOfHope_BT_024_ShouldHealAndSummonGuardian()
		{
			Game game = CreateGame();
			game.CurrentPlayer.Hero.Damage = 10;

			game.ProcessCard("Libram of Hope", game.CurrentPlayer.Hero, asZeroCost: true);

			Assert.Equal(2, game.CurrentPlayer.Hero.Damage);
			Minion guardian = Assert.Single(game.CurrentPlayer.BoardZone.Where(p => p.Card.Id == "BT_024t"));
			Assert.Equal(8, guardian.AttackDamage);
			Assert.Equal(8, guardian.Health);
			Assert.Equal(1, guardian[GameTag.TAUNT]);
			Assert.Equal(1, guardian[GameTag.DIVINE_SHIELD]);
		}

		[Fact]
		public void LibramOfWisdom_BT_025_ShouldBuffAndReturnToHandOnDeathrattle()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard("Libram of Wisdom", target, asZeroCost: true);
			game.ProcessCard("Fireball", target, asZeroCost: true);

			Assert.Equal(2, target.AttackDamage);
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Id == "BT_025");
		}

		[Fact]
		public void HandOfAdal_BT_292_ShouldBuffAndDraw()
		{
			Game game = CreateGame();
			SetDeck(game, "Wisp");
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			int handCount = game.CurrentPlayer.HandZone.Count;

			game.ProcessCard("Hand of A'dal", target, asZeroCost: true);

			Assert.Equal(3, target.AttackDamage);
			Assert.Equal(3, target.Health);
			Assert.Equal(handCount + 1, game.CurrentPlayer.HandZone.Count);
		}
	}
}
