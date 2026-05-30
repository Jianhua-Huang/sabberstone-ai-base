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
	}
}
