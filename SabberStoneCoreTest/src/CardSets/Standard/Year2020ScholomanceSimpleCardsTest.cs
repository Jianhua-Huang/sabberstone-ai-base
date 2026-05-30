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
	public class Year2020ScholomanceSimpleCardsTest
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

		private static void AddSoulFragments(Game game, int amount)
		{
			for (int i = 0; i < amount; i++)
				game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromId("SCH_307t")));
		}

		[Fact]
		public void Wolpertinger_ShouldSummonCopyOfItself()
		{
			Game game = CreateGame();

			Minion wolpertinger = game.ProcessCard<Minion>("Wolpertinger", asZeroCost: true);

			Minion[] copies = game.Player1.BoardZone.GetAll(p => p.Card.Id == "SCH_133");
			Assert.Equal(2, copies.Length);
			Assert.Contains(wolpertinger, copies);
			Assert.All(copies, copy =>
			{
				Assert.Equal(1, copy.AttackDamage);
				Assert.Equal(1, copy.Health);
			});
		}

		[Fact]
		public void BonewebEgg_ShouldSummonTwoSpidersOnDeathrattle()
		{
			Game game = CreateGame();
			Minion egg = game.ProcessCard<Minion>("Boneweb Egg", asZeroCost: true);

			egg.Kill();

			Assert.Equal(2, game.Player1.BoardZone.Count(p => p.Card.Id == "SCH_147t"));
		}

		[Fact]
		public void BonewebEgg_ShouldTriggerDeathrattleWhenDiscarded()
		{
			Game game = CreateGame();
			IPlayable egg = Generic.DrawCard(game.Player1, Cards.FromId("SCH_147"));

			Generic.DiscardBlock(game.Player1, egg);

			Assert.Equal(2, game.Player1.BoardZone.Count(p => p.Card.Id == "SCH_147t"));
			Assert.DoesNotContain(game.Player1.HandZone, p => p.Card.Id == "SCH_147");
		}

		[Fact]
		public void AnimatedBroomstick_ShouldGiveOtherFriendlyMinionsRush()
		{
			Game game = CreateGame();
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			Minion broomstick = game.ProcessCard<Minion>("Animated Broomstick", asZeroCost: true);

			Assert.True(wisp.IsRush);
			Assert.True(broomstick.IsRush);
		}

		[Fact]
		public void BloatedPython_ShouldSummonHaplessHandlerOnDeathrattle()
		{
			Game game = CreateGame();
			Minion python = game.ProcessCard<Minion>("Bloated Python", asZeroCost: true);

			python.Kill();

			Minion handler = game.Player1.BoardZone.Single(p => p.Card.Id == "SCH_340t");
			Assert.Equal(4, handler.AttackDamage);
			Assert.Equal(4, handler.Health);
		}

		[Theory]
		[InlineData("Fishy Flyer", "SCH_707t", true, false, false)]
		[InlineData("Sneaky Delinquent", "SCH_708t", false, true, false)]
		[InlineData("Smug Senior", "SCH_709t", false, false, true)]
		public void ScholomanceGhostDeathrattles_ShouldAddMatchingGhostToHand(
			string cardName,
			string ghostId,
			bool rush,
			bool stealth,
			bool taunt)
		{
			Game game = CreateGame();
			Minion minion = game.ProcessCard<Minion>(cardName, asZeroCost: true);

			minion.Kill();

			Minion ghost = Assert.IsType<Minion>(game.Player1.HandZone.Single(p => p.Card.Id == ghostId));
			Assert.Equal(rush, ghost.IsRush);
			Assert.Equal(stealth, ghost.HasStealth);
			Assert.Equal(taunt, ghost.HasTaunt);
		}

		[Fact]
		public void Overwhelm_ShouldScaleWithFriendlyBeastsAndSpellDamage()
		{
			Game game = CreateGame();
			game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			game.ProcessCard<Minion>("Kobold Geomancer", asZeroCost: true);
			Minion target = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);

			game.ProcessCard("Overwhelm", target, asZeroCost: true);

			Assert.Equal(4, target.Damage);
		}

		[Fact]
		public void MoltenBlast_ShouldDealDamageAndSummonThatManyElementals()
		{
			Game game = CreateGame();
			game.ProcessCard<Minion>("Kobold Geomancer", asZeroCost: true);

			game.ProcessCard("Molten Blast", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(27, game.Player2.Hero.Health);
			Assert.Equal(3, game.Player1.BoardZone.Count(p => p.Card.Id == "SCH_271t"));
		}

		[Fact]
		public void ManafeederPanthara_ShouldDrawOnlyAfterHeroPowerWasUsedThisTurn()
		{
			Game inactiveGame = CreateGame();

			inactiveGame.ProcessCard("Manafeeder Panthara", asZeroCost: true);

			Assert.Empty(inactiveGame.Player1.HandZone);

			Game activeGame = CreateGame();
			activeGame.PlayHeroPower(asZeroCost: true);

			activeGame.ProcessCard("Manafeeder Panthara", asZeroCost: true);

			Assert.Single(activeGame.Player1.HandZone);
		}

		[Fact]
		public void TuralyonTheTenured_ShouldSetDefenderToThreeThreeWhenAttackingMinion()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion defender = game.ProcessCard<Minion>("War Golem", asZeroCost: true);
			game.EndTurn();
			Minion turalyon = game.ProcessCard<Minion>("Turalyon, the Tenured", asZeroCost: true);

			game.Process(MinionAttackTask.Any(game.CurrentPlayer, turalyon, defender));

			Assert.Equal(3, defender.AttackDamage);
			Assert.Equal(0, defender.Health);
			Assert.True(defender.ToBeDestroyed);
			Assert.Equal(3, turalyon.Damage);
		}

		[Fact]
		public void LakeThresher_ShouldDamageAdjacentMinionsWhenAttacking()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion left = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			Minion defender = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			Minion right = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();
			Minion thresher = game.ProcessCard<Minion>("Lake Thresher", asZeroCost: true);
			game.EndTurn();
			game.EndTurn();

			game.Process(MinionAttackTask.Any(game.CurrentPlayer, thresher, defender));

			Assert.Equal(4, left.Damage);
			Assert.Equal(4, defender.Damage);
			Assert.Equal(4, right.Damage);
			Assert.Equal(6, thresher.Damage);
		}

		[Fact]
		public void AdorableInfestation_ShouldBuffSummonAndAddCubToHand()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);

			game.ProcessCard("Adorable Infestation", target, asZeroCost: true);

			Assert.Equal(3, target.AttackDamage);
			Assert.Equal(4, target.Health);
			Assert.Contains(game.Player1.BoardZone, p => p.Card.Id == "SCH_617t");
			Assert.Contains(game.Player1.HandZone, p => p.Card.Id == "SCH_617t");
		}

		[Fact]
		public void BlessingOfAuthority_ShouldBuffAndPreventHeroAttacksThisTurn()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Stonetusk Boar", asZeroCost: true);

			game.ProcessCard("Blessing of Authority", target, asZeroCost: true);

			Assert.Equal(9, target.AttackDamage);
			Assert.Equal(9, target.Health);
			Assert.True(target.CantAttackHeroes);

			game.EndTurn();
			game.EndTurn();

			Assert.False(target.CantAttackHeroes);
		}

		[Fact]
		public void WaveOfApathy_ShouldSetEnemyMinionAttackToOneUntilNextTurn()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Wave of Apathy", asZeroCost: true);

			Assert.Equal(1, enemy.AttackDamage);

			game.EndTurn();
			Assert.Equal(1, enemy.AttackDamage);

			game.EndTurn();
			Assert.Equal(6, enemy.AttackDamage);
		}

		[Fact]
		public void PowerWordFeast_ShouldBuffAndHealTargetAtEndOfTurn()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			target.Damage = 2;

			game.ProcessCard("Power Word: Feast", target, asZeroCost: true);

			Assert.Equal(4, target.AttackDamage);
			Assert.Equal(3, target.Health);
			Assert.Equal(2, target.Damage);

			game.EndTurn();

			Assert.Equal(0, target.Damage);
			Assert.Equal(5, target.Health);
		}

		[Fact]
		public void ArgentBraggart_ShouldMatchHighestAttackAndHealthOnBattlefield()
		{
			Game game = CreateGame();
			game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();
			game.ProcessCard<Minion>("War Golem", asZeroCost: true);
			game.EndTurn();

			Minion braggart = game.ProcessCard<Minion>("Argent Braggart", asZeroCost: true);

			Assert.Equal(7, braggart.AttackDamage);
			Assert.Equal(7, braggart.Health);
		}

		[Fact]
		public void LordBarov_ShouldSetOtherMinionsToOneHealthAndDeathrattleDamageAllMinions()
		{
			Game game = CreateGame();
			Minion friendly = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			game.EndTurn();

			Minion barov = game.ProcessCard<Minion>("Lord Barov", asZeroCost: true);

			Assert.Equal(1, friendly.Health);
			Assert.Equal(1, enemy.Health);
			Assert.Equal(2, barov.Health);

			barov.Kill();

			Assert.True(friendly.ToBeDestroyed);
			Assert.True(enemy.ToBeDestroyed);
		}

		[Fact]
		public void Initiation_ShouldDamageOnlyWhenTargetSurvives()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);

			game.ProcessCard("Initiation", target, asZeroCost: true);

			Assert.Equal(4, target.Damage);
			Assert.Single(game.Player1.BoardZone.GetAll(p => p.Card.Name == "Boulderfist Ogre"));
		}

		[Fact]
		public void Initiation_ShouldSummonFreshCopyWhenDamageKillsTarget()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);

			game.ProcessCard("Initiation", target, asZeroCost: true);

			Minion copy = game.Player1.BoardZone.Single(p => p.Card.Name == "River Crocolisk");
			Assert.NotSame(target, copy);
			Assert.Equal(0, copy.Damage);
			Assert.Equal(2, copy.AttackDamage);
			Assert.Equal(3, copy.Health);
		}

		[Fact]
		public void ShieldOfHonor_ShouldBuffDamagedMinionAndGiveDivineShield()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			target.Damage = 1;

			game.ProcessCard("Shield of Honor", target, asZeroCost: true);

			Assert.Equal(5, target.AttackDamage);
			Assert.True(target.HasDivineShield);
		}

		[Fact]
		public void WretchedTutor_ShouldDealTwoToAllOtherMinionsOnSpellburst()
		{
			Game game = CreateGame();
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion tutor = game.ProcessCard<Minion>("Wretched Tutor", asZeroCost: true);
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.True(wisp.ToBeDestroyed);
			Assert.Equal(0, tutor.Damage);
			Assert.Equal(2, enemy.Damage);
		}

		[Fact]
		public void SoulFragment_ShouldHealHeroAndNotRemainInHandWhenDrawn()
		{
			Game game = CreateGame();
			game.Player1.Hero.Damage = 5;
			IPlayable fragment = Entity.FromCard(game.Player1, Cards.FromId("SCH_307t"));
			game.Player1.DeckZone.Add(fragment);

			Generic.Draw(game.Player1, fragment);

			Assert.Equal(3, game.Player1.Hero.Damage);
			Assert.DoesNotContain(game.Player1.HandZone, p => p.Card.Id == "SCH_307t");
			Assert.Contains(game.Player1.SetasideZone, p => p.Card.Id == "SCH_307t");
		}

		[Fact]
		public void SpiritJailer_ShouldShuffleTwoSoulFragmentsIntoDeck()
		{
			Game game = CreateGame();

			game.ProcessCard("Spirit Jailer", asZeroCost: true);

			Assert.Equal(2, game.Player1.DeckZone.Count(p => p.Card.Id == "SCH_307t"));
		}

		[Fact]
		public void SoulShear_ShouldDamageMinionWithSpellPowerAndShuffleSoulFragments()
		{
			Game game = CreateGame();
			game.ProcessCard<Minion>("Kobold Geomancer", asZeroCost: true);
			Minion target = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);

			game.ProcessCard("Soul Shear", target, asZeroCost: true);

			Assert.Equal(4, target.Damage);
			Assert.Equal(2, game.Player1.DeckZone.Count(p => p.Card.Id == "SCH_307t"));
		}

		[Fact]
		public void Marrowslicer_ShouldEquipWeaponAndShuffleSoulFragments()
		{
			Game game = CreateGame();

			game.ProcessCard("Marrowslicer", asZeroCost: true);

			Assert.NotNull(game.Player1.Hero.Weapon);
			Assert.Equal("SCH_252", game.Player1.Hero.Weapon.Card.Id);
			Assert.Equal(2, game.Player1.DeckZone.Count(p => p.Card.Id == "SCH_307t"));
		}

		[Fact]
		public void SchoolSpirits_ShouldDamageAllMinionsWithSpellPowerAndShuffleSoulFragments()
		{
			Game game = CreateGame();
			Minion friendly = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.ProcessCard<Minion>("Kobold Geomancer", asZeroCost: true);
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("School Spirits", asZeroCost: true);

			Assert.Equal(3, friendly.Damage);
			Assert.Equal(3, enemy.Damage);
			Assert.Equal(2, game.Player1.DeckZone.Count(p => p.Card.Id == "SCH_307t"));
		}

		[Fact]
		public void VoidDrinker_ShouldConsumeSoulFragmentAndGainStats()
		{
			Game game = CreateGame();
			AddSoulFragments(game, 1);

			Minion voidDrinker = game.ProcessCard<Minion>("Void Drinker", asZeroCost: true);

			Assert.Equal(0, game.Player1.DeckZone.Count(p => p.Card.Id == "SCH_307t"));
			Assert.Equal(7, voidDrinker.AttackDamage);
			Assert.Equal(8, voidDrinker.Health);
		}

		[Fact]
		public void ShardshatterMystic_ShouldConsumeSoulFragmentAndDamageOtherMinions()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();
			AddSoulFragments(game, 1);
			Minion friendly = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			Minion mystic = game.ProcessCard<Minion>("Shardshatter Mystic", asZeroCost: true);

			Assert.Equal(0, game.Player1.DeckZone.Count(p => p.Card.Id == "SCH_307t"));
			Assert.Equal(3, friendly.Damage);
			Assert.Equal(0, mystic.Damage);
			Assert.Equal(3, enemy.Damage);
		}

		[Fact]
		public void ShadowlightScholar_ShouldConsumeSoulFragmentAndDealTargetedDamage()
		{
			Game game = CreateGame();
			AddSoulFragments(game, 1);

			game.ProcessCard("Shadowlight Scholar", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(0, game.Player1.DeckZone.Count(p => p.Card.Id == "SCH_307t"));
			Assert.Equal(27, game.Player2.Hero.Health);
		}

		[Fact]
		public void SoulciologistMalicia_ShouldSummonOneSoulForEachSoulFragmentWithoutConsumingThem()
		{
			Game game = CreateGame();
			AddSoulFragments(game, 3);

			game.ProcessCard("Soulciologist Malicia", asZeroCost: true);

			Minion[] souls = game.Player1.BoardZone.GetAll(p => p.Card.Id == "SCH_703t");
			Assert.Equal(3, souls.Length);
			Assert.All(souls, soul => Assert.True(soul.IsRush));
			Assert.Equal(3, game.Player1.DeckZone.Count(p => p.Card.Id == "SCH_307t"));
		}

		[Fact]
		public void SoulshardLapidary_ShouldConsumeSoulFragmentAndGiveHeroAttack()
		{
			Game game = CreateGame();
			AddSoulFragments(game, 1);

			game.ProcessCard("Soulshard Lapidary", asZeroCost: true);

			Assert.Equal(0, game.Player1.DeckZone.Count(p => p.Card.Id == "SCH_307t"));
			Assert.Equal(5, game.Player1.Hero.AttackDamage);
		}
	}
}
