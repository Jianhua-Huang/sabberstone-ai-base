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
		public void ConsumeMagic_BT_490_ShouldSilenceEnemyMinion()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Goldshire Footman", asZeroCost: true);
			Assert.Equal(1, target[GameTag.TAUNT]);
			game.EndTurn();

			game.ProcessCard("Consume Magic", target, asZeroCost: true);

			Assert.Equal(0, target[GameTag.TAUNT]);
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
