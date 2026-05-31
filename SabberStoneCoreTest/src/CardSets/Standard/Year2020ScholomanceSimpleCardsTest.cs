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

		private static void AddSoulFragments(Game game, int amount)
		{
			for (int i = 0; i < amount; i++)
				game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromId("SCH_307t")));
		}

		private static IPlayable AddHandCard(Game game, string cardName)
		{
			return Generic.DrawCard(game.Player1, Cards.FromName(cardName));
		}

		[Fact]
		public void BloodHerald_ShouldGainStatsInHandWhenFriendlyMinionDies()
		{
			Game game = CreateGame();
			Minion herald = Assert.IsType<Minion>(Generic.DrawCard(game.Player1, Cards.FromId("SCH_618")));
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			wisp.Kill();

			Assert.Contains(herald, game.Player1.HandZone);
			Assert.Equal(2, herald.AttackDamage);
			Assert.Equal(2, herald.Health);
		}

		[Fact]
		public void FelGuardians_ShouldCostLessInHandWhenFriendlyMinionDies()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DEMONHUNTER);
			IPlayable felGuardians = Generic.DrawCard(game.Player1, Cards.FromId("SCH_357"));
			Minion first = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion second = game.ProcessCard<Minion>("Murloc Raider", asZeroCost: true);

			Assert.Equal(7, felGuardians.Cost);

			first.Kill();

			Assert.Contains(felGuardians, game.Player1.HandZone);
			Assert.Equal(6, felGuardians.Cost);

			second.Kill();

			Assert.Contains(felGuardians, game.Player1.HandZone);
			Assert.Equal(5, felGuardians.Cost);
		}

		[Fact]
		public void FelGuardians_ShouldSummonThreeTauntDemons()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DEMONHUNTER);

			game.ProcessCard("Fel Guardians", asZeroCost: true);

			Minion[] felhounds = game.Player1.BoardZone.GetAll(p => p.Card.Id == "SCH_357t");
			Assert.Equal(3, felhounds.Length);
			Assert.All(felhounds, felhound =>
			{
				Assert.Equal(1, felhound.AttackDamage);
				Assert.Equal(2, felhound.Health);
				Assert.True(felhound.HasTaunt);
				Assert.True(felhound.Card.IsRace(Race.DEMON));
			});
		}

		[Fact]
		public void InfiltratorLilian_ShouldSummonForsakenLilianThatAttacksRandomEnemyOnDeathrattle()
		{
			Game game = CreateGame(player1HeroClass: CardClass.ROGUE);
			Minion infiltrator = game.ProcessCard<Minion>("Infiltrator Lilian", asZeroCost: true);

			Assert.True(infiltrator.HasStealth);

			infiltrator.Kill();

			Assert.DoesNotContain(infiltrator, game.Player1.BoardZone);
			Minion forsaken = Assert.Single(game.Player1.BoardZone.GetAll(p => p.Card.Id == "SCH_426t"));
			Assert.Equal(4, forsaken.AttackDamage);
			Assert.Equal(2, forsaken.Health);
			Assert.Equal(4, game.Player2.Hero.Damage);
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
		public void RobesOfProtection_ShouldPreventTargetingFriendlyMinionsWithSpellsAndHeroPowers()
		{
			Game game = CreateGame(player2HeroClass: CardClass.MAGE);
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard<Minion>("Robes of Protection", asZeroCost: true);

			Assert.True(wisp.CantBeTargetedBySpells);
			Assert.True(wisp.CantBeTargetedByHeroPowers);
			Spell moonfire = Assert.IsType<Spell>(Entity.FromCard(game.Player2, Cards.FromName("Moonfire")));
			Assert.False(moonfire.TargetingRequirements(wisp));
			Assert.False(game.Player2.Hero.HeroPower.TargetingRequirements(wisp));
		}

		[Fact]
		public void CabalAcolyte_ShouldStealRandomEnemyMinionWithTwoOrLessAttackOnSpellburst()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PRIEST);
			game.ProcessCard<Minion>("Cabal Acolyte", asZeroCost: true);
			game.EndTurn();
			Minion lowAttack = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			Minion highAttack = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Contains(lowAttack, game.Player1.BoardZone);
			Assert.DoesNotContain(lowAttack, game.Player2.BoardZone);
			Assert.Equal(game.Player1, lowAttack.Controller);
			Assert.Contains(highAttack, game.Player2.BoardZone);

			game.EndTurn();
			Minion secondLowAttack = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			game.EndTurn();
			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Contains(secondLowAttack, game.Player2.BoardZone);
			Assert.Equal(game.Player2, secondLowAttack.Controller);
		}

		[Fact]
		public void DisciplinarianGandling_ShouldDestroyPlayedMinionAndSummonFailedStudent()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PRIEST);

			Minion gandling = game.ProcessCard<Minion>("Disciplinarian Gandling", asZeroCost: true);
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			Assert.Contains(gandling, game.Player1.BoardZone);
			Assert.DoesNotContain(wisp, game.Player1.BoardZone);
			Assert.Contains(game.Player1.GraveyardZone, p => p.Card.Name == "Wisp");
			Minion failedStudent = game.Player1.BoardZone.Single(p => p.Card.Id == "SCH_126t");
			Assert.Equal(4, failedStudent.AttackDamage);
			Assert.Equal(4, failedStudent.Health);
		}

		[Fact]
		public void DevoutPupil_ShouldCostLessForSpellsCastOnFriendlyCharacters()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PRIEST);
			Minion friendly = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			IPlayable pupilInHand = Generic.DrawCard(game.Player1, Cards.FromId("SCH_139"));

			Assert.Equal(6, pupilInHand.Cost);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(6, pupilInHand.Cost);

			game.ProcessCard("Power Word: Feast", friendly, asZeroCost: true);

			Assert.Equal(5, pupilInHand.Cost);

			IPlayable laterPupil = Generic.DrawCard(game.Player1, Cards.FromId("SCH_139"));

			Assert.Equal(5, laterPupil.Cost);
		}

		[Fact]
		public void FleshGiant_ShouldCostLessWhenOwnHeroHealthChangesDuringOwnTurns()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PRIEST);
			IPlayable giantInHand = Generic.DrawCard(game.Player1, Cards.FromId("SCH_140"));

			Assert.Equal(8, giantInHand.Cost);

			game.ProcessCard("Raise Dead", asZeroCost: true);

			Assert.Equal(27, game.Player1.Hero.Health);
			Assert.Equal(7, giantInHand.Cost);

			game.EndTurn();
			game.ProcessCard("Moonfire", game.Player1.Hero, asZeroCost: true);

			Assert.Equal(26, game.Player1.Hero.Health);
			Assert.Equal(7, giantInHand.Cost);

			game.EndTurn();
			game.ProcessCard("Holy Light", game.Player1.Hero, asZeroCost: true);

			Assert.Equal(30, game.Player1.Hero.Health);
			Assert.Equal(6, giantInHand.Cost);

			IPlayable laterGiant = Generic.DrawCard(game.Player1, Cards.FromId("SCH_140"));

			Assert.Equal(6, laterGiant.Cost);
		}

		[Fact]
		public void BrittleboneDestroyer_ShouldDestroyMinionOnlyIfHeroHealthChangedThisTurn()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PRIEST);
			Minion unchangedTarget = game.ProcessCard<Minion>("Oasis Snapjaw", asZeroCost: true);

			game.ProcessCard<Minion>("Brittlebone Destroyer", unchangedTarget, asZeroCost: true);

			Assert.False(unchangedTarget.ToBeDestroyed);

			EmptyZone(game.Player1.BoardZone.GetAll());
			game.ProcessCard("Raise Dead", asZeroCost: true);
			game.EndTurn();
			game.EndTurn();
			Minion previousTurnTarget = game.ProcessCard<Minion>("Oasis Snapjaw", asZeroCost: true);

			game.ProcessCard<Minion>("Brittlebone Destroyer", previousTurnTarget, asZeroCost: true);

			Assert.False(previousTurnTarget.ToBeDestroyed);

			EmptyZone(game.Player1.BoardZone.GetAll());
			Minion damagedThisTurnTarget = game.ProcessCard<Minion>("Oasis Snapjaw", asZeroCost: true);
			game.ProcessCard("Raise Dead", asZeroCost: true);

			game.ProcessCard<Minion>("Brittlebone Destroyer", damagedThisTurnTarget, asZeroCost: true);

			Assert.True(damagedThisTurnTarget.ToBeDestroyed);
		}

		[Fact]
		public void BrittleboneDestroyer_ShouldCountHeroHealingAsHealthChangedThisTurn()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PRIEST);
			game.Player1.Hero.Damage = 3;
			Minion target = game.ProcessCard<Minion>("Oasis Snapjaw", asZeroCost: true);
			game.ProcessCard("Holy Light", game.Player1.Hero, asZeroCost: true);

			game.ProcessCard<Minion>("Brittlebone Destroyer", target, asZeroCost: true);

			Assert.True(target.ToBeDestroyed);
		}

		[Fact]
		public void Vectus_ShouldGiveWhelpsFriendlyDeathrattlesThatDiedThisGame()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion enemyPython = game.ProcessCard<Minion>("Bloated Python", asZeroCost: true);
			enemyPython.Kill();
			game.EndTurn();
			Minion friendlyPython = game.ProcessCard<Minion>("Bloated Python", asZeroCost: true);
			friendlyPython.Kill();
			Minion existingHandler = game.Player1.BoardZone.Single(p => p.Card.Id == "SCH_340t");

			game.ProcessCard<Minion>("Vectus", asZeroCost: true);

			Minion[] whelps = game.Player1.BoardZone.GetAll(p => p.Card.Id == "SCH_162t");
			Assert.Equal(2, whelps.Length);
			Assert.All(whelps, whelp => Assert.True(whelp.HasDeathrattle));

			foreach (Minion whelp in whelps)
				whelp.Kill();

			Minion[] handlers = game.Player1.BoardZone.GetAll(p => p.Card.Id == "SCH_340t");
			Assert.Equal(3, handlers.Length);
			Assert.Contains(existingHandler, handlers);
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
		public void TourGuide_ShouldMakeNextHeroPowerCostZeroUntilUsed()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE, player2HeroClass: CardClass.WARLOCK);

			game.ProcessCard("Tour Guide", asZeroCost: true);

			Assert.Equal(0, game.Player1.Hero.HeroPower.Cost);

			game.EndTurn();
			game.EndTurn();

			Assert.Equal(0, game.Player1.Hero.HeroPower.Cost);

			game.PlayHeroPower(game.Player2.Hero);

			Assert.Equal(2, game.Player1.Hero.HeroPower.Cost);
			Assert.Equal(29, game.Player2.Hero.Health);
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

		[Fact]
		public void PlaguedProtodrake_ShouldSummonRandomSevenCostMinionOnDeathrattle()
		{
			Game game = CreateGame();
			Minion protodrake = game.ProcessCard<Minion>("Plagued Protodrake", asZeroCost: true);

			protodrake.Kill();

			Minion summoned = Assert.Single(game.Player1.BoardZone);
			Assert.NotEqual("SCH_711", summoned.Card.Id);
			Assert.Equal(7, summoned.Card.Cost);
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
		public void RasFrostwhisper_ShouldDamageAllEnemiesAtEndOfTurnWithSpellPower()
		{
			Game game = CreateGame(player1HeroClass: CardClass.SHAMAN);
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();
			Minion friendly = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.ProcessCard<Minion>("Kobold Geomancer", asZeroCost: true);
			game.ProcessCard<Minion>("Ras Frostwhisper", asZeroCost: true);

			game.EndTurn();

			Assert.Equal(28, game.Player2.Hero.Health);
			Assert.Equal(2, enemy.Damage);
			Assert.Equal(0, friendly.Damage);
		}

		[Fact]
		public void BrainFreeze_ShouldFreezeMinionWithoutCombo()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Brain Freeze", target, asZeroCost: true);

			Assert.Equal(1, target[GameTag.FROZEN]);
			Assert.Equal(0, target.Damage);
		}

		[Fact]
		public void BrainFreeze_ShouldFreezeAndDamageMinionWithCombo()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();
			game.ProcessCard("Wisp", asZeroCost: true);

			game.ProcessCard("Brain Freeze", target, asZeroCost: true);

			Assert.Equal(1, target[GameTag.FROZEN]);
			Assert.Equal(3, target.Damage);
		}

		[Fact]
		public void WandThief_ShouldNotDiscoverWithoutCombo()
		{
			Game game = CreateGame(player1HeroClass: CardClass.ROGUE);

			game.ProcessCard<Minion>("Wand Thief", asZeroCost: true);

			Assert.Null(game.Player1.Choice);
		}

		[Fact]
		public void WandThief_ShouldDiscoverMageSpellWithCombo()
		{
			Game game = CreateGame(player1HeroClass: CardClass.ROGUE);
			game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard<Minion>("Wand Thief", asZeroCost: true);

			Assert.NotNull(game.Player1.Choice);
			Assert.All(game.Player1.Choice.Choices, choice =>
			{
				Card card = game.IdEntityDic[choice].Card;
				Assert.Equal(CardType.SPELL, card.Type);
				Assert.Equal(CardClass.MAGE, card.Class);
			});
		}

		[Fact]
		public void JandiceBarov_ShouldSummonTwoFiveCostMinionsAndChosenIllusionDiesWhenDamaged()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE);

			Minion jandice = game.ProcessCard<Minion>("Jandice Barov", asZeroCost: true);

			Minion[] summoned = game.Player1.BoardZone.GetAll(p => p != jandice);
			Assert.Equal(2, summoned.Length);
			Assert.All(summoned, minion => Assert.Equal(5, minion.Card.Cost));
			Assert.NotNull(game.Player1.Choice);
			Assert.All(summoned, minion => Assert.Contains(minion.Id, game.Player1.Choice.Choices));

			Minion illusion = summoned[0];
			Minion real = summoned[1];

			game.Process(ChooseTask.Pick(game.Player1, illusion.Id));

			Assert.Null(game.Player1.Choice);
			Assert.Contains(illusion, game.Player1.BoardZone);
			Assert.Contains(real, game.Player1.BoardZone);

			illusion.HasDivineShield = false;
			Generic.DamageCharFunc.Invoke(jandice, illusion, 1, false);
			game.DeathProcessingAndAuraUpdate();

			Assert.DoesNotContain(illusion, game.Player1.BoardZone);
			Assert.Contains(real, game.Player1.BoardZone);
		}

		[Fact]
		public void DevolvingMissiles_ShouldTransformRandomEnemyMinionsIntoLowerCostMinions()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE);
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			string originalCardId = target.Card.Id;
			game.EndTurn();

			game.ProcessCard("Devolving Missiles", asZeroCost: true);

			Minion transformed = Assert.Single(game.Player2.BoardZone);
			Assert.NotEqual(originalCardId, transformed.Card.Id);
			Assert.Same(game.Player2, transformed.Controller);
			Assert.Equal(0, transformed.Card.Cost);
		}

		[Fact]
		public void Firebrand_ShouldSplitFourDamageAmongEnemyMinionsOnSpellburst()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();
			Minion firebrand = game.ProcessCard<Minion>("Firebrand", asZeroCost: true);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(4, enemy.Damage);
			Assert.Equal(0, firebrand.Damage);
		}

		[Fact]
		public void WyrmWeaver_ShouldSummonTwoManaWyrmsOnSpellburst()
		{
			Game game = CreateGame();
			game.ProcessCard<Minion>("Wyrm Weaver", asZeroCost: true);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Minion[] wyrms = game.Player1.BoardZone.GetAll(p => p.Card.Id == "NEW1_012");
			Assert.Equal(2, wyrms.Length);
			Assert.All(wyrms, wyrm =>
			{
				Assert.Equal(1, wyrm.AttackDamage);
				Assert.Equal(3, wyrm.Health);
			});
		}

		[Fact]
		public void Combustion_ShouldDamageTargetAndBothNeighborsWithExcessIncludingSpellDamage()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE);
			game.ProcessCard<Minion>("Lab Partner", asZeroCost: true);
			game.EndTurn();
			Minion left = game.ProcessCard<Minion>("Oasis Snapjaw", asZeroCost: true);
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion right = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Combustion", target, asZeroCost: true);

			Assert.DoesNotContain(target, game.Player2.BoardZone);
			Assert.Equal(4, left.Damage);
			Assert.Equal(4, right.Damage);
		}

		[Fact]
		public void CramSession_ShouldDrawOnePlusSpellDamageCards()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE);
			EmptyZone(game.Player1.DeckZone.GetAll());
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Wisp")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Murloc Raider")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Bloodfen Raptor")));
			game.ProcessCard<Minion>("Lab Partner", asZeroCost: true);

			game.ProcessCard("Cram Session", asZeroCost: true);

			Assert.Equal(2, game.Player1.HandZone.Count);
			Assert.Contains(game.Player1.HandZone, p => p.Card.Name == "Murloc Raider");
			Assert.Contains(game.Player1.HandZone, p => p.Card.Name == "Bloodfen Raptor");
			Assert.Single(game.Player1.DeckZone);
			Assert.Contains(game.Player1.DeckZone, p => p.Card.Name == "Wisp");
		}

		[Fact]
		public void PotionOfIllusion_ShouldAddOneOneCopiesThatCostOne()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE);
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion yeti = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);

			game.ProcessCard("Potion of Illusion", asZeroCost: true);

			Assert.Contains(wisp, game.Player1.BoardZone);
			Assert.Contains(yeti, game.Player1.BoardZone);
			Assert.Equal(2, game.Player1.HandZone.Count);

			IPlayable wispCopy = Assert.Single(game.Player1.HandZone.Where(p => p.Card.Name == "Wisp"));
			Assert.Equal(1, wispCopy.Cost);
			Assert.Equal(1, wispCopy[GameTag.ATK]);
			Assert.Equal(1, wispCopy[GameTag.HEALTH]);

			IPlayable yetiCopy = Assert.Single(game.Player1.HandZone.Where(p => p.Card.Name == "Chillwind Yeti"));
			Assert.Equal(1, yetiCopy.Cost);
			Assert.Equal(1, yetiCopy[GameTag.ATK]);
			Assert.Equal(1, yetiCopy[GameTag.HEALTH]);
		}

		[Fact]
		public void MozakiMasterDuelist_ShouldGainSpellDamageAfterEachFriendlySpell()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE);
			Minion mozaki = game.ProcessCard<Minion>("Mozaki, Master Duelist", asZeroCost: true);

			Assert.Equal(0, game.Player1.CurrentSpellPower);
			Assert.Equal(0, mozaki[GameTag.SPELLPOWER]);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(1, game.Player2.Hero.Damage);
			Assert.Equal(1, game.Player1.CurrentSpellPower);
			Assert.Equal(1, mozaki[GameTag.SPELLPOWER]);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(3, game.Player2.Hero.Damage);
			Assert.Equal(2, game.Player1.CurrentSpellPower);
			Assert.Equal(2, mozaki[GameTag.SPELLPOWER]);
		}

		[Fact]
		public void KroluskBarkstripper_ShouldDestroyRandomEnemyMinionOnSpellburst()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();
			Minion barkstripper = game.ProcessCard<Minion>("Krolusk Barkstripper", asZeroCost: true);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.True(enemy.ToBeDestroyed);
			Assert.False(barkstripper.ToBeDestroyed);
		}

		[Fact]
		public void CrimsonHothead_ShouldGainAttackAndTauntOnSpellburstOnlyOnce()
		{
			Game game = CreateGame();
			Minion hothead = game.ProcessCard<Minion>("Crimson Hothead", asZeroCost: true);

			Assert.Equal(3, hothead.AttackDamage);
			Assert.False(hothead.HasTaunt);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(4, hothead.AttackDamage);
			Assert.True(hothead.HasTaunt);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(4, hothead.AttackDamage);
			Assert.True(hothead.HasTaunt);
		}

		[Fact]
		public void TwilightRunner_ShouldDrawTwoCardsWhenItAttacks()
		{
			Game game = CreateGame();
			Minion runner = game.ProcessCard<Minion>("Twilight Runner", asZeroCost: true);
			game.EndTurn();
			game.EndTurn();
			int handCount = game.Player1.HandZone.Count;

			game.Process(MinionAttackTask.Any(game.CurrentPlayer, runner, game.Player2.Hero));

			Assert.Equal(handCount + 2, game.Player1.HandZone.Count);
		}

		[Fact]
		public void SelfSharpeningSword_ShouldGainOneAttackAfterHeroAttacks()
		{
			Game game = CreateGame();

			game.ProcessCard("Self-Sharpening Sword", asZeroCost: true);
			game.Process(HeroAttackTask.Any(game.CurrentPlayer, game.Player2.Hero));

			Assert.NotNull(game.Player1.Hero.Weapon);
			Assert.Equal(2, game.Player1.Hero.Weapon.AttackDamage);
		}

		[Fact]
		public void DoctorKrastinov_ShouldGiveWeaponAttackAndDurabilityWhenItAttacks()
		{
			Game game = CreateGame(player1HeroClass: CardClass.WARRIOR);
			game.ProcessCard("Fiery War Axe", asZeroCost: true);
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Oasis Snapjaw", asZeroCost: true);
			game.EndTurn();
			Minion doctor = game.ProcessCard<Minion>("Doctor Krastinov", asZeroCost: true);

			game.Process(MinionAttackTask.Any(game.CurrentPlayer, doctor, target));

			Assert.NotNull(game.Player1.Hero.Weapon);
			Assert.Equal(4, game.Player1.Hero.Weapon.AttackDamage);
			Assert.Equal(3, game.Player1.Hero.Weapon.Durability);
		}

		[Fact]
		public void VulperaToxinblade_ShouldGiveWeaponTwoAttackWhileAlive()
		{
			Game game = CreateGame(player1HeroClass: CardClass.ROGUE);
			game.ProcessCard("Fiery War Axe", asZeroCost: true);

			Assert.Equal(3, game.Player1.Hero.Weapon.AttackDamage);

			Minion toxinblade = game.ProcessCard<Minion>("Vulpera Toxinblade", asZeroCost: true);

			Assert.Equal(5, game.Player1.Hero.Weapon.AttackDamage);

			toxinblade.Kill();
			game.DeathProcessingAndAuraUpdate();

			Assert.Equal(3, game.Player1.Hero.Weapon.AttackDamage);
		}

		[Fact]
		public void Steeldancer_ShouldSummonRandomMinionWithCostEqualToWeaponAttack()
		{
			Game game = CreateGame(player1HeroClass: CardClass.ROGUE);

			game.ProcessCard<Minion>("Steeldancer", asZeroCost: true);

			Assert.Single(game.Player1.BoardZone);

			EmptyZone(game.Player1.BoardZone.GetAll());
			game.ProcessCard("Fiery War Axe", asZeroCost: true);

			game.ProcessCard<Minion>("Steeldancer", asZeroCost: true);

			Minion summoned = Assert.Single(game.Player1.BoardZone.GetAll(p => p.Card.Id != "SCH_522"));
			Assert.Equal(3, summoned.Card.Cost);
		}

		[Fact]
		public void CuttingClass_ShouldCostLessPerWeaponAttackAndDrawTwoCards()
		{
			Game game = CreateGame(player1HeroClass: CardClass.ROGUE);
			game.Player1.BaseMana = 10;
			EmptyZone(game.Player1.DeckZone.GetAll());
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Wisp")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Bloodfen Raptor")));

			IPlayable cuttingClass = Generic.DrawCard(game.Player1, Cards.FromName("Cutting Class"));
			Assert.Equal(5, cuttingClass.Cost);

			game.ProcessCard("Fiery War Axe", asZeroCost: true);
			Assert.Equal(3, game.Player1.Hero.Weapon.AttackDamage);
			Assert.Equal(2, cuttingClass.Cost);

			Assert.True(game.Process(PlayCardTask.Any(game.Player1, cuttingClass)));

			Assert.Equal(8, game.Player1.RemainingMana);
			Assert.Empty(game.Player1.DeckZone);
			Assert.Contains(game.Player1.HandZone, p => p.Card.Name == "Wisp");
			Assert.Contains(game.Player1.HandZone, p => p.Card.Name == "Bloodfen Raptor");
		}

		[Fact]
		public void RuneDagger_ShouldGrantSpellDamageAfterHeroAttacksForThisTurn()
		{
			Game game = CreateGame(player1HeroClass: CardClass.SHAMAN);

			game.ProcessCard("Rune Dagger", asZeroCost: true);
			game.Player1.Hero.Attack(game.Player2.Hero);

			Assert.Equal(1, game.Player1.CurrentSpellPower);
			Assert.Equal(29, game.Player2.Hero.Health);

			IPlayable fireball = Generic.DrawCard(game.Player1, Cards.FromName("Fireball"));
			fireball.Cost = 0;
			Assert.True(game.Process(PlayCardTask.Any(game.Player1, fireball, game.Player2.Hero)));

			Assert.Equal(22, game.Player2.Hero.Health);
			Assert.Equal(1, game.Player1.CurrentSpellPower);

			game.EndTurn();
			game.EndTurn();

			Assert.Equal(0, game.Player1.CurrentSpellPower);
		}

		[Fact]
		public void LightningBloom_ShouldGainTemporaryManaAndOverloadNextTurn()
		{
			Game game = CreateGame(player1HeroClass: CardClass.SHAMAN);
			game.Player1.BaseMana = 5;
			game.Player1.UsedMana = 5;

			game.ProcessCard("Lightning Bloom", asZeroCost: true);

			Assert.Equal(2, game.Player1.RemainingMana);
			Assert.Equal(2, game.Player1.TemporaryMana);
			Assert.Equal(2, game.Player1.OverloadOwed);

			game.EndTurn();
			game.EndTurn();

			Assert.Equal(0, game.Player1.TemporaryMana);
			Assert.Equal(2, game.Player1.OverloadLocked);
		}

		[Fact]
		public void TidalWave_ShouldDamageAllMinionsAndHealFromLifesteal()
		{
			Game game = CreateGame(player1HeroClass: CardClass.SHAMAN);
			game.Player1.Hero.Damage = 10;
			Minion friendly = game.ProcessCard<Minion>("Oasis Snapjaw", asZeroCost: true);
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Oasis Snapjaw", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Tidal Wave", asZeroCost: true);

			Assert.Equal(3, friendly.Damage);
			Assert.Equal(3, enemy.Damage);
			Assert.Equal(4, game.Player1.Hero.Damage);
		}

		[Fact]
		public void SorcerousSubstitute_ShouldSummonCopyOnlyWithSpellDamage()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE);

			Minion substituteWithoutSpellDamage = game.ProcessCard<Minion>("Sorcerous Substitute", asZeroCost: true);

			Assert.Single(game.Player1.BoardZone.GetAll(p => p.Card.Id == "SCH_530"));
			Assert.Contains(substituteWithoutSpellDamage, game.Player1.BoardZone);

			EmptyZone(game.Player1.BoardZone.GetAll());
			game.ProcessCard<Minion>("Lab Partner", asZeroCost: true);
			Minion substituteWithSpellDamage = game.ProcessCard<Minion>("Sorcerous Substitute", asZeroCost: true);

			Minion[] substitutes = game.Player1.BoardZone.GetAll(p => p.Card.Id == "SCH_530");
			Assert.Equal(2, substitutes.Length);
			Assert.Contains(substituteWithSpellDamage, substitutes);
			Assert.All(substitutes, substitute =>
			{
				Assert.Equal(6, substitute.AttackDamage);
				Assert.Equal(6, substitute.Health);
			});
		}

		[Fact]
		public void LabPartner_ShouldProvideSpellDamageWhileAlive()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE);

			Minion labPartner = game.ProcessCard<Minion>("Lab Partner", asZeroCost: true);

			Assert.Equal(1, game.Player1.CurrentSpellPower);

			game.ProcessCard("Fireball", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(23, game.Player2.Hero.Health);

			labPartner.Kill();

			Assert.Equal(0, game.Player1.CurrentSpellPower);

			game.ProcessCard("Fireball", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(17, game.Player2.Hero.Health);
		}

		[Fact]
		public void HeadmasterKelThuzad_ShouldSummonMinionsDestroyedBySpellburstSpellOnlyOnce()
		{
			Game game = CreateGame(player1HeroClass: CardClass.WARLOCK);
			Minion earlierDeadMinion = game.ProcessCard<Minion>("Murloc Raider", asZeroCost: true);
			earlierDeadMinion.Kill();
			Minion headmaster = game.ProcessCard<Minion>("Headmaster Kel'Thuzad", asZeroCost: true);
			game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			game.EndTurn();
			game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Hellfire", asZeroCost: true);

			Assert.Contains(headmaster, game.Player1.BoardZone);
			Assert.Equal(3, headmaster.Damage);
			Assert.Equal(2, game.Player1.BoardZone.GetAll(p => p.Card.Name == "Wisp").Length);
			Assert.DoesNotContain(game.Player1.BoardZone, p => p.Card.Name == "Murloc Raider");
			Assert.Empty(game.Player2.BoardZone);

			game.ProcessCard("Hellfire", asZeroCost: true);

			Assert.Empty(game.Player1.BoardZone.GetAll(p => p.Card.Name == "Wisp"));
		}

		[Fact]
		public void SecretPassage_ShouldTemporarilyReplaceHandWithFourCardsFromDeckAndSwapBackNextTurn()
		{
			Game game = CreateGame(player1HeroClass: CardClass.ROGUE);
			EmptyZone(game.Player1.DeckZone.GetAll());
			IPlayable originalMinion = Generic.DrawCard(game.Player1, Cards.FromName("Murloc Raider"));
			IPlayable originalSpell = Generic.DrawCard(game.Player1, Cards.FromName("Backstab"));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Wisp")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Bloodfen Raptor")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("River Crocolisk")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Boulderfist Ogre")));

			game.ProcessCard("Secret Passage", asZeroCost: true);

			Assert.DoesNotContain(originalMinion, game.Player1.HandZone);
			Assert.DoesNotContain(originalSpell, game.Player1.HandZone);
			Assert.Contains(originalMinion, game.Player1.SetasideZone);
			Assert.Contains(originalSpell, game.Player1.SetasideZone);
			Assert.Equal(4, game.Player1.HandZone.Count);
			Assert.Contains(game.Player1.HandZone, p => p.Card.Name == "Wisp");
			Assert.Contains(game.Player1.HandZone, p => p.Card.Name == "Bloodfen Raptor");
			Assert.Contains(game.Player1.HandZone, p => p.Card.Name == "River Crocolisk");
			Assert.Contains(game.Player1.HandZone, p => p.Card.Name == "Boulderfist Ogre");
			Assert.Empty(game.Player1.DeckZone);

			IPlayable temporaryWisp = game.Player1.HandZone.Single(p => p.Card.Name == "Wisp");
			game.ProcessCard(temporaryWisp, asZeroCost: true);
			game.EndTurn();

			Assert.DoesNotContain(originalMinion, game.Player1.HandZone);
			Assert.DoesNotContain(originalSpell, game.Player1.HandZone);

			game.EndTurn();

			Assert.Contains(originalMinion, game.Player1.HandZone);
			Assert.Contains(originalSpell, game.Player1.HandZone);
			Assert.Contains(temporaryWisp, game.Player1.BoardZone);
			Assert.Equal(3, game.Player1.HandZone
				.Concat(game.Player1.DeckZone)
				.Count(p => p.Card.Name == "Bloodfen Raptor" ||
					p.Card.Name == "River Crocolisk" ||
					p.Card.Name == "Boulderfist Ogre"));
			Assert.DoesNotContain(game.Player1.SetasideZone, p => p.Card.Name == "Murloc Raider");
			Assert.DoesNotContain(game.Player1.SetasideZone, p => p.Card.Name == "Backstab");
		}

		[Fact]
		public void Playmaker_ShouldSummonOneHealthCopyAfterRushMinionIsPlayed()
		{
			Game game = CreateGame(player1HeroClass: CardClass.WARRIOR);
			game.ProcessCard<Minion>("Playmaker", asZeroCost: true);

			Minion rush = game.ProcessCard<Minion>("Rabid Worgen", asZeroCost: true);

			Minion[] worgens = game.Player1.BoardZone.GetAll(p => p.Card.Name == "Rabid Worgen");
			Assert.Equal(2, worgens.Length);
			Assert.Contains(rush, worgens);
			Minion copy = Assert.Single(worgens.Where(p => p.Id != rush.Id));
			Assert.True(copy.IsRush);
			Assert.Equal(rush.AttackDamage, copy.AttackDamage);
			Assert.Equal(1, copy.Health);
			Assert.Equal(3, rush.Health);

			int boardCount = game.Player1.BoardZone.Count;

			game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);

			Assert.Equal(boardCount + 1, game.Player1.BoardZone.Count);
			Assert.Single(game.Player1.BoardZone.GetAll(p => p.Card.Name == "River Crocolisk"));
		}

		[Fact]
		public void TrueaimCrescent_ShouldMakeFriendlyMinionsAttackHeroAttackTarget()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DEMONHUNTER);
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion crocolisk = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Oasis Snapjaw", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Trueaim Crescent", asZeroCost: true);
			game.Process(HeroAttackTask.Any(game.CurrentPlayer, target));

			Assert.Equal(4, target.Damage);
			Assert.Equal(2, game.Player1.Hero.Damage);
			Assert.DoesNotContain(wisp, game.Player1.BoardZone);
			Assert.Contains(crocolisk, game.Player1.BoardZone);
			Assert.Equal(2, crocolisk.Damage);
		}

		[Fact]
		public void ReapersScythe_ShouldDamageAdjacentMinionsAfterSpellburst()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion left = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			Minion defender = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			Minion right = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Reaper's Scythe", asZeroCost: true);
			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);
			game.Process(HeroAttackTask.Any(game.CurrentPlayer, defender));

			Assert.Equal(4, left.Damage);
			Assert.Equal(4, defender.Damage);
			Assert.Equal(4, right.Damage);
			Assert.Equal(6, game.Player1.Hero.Damage);
		}

		[Fact]
		public void ReapersScythe_ShouldOnlyDamageAdjacentMinionsThisTurn()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion left = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			Minion defender = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			Minion right = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Reaper's Scythe", asZeroCost: true);
			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);
			game.EndTurn();
			game.EndTurn();
			game.Process(HeroAttackTask.Any(game.CurrentPlayer, defender));

			Assert.Equal(0, left.Damage);
			Assert.Equal(4, defender.Damage);
			Assert.Equal(0, right.Damage);
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
		public void Magehunter_ShouldSilenceMinionItAttacks()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DEMONHUNTER);
			game.EndTurn();
			Minion defender = game.ProcessCard<Minion>("Sen'jin Shieldmasta", asZeroCost: true);
			game.EndTurn();
			Minion magehunter = game.ProcessCard<Minion>("Magehunter", asZeroCost: true);

			Assert.True(defender.HasTaunt);

			game.Process(MinionAttackTask.Any(game.CurrentPlayer, magehunter, defender));

			Assert.False(defender.HasTaunt);
			Assert.True(defender.IsSilenced);
			Assert.Equal(2, defender.Damage);
		}

		[Fact]
		public void AncientVoidHound_ShouldStealAttackAndHealthFromAllEnemyMinionsAtEndOfTurn()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DEMONHUNTER);
			game.EndTurn();
			Minion firstEnemy = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			Minion secondEnemy = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			game.EndTurn();
			Minion hound = game.ProcessCard<Minion>("Ancient Void Hound", asZeroCost: true);

			game.EndTurn();

			Assert.Equal(12, hound.AttackDamage);
			Assert.Equal(12, hound.Health);
			Assert.Equal(5, firstEnemy.AttackDamage);
			Assert.Equal(6, firstEnemy.Health);
			Assert.Equal(1, secondEnemy.AttackDamage);
			Assert.Equal(2, secondEnemy.Health);
		}

		[Fact]
		public void DoubleJump_ShouldDrawOutcastCardFromDeck()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DEMONHUNTER);
			EmptyZone(game.Player1.DeckZone.GetAll());
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Wisp")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Spectral Sight")));

			game.ProcessCard("Double Jump", asZeroCost: true);

			IPlayable drawn = Assert.Single(game.Player1.HandZone);
			Assert.Equal("BT_491", drawn.Card.Id);
			Assert.Single(game.Player1.DeckZone);
			Assert.Equal("Wisp", game.Player1.DeckZone.Single().Card.Name);
		}

		[Fact]
		public void Glide_ShouldShuffleOwnHandIntoDeckAndDrawFourCards()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DEMONHUNTER);
			EmptyZone(game.Player1.DeckZone.GetAll());
			EmptyZone(game.Player2.DeckZone.GetAll());
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Chillwind Yeti")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Boulderfist Ogre")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("River Crocolisk")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Bloodfen Raptor")));
			game.Player2.DeckZone.Add(Entity.FromCard(game.Player2, Cards.FromName("Murloc Raider")));
			IPlayable wisp = AddHandCard(game, "Wisp");
			IPlayable glide = AddHandCard(game, "Glide");
			IPlayable murloc = AddHandCard(game, "Murloc Raider");
			IPlayable opponentCard = Generic.DrawCard(game.Player2, Cards.FromName("River Crocolisk"));

			game.ProcessCard(glide, asZeroCost: true);

			Assert.Equal(4, game.Player1.HandZone.Count);
			Assert.Equal(2, game.Player1.DeckZone.Count);
			Assert.True(wisp.Zone == game.Player1.HandZone || wisp.Zone == game.Player1.DeckZone);
			Assert.True(murloc.Zone == game.Player1.HandZone || murloc.Zone == game.Player1.DeckZone);
			Assert.Contains(opponentCard, game.Player2.HandZone);
			Assert.Single(game.Player2.HandZone);
			Assert.Single(game.Player2.DeckZone);
		}

		[Fact]
		public void Glide_ShouldAlsoShuffleOpponentHandAndDrawFourWhenOutcast()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DEMONHUNTER);
			EmptyZone(game.Player1.DeckZone.GetAll());
			EmptyZone(game.Player2.DeckZone.GetAll());
			for (int i = 0; i < 4; i++)
			{
				game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Wisp")));
				game.Player2.DeckZone.Add(Entity.FromCard(game.Player2, Cards.FromName("Murloc Raider")));
			}

			IPlayable glide = AddHandCard(game, "Glide");
			IPlayable opponentWisp = Generic.DrawCard(game.Player2, Cards.FromName("Wisp"));
			IPlayable opponentRaptor = Generic.DrawCard(game.Player2, Cards.FromName("Bloodfen Raptor"));

			game.ProcessCard(glide, asZeroCost: true);

			Assert.Equal(4, game.Player1.HandZone.Count);
			Assert.Empty(game.Player1.DeckZone);
			Assert.Equal(4, game.Player2.HandZone.Count);
			Assert.Equal(2, game.Player2.DeckZone.Count);
			Assert.True(opponentWisp.Zone == game.Player2.HandZone || opponentWisp.Zone == game.Player2.DeckZone);
			Assert.True(opponentRaptor.Zone == game.Player2.HandZone || opponentRaptor.Zone == game.Player2.DeckZone);
		}

		[Fact]
		public void VilefiendTrainer_ShouldSummonTwoDemonsOnlyWhenOutcast()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DEMONHUNTER);
			AddHandCard(game, "Wisp");
			IPlayable trainer = AddHandCard(game, "Vilefiend Trainer");
			AddHandCard(game, "Wisp");

			game.ProcessCard<Minion>((Minion)trainer, asZeroCost: true);

			Assert.Single(game.Player1.BoardZone);
			Assert.DoesNotContain(game.Player1.BoardZone, p => p.Card.Id == "SCH_705t");

			game = CreateGame(player1HeroClass: CardClass.DEMONHUNTER);
			trainer = AddHandCard(game, "Vilefiend Trainer");

			game.ProcessCard<Minion>((Minion)trainer, asZeroCost: true);

			Assert.Single(game.Player1.BoardZone, p => p.Card.Id == "SCH_705");
			Minion[] vilefiends = game.Player1.BoardZone.GetAll(p => p.Card.Id == "SCH_705t");
			Assert.Equal(2, vilefiends.Length);
			Assert.All(vilefiends, vilefiend =>
			{
				Assert.Equal(1, vilefiend.AttackDamage);
				Assert.Equal(1, vilefiend.Health);
				Assert.True(vilefiend.Card.IsRace(Race.DEMON));
			});
		}

		[Fact]
		public void Felosophy_ShouldCopyLowestCostDemonAndBuffBothWhenOutcast()
		{
			Game game = CreateGame(player1HeroClass: CardClass.WARLOCK);
			AddHandCard(game, "Voidwalker");
			AddHandCard(game, "Dread Infernal");
			AddHandCard(game, "Wisp");
			IPlayable felosophy = AddHandCard(game, "Felosophy");
			AddHandCard(game, "Wisp");

			game.ProcessCard(felosophy, asZeroCost: true);

			Minion[] unbuffedVoidwalkers = game.Player1.HandZone.OfType<Minion>()
				.Where(p => p.Card.Name == "Voidwalker")
				.ToArray();
			Assert.Equal(2, unbuffedVoidwalkers.Length);
			Assert.All(unbuffedVoidwalkers, voidwalker =>
			{
				Assert.Equal(1, voidwalker.AttackDamage);
				Assert.Equal(3, voidwalker.Health);
			});
			Assert.Single(game.Player1.HandZone.OfType<Minion>().Where(p => p.Card.Name == "Dread Infernal"));

			game = CreateGame(player1HeroClass: CardClass.WARLOCK);
			AddHandCard(game, "Voidwalker");
			AddHandCard(game, "Dread Infernal");
			felosophy = AddHandCard(game, "Felosophy");

			game.ProcessCard(felosophy, asZeroCost: true);

			Minion[] buffedVoidwalkers = game.Player1.HandZone.OfType<Minion>()
				.Where(p => p.Card.Name == "Voidwalker")
				.ToArray();
			Assert.Equal(2, buffedVoidwalkers.Length);
			Assert.All(buffedVoidwalkers, voidwalker =>
			{
				Assert.Equal(2, voidwalker.AttackDamage);
				Assert.Equal(4, voidwalker.Health);
				Assert.Equal(1, voidwalker.Cost);
			});
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
		public void FirstDayOfSchool_ShouldAddTwoRandomOneCostMinions()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PALADIN);

			game.ProcessCard("First Day of School", asZeroCost: true);

			Assert.Equal(2, game.Player1.HandZone.Count);
			Assert.All(game.Player1.HandZone, minion =>
			{
				Assert.Equal(CardType.MINION, minion.Card.Type);
				Assert.Equal(1, minion.Card.Cost);
			});
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
		public void GoodyTwoShields_ShouldRegainDivineShieldOnSpellburstOnlyOnce()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PALADIN);
			Minion goody = game.ProcessCard<Minion>("Goody Two-Shields", asZeroCost: true);

			Assert.True(goody.HasDivineShield);

			game.ProcessCard("Elven Archer", goody, asZeroCost: true);

			Assert.False(goody.HasDivineShield);
			Assert.Equal(0, goody.Damage);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.True(goody.HasDivineShield);

			game.ProcessCard("Elven Archer", goody, asZeroCost: true);

			Assert.False(goody.HasDivineShield);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.False(goody.HasDivineShield);
		}

		[Fact]
		public void CeremonialMaul_ShouldSummonStudentWithStatsEqualToSpellCostOnSpellburst()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PALADIN);
			game.ProcessCard("Ceremonial Maul", asZeroCost: true);

			game.ProcessCard("Fireball", game.Player2.Hero);

			Minion student = Assert.Single(game.Player1.BoardZone.GetAll(p => p.Card.Id == "SCH_523t"));
			Assert.Equal(4, student.AttackDamage);
			Assert.Equal(4, student.Health);
			Assert.True(student.HasTaunt);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Single(game.Player1.BoardZone.GetAll(p => p.Card.Id == "SCH_523t"));
		}

		[Fact]
		public void Commencement_ShouldSummonMinionFromDeckWithTauntAndDivineShield()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PALADIN);
			EmptyZone(game.Player1.DeckZone.GetAll());
			IPlayable deckSpell = Entity.FromCard(game.Player1, Cards.FromName("Moonfire"));
			IPlayable deckMinion = Entity.FromCard(game.Player1, Cards.FromName("River Crocolisk"));
			game.Player1.DeckZone.Add(deckSpell);
			game.Player1.DeckZone.Add(deckMinion);

			game.ProcessCard("Commencement", asZeroCost: true);

			Minion summoned = Assert.Single(game.Player1.BoardZone);
			Assert.Equal(deckMinion, summoned);
			Assert.Equal("River Crocolisk", summoned.Card.Name);
			Assert.True(summoned.HasTaunt);
			Assert.True(summoned.HasDivineShield);
			Assert.DoesNotContain(deckMinion, game.Player1.DeckZone);
			Assert.Contains(deckSpell, game.Player1.DeckZone);
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
		public void Gibberling_ShouldSummonAnotherGibberlingForEachUnusedSpellburst()
		{
			Game game = CreateGame();
			game.ProcessCard<Minion>("Gibberling", asZeroCost: true);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);
			Assert.Equal(2, game.Player1.BoardZone.Count(p => p.Card.Id == "SCH_242"));

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);
			Assert.Equal(3, game.Player1.BoardZone.Count(p => p.Card.Id == "SCH_242"));
		}

		[Fact]
		public void SpeakerGidra_ShouldGainStatsEqualToFirstSpellCost()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DRUID);
			Minion gidra = game.ProcessCard<Minion>("Speaker Gidra", asZeroCost: true);

			game.ProcessCard("Arcane Intellect");

			Assert.Equal(4, gidra.AttackDamage);
			Assert.Equal(7, gidra.Health);

			game.ProcessCard("Silence", gidra);

			Assert.Equal(1, gidra.AttackDamage);
			Assert.Equal(4, gidra.Health);
			Assert.True(gidra.IsSilenced);
		}

		[Fact]
		public void RaiseDead_ShouldDamageHeroAndReturnTwoFriendlyDeadMinions()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PRIEST);
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion raptor = game.ProcessCard<Minion>("Bloodfen Raptor", asZeroCost: true);
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			game.EndTurn();

			wisp.Kill();
			raptor.Kill();
			enemy.Kill();
			EmptyZone(game.Player1.HandZone.GetAll());

			game.ProcessCard("Raise Dead", asZeroCost: true);

			Assert.Equal(27, game.Player1.Hero.Health);
			Assert.Contains(game.Player1.HandZone, p => p.Card.Name == "Wisp");
			Assert.Contains(game.Player1.HandZone, p => p.Card.Name == "Bloodfen Raptor");
			Assert.DoesNotContain(game.Player1.HandZone, p => p.Card.Name == "River Crocolisk");
			Assert.Equal(2, game.Player1.HandZone.Count);
		}

		[Fact]
		public void HighAbbessAlura_ShouldCastDeckSpellTargetingHerselfOnSpellburst()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PRIEST);
			EmptyZone(game.Player1.DeckZone.GetAll());
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Power Word: Feast")));
			Minion alura = game.ProcessCard<Minion>("High Abbess Alura", asZeroCost: true);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(0, game.Player1.DeckZone.Count);
			Assert.Equal(5, alura.AttackDamage);
			Assert.Equal(8, alura.Health);
			Assert.Contains(game.Player1.GraveyardZone, p => p.Card.Name == "Power Word: Feast");
		}

		[Fact]
		public void VoraciousReader_ShouldDrawUntilThreeCardsAtEndOfTurn()
		{
			Game game = CreateGame();
			EmptyZone(game.Player1.DeckZone.GetAll());
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Wisp")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Bloodfen Raptor")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("River Crocolisk")));
			game.ProcessCard<Minion>("Voracious Reader", asZeroCost: true);

			game.EndTurn();

			Assert.Equal(3, game.Player1.HandZone.Count);
			Assert.Equal(0, game.Player1.DeckZone.Count);
			Assert.Contains(game.Player1.HandZone, p => p.Card.Name == "Wisp");
			Assert.Contains(game.Player1.HandZone, p => p.Card.Name == "Bloodfen Raptor");
			Assert.Contains(game.Player1.HandZone, p => p.Card.Name == "River Crocolisk");
		}

		[Fact]
		public void EnchantedCauldron_ShouldCastRandomSpellWithSameCostOnSpellburst()
		{
			Game game = CreateGame();
			game.ProcessCard<Minion>("Enchanted Cauldron", asZeroCost: true);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Spell randomSpell = game.Player1.GraveyardZone
				.OfType<Spell>()
				.Single(p => p.Card.Name != "Moonfire");
			Assert.Equal(0, randomSpell.Card.Cost);
			Assert.Equal(CardType.SPELL, randomSpell.Card.Type);
		}

		[Fact]
		public void TeachersPet_ShouldSummonRandomThreeCostBeastOnDeathrattle()
		{
			Game game = CreateGame();
			Minion pet = game.ProcessCard<Minion>("Teacher's Pet", asZeroCost: true);

			Assert.True(pet.HasTaunt);

			pet.Kill();

			Minion beast = game.Player1.BoardZone.Single();
			Assert.Equal(3, beast.Card.Cost);
			Assert.True(beast.Card.IsRace(Race.BEAST));
		}

		[Fact]
		public void StewardOfScrolls_ShouldDiscoverSpellAndProvideSpellDamage()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE);

			game.ProcessCard<Minion>("Steward of Scrolls", asZeroCost: true);

			Assert.NotNull(game.Player1.Choice);
			Assert.All(game.Player1.Choice.Choices, choice =>
				Assert.Equal(CardType.SPELL, game.IdEntityDic[choice].Card.Type));
			int handCount = game.Player1.HandZone.Count;

			game.Process(ChooseTask.Pick(game.Player1, game.Player1.Choice.Choices[0]));

			Assert.Equal(handCount + 1, game.Player1.HandZone.Count);
			Assert.Equal(CardType.SPELL, game.Player1.HandZone.Last().Card.Type);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(28, game.Player2.Hero.Health);
		}

		[Fact]
		public void Wandmaker_ShouldAddOneCostSpellFromYourClass()
		{
			Game game = CreateGame();

			game.ProcessCard<Minion>("Wandmaker", asZeroCost: true);

			IPlayable spell = game.Player1.HandZone.Single();
			Assert.Equal(CardType.SPELL, spell.Card.Type);
			Assert.Equal(1, spell.Card.Cost);
			Assert.Equal(game.Player1.HeroClass, spell.Card.Class);
		}

		[Fact]
		public void OnyxMagescribe_ShouldAddTwoRandomSpellsFromYourClassOnSpellburstOnlyOnce()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE);
			game.ProcessCard<Minion>("Onyx Magescribe", asZeroCost: true);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(2, game.Player1.HandZone.Count);
			Assert.All(game.Player1.HandZone, spell =>
			{
				Assert.Equal(CardType.SPELL, spell.Card.Type);
				Assert.Equal(CardClass.MAGE, spell.Card.Class);
			});

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(2, game.Player1.HandZone.Count);
		}

		[Fact]
		public void Ogremancer_ShouldSummonTauntSkeletonWhenOpponentCastsSpell()
		{
			Game game = CreateGame();
			game.ProcessCard<Minion>("Ogremancer", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Moonfire", game.Player1.Hero, asZeroCost: true);

			Minion skeleton = game.Player1.BoardZone.Single(p => p.Card.Id == "SCH_710t");
			Assert.Equal(2, skeleton.AttackDamage);
			Assert.Equal(2, skeleton.Health);
			Assert.True(skeleton.HasTaunt);
		}

		[Fact]
		public void CultNeophyte_ShouldIncreaseOpponentSpellCostsOnlyForNextTurn()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE, player2HeroClass: CardClass.DRUID);
			Spell opponentSpell = Assert.IsType<Spell>(Generic.DrawCard(game.Player2, Cards.FromName("Moonfire")));

			game.ProcessCard("Cult Neophyte", asZeroCost: true);

			game.EndTurn();

			Assert.Equal(opponentSpell.Card.Cost + 1, opponentSpell.Cost);

			game.EndTurn();
			game.EndTurn();

			Assert.Equal(opponentSpell.Card.Cost, opponentSpell.Cost);
		}

		[Fact]
		public void EducatedElekk_ShouldRememberPlayedSpellsAndShuffleThemOnDeathrattle()
		{
			Game game = CreateGame(player1HeroClass: CardClass.MAGE, player2HeroClass: CardClass.DRUID);
			EmptyZone(game.Player1.DeckZone.GetAll());
			Minion elekk = game.ProcessCard<Minion>("Educated Elekk", asZeroCost: true);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);
			game.EndTurn();
			game.ProcessCard("Moonfire", game.Player1.Hero, asZeroCost: true);
			game.EndTurn();

			elekk.Kill();

			Assert.Equal(2, game.Player1.DeckZone.Count(p => p.Card.Name == "Moonfire"));
		}

		[Fact]
		public void KeymasterAlabaster_ShouldCopyOpponentDrawnCardToHandWithCostOne()
		{
			Game game = CreateGame();
			EmptyZone(game.Player1.DeckZone.GetAll());
			EmptyZone(game.Player2.DeckZone.GetAll());
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("River Crocolisk")));
			game.Player2.DeckZone.Add(Entity.FromCard(game.Player2, Cards.FromName("Boulderfist Ogre")));
			game.ProcessCard<Minion>("Keymaster Alabaster", asZeroCost: true);

			Generic.Draw(game.Player1);

			Assert.DoesNotContain(game.Player1.HandZone, p => p.Card.Name == "Boulderfist Ogre");

			IPlayable drawn = Generic.Draw(game.Player2);

			Assert.Equal("Boulderfist Ogre", drawn.Card.Name);
			Assert.Contains(drawn, game.Player2.HandZone);

			IPlayable copied = game.Player1.HandZone.Single(p => p.Card.Name == "Boulderfist Ogre");
			Assert.Equal(drawn.Card.Id, copied.Card.Id);
			Assert.Equal(6, copied.Card.Cost);
			Assert.Equal(1, copied.Cost);
		}

		[Fact]
		public void PenFlinger_ShouldDamageTargetAndReturnToHandOnSpellburst()
		{
			Game game = CreateGame();

			Minion penFlinger = game.ProcessCard<Minion>("Pen Flinger", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(29, game.Player2.Hero.Health);
			Assert.Contains(penFlinger, game.Player1.BoardZone);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.DoesNotContain(penFlinger, game.Player1.BoardZone);
			Assert.Contains(game.Player1.HandZone, p => p.Card.Id == "SCH_248");
		}

		[Fact]
		public void DiligentNotetaker_ShouldReturnFirstSpellToHandOnSpellburstOnlyOnce()
		{
			Game game = CreateGame();
			game.ProcessCard<Minion>("Diligent Notetaker", asZeroCost: true);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(1, game.Player1.HandZone.Count(p => p.Card.Name == "Moonfire"));
			Assert.Equal(29, game.Player2.Hero.Health);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Equal(1, game.Player1.HandZone.Count(p => p.Card.Name == "Moonfire"));
			Assert.Equal(28, game.Player2.Hero.Health);
		}

		[Fact]
		public void DemonCompanion_ShouldSummonOneDemonCompanion()
		{
			Game game = CreateGame();
			var companionIds = new HashSet<string> {"SCH_600t1", "SCH_600t2", "SCH_600t3"};

			game.ProcessCard("Demon Companion", asZeroCost: true);

			Minion companion = game.Player1.BoardZone.Single();
			Assert.Contains(companion.Card.Id, companionIds);
			Assert.Equal(Race.DEMON, companion.Card.GetRawRace());

			if (companion.Card.Id == "SCH_600t1")
				Assert.True(companion.HasCharge);
			if (companion.Card.Id == "SCH_600t2")
				Assert.True(companion.HasTaunt);
		}

		[Fact]
		public void Kolek_ShouldGiveOtherFriendlyMinionsOneAttack()
		{
			Game game = CreateGame();
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			Generic.SummonBlock.Invoke(game, (Minion)Entity.FromCard(game.Player1, Cards.FromId("SCH_600t3")), -1, null);

			Minion kolek = game.Player1.BoardZone.Single(p => p.Card.Id == "SCH_600t3");
			Assert.Equal(2, wisp.AttackDamage);
			Assert.Equal(1, kolek.AttackDamage);
		}

		[Fact]
		public void SurvivalOfTheFittest_ShouldBuffMinionsInHandDeckAndBattlefield()
		{
			Game game = CreateGame();
			Minion boardMinion = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			Minion handMinion = Assert.IsType<Minion>(Generic.DrawCard(game.Player1, Cards.FromName("Wisp")));
			Minion deckMinion = Assert.IsType<Minion>(game.Player1.DeckZone.First(p => p.Card.Name == "Wisp"));

			game.ProcessCard("Survival of the Fittest", asZeroCost: true);

			Assert.Equal(6, boardMinion.AttackDamage);
			Assert.Equal(7, boardMinion.Health);
			Assert.Equal(5, handMinion.AttackDamage);
			Assert.Equal(5, handMinion.Health);
			Assert.Equal(5, deckMinion.AttackDamage);
			Assert.Equal(5, deckMinion.Health);
		}

		[Fact]
		public void GiftOfLuminance_ShouldGiveDivineShieldAndSummonOneOneCopy()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);

			game.ProcessCard("Gift of Luminance", target, asZeroCost: true);

			Minion copy = game.Player1.BoardZone.Single(p => p != target && p.Card.Id == target.Card.Id);
			Assert.True(target.HasDivineShield);
			Assert.True(copy.HasDivineShield);
			Assert.Equal(1, copy.AttackDamage);
			Assert.Equal(1, copy.Health);
		}

		[Fact]
		public void LorekeeperPolkelt_ShouldReorderDeckFromHighestCostToLowestCost()
		{
			Game game = CreateGame();
			EmptyZone(game.Player1.DeckZone.GetAll());
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Wisp")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("River Crocolisk")));
			game.Player1.DeckZone.Add(Entity.FromCard(game.Player1, Cards.FromName("Boulderfist Ogre")));

			game.ProcessCard("Lorekeeper Polkelt", asZeroCost: true);

			Assert.Equal("Boulderfist Ogre", game.Player1.DeckZone.TopCard.Card.Name);
			Generic.Draw(game.Player1);
			Assert.Equal("River Crocolisk", game.Player1.DeckZone.TopCard.Card.Name);
			Generic.Draw(game.Player1);
			Assert.Equal("Wisp", game.Player1.DeckZone.TopCard.Card.Name);
		}

		[Fact]
		public void Groundskeeper_ShouldRestoreHeroIfHoldingExpensiveSpell()
		{
			Game game = CreateGame();
			game.Player1.Hero.Damage = 7;
			Generic.DrawCard(game.Player1, Cards.FromId("SCH_609"));

			game.ProcessCard("Groundskeeper", asZeroCost: true);

			Assert.Equal(2, game.Player1.Hero.Damage);
		}

		[Fact]
		public void TotemGoliath_ShouldSummonAllFourBasicTotemsOnDeathrattle()
		{
			Game game = CreateGame();
			Minion goliath = game.ProcessCard<Minion>("Totem Goliath", asZeroCost: true);

			goliath.Kill();

			Assert.Contains(game.Player1.BoardZone, p => p.Card.Id == "CS2_050");
			Assert.Contains(game.Player1.BoardZone, p => p.Card.Id == "CS2_051");
			Assert.Contains(game.Player1.BoardZone, p => p.Card.Id == "CS2_052");
			Assert.Contains(game.Player1.BoardZone, p => p.Card.Id == "NEW1_009");
			Assert.Equal(4, game.Player1.BoardZone.Count);
		}

		[Fact]
		public void PartnerAssignment_ShouldAddRandomTwoAndThreeCostBeasts()
		{
			Game game = CreateGame();

			game.ProcessCard("Partner Assignment", asZeroCost: true);

			Assert.Equal(2, game.Player1.HandZone.Count);
			Assert.Contains(game.Player1.HandZone, p => p.Card.Cost == 2 && p.Card.IsRace(Race.BEAST));
			Assert.Contains(game.Player1.HandZone, p => p.Card.Cost == 3 && p.Card.IsRace(Race.BEAST));
		}

		[Fact]
		public void InFormation_ShouldAddTwoRandomTauntMinions()
		{
			Game game = CreateGame();

			game.ProcessCard("In Formation!", asZeroCost: true);

			Assert.Equal(2, game.Player1.HandZone.Count);
			Assert.All(game.Player1.HandZone, p => Assert.Equal(1, p.Card[GameTag.TAUNT]));
		}

		[Fact]
		public void Coerce_ShouldDestroyOnlyDamagedMinionWithoutCombo()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion damaged = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			Minion undamaged = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			game.EndTurn();
			damaged.Damage = 1;

			game.ProcessCard("Coerce", damaged, asZeroCost: true);

			Assert.True(damaged.ToBeDestroyed);
			Assert.False(undamaged.ToBeDestroyed);
		}

		[Fact]
		public void Coerce_ShouldDestroyAnyMinionWithCombo()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();
			game.ProcessCard("Wisp", asZeroCost: true);

			game.ProcessCard("Coerce", target, asZeroCost: true);

			Assert.True(target.ToBeDestroyed);
		}

		[Fact]
		public void ShiftySophomore_ShouldAddComboCardToHandOnSpellburstOnlyOnce()
		{
			Game game = CreateGame(player1HeroClass: CardClass.ROGUE);
			Minion sophomore = game.ProcessCard<Minion>("Shifty Sophomore", asZeroCost: true);

			Assert.True(sophomore.HasStealth);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Single(game.Player1.HandZone);
			Assert.True(game.Player1.HandZone[0].Card.Combo);

			game.ProcessCard("Moonfire", game.Player2.Hero, asZeroCost: true);

			Assert.Single(game.Player1.HandZone);
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
		public void CycleOfHatred_ShouldDamageAllMinionsAndSummonSpiritForEachKilled()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DEMONHUNTER);
			Minion friendlyKilled = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			game.EndTurn();
			Minion enemyKilled = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion enemySurvives = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Cycle of Hatred", asZeroCost: true);

			Assert.DoesNotContain(friendlyKilled, game.Player1.BoardZone);
			Assert.DoesNotContain(enemyKilled, game.Player2.BoardZone);
			Assert.Contains(enemySurvives, game.Player2.BoardZone);
			Assert.Equal(3, enemySurvives.Damage);

			Minion[] spirits = game.Player1.BoardZone.GetAll(p => p.Card.Id == "SCH_253t");
			Assert.Equal(2, spirits.Length);
			Assert.All(spirits, spirit =>
			{
				Assert.Equal(3, spirit.AttackDamage);
				Assert.Equal(3, spirit.Health);
			});
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
		public void DemonicStudies_ShouldDiscoverDemonAndDiscountNextDemon()
		{
			Game game = CreateGame(player1HeroClass: CardClass.WARLOCK);
			IPlayable firstDemon = Generic.DrawCard(game.Player1, Cards.FromName("Flame Imp"));
			IPlayable secondDemon = Generic.DrawCard(game.Player1, Cards.FromName("Voidwalker"));
			IPlayable nonDemon = Generic.DrawCard(game.Player1, Cards.FromName("River Crocolisk"));

			game.ProcessCard("Demonic Studies", asZeroCost: true);

			Assert.NotNull(game.Player1.Choice);
			Assert.All(game.Player1.Choice.Choices, choice =>
				Assert.True(game.IdEntityDic[choice].Card.IsRace(Race.DEMON)));
			Assert.Equal(0, firstDemon.Cost);
			Assert.Equal(0, secondDemon.Cost);
			Assert.Equal(2, nonDemon.Cost);
			int handCount = game.Player1.HandZone.Count;

			game.Process(ChooseTask.Pick(game.Player1, game.Player1.Choice.Choices[0]));

			Assert.Equal(handCount + 1, game.Player1.HandZone.Count);
			Assert.True(game.Player1.HandZone.Last().Card.IsRace(Race.DEMON));

			game.ProcessCard(firstDemon, asZeroCost: false);

			Assert.DoesNotContain(firstDemon, game.Player1.HandZone);
			Assert.Equal(1, secondDemon.Cost);
		}

		[Fact]
		public void DraconicStudies_ShouldDiscoverDragonAndDiscountNextDragon()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PRIEST);
			IPlayable firstDragon = Generic.DrawCard(game.Player1, Cards.FromName("Faerie Dragon"));
			IPlayable secondDragon = Generic.DrawCard(game.Player1, Cards.FromName("Twilight Drake"));
			IPlayable nonDragon = Generic.DrawCard(game.Player1, Cards.FromName("River Crocolisk"));

			game.ProcessCard("Draconic Studies", asZeroCost: true);

			Assert.NotNull(game.Player1.Choice);
			Assert.All(game.Player1.Choice.Choices, choice =>
				Assert.True(game.IdEntityDic[choice].Card.IsRace(Race.DRAGON)));
			Assert.Equal(1, firstDragon.Cost);
			Assert.Equal(3, secondDragon.Cost);
			Assert.Equal(2, nonDragon.Cost);
			int handCount = game.Player1.HandZone.Count;

			game.Process(ChooseTask.Pick(game.Player1, game.Player1.Choice.Choices[0]));

			Assert.Equal(handCount + 1, game.Player1.HandZone.Count);
			Assert.True(game.Player1.HandZone.Last().Card.IsRace(Race.DRAGON));

			game.ProcessCard(firstDragon, asZeroCost: false);

			Assert.DoesNotContain(firstDragon, game.Player1.HandZone);
			Assert.Equal(4, secondDragon.Cost);
		}

		[Fact]
		public void AthleticStudies_ShouldDiscoverRushMinionAndDiscountNextRushMinion()
		{
			Game game = CreateGame(player1HeroClass: CardClass.WARRIOR);
			IPlayable firstRush = Generic.DrawCard(game.Player1, Cards.FromName("Rabid Worgen"));
			IPlayable secondRush = Generic.DrawCard(game.Player1, Cards.FromName("Militia Commander"));
			IPlayable nonRush = Generic.DrawCard(game.Player1, Cards.FromName("River Crocolisk"));

			game.ProcessCard("Athletic Studies", asZeroCost: true);

			Assert.NotNull(game.Player1.Choice);
			Assert.All(game.Player1.Choice.Choices, choice =>
				Assert.True(game.IdEntityDic[choice].Card[GameTag.RUSH] >= 1));
			Assert.Equal(2, firstRush.Cost);
			Assert.Equal(3, secondRush.Cost);
			Assert.Equal(2, nonRush.Cost);
			int handCount = game.Player1.HandZone.Count;

			game.Process(ChooseTask.Pick(game.Player1, game.Player1.Choice.Choices[0]));

			Assert.Equal(handCount + 1, game.Player1.HandZone.Count);
			Assert.True(game.Player1.HandZone.Last().Card[GameTag.RUSH] >= 1);

			game.ProcessCard(firstRush, asZeroCost: false);

			Assert.DoesNotContain(firstRush, game.Player1.HandZone);
			Assert.Equal(4, secondRush.Cost);
		}

		[Fact]
		public void Troublemaker_ShouldSummonTwoRuffiansThatAttackRandomEnemiesAtEndOfTurn()
		{
			Game game = CreateGame(player1HeroClass: CardClass.WARRIOR);
			game.ProcessCard<Minion>("Troublemaker", asZeroCost: true);

			game.EndTurn();

			Minion[] ruffians = game.Player1.BoardZone.GetAll(p => p.Card.Id == "SCH_337t");
			Assert.Equal(2, ruffians.Length);
			Assert.All(ruffians, ruffian =>
			{
				Assert.Equal(3, ruffian.AttackDamage);
				Assert.Equal(3, ruffian.Health);
			});
			Assert.Equal(24, game.Player2.Hero.Health);
		}

		[Fact]
		public void PrimordialStudies_ShouldDiscoverSpellDamageMinionAndDiscountNextSpellDamageMinion()
		{
			Game game = CreateGame(player1HeroClass: CardClass.SHAMAN);
			IPlayable firstSpellDamage = Generic.DrawCard(game.Player1, Cards.FromName("Kobold Geomancer"));
			IPlayable secondSpellDamage = Generic.DrawCard(game.Player1, Cards.FromName("Dalaran Mage"));
			IPlayable nonSpellDamage = Generic.DrawCard(game.Player1, Cards.FromName("River Crocolisk"));

			game.ProcessCard("Primordial Studies", asZeroCost: true);

			Assert.NotNull(game.Player1.Choice);
			Assert.All(game.Player1.Choice.Choices, choice =>
				Assert.True(game.IdEntityDic[choice].Card[GameTag.SPELLPOWER] >= 1));
			Assert.Equal(1, firstSpellDamage.Cost);
			Assert.Equal(2, secondSpellDamage.Cost);
			Assert.Equal(2, nonSpellDamage.Cost);
			int handCount = game.Player1.HandZone.Count;

			game.Process(ChooseTask.Pick(game.Player1, game.Player1.Choice.Choices[0]));

			Assert.Equal(handCount + 1, game.Player1.HandZone.Count);
			Assert.True(game.Player1.HandZone.Last().Card[GameTag.SPELLPOWER] >= 1);

			game.ProcessCard(firstSpellDamage, asZeroCost: false);

			Assert.DoesNotContain(firstSpellDamage, game.Player1.HandZone);
			Assert.Equal(3, secondSpellDamage.Cost);
		}

		[Fact]
		public void CarrionStudies_ShouldDiscoverDeathrattleMinionAndDiscountNextDeathrattleMinion()
		{
			Game game = CreateGame(player1HeroClass: CardClass.ROGUE);
			IPlayable firstDeathrattle = Generic.DrawCard(game.Player1, Cards.FromName("Loot Hoarder"));
			IPlayable secondDeathrattle = Generic.DrawCard(game.Player1, Cards.FromName("Harvest Golem"));
			IPlayable nonDeathrattle = Generic.DrawCard(game.Player1, Cards.FromName("River Crocolisk"));

			game.ProcessCard("Carrion Studies", asZeroCost: true);

			Assert.NotNull(game.Player1.Choice);
			Assert.All(game.Player1.Choice.Choices, choice =>
				Assert.True(game.IdEntityDic[choice].Card.Deathrattle));
			Assert.Equal(1, firstDeathrattle.Cost);
			Assert.Equal(2, secondDeathrattle.Cost);
			Assert.Equal(2, nonDeathrattle.Cost);
			int handCount = game.Player1.HandZone.Count;

			game.Process(ChooseTask.Pick(game.Player1, game.Player1.Choice.Choices[0]));

			Assert.Equal(handCount + 1, game.Player1.HandZone.Count);
			Assert.True(game.Player1.HandZone.Last().Card.Deathrattle);

			game.ProcessCard(firstDeathrattle, asZeroCost: false);

			Assert.DoesNotContain(firstDeathrattle, game.Player1.HandZone);
			Assert.Equal(3, secondDeathrattle.Cost);
		}

		[Fact]
		public void NatureStudies_ShouldDiscoverSpellAndDiscountNextSpell()
		{
			Game game = CreateGame(player1HeroClass: CardClass.DRUID);
			IPlayable firstSpell = Generic.DrawCard(game.Player1, Cards.FromName("Swipe"));
			IPlayable secondSpell = Generic.DrawCard(game.Player1, Cards.FromName("Starfire"));
			IPlayable nonSpell = Generic.DrawCard(game.Player1, Cards.FromName("River Crocolisk"));

			game.ProcessCard("Nature Studies", asZeroCost: true);

			Assert.NotNull(game.Player1.Choice);
			Assert.All(game.Player1.Choice.Choices, choice =>
				Assert.Equal(CardType.SPELL, game.IdEntityDic[choice].Card.Type));
			Assert.Equal(3, firstSpell.Cost);
			Assert.Equal(5, secondSpell.Cost);
			Assert.Equal(2, nonSpell.Cost);
			int handCount = game.Player1.HandZone.Count;

			game.Process(ChooseTask.Pick(game.Player1, game.Player1.Choice.Choices[0]));

			Assert.Equal(handCount + 1, game.Player1.HandZone.Count);
			Assert.Equal(CardType.SPELL, game.Player1.HandZone.Last().Card.Type);

			game.ProcessCard(firstSpell, game.Player2.Hero, asZeroCost: false);

			Assert.DoesNotContain(firstSpell, game.Player1.HandZone);
			Assert.Equal(6, secondSpell.Cost);
		}

		[Fact]
		public void MindrenderIllucia_ShouldSwapHandsAndDecksUntilNextTurn()
		{
			Game game = CreateGame(player1HeroClass: CardClass.PRIEST, player2HeroClass: CardClass.MAGE);
			EmptyZone(game.Player1.DeckZone.GetAll());
			EmptyZone(game.Player2.DeckZone.GetAll());
			IPlayable playerHand = Generic.DrawCard(game.Player1, Cards.FromName("Moonfire"));
			IPlayable opponentHand = Generic.DrawCard(game.Player2, Cards.FromName("Fireball"));
			IPlayable playerDeck = Entity.FromCard(game.Player1, Cards.FromName("Wisp"));
			IPlayable opponentDeck = Entity.FromCard(game.Player2, Cards.FromName("River Crocolisk"));
			game.Player1.DeckZone.Add(playerDeck);
			game.Player2.DeckZone.Add(opponentDeck);

			game.ProcessCard("Mindrender Illucia", asZeroCost: true);

			Assert.Contains(opponentHand, game.Player1.HandZone);
			Assert.Contains(playerHand, game.Player2.HandZone);
			Assert.Contains(opponentDeck, game.Player1.DeckZone);
			Assert.Contains(playerDeck, game.Player2.DeckZone);
			Assert.Equal(game.Player1, opponentHand.Controller);
			Assert.Equal(game.Player2, playerHand.Controller);
			Assert.Equal(game.Player1, opponentDeck.Controller);
			Assert.Equal(game.Player2, playerDeck.Controller);

			game.EndTurn();
			Assert.Contains(playerHand, game.Player2.HandZone);
			Assert.Contains(playerDeck, game.Player2.HandZone);

			game.EndTurn();

			Assert.Contains(playerHand, game.Player1.HandZone);
			Assert.Contains(playerDeck, game.Player1.HandZone);
			Assert.Contains(opponentHand, game.Player2.HandZone);
			Assert.Contains(opponentDeck, game.Player2.DeckZone);
			Assert.Equal(game.Player1, playerHand.Controller);
			Assert.Equal(game.Player1, playerDeck.Controller);
			Assert.Equal(game.Player2, opponentHand.Controller);
			Assert.Equal(game.Player2, opponentDeck.Controller);
		}

		[Fact]
		public void ArchwitchWillow_ShouldSummonDemonFromHandAndDeck()
		{
			Game game = CreateGame(player1HeroClass: CardClass.WARLOCK);
			IPlayable handDemon = Generic.DrawCard(game.Player1, Cards.FromName("Flame Imp"));
			IPlayable deckDemon = Entity.FromCard(game.Player1, Cards.FromName("Voidwalker"));
			game.Player1.DeckZone.Add(deckDemon);
			int wispsBefore = game.Player1.DeckZone.Count(p => p.Card.Name == "Wisp");

			game.ProcessCard("Archwitch Willow", asZeroCost: true);

			Assert.DoesNotContain(handDemon, game.Player1.HandZone);
			Assert.DoesNotContain(deckDemon, game.Player1.DeckZone);
			Assert.Contains(game.Player1.BoardZone, p => p.Card.Name == "Archwitch Willow");
			Assert.Contains(game.Player1.BoardZone, p => p.Card.Name == "Flame Imp");
			Assert.Contains(game.Player1.BoardZone, p => p.Card.Name == "Voidwalker");
			Assert.Equal(wispsBefore, game.Player1.DeckZone.Count(p => p.Card.Name == "Wisp"));
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

		[Fact]
		public void SphereOfSapience_ShouldKeepTopCardWithoutLosingDurability()
		{
			Game game = CreateGame();
			EmptyZone(game.Player1.DeckZone.GetAll());
			IPlayable bottom = Entity.FromCard(game.Player1, Cards.FromName("Wisp"));
			IPlayable top = Entity.FromCard(game.Player1, Cards.FromName("Boulderfist Ogre"));
			game.Player1.DeckZone.Add(bottom);
			game.Player1.DeckZone.Add(top);
			game.ProcessCard("Sphere of Sapience", asZeroCost: true);

			game.EndTurn();
			game.EndTurn();

			Assert.NotNull(game.Player1.Choice);
			Assert.Contains(top.Id, game.Player1.Choice.Choices);

			game.Process(ChooseTask.Pick(game.Player1, top.Id));

			Assert.Equal(4, game.Player1.Hero.Weapon.Durability);
			Assert.Contains(top, game.Player1.HandZone);
			Assert.Equal(bottom, game.Player1.DeckZone.TopCard);
		}

		[Fact]
		public void SphereOfSapience_ShouldPutTopCardOnBottomAndLoseDurability()
		{
			Game game = CreateGame();
			EmptyZone(game.Player1.DeckZone.GetAll());
			IPlayable bottom = Entity.FromCard(game.Player1, Cards.FromName("Wisp"));
			IPlayable top = Entity.FromCard(game.Player1, Cards.FromName("Boulderfist Ogre"));
			game.Player1.DeckZone.Add(bottom);
			game.Player1.DeckZone.Add(top);
			game.ProcessCard("Sphere of Sapience", asZeroCost: true);

			game.EndTurn();
			game.EndTurn();

			Assert.NotNull(game.Player1.Choice);
			int newFateChoice = game.Player1.Choice.Choices.Single(choice => game.IdEntityDic[choice].Card.Id == "SCH_259t");

			game.Process(ChooseTask.Pick(game.Player1, newFateChoice));

			Assert.Equal(3, game.Player1.Hero.Weapon.Durability);
			Assert.Contains(bottom, game.Player1.HandZone);
			Assert.Equal(top, game.Player1.DeckZone.TopCard);
		}
	}
}
