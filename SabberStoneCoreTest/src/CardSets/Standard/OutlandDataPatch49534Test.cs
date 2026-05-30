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
	public class OutlandDataPatch49534Test
	{
		public static IEnumerable<object[]> AddedTrialByFelfireEntities => new[]
		{
			"BTA_01s", "BTA_02pe", "BTA_02pe2", "BTA_02s", "BTA_03e", "BTA_05e", "BTA_06e", "BTA_09e",
			"BTA_11e", "BTA_18s", "BTA_BOSS_01e", "BTA_BOSS_02e", "BTA_BOSS_04s", "BTA_BOSS_06te",
			"BTA_BOSS_07e", "BTA_BOSS_09h2", "BTA_BOSS_10e", "BTA_BOSS_10h3", "BTA_BOSS_10h4",
			"BTA_BOSS_14p2e2", "BTA_BOSS_15e", "BTA_BOSS_15e2", "BTA_BOSS_15s", "BTA_BOSS_16e",
			"BTA_BOSS_16se", "BTA_BOSS_16t2e", "BTA_BOSS_16t2e2", "BTA_BOSS_16te", "BTA_BOSS_16te2",
			"BTA_BOSS_16te3", "BTA_BOSS_17h2", "BTA_BOSS_19s", "BTA_BOSS_20h", "BTA_BOSS_20p",
			"BTA_BOSS_20t", "BTA_BOSS_21h", "BTA_BOSS_21p", "BTA_BOSS_22h", "BTA_BOSS_22p",
			"BTA_BOSS_22s", "BTA_BOSS_22t", "BTA_BOSS_23h", "BTA_BOSS_23p", "BTA_BOSS_24h",
			"BTA_BOSS_24p", "BTA_BOSS_24t", "BTA_BOSS_25h", "BTA_BOSS_25p", "BTA_BOSS_25p2",
			"BTA_BOSS_25pe", "BTA_BOSS_25s", "BTA_BOSS_25se", "BTA_BOSS_26h", "BTA_BOSS_26p",
			"BTA_BOSS_26s", "BTA_BOSS_26se", "BTA_Prevent_First_turn_Attack"
		}.Select(id => new object[] { id });

		private static Game CreateGame()
		{
			var fillerDeck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.DEMONHUNTER,
				Player2HeroClass = CardClass.WARLOCK,
				Player1Deck = fillerDeck,
				Player2Deck = fillerDeck.ToList(),
				Shuffle = false,
				FillDecks = false,
				SkipMulligan = true,
				RandomSeed = 1
			});
			game.StartGame();
			EmptyZone(game.Player1.HandZone.GetAll());
			EmptyZone(game.Player2.HandZone.GetAll());
			EmptyZone(game.Player1.DeckZone.GetAll());
			EmptyZone(game.Player2.DeckZone.GetAll());
			game.Player1.BaseMana = 10;
			game.Player2.BaseMana = 10;
			return game;
		}

		private static void EmptyZone(IEnumerable<IPlayable> cards)
		{
			foreach (IPlayable card in cards.ToArray())
				Generic.RemoveFromZone(card.Controller, card);
		}

		private static IPlayable DrawToHand(Controller controller, string cardId)
		{
			return Generic.DrawCard(controller, Cards.FromId(cardId));
		}

		private static void SetHeroPower(Controller controller, string cardId)
		{
			controller.Hero.HeroPower = (HeroPower)Entity.FromCard(controller, Cards.FromId(cardId));
		}

		private static void PlayHeroPower(Game game, string cardId, IPlayable target = null)
		{
			SetHeroPower(game.CurrentPlayer, cardId);
			game.PlayHeroPower(target, asZeroCost: true);
		}

		[Theory]
		[MemberData(nameof(AddedTrialByFelfireEntities))]
		public void Build49534_ShouldLoadAddedTrialByFelfireEntity(string cardId)
		{
			Assert.NotNull(Cards.FromId(cardId));
		}

		[Fact]
		public void BTA_01p_ShouldDiscoverCardFromOwnDeck()
		{
			Game game = CreateGame();
			game.CurrentPlayer.DeckZone.Add(Entity.FromCard(game.CurrentPlayer, Cards.FromId("CS2_029")));
			game.CurrentPlayer.DeckZone.Add(Entity.FromCard(game.CurrentPlayer, Cards.FromId("CS2_032")));
			game.CurrentPlayer.DeckZone.Add(Entity.FromCard(game.CurrentPlayer, Cards.FromId("CS2_231")));

			PlayHeroPower(game, "BTA_01p");
			Card[] choices = game.GetChoiceCards();
			game.ChooseNthChoice(1);

			Assert.Equal(3, choices.Length);
			Assert.Contains(game.CurrentPlayer.HandZone.GetAll(), p => p.Card.Id == choices[0].Id);
		}

		[Fact]
		public void BTA_02p_ShouldGiveMoreAttackAfterFriendlyMinionAttacked()
		{
			Game game = CreateGame();
			Minion attacker = game.ProcessCard<Minion>("Stonetusk Boar", asZeroCost: true);
			attacker.Attack(game.CurrentOpponent.Hero);

			PlayHeroPower(game, "BTA_02p");

			Assert.Equal(2, game.CurrentPlayer.Hero.AttackDamage);
		}

		[Fact]
		public void BTA_03_ShouldDestroyEnemyMinionAndGainOutcastKeywords()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			var baduu = (Minion)DrawToHand(game.CurrentPlayer, "BTA_03");
			game.ProcessCard(baduu, target, asZeroCost: true);
			game.DeathProcessingAndAuraUpdate();

			Assert.Equal(Zone.GRAVEYARD, target.Zone.Type);
			Assert.True(baduu.HasStealth);
			Assert.True(baduu.Poisonous);
		}

		[Fact]
		public void BTA_05AndBTA_06_ShouldApplyAurasAndOutcastSummons()
		{
			Game game = CreateGame();
			Minion left = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion sklibb = (Minion)DrawToHand(game.CurrentPlayer, "BTA_05");
			game.ProcessCard(sklibb, asZeroCost: true);
			game.AuraUpdate();

			Assert.True(left.HasTaunt);
			Assert.Equal(2, left.AttackDamage);
			Assert.Equal(2, game.CurrentPlayer.BoardZone.Count(p => p.Card.Id == "BTA_BOSS_03t"));

			game = CreateGame();
			Minion other = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			var dhSklibb = (Minion)DrawToHand(game.CurrentPlayer, "BTA_06");
			game.ProcessCard(dhSklibb, asZeroCost: true);
			game.AuraUpdate();

			Assert.Equal(3, other.AttackDamage);
			Assert.Equal(3, game.CurrentPlayer.BoardZone.Count(p => p.Card.Id == "BT_036t"));
		}

		[Fact]
		public void BTA_07ToBTA_10_ShouldApplyOutcastSummonOrHandEffects()
		{
			Game game = CreateGame();
			game.ProcessCard(DrawToHand(game.CurrentPlayer, "BTA_07"), asZeroCost: true);
			Assert.Contains(game.CurrentPlayer.BoardZone.GetAll(), p => p.Card.Id == "BT_258");

			game = CreateGame();
			game.ProcessCard(DrawToHand(game.CurrentPlayer, "BTA_08"), asZeroCost: true);
			Assert.Contains(game.CurrentPlayer.BoardZone.GetAll(), p => p.Card.Id == "BT_934");

			game = CreateGame();
			game.ProcessCard(DrawToHand(game.CurrentPlayer, "BTA_09"), asZeroCost: true);
			Assert.Equal(3, game.CurrentPlayer.HandZone.Count);
			Assert.All(game.CurrentPlayer.HandZone.GetAll(), p => Assert.Equal(CardType.SPELL, p.Card.Type));

			game = CreateGame();
			game.ProcessCard(DrawToHand(game.CurrentPlayer, "BTA_10"), asZeroCost: true);
			Assert.NotNull(game.CurrentPlayer.Hero.Weapon);
			Assert.Equal(3, game.CurrentPlayer.HandZone.Count);
			Assert.All(game.CurrentPlayer.HandZone.GetAll(), p => Assert.Equal(CardType.WEAPON, p.Card.Type));
		}

		[Fact]
		public void BTA_11AndBTA_BOSS_02p_ShouldCorruptOpponentPlayableCard()
		{
			Game game = CreateGame();
			DrawToHand(game.CurrentOpponent, "CS2_029");

			game.ProcessCard(DrawToHand(game.CurrentPlayer, "BTA_11"), asZeroCost: true);

			Assert.Contains(game.CurrentOpponent.HandZone.GetAll().Single().AppliedEnchantments, e => e.Card.Id == "BTA_11e");

			game = CreateGame();
			DrawToHand(game.CurrentOpponent, "CS2_029");
			PlayHeroPower(game, "BTA_BOSS_02p");

			Assert.Contains(game.CurrentOpponent.HandZone.GetAll().Single().AppliedEnchantments, e => e.Card.Id == "BTA_BOSS_02e");
		}

		[Fact]
		public void BTA_12ToBTA_17_ShouldRunTheirBoardEffects()
		{
			Game game = CreateGame();
			IPlayable expensive = DrawToHand(game.CurrentPlayer, "CS2_200");
			int oldCost = expensive.Cost;
			game.ProcessCard(DrawToHand(game.CurrentPlayer, "BTA_12"), asZeroCost: true);
			game.EndTurn();
			Assert.True(expensive.Cost < oldCost);

			game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Bloodfen Raptor", asZeroCost: true);
			game.ProcessCard(DrawToHand(game.CurrentPlayer, "BTA_13"), target, asZeroCost: true);
			game.DeathProcessingAndAuraUpdate();
			Assert.Contains(game.CurrentPlayer.BoardZone.GetAll(), p => p.Card.Id == "BT_305");

			game = CreateGame();
			game.EndTurn();
			int health = game.CurrentPlayer.Hero.Health;
			game.EndTurn();
			game.ProcessCard(DrawToHand(game.CurrentPlayer, "BTA_14"), asZeroCost: true);
			game.EndTurn();
			game.EndTurn();
			Assert.True(game.CurrentOpponent.Hero.Health < health);

			game = CreateGame();
			game.CurrentPlayer.DeckZone.Add(Entity.FromCard(game.CurrentPlayer, Cards.FromId("CS2_231")));
			game.CurrentPlayer.DeckZone.Add(Entity.FromCard(game.CurrentPlayer, Cards.FromId("CS2_029")));
			game.CurrentPlayer.DeckZone.Add(Entity.FromCard(game.CurrentPlayer, Cards.FromId("CS2_232")));
			game.ProcessCard(DrawToHand(game.CurrentPlayer, "BTA_15"), asZeroCost: true);
			Assert.Equal(2, game.CurrentPlayer.BoardZone.Count);
			Assert.Single(game.CurrentPlayer.HandZone.GetAll());

			game = CreateGame();
			Minion giant = game.ProcessCard<Minion>((Minion)DrawToHand(game.CurrentPlayer, "BTA_16"), asZeroCost: true);
			giant.Kill();
			Assert.Contains(game.CurrentPlayer.BoardZone.GetAll(), p => p.Card.IsRace(Race.MECHANICAL));

			game = CreateGame();
			Minion voidwalker = game.ProcessCard<Minion>((Minion)DrawToHand(game.CurrentPlayer, "BTA_17"), asZeroCost: true);
			voidwalker.Damage = 2;
			game.EndTurn();
			game.EndTurn();
			Assert.Equal(0, voidwalker.Damage);
		}

		[Fact]
		public void BossHeroPowers_ShouldRunRepresentativeCombatEffects()
		{
			Game game = CreateGame();
			Minion cube = game.ProcessCard<Minion>((Minion)DrawToHand(game.CurrentPlayer, "BTA_BOSS_10t"), asZeroCost: true);
			PlayHeroPower(game, "BTA_BOSS_01p", cube);
			game.DeathProcessingAndAuraUpdate();
			Assert.True(game.CurrentPlayer.DeckZone.Count(p => p.Card.Id == "BTA_BOSS_10t") >= 2);

			game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Bloodfen Raptor", asZeroCost: true);
			PlayHeroPower(game, "BTA_BOSS_10p", target);
			game.DeathProcessingAndAuraUpdate();
			Assert.Equal(Zone.GRAVEYARD, target.Zone.Type);

			game = CreateGame();
			PlayHeroPower(game, "BTA_BOSS_14p");
			Assert.Contains(game.CurrentPlayer.BoardZone.GetAll(), p => p.Card.Id == "EX1_317t");

			game = CreateGame();
			PlayHeroPower(game, "BTA_BOSS_20p");
			Assert.Contains(game.CurrentPlayer.BoardZone.GetAll(), p => p.Card.Id == "BTA_BOSS_20t");

			game = CreateGame();
			PlayHeroPower(game, "BTA_BOSS_22p");
			Assert.Contains(game.CurrentPlayer.BoardZone.GetAll(), p => p.Card.Id == "BTA_BOSS_22t");
		}

		[Fact]
		public void BossSpellsAndPassives_ShouldRunRepresentativeEffects()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();
			game.CurrentPlayer.DeckZone.Add(Entity.FromCard(game.CurrentPlayer, Cards.FromId("CS2_029")));
			PlayHeroPower(game, "BTA_BOSS_24p", target);
			Assert.Single(game.CurrentPlayer.HandZone.GetAll());

			game = CreateGame();
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();
			game.ProcessCard(DrawToHand(game.CurrentPlayer, "BTA_BOSS_25s"), asZeroCost: true);
			Assert.Empty(game.CurrentOpponent.BoardZone.GetAll());
			Assert.Contains(enemy, game.CurrentOpponent.HandZone.GetAll());
			Assert.True(enemy.Cost > enemy.Card.Cost);

			game = CreateGame();
			Minion minion = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			PlayHeroPower(game, "BTA_BOSS_11p");
			Assert.True(minion.HasWindfury);
			Assert.True(minion.AttackableByRush);
		}
	}
}
