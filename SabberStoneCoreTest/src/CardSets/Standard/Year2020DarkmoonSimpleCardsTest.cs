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
	public class Year2020DarkmoonSimpleCardsTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null)
		{
			var defaultDeck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.WARRIOR,
				Player2HeroClass = CardClass.WARLOCK,
				Player1Deck = (playerDeck ?? defaultDeck).ToList(),
				Player2Deck = (opponentDeck ?? defaultDeck).ToList(),
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
		public void BananaVendor_ShouldAddBananasToBothHands_AndBananaBuffsMinion()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard("Banana Vendor", asZeroCost: true);

			Assert.Equal(2, game.Player1.HandZone.Count(p => p.Card.Id == "DMF_065t"));
			Assert.Equal(2, game.Player2.HandZone.Count(p => p.Card.Id == "DMF_065t"));

			game.ProcessCard(game.Player1.HandZone.First(p => p.Card.Id == "DMF_065t"), target, asZeroCost: true);

			Assert.Equal(2, target.AttackDamage);
			Assert.Equal(2, target.Health);
		}

		[Fact]
		public void KnifeVendor_ShouldDamageBothHeroes()
		{
			Game game = CreateGame();

			game.ProcessCard("Knife Vendor", asZeroCost: true);

			Assert.Equal(26, game.Player1.Hero.Health);
			Assert.Equal(26, game.Player2.Hero.Health);
		}

		[Fact]
		public void PrizeVendor_ShouldDrawForBothPlayers()
		{
			Game game = CreateGame();

			game.ProcessCard("Prize Vendor", asZeroCost: true);

			Assert.Single(game.Player1.HandZone);
			Assert.Single(game.Player2.HandZone);
		}

		[Fact]
		public void WrigglingHorror_ShouldBuffAdjacentMinionsOnly()
		{
			Game game = CreateGame();
			Minion left = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion right = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion horror = game.ProcessCard<Minion>("Wriggling Horror", asZeroCost: true, zonePosition: 1);

			Assert.Equal(2, left.AttackDamage);
			Assert.Equal(2, left.Health);
			Assert.Equal(2, right.AttackDamage);
			Assert.Equal(2, right.Health);
			Assert.Equal(2, horror.AttackDamage);
			Assert.Equal(1, horror.Health);
		}

		[Fact]
		public void ConfectionCyclone_ShouldAddTwoSugarElementals()
		{
			Game game = CreateGame();

			game.ProcessCard("Confection Cyclone", asZeroCost: true);

			Assert.Equal(2, game.Player1.HandZone.Count(p => p.Card.Id == "DMF_100t"));
		}

		[Fact]
		public void FireBreather_ShouldDamageAllNonDemonMinions()
		{
			Game game = CreateGame();
			Minion friendlyDemon = game.ProcessCard<Minion>("Flame Imp", asZeroCost: true);
			Minion friendlyNonDemon = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			game.EndTurn();
			Minion enemyNonDemon = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Fire Breather", asZeroCost: true);

			Assert.Equal(0, friendlyDemon.Damage);
			Assert.Equal(2, friendlyNonDemon.Damage);
			Assert.Equal(2, enemyNonDemon.Damage);
		}

		[Fact]
		public void RevenantRascal_ShouldDestroyManaCrystalForBothPlayers()
		{
			Game game = CreateGame();
			game.Player1.BaseMana = 5;
			game.Player2.BaseMana = 6;

			game.ProcessCard("Revenant Rascal", asZeroCost: true);

			Assert.Equal(4, game.Player1.BaseMana);
			Assert.Equal(5, game.Player2.BaseMana);
		}

		[Fact]
		public void CostumedEntertainer_ShouldBuffOneMinionInHand()
		{
			Game game = CreateGame();
			Generic.DrawCard(game.Player1, Cards.FromName("Wisp"));

			game.ProcessCard("Costumed Entertainer", asZeroCost: true);

			IPlayable buffed = game.Player1.HandZone.Single(p => p.Card.Name == "Wisp");
			Assert.Equal(3, buffed[GameTag.ATK]);
			Assert.Equal(3, buffed[GameTag.HEALTH]);
		}

		[Fact]
		public void HammerOfTheNaaru_ShouldSummonHolyElemental()
		{
			Game game = CreateGame();

			game.ProcessCard("Hammer of the Naaru", asZeroCost: true);

			Minion elemental = game.Player1.BoardZone.Single(p => p.Card.Id == "DMF_238t");
			Assert.Equal(6, elemental.AttackDamage);
			Assert.Equal(6, elemental.Health);
			Assert.True(elemental.HasTaunt);
		}

		[Fact]
		public void SwordEater_ShouldEquipJawbreaker()
		{
			Game game = CreateGame();

			game.ProcessCard("Sword Eater", asZeroCost: true);

			Assert.Equal("DMF_521t", game.Player1.Hero.Weapon.Card.Id);
			Assert.Equal(3, game.Player1.Hero.Weapon.AttackDamage);
			Assert.Equal(2, game.Player1.Hero.Weapon.Durability);
		}

		[Fact]
		public void BumperCar_ShouldAddTwoRidersOnDeathrattle()
		{
			Game game = CreateGame();
			Minion bumperCar = game.ProcessCard<Minion>("Bumper Car", asZeroCost: true);

			bumperCar.Kill();

			Assert.Equal(2, game.Player1.HandZone.Count(p => p.Card.Id == "DMF_523t"));
			Assert.All(game.Player1.HandZone.Where(p => p.Card.Id == "DMF_523t"), p => Assert.Equal(1, p.Card[GameTag.RUSH]));
		}

		[Fact]
		public void RingMatron_ShouldSummonTwoFieryImpsOnDeathrattle()
		{
			Game game = CreateGame();
			Minion matron = game.ProcessCard<Minion>("Ring Matron", asZeroCost: true);

			matron.Kill();

			Assert.Equal(2, game.Player1.BoardZone.Count(p => p.Card.Id == "DMF_533t"));
		}
	}
}
