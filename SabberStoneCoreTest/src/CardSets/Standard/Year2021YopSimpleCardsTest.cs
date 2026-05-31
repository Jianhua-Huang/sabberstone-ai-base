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

		[Fact]
		public void Felsaber_ShouldOnlyAttackAfterHeroAttackedThisTurn()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DEMONHUNTER);
			Minion felsaber = game.ProcessCard<Minion>("Felsaber", asZeroCost: true);
			game.EndTurn();
			game.EndTurn();

			game.Process(MinionAttackTask.Any(game.Player1, felsaber, game.Player2.Hero));

			Assert.Equal(30, game.Player2.Hero.Health);

			game.Process(HeroPowerTask.Any(game.Player1));
			game.Process(HeroAttackTask.Any(game.Player1, game.Player2.Hero));
			game.Process(MinionAttackTask.Any(game.Player1, felsaber, game.Player2.Hero));

			Assert.Equal(24, game.Player2.Hero.Health);
		}

		[Fact]
		public void LibramOfJudgment_ShouldGainLifestealOnlyWhenCorrupted()
		{
			Game normal = CreateGame(player1HeroClass: CardClass.PALADIN);

			normal.ProcessCard("Libram of Judgment", asZeroCost: true);

			Assert.Equal(5, normal.Player1.Hero.Weapon.AttackDamage);
			Assert.Equal(3, normal.Player1.Hero.Weapon.Durability);
			Assert.False(normal.Player1.Hero.Weapon.HasLifeSteal);

			Game corrupted = CreateGame(player1HeroClass: CardClass.PALADIN);
			Generic.DrawCard(corrupted.Player1, Cards.FromId("YOP_011"));

			corrupted.ProcessCard("Burly Shovelfist");
			corrupted.ProcessCard(corrupted.Player1.HandZone.Single(p => p.Card.Id == "YOP_011t"), asZeroCost: true);

			Assert.True(corrupted.Player1.Hero.Weapon.HasLifeSteal);
		}

		[Fact]
		public void SpikedWheel_ShouldHaveAttackOnlyWhileHeroHasArmor()
		{
			Game withoutArmor = CreateGame(player1HeroClass: CardClass.WARRIOR);

			withoutArmor.ProcessCard("Spiked Wheel", asZeroCost: true);

			Assert.Equal(0, withoutArmor.Player1.Hero.Weapon.AttackDamage);
			Assert.Equal(2, withoutArmor.Player1.Hero.Weapon.Durability);

			Game withArmor = CreateGame(player1HeroClass: CardClass.WARRIOR);
			withArmor.Player1.Hero.Armor = 1;

			withArmor.ProcessCard("Spiked Wheel", asZeroCost: true);

			Assert.Equal(3, withArmor.Player1.Hero.Weapon.AttackDamage);
		}

		[Fact]
		public void NitroboostPoison_ShouldBuffMinionAndCorruptedAlsoBuffsWeapon()
		{
			Game normal = CreateGame(player1HeroClass: CardClass.ROGUE);
			Minion normalTarget = normal.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);

			normal.ProcessCard("Nitroboost Poison", normalTarget, asZeroCost: true);

			Assert.Equal(4, normalTarget.AttackDamage);
			Assert.Null(normal.Player1.Hero.Weapon);

			Game corrupted = CreateGame(player1HeroClass: CardClass.ROGUE);
			Minion corruptedTarget = corrupted.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			corrupted.ProcessCard("Fiery War Axe", asZeroCost: true);
			Generic.DrawCard(corrupted.Player1, Cards.FromId("YOP_015"));

			corrupted.ProcessCard("Boulderfist Ogre");
			corrupted.ProcessCard(corrupted.Player1.HandZone.Single(p => p.Card.Id == "YOP_015t"), corruptedTarget, asZeroCost: true);

			Assert.Equal(4, corruptedTarget.AttackDamage);
			Assert.Equal(5, corrupted.Player1.Hero.Weapon.AttackDamage);
		}

		[Fact]
		public void DreamingDrake_ShouldUseCorruptedTauntStats()
		{
			Game normal = CreateGame(player1HeroClass: CardClass.DRUID);

			Minion normalDrake = normal.ProcessCard<Minion>("Dreaming Drake", asZeroCost: true);

			Assert.Equal(3, normalDrake.AttackDamage);
			Assert.Equal(4, normalDrake.Health);
			Assert.True(normalDrake.HasTaunt);

			Game corrupted = CreateGame(player1HeroClass: CardClass.DRUID);
			Generic.DrawCard(corrupted.Player1, Cards.FromId("YOP_025"));

			corrupted.ProcessCard("Boulderfist Ogre");
			corrupted.ProcessCard(corrupted.Player1.HandZone.Single(p => p.Card.Id == "YOP_025t"), asZeroCost: true);

			Minion corruptedDrake = corrupted.Player1.BoardZone.Single(p => p.Card.Id == "YOP_025t");
			Assert.Equal(5, corruptedDrake.AttackDamage);
			Assert.Equal(6, corruptedDrake.Health);
			Assert.True(corruptedDrake.HasTaunt);
		}

		[Fact]
		public void Crabrider_ShouldHaveRushAndTemporaryWindfury()
		{
			Game game = CreateGame();

			Minion crabrider = game.ProcessCard<Minion>("Crabrider", asZeroCost: true);

			Assert.True(crabrider.IsRush);
			Assert.True(crabrider.HasWindfury);

			game.EndTurn();

			Assert.False(crabrider.HasWindfury);
		}

		[Fact]
		public void Moonfang_ShouldOnlyTakeOneDamageAtATime()
		{
			Game game = CreateGame();
			Minion moonfang = game.ProcessCard<Minion>("Moonfang", asZeroCost: true);

			Generic.DamageCharFunc.Invoke(game.Player2.Hero, moonfang, 5, false);

			Assert.Equal(1, moonfang.Damage);

			Generic.DamageCharFunc.Invoke(game.Player2.Hero, moonfang, 3, false);

			Assert.Equal(2, moonfang.Damage);
		}

		[Fact]
		public void DarkInquisitorXanesh_ShouldReduceCorruptCardsInHandAndDeck()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PRIEST);
			IPlayable handCorrupt = Generic.DrawCard(game.Player1, Cards.FromId("YOP_003"));
			IPlayable nonCorrupt = Generic.DrawCard(game.Player1, Cards.FromName("River Crocolisk"));
			IPlayable deckCorrupt = Entity.FromCard(game.Player1, Cards.FromId("YOP_025"));
			game.Player1.DeckZone.Add(deckCorrupt);

			game.ProcessCard("Dark Inquisitor Xanesh", asZeroCost: true);

			Assert.Equal(1, handCorrupt.Cost);
			Assert.Equal(2, nonCorrupt.Cost);
			Assert.Equal(1, deckCorrupt.Cost);
		}

		[Fact]
		public void EnvoyRustwix_ShouldShuffleThreePrimeLegendaryMinionsOnDeathrattle()
		{
			Game game = CreateGame(player1HeroClass: CardClass.WARLOCK);
			Minion rustwix = game.ProcessCard<Minion>("Envoy Rustwix", asZeroCost: true);
			int deckCount = game.Player1.DeckZone.Count;
			int primeCount = game.Player1.DeckZone.Count(p => p.Card.Name.EndsWith("Prime"));

			rustwix.Kill();
			game.DeathProcessingAndAuraUpdate();

			IPlayable[] added = game.Player1.DeckZone.GetAll()
				.Where(p => p.Card.Name.EndsWith("Prime"))
				.Skip(primeCount)
				.ToArray();
			Assert.Equal(3, game.Player1.DeckZone.Count - deckCount);
			Assert.All(added, card =>
			{
				Assert.Equal(CardType.MINION, card.Card.Type);
				Assert.EndsWith("Prime", card.Card.Name);
			});
		}

		[Fact]
		public void ImprisonedCelestial_ShouldSpellburstDivineShieldOnlyAfterAwake()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PALADIN);
			Minion celestial = game.ProcessCard<Minion>("Imprisoned Celestial", asZeroCost: true);
			Minion friendly = game.ProcessCard<Minion>("Murloc Raider", asZeroCost: true);

			game.ProcessCard("Blessing of Might", friendly, asZeroCost: true);

			Assert.Equal(2, celestial[GameTag.DORMANT]);
			Assert.False(celestial.HasDivineShield);
			Assert.False(friendly.HasDivineShield);

			game.EndTurn();
			game.EndTurn();
			game.EndTurn();
			game.EndTurn();

			game.ProcessCard("Blessing of Might", friendly, asZeroCost: true);

			Assert.Equal(0, celestial[GameTag.DORMANT]);
			Assert.True(celestial.HasDivineShield);
			Assert.True(friendly.HasDivineShield);
		}

		[Fact]
		public void Deathwarden_ShouldPreventDeathrattlesFromTriggering()
		{
			Game withoutDeathwarden = CreateGame();
			Minion normalLootHoarder = withoutDeathwarden.ProcessCard<Minion>("Loot Hoarder", asZeroCost: true);

			normalLootHoarder.Kill();
			withoutDeathwarden.DeathProcessingAndAuraUpdate();

			Assert.Single(withoutDeathwarden.Player1.HandZone);

			Game withDeathwarden = CreateGame();
			withDeathwarden.ProcessCard("Deathwarden", asZeroCost: true);
			Minion suppressedLootHoarder = withDeathwarden.ProcessCard<Minion>("Loot Hoarder", asZeroCost: true);

			suppressedLootHoarder.Kill();
			withDeathwarden.DeathProcessingAndAuraUpdate();

			Assert.Empty(withDeathwarden.Player1.HandZone);
			Assert.Contains(withDeathwarden.Player1.GraveyardZone, p => p.Card.Name == "Loot Hoarder");
		}

		[Fact]
		public void Rally_ShouldResurrectFriendlyOneTwoAndThreeCostMinions()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PRIEST);
			Minion oneCost = game.ProcessCard<Minion>("Murloc Raider", asZeroCost: true);
			Minion twoCost = game.ProcessCard<Minion>("Loot Hoarder", asZeroCost: true);
			Minion threeCost = game.ProcessCard<Minion>("Ironfur Grizzly", asZeroCost: true);

			oneCost.Kill();
			twoCost.Kill();
			threeCost.Kill();
			game.DeathProcessingAndAuraUpdate();
			EmptyZone(game.Player1.HandZone.GetAll());

			game.ProcessCard("Rally!", asZeroCost: true);

			Assert.Contains(game.Player1.BoardZone, p => p.Card.Name == "Murloc Raider");
			Assert.Contains(game.Player1.BoardZone, p => p.Card.Name == "Loot Hoarder");
			Assert.Contains(game.Player1.BoardZone, p => p.Card.Name == "Ironfur Grizzly");
		}

		[Fact]
		public void Saddlemaster_ShouldAddRandomBeastAfterFriendlyBeastIsPlayed()
		{
			Game game = CreateGame(player1HeroClass: CardClass.HUNTER);
			game.ProcessCard("Saddlemaster", asZeroCost: true);

			game.ProcessCard("Bloodfen Raptor", asZeroCost: true);

			IPlayable added = Assert.Single(game.Player1.HandZone);
			Assert.Equal(CardType.MINION, added.Card.Type);
			Assert.True(added.Card.IsRace(Race.BEAST));

			Game nonBeastGame = CreateGame(player1HeroClass: CardClass.HUNTER);
			nonBeastGame.ProcessCard("Saddlemaster", asZeroCost: true);

			nonBeastGame.ProcessCard("Murloc Raider", asZeroCost: true);

			Assert.Empty(nonBeastGame.Player1.HandZone);
		}

		[Fact]
		public void GlacierRacer_ShouldDamageOnlyFrozenEnemiesOnSpellburst()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE);
			game.ProcessCard("Glacier Racer", asZeroCost: true);
			var frozenEnemy = (Minion)Entity.FromCard(game.Player2, Cards.FromName("Boulderfist Ogre"));
			var unfrozenEnemy = (Minion)Entity.FromCard(game.Player2, Cards.FromName("River Crocolisk"));
			Generic.SummonBlock.Invoke(game, frozenEnemy, -1, null);
			Generic.SummonBlock.Invoke(game, unfrozenEnemy, -1, null);
			Minion frozenFriendly = game.ProcessCard<Minion>("Murloc Raider", asZeroCost: true);
			frozenEnemy.IsFrozen = true;
			unfrozenEnemy.IsFrozen = false;
			frozenFriendly.IsFrozen = true;
			game.Player2.Hero.IsFrozen = true;

			game.ProcessCard("Arcane Intellect", asZeroCost: true);

			Assert.Equal(3, frozenEnemy.Damage);
			Assert.Equal(0, unfrozenEnemy.Damage);
			Assert.Equal(0, frozenFriendly.Damage);
			Assert.Equal(27, game.Player2.Hero.Health);
		}

		[Fact]
		public void ImprisonedPhoenix_ShouldGiveSpellDamageOnlyAfterDormantAwakens()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE);

			Minion phoenix = game.ProcessCard<Minion>("Imprisoned Phoenix", asZeroCost: true);

			Assert.Equal(2, phoenix[GameTag.DORMANT]);
			Assert.Equal(1, phoenix[GameTag.UNTOUCHABLE]);
			Assert.Equal(0, game.Player1.CurrentSpellPower);

			game.EndTurn();
			game.EndTurn();

			Assert.Equal(1, phoenix[GameTag.DORMANT]);
			Assert.Equal(0, game.Player1.CurrentSpellPower);

			game.EndTurn();
			game.EndTurn();

			Assert.Equal(0, phoenix[GameTag.DORMANT]);
			Assert.Equal(0, phoenix[GameTag.UNTOUCHABLE]);
			Assert.Equal(2, game.Player1.CurrentSpellPower);
		}
	}
}
