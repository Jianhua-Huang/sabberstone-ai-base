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
	public class Year2021YopSimpleCardsTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null,
			CardClass player1HeroClass = CardClass.WARRIOR, CardClass player2HeroClass = CardClass.WARLOCK)
		{
			var defaultDeck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = player1HeroClass,
				Player2HeroClass = player2HeroClass,
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
		public void ArmorVendor_ShouldGiveBothHeroesFourArmor()
		{
			Game game = CreateGame();

			game.ProcessCard("Armor Vendor", asZeroCost: true);

			Assert.Equal(4, game.Player1.Hero.Armor);
			Assert.Equal(4, game.Player2.Hero.Armor);
		}

		[Fact]
		public void Backfire_ShouldDrawThreeAndDamageYourHero()
		{
			Game game = CreateGame(player1HeroClass: CardClass.WARLOCK);

			game.ProcessCard("Backfire", asZeroCost: true);

			Assert.Equal(3, game.Player1.HandZone.Count);
			Assert.Equal(27, game.Player1.Hero.Health);
			Assert.Equal(30, game.Player2.Hero.Health);
		}

		[Fact]
		public void Barricade_ShouldSummonSecondGuardOnlyWhenItIsYourOnlyMinion()
		{
			Game emptyBoardGame = CreateGame();

			emptyBoardGame.ProcessCard("Barricade", asZeroCost: true);

			Minion[] emptyBoardGuards = emptyBoardGame.Player1.BoardZone.GetAll(p => p.Card.Id == "YOP_005t");
			Assert.Equal(2, emptyBoardGuards.Length);
			Assert.All(emptyBoardGuards, guard =>
			{
				Assert.Equal(2, guard.AttackDamage);
				Assert.Equal(4, guard.Health);
				Assert.True(guard.HasTaunt);
			});

			Game occupiedBoardGame = CreateGame();
			occupiedBoardGame.ProcessCard<Minion>("Wisp", asZeroCost: true);

			occupiedBoardGame.ProcessCard("Barricade", asZeroCost: true);

			Assert.Single(occupiedBoardGame.Player1.BoardZone.GetAll(p => p.Card.Id == "YOP_005t"));
			Assert.Contains(occupiedBoardGame.Player1.BoardZone, p => p.Card.Name == "Wisp");
		}

		[Fact]
		public void ConjureManaBiscuit_ShouldAddBiscuitThatRefreshesTwoManaCrystals()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE);
			game.Player1.UsedMana = 5;

			game.ProcessCard("Conjure Mana Biscuit", asZeroCost: true);

			IPlayable biscuit = Assert.Single(game.Player1.HandZone);
			Assert.Equal("YOP_019t", biscuit.Card.Id);

			game.ProcessCard(biscuit, asZeroCost: true);

			Assert.Equal(3, game.Player1.UsedMana);
			Assert.Equal(7, game.Player1.RemainingMana);
		}

		[Fact]
		public void Mistrunner_ShouldBuffFriendlyMinionAndOverloadOne()
		{
			Game game = CreateGame(player1HeroClass: CardClass.SHAMAN);
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard("Mistrunner", target, asZeroCost: true);

			Assert.Equal(4, target.AttackDamage);
			Assert.Equal(4, target.Health);
			Assert.Equal(1, game.Player1.OverloadOwed);
		}

		[Fact]
		public void BolaShot_ShouldDamageTargetForOneAndNeighborsForTwo()
		{
			Game game = CreateGame(player1HeroClass: CardClass.HUNTER);
			game.EndTurn();
			Minion left = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion middle = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			Minion right = game.ProcessCard<Minion>("Murloc Raider", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Bola Shot", middle, asZeroCost: true);

			Assert.True(left.ToBeDestroyed);
			Assert.Equal(1, middle.Damage);
			Assert.True(right.ToBeDestroyed);
		}

		[Fact]
		public void ArborUp_ShouldSummonTreantsAndBuffAllFriendlyMinions()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DRUID);
			Minion existing = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard("Arbor Up", asZeroCost: true);

			Assert.Equal(3, game.Player1.BoardZone.Count);
			Assert.Equal(3, existing.AttackDamage);
			Assert.Equal(2, existing.Health);

			Minion[] treants = game.Player1.BoardZone.GetAll(p => p.Card.Id == "EX1_158t");
			Assert.Equal(2, treants.Length);
			Assert.All(treants, treant =>
			{
				Assert.Equal(4, treant.AttackDamage);
				Assert.Equal(3, treant.Health);
			});
		}

		[Fact]
		public void LuckysoulHoarder_ShouldShuffleTwoSoulFragmentsOrDrawWhenCorrupted()
		{
			Game normalGame = CreateGame(player1HeroClass: CardClass.WARLOCK);

			normalGame.ProcessCard("Luckysoul Hoarder", asZeroCost: true);

			Assert.Equal(2, normalGame.Player1.DeckZone.Count(p => p.Card.Id == "SCH_307t"));

			Game corruptedGame = CreateGame(player1HeroClass: CardClass.WARLOCK);
			Generic.DrawCard(corruptedGame.Player1, Cards.FromId("YOP_003"));

			corruptedGame.ProcessCard("Boulderfist Ogre");
			corruptedGame.ProcessCard(corruptedGame.Player1.HandZone.Single(p => p.Card.Id == "YOP_003t"), asZeroCost: true);

			Assert.Equal(0, corruptedGame.Player1.DeckZone.Count(p => p.Card.Id == "SCH_307t"));
			Assert.Single(corruptedGame.Player1.HandZone);
		}

		[Fact]
		public void Ironclad_ShouldGainStatsOnlyWhenHeroHasArmor()
		{
			Game withoutArmor = CreateGame();

			Minion normal = withoutArmor.ProcessCard<Minion>("Ironclad", asZeroCost: true);

			Assert.Equal(2, normal.AttackDamage);
			Assert.Equal(4, normal.Health);

			Game withArmor = CreateGame();
			withArmor.Player1.Hero.Armor = 1;

			Minion buffed = withArmor.ProcessCard<Minion>("Ironclad", asZeroCost: true);

			Assert.Equal(4, buffed.AttackDamage);
			Assert.Equal(6, buffed.Health);
		}

		[Fact]
		public void Landslide_ShouldDamageEnemyMinionsAgainWhenOverloaded()
		{
			Game normal = CreateGame(player1HeroClass: CardClass.SHAMAN);
			normal.EndTurn();
			Minion normalTarget = normal.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			normal.EndTurn();

			normal.ProcessCard("Landslide", asZeroCost: true);

			Assert.Equal(1, normalTarget.Damage);

			Game overloaded = CreateGame(player1HeroClass: CardClass.SHAMAN);
			overloaded.EndTurn();
			Minion overloadedTarget = overloaded.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			overloaded.EndTurn();
			overloaded.Player1.OverloadLocked = 1;

			overloaded.ProcessCard("Landslide", asZeroCost: true);

			Assert.Equal(2, overloadedTarget.Damage);
		}

		[Fact]
		public void FelfireDeadeye_ShouldReduceHeroPowerCostByOne()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DEMONHUNTER);

			Assert.Equal(1, game.Player1.Hero.HeroPower.Cost);

			game.ProcessCard("Felfire Deadeye", asZeroCost: true);

			Assert.Equal(0, game.Player1.Hero.HeroPower.Cost);
		}

		[Fact]
		public void RunawayBlackwing_ShouldDealNineToRandomEnemyMinionAtEndOfTurn()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Runaway Blackwing", asZeroCost: true);
			game.EndTurn();

			Assert.True(target.ToBeDestroyed || !game.Player2.BoardZone.Contains(target));
		}
	}
}
