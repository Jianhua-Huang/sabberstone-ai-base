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
	public class OutlandDemonHunterCardsGenTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null)
		{
			var deck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.DEMONHUNTER,
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
		public void ChaosStrike_BT_035_ShouldBuffHeroAndDraw()
		{
			Game game = CreateGame();
			int handCount = game.CurrentPlayer.HandZone.Count;

			game.ProcessCard("Chaos Strike", asZeroCost: true);

			Assert.Equal(2, game.CurrentPlayer.Hero.AttackDamage);
			Assert.Equal(handCount + 1, game.CurrentPlayer.HandZone.Count);
			game.EndTurn();
			Assert.Equal(0, game.CurrentOpponent.Hero.AttackDamage);
		}

		[Theory]
		[InlineData("Coordinated Strike", 3)]
		[InlineData("Command the Illidari", 6)]
		public void IllidariSummonSpells_ShouldSummonRushInitiates(string cardName, int amount)
		{
			Game game = CreateGame();

			game.ProcessCard(cardName, asZeroCost: true);

			Assert.Equal(amount, game.CurrentPlayer.BoardZone.Count);
			Assert.All(game.CurrentPlayer.BoardZone, minion =>
			{
				Assert.Equal("Illidari Initiate", minion.Card.Name);
				Assert.Equal(1, minion.AttackDamage);
				Assert.Equal(1, minion.Health);
				Assert.Equal(1, minion[GameTag.RUSH]);
			});
		}

		[Fact]
		public void ShadowhoofSlayer_BT_142_ShouldGiveHeroOneAttack()
		{
			Game game = CreateGame();

			game.ProcessCard("Shadowhoof Slayer", asZeroCost: true);

			Assert.Equal(1, game.CurrentPlayer.Hero.AttackDamage);
			game.EndTurn();
			Assert.Equal(0, game.CurrentOpponent.Hero.AttackDamage);
		}

		[Fact]
		public void TwinSlice_BT_175_ShouldBuffHeroAndAddSecondSlice()
		{
			Game game = CreateGame();

			game.ProcessCard("Twin Slice", asZeroCost: true);

			Assert.Equal(1, game.CurrentPlayer.Hero.AttackDamage);
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Id == "BT_175t");

			IPlayable secondSlice = game.CurrentPlayer.HandZone.First(p => p.Card.Id == "BT_175t");
			game.ProcessCard(secondSlice, asZeroCost: true);
			Assert.Equal(2, game.CurrentPlayer.Hero.AttackDamage);
		}

		[Fact]
		public void ChaosNova_BT_235_ShouldDamageAllMinions()
		{
			Game game = CreateGame();
			Minion friendly = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Chaos Nova", asZeroCost: true);

			Assert.Equal(4, friendly.Damage);
			Assert.Equal(4, enemy.Damage);
		}

		[Fact]
		public void Netherwalker_BT_321_ShouldDiscoverDemon()
		{
			Game game = CreateGame();

			game.ProcessCard("Netherwalker", asZeroCost: true);

			Assert.NotNull(game.CurrentPlayer.Choice);
			Assert.All(game.CurrentPlayer.Choice.Choices, choice =>
				Assert.True(game.IdEntityDic[choice].Card.IsRace(Race.DEMON)));
			int handCount = game.CurrentPlayer.HandZone.Count;
			game.Process(ChooseTask.Pick(game.CurrentPlayer, game.CurrentPlayer.Choice.Choices[0]));
			Assert.Equal(handCount + 1, game.CurrentPlayer.HandZone.Count);
			Assert.True(game.CurrentPlayer.HandZone.Last().Card.IsRace(Race.DEMON));
		}

		[Fact]
		public void RagingFelscreamer_BT_416_ShouldReduceNextDemonByTwo()
		{
			Game game = CreateGame();
			IPlayable demon = AddHandCard(game, "Ur'zul Horror");

			game.ProcessCard("Raging Felscreamer", asZeroCost: true);

			Assert.Equal(0, demon.Cost);
			game.ProcessCard(demon, asZeroCost: false);
			IPlayable secondDemon = AddHandCard(game, "Ur'zul Horror");
			Assert.Equal(1, secondDemon.Cost);
		}

		[Fact]
		public void SatyrOverseer_BT_352_ShouldSummonSatyrAfterHeroAttack()
		{
			Game game = CreateGame();
			game.ProcessCard("Satyr Overseer", asZeroCost: true);
			game.ProcessCard("Umberwing", asZeroCost: true);
			int boardCount = game.CurrentPlayer.BoardZone.Count;

			game.Process(HeroAttackTask.Any(game.CurrentPlayer, game.CurrentOpponent.Hero));

			Assert.Equal(boardCount + 1, game.CurrentPlayer.BoardZone.Count);
			Assert.Contains(game.CurrentPlayer.BoardZone, p => p.Card.Id == "BT_352t");
		}

		[Fact]
		public void Battlefiend_BT_351_ShouldGainAttackAfterHeroAttack()
		{
			Game game = CreateGame();
			Minion battlefiend = game.ProcessCard<Minion>("Battlefiend", asZeroCost: true);
			game.ProcessCard("Umberwing", asZeroCost: true);

			game.Process(HeroAttackTask.Any(game.CurrentPlayer, game.CurrentOpponent.Hero));

			Assert.Equal(3, battlefiend.AttackDamage);
		}

		[Fact]
		public void BladeDance_BT_354_ShouldDamageThreeEnemyMinionsForHeroAttack()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion enemy1 = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			Minion enemy2 = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			Minion enemy3 = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();
			game.ProcessCard("Inner Demon", asZeroCost: true);

			game.ProcessCard("Blade Dance", asZeroCost: true);

			Assert.Equal(8, enemy1.Damage);
			Assert.Equal(8, enemy2.Damage);
			Assert.Equal(8, enemy3.Damage);
		}

		[Fact]
		public void WrathscaleNaga_BT_355_ShouldDamageRandomEnemyAfterFriendlyMinionDies()
		{
			Game game = CreateGame();
			game.ProcessCard("Wrathscale Naga", asZeroCost: true);
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard("Moonfire", wisp, asZeroCost: true);

			Assert.Equal(3, game.CurrentOpponent.Hero.Damage);
		}

		[Fact]
		public void UrzulHorror_BT_407_ShouldAddLostSoulOnDeathrattle()
		{
			Game game = CreateGame();
			Minion horror = game.ProcessCard<Minion>("Ur'zul Horror", asZeroCost: true);

			game.ProcessCard("Moonfire", horror, asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Id == "BT_407t");
		}

		[Fact]
		public void KaynSunfury_BT_187_ShouldLetFriendlyAttacksIgnoreTaunt()
		{
			Game game = CreateGame();
			game.EndTurn();
			game.ProcessCard("Goldshire Footman", asZeroCost: true);
			game.EndTurn();

			Minion kayn = game.ProcessCard<Minion>("Kayn Sunfury", asZeroCost: true);
			game.Process(MinionAttackTask.Any(game.CurrentPlayer, kayn, game.CurrentOpponent.Hero));

			Assert.Equal(3, game.CurrentOpponent.Hero.Damage);
		}

		[Fact]
		public void Flamereaper_BT_271_ShouldDamageAdjacentMinions()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion left = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			Minion middle = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			Minion right = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Flamereaper", asZeroCost: true);
			game.Process(HeroAttackTask.Any(game.CurrentPlayer, middle));

			Assert.Equal(4, left.Damage);
			Assert.Equal(4, middle.Damage);
			Assert.Equal(4, right.Damage);
		}

		[Fact]
		public void AshtongueBattlelord_BT_423_ShouldHaveTauntAndLifesteal()
		{
			Game game = CreateGame();
			game.CurrentPlayer.Hero.Damage = 5;
			Minion battlelord = game.ProcessCard<Minion>("Ashtongue Battlelord", asZeroCost: true);
			battlelord.IsExhausted = false;

			game.Process(MinionAttackTask.Any(game.CurrentPlayer, battlelord, game.CurrentOpponent.Hero));

			Assert.Equal(1, battlelord[GameTag.TAUNT]);
			Assert.Equal(1, battlelord[GameTag.LIFESTEAL]);
			Assert.Equal(2, game.CurrentPlayer.Hero.Damage);
		}

		[Fact]
		public void WarglaivesOfAzzinoth_BT_430_ShouldReadyHeroAfterAttackingMinion()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Warglaives of Azzinoth", asZeroCost: true);
			game.Process(HeroAttackTask.Any(game.CurrentPlayer, target));

			Assert.Equal(3, target.Damage);
			Assert.False(game.CurrentPlayer.Hero.IsExhausted);
		}

		[Fact]
		public void GlaiveboundAdept_BT_495_ShouldDealFourIfHeroAttacked()
		{
			Game game = CreateGame();
			game.ProcessCard("Umberwing", asZeroCost: true);
			game.Process(HeroAttackTask.Any(game.CurrentPlayer, game.CurrentOpponent.Hero));
			int damage = game.CurrentOpponent.Hero.Damage;

			game.ProcessCard("Glaivebound Adept", game.CurrentOpponent.Hero, asZeroCost: true);

			Assert.Equal(damage + 4, game.CurrentOpponent.Hero.Damage);
		}

		[Fact]
		public void InnerDemon_BT_512_ShouldGiveEightAttackThisTurn()
		{
			Game game = CreateGame();

			game.ProcessCard("Inner Demon", asZeroCost: true);

			Assert.Equal(8, game.CurrentPlayer.Hero.AttackDamage);
			game.EndTurn();
			Assert.Equal(0, game.CurrentOpponent.Hero.AttackDamage);
		}

		[Fact]
		public void FeastOfSouls_BT_427_ShouldDrawForFriendlyMinionsDiedThisTurn()
		{
			Game game = CreateGame();
			Minion wisp1 = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion wisp2 = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			game.ProcessCard("Moonfire", wisp1, asZeroCost: true);
			game.ProcessCard("Moonfire", wisp2, asZeroCost: true);
			int handCount = game.CurrentPlayer.HandZone.Count;

			game.ProcessCard("Feast of Souls", asZeroCost: true);

			Assert.Equal(handCount + 2, game.CurrentPlayer.HandZone.Count);
		}

		[Fact]
		public void CrimsonSigilRunner_BT_480_ShouldDrawOnlyWhenPlayedFromOutcastPosition()
		{
			Game game = CreateGame();
			SetDeck(game, "Chillwind Yeti");
			AddHandCard(game, "Wisp");
			IPlayable runner = AddHandCard(game, "Crimson Sigil Runner");
			AddHandCard(game, "Wisp");

			game.ProcessCard(runner, asZeroCost: true);

			Assert.DoesNotContain(game.CurrentPlayer.HandZone, p => p.Card.Name == "Chillwind Yeti");

			game = CreateGame();
			SetDeck(game, "Chillwind Yeti");
			runner = AddHandCard(game, "Crimson Sigil Runner");

			game.ProcessCard(runner, asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Chillwind Yeti");
		}

		[Fact]
		public void SoulSplit_BT_488_ShouldCopyFriendlyDemon()
		{
			Game game = CreateGame();
			Minion demon = game.ProcessCard<Minion>("Shadowhoof Slayer", asZeroCost: true);

			game.ProcessCard("Soul Split", demon, asZeroCost: true);

			Assert.Equal(2, game.CurrentPlayer.BoardZone.Count(p => p.Card.Name == "Shadowhoof Slayer"));
		}

		[Fact]
		public void PriestessOfFury_BT_493_ShouldDealSixRandomlySplitAtEndOfTurn()
		{
			Game game = CreateGame();
			game.ProcessCard("Priestess of Fury", asZeroCost: true);

			game.EndTurn();

			Assert.Equal(6, game.CurrentPlayer.Hero.Damage);
		}

		[Fact]
		public void FuriousFelfin_BT_496_ShouldGainAttackAndRushIfHeroAttacked()
		{
			Game game = CreateGame();
			game.ProcessCard("Umberwing", asZeroCost: true);
			game.Process(HeroAttackTask.Any(game.CurrentPlayer, game.CurrentOpponent.Hero));

			Minion felfin = game.ProcessCard<Minion>("Furious Felfin", asZeroCost: true);

			Assert.Equal(4, felfin.AttackDamage);
			Assert.Equal(1, felfin[GameTag.RUSH]);
			Assert.False(felfin.IsExhausted);
		}

		[Fact]
		public void PitCommander_BT_486_ShouldSummonDemonFromDeckAtEndOfTurn()
		{
			Game game = CreateGame(playerDeck: Enumerable.Repeat(Cards.FromName("Shadowhoof Slayer"), 30));
			game.ProcessCard("Pit Commander", asZeroCost: true);
			int deckCount = game.CurrentPlayer.DeckZone.Count;

			game.EndTurn();

			Assert.Equal(deckCount - 1, game.CurrentOpponent.DeckZone.Count);
			Assert.Contains(game.CurrentOpponent.BoardZone, p => p.Card.Name == "Shadowhoof Slayer");
		}

		[Fact]
		public void FelSummoner_BT_509_ShouldSummonRandomDemonFromHandOnDeathrattle()
		{
			Game game = CreateGame();
			Minion summoner = game.ProcessCard<Minion>("Fel Summoner", asZeroCost: true);
			Generic.DrawCard(game.CurrentPlayer, Cards.FromName("Shadowhoof Slayer"));

			game.ProcessCard("Fireball", summoner, asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.BoardZone, p => p.Card.Name == "Shadowhoof Slayer");
			Assert.DoesNotContain(game.CurrentPlayer.HandZone, p => p.Card.Name == "Shadowhoof Slayer");
		}

		[Fact]
		public void HulkingOverfiend_BT_487_ShouldReadyAfterKillingMinion()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion first = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion second = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			game.EndTurn();
			Minion overfiend = game.ProcessCard<Minion>("Hulking Overfiend", asZeroCost: true);

			game.Process(MinionAttackTask.Any(game.CurrentPlayer, overfiend, first));

			Assert.True(first.ToBeDestroyed);
			Assert.False(overfiend.IsExhausted);
			game.Process(MinionAttackTask.Any(game.CurrentPlayer, overfiend, second));
			Assert.True(second.ToBeDestroyed);
		}

		[Fact]
		public void WrathspikeBrute_BT_510_ShouldDamageEnemiesAfterBeingAttacked()
		{
			Game game = CreateGame();
			game.ProcessCard("Wrathspike Brute", asZeroCost: true);
			game.EndTurn();
			Minion attacker = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			attacker.IsExhausted = false;

			game.Process(MinionAttackTask.Any(game.CurrentPlayer, attacker, game.CurrentOpponent.BoardZone[0]));

			Assert.Equal(1, game.CurrentPlayer.Hero.Damage);
		}

		[Fact]
		public void ImmolationAura_BT_514_ShouldDealOneDamageToAllMinionsTwice()
		{
			Game game = CreateGame();
			Minion friendly = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Immolation Aura", asZeroCost: true);

			Assert.Equal(2, friendly.Damage);
			Assert.Equal(2, enemy.Damage);
		}

		[Fact]
		public void CoilfangWarlord_BT_761_ShouldSummonTauntWarlordOnDeathrattle()
		{
			Game game = CreateGame();
			Minion warlord = game.ProcessCard<Minion>("Coilfang Warlord", asZeroCost: true);

			game.ProcessCard("Fireball", warlord, asZeroCost: true);

			Minion token = Assert.Single(game.CurrentPlayer.BoardZone.Where(p => p.Card.Id == "BT_761t"));
			Assert.Equal(5, token.AttackDamage);
			Assert.Equal(9, token.Health);
			Assert.Equal(1, token[GameTag.TAUNT]);
		}

		[Fact]
		public void ConsumeMagic_BT_490_ShouldSilenceEnemyMinionAndDrawWhenOutcast()
		{
			Game game = CreateGame();
			SetDeck(game, "Chillwind Yeti");
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Goldshire Footman", asZeroCost: true);
			Assert.Equal(1, target[GameTag.TAUNT]);
			game.EndTurn();
			IPlayable consumeMagic = AddHandCard(game, "Consume Magic");

			game.ProcessCard(consumeMagic, target, asZeroCost: true);

			Assert.Equal(0, target[GameTag.TAUNT]);
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Chillwind Yeti");
		}

		[Fact]
		public void SpectralSight_BT_491_ShouldDrawOneCardOrTwoWhenOutcast()
		{
			Game game = CreateGame();
			SetDeck(game, "Wisp", "Chillwind Yeti");
			AddHandCard(game, "Wisp");
			IPlayable spectralSight = AddHandCard(game, "Spectral Sight");
			AddHandCard(game, "Wisp");

			game.ProcessCard(spectralSight, asZeroCost: true);

			Assert.Single(game.CurrentPlayer.HandZone, p => p.Card.Name == "Chillwind Yeti");
			Assert.Equal(1, game.CurrentPlayer.DeckZone.Count);

			game = CreateGame();
			SetDeck(game, "Wisp", "Chillwind Yeti");
			spectralSight = AddHandCard(game, "Spectral Sight");

			game.ProcessCard(spectralSight, asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Chillwind Yeti");
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Wisp");
			Assert.Equal(0, game.CurrentPlayer.DeckZone.Count);
		}

		[Fact]
		public void SkullOfGuldan_BT_601_ShouldReduceDrawnCardsOnlyWhenOutcast()
		{
			Game game = CreateGame();
			SetDeck(game, "Boulderfist Ogre", "Chillwind Yeti", "River Crocolisk");
			IPlayable skull = AddHandCard(game, "Skull of Gul'dan");

			game.ProcessCard(skull, asZeroCost: true);

			Assert.Equal(0, game.CurrentPlayer.DeckZone.Count);
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "River Crocolisk" && p.Cost == 0);
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Chillwind Yeti" && p.Cost == 1);
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Boulderfist Ogre" && p.Cost == 3);

			game = CreateGame();
			SetDeck(game, "Boulderfist Ogre", "Chillwind Yeti", "River Crocolisk");
			AddHandCard(game, "Wisp");
			skull = AddHandCard(game, "Skull of Gul'dan");
			AddHandCard(game, "Wisp");

			game.ProcessCard(skull, asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "River Crocolisk" && p.Cost == 2);
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Chillwind Yeti" && p.Cost == 4);
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Boulderfist Ogre" && p.Cost == 6);
		}

		[Fact]
		public void SoulCleave_BT_740_ShouldDamageTwoEnemyMinionsAndLifesteal()
		{
			Game game = CreateGame();
			game.CurrentPlayer.Hero.Damage = 10;
			game.EndTurn();
			Minion enemy1 = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			Minion enemy2 = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Soul Cleave", asZeroCost: true);

			Assert.Equal(2, enemy1.Damage);
			Assert.Equal(2, enemy2.Damage);
			Assert.Equal(6, game.CurrentPlayer.Hero.Damage);
		}

		[Fact]
		public void Blur_BT_752_ShouldPreventHeroDamageThisTurn()
		{
			Game game = CreateGame();

			game.ProcessCard("Blur", asZeroCost: true);
			game.ProcessCard("Fireball", game.CurrentPlayer.Hero, asZeroCost: true);

			Assert.Equal(0, game.CurrentPlayer.Hero.Damage);
		}

		[Fact]
		public void EyeBeam_BT_801_ShouldDamageMinionAndLifesteal()
		{
			Game game = CreateGame();
			game.CurrentPlayer.Hero.Damage = 6;
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Eye Beam", target, asZeroCost: true);

			Assert.Equal(3, target.Damage);
			Assert.Equal(3, game.CurrentPlayer.Hero.Damage);
		}

		[Fact]
		public void EyeBeam_BT_801_ShouldCostZeroOnlyAtOutcastPosition()
		{
			Game game = CreateGame();
			AddHandCard(game, "Wisp");
			IPlayable eyeBeam = AddHandCard(game, "Eye Beam");
			AddHandCard(game, "Wisp");

			Assert.Equal(3, eyeBeam.Cost);

			EmptyZone(game.CurrentPlayer.HandZone.GetAll());
			eyeBeam = AddHandCard(game, "Eye Beam");

			Assert.Equal(0, eyeBeam.Cost);
		}

		[Fact]
		public void IllidariFelblade_BT_814_ShouldGainImmuneThisTurnOnlyWhenOutcast()
		{
			Game game = CreateGame();
			IPlayable felbladeCard = AddHandCard(game, "Illidari Felblade");

			Minion felblade = game.ProcessCard((Minion)felbladeCard, asZeroCost: true);

			Assert.Equal(1, felblade[GameTag.RUSH]);
			Assert.Equal(1, felblade[GameTag.IMMUNE]);
			game.ProcessCard("Fireball", felblade, asZeroCost: true);
			Assert.Equal(0, felblade.Damage);
			game.EndTurn();
			Assert.Equal(0, game.CurrentOpponent.BoardZone[0][GameTag.IMMUNE]);

			game = CreateGame();
			AddHandCard(game, "Wisp");
			felbladeCard = AddHandCard(game, "Illidari Felblade");
			AddHandCard(game, "Wisp");

			felblade = game.ProcessCard((Minion)felbladeCard, asZeroCost: true);

			Assert.Equal(0, felblade[GameTag.IMMUNE]);
		}

		[Fact]
		public void ManaBurn_BT_753_ShouldReduceOpponentManaNextTurn()
		{
			Game game = CreateGame();

			game.ProcessCard("Mana Burn", asZeroCost: true);
			game.EndTurn();

			Assert.Equal(2, game.CurrentPlayer.OverloadLocked);
			Assert.Equal(8, game.CurrentPlayer.RemainingMana);
		}

		[Fact]
		public void AldrachiWarblades_BT_921_ShouldHaveWeaponLifesteal()
		{
			Game game = CreateGame();
			game.CurrentPlayer.Hero.Damage = 5;

			game.ProcessCard("Aldrachi Warblades", asZeroCost: true);
			game.Process(HeroAttackTask.Any(game.CurrentPlayer, game.CurrentOpponent.Hero));

			Assert.Equal(1, game.CurrentPlayer.Hero.Weapon[GameTag.LIFESTEAL]);
			Assert.Equal(2, game.CurrentOpponent.Hero.Damage);
			Assert.Equal(3, game.CurrentPlayer.Hero.Damage);
		}

		[Fact]
		public void AltruisTheOutcast_BT_937_ShouldDamageEnemiesAfterOutcastPositionCard()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();
			EmptyZone(game.CurrentPlayer.HandZone.GetAll());
			Minion altruis = game.ProcessCard<Minion>("Altruis the Outcast", asZeroCost: true);
			Assert.NotNull(altruis.ActivatedTrigger);
			IPlayable wisp = AddHandCard(game, "Wisp");
			AddHandCard(game, "River Crocolisk");
			Assert.Equal(0, wisp.ZonePosition);

			game.ProcessCard(wisp, asZeroCost: true);

			Assert.Equal(1, enemy.Damage);
			Assert.Equal(1, game.CurrentOpponent.Hero.Damage);
		}

		[Fact]
		public void Umberwing_BT_922_ShouldEquipWeaponAndSummonFelwings()
		{
			Game game = CreateGame();

			game.ProcessCard("Umberwing", asZeroCost: true);

			Assert.NotNull(game.CurrentPlayer.Hero.Weapon);
			Assert.Equal("Umberwing", game.CurrentPlayer.Hero.Weapon.Card.Name);
			Assert.Equal(2, game.CurrentPlayer.BoardZone.Count);
			Assert.All(game.CurrentPlayer.BoardZone, p => Assert.Equal("Felwing", p.Card.Name));
		}
	}
}
