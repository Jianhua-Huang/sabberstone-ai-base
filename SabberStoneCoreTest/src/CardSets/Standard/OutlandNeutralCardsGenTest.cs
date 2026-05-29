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
	public class OutlandNeutralCardsGenTest
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
		public void RustswornInitiate_BT_008_ShouldSummonSpellDamageImpcasterOnDeathrattle()
		{
			Game game = CreateGame();
			Minion initiate = game.ProcessCard<Minion>("Rustsworn Initiate", asZeroCost: true);

			game.ProcessCard("Fireball", initiate, asZeroCost: true);

			Minion impcaster = Assert.Single(game.CurrentPlayer.BoardZone.Where(p => p.Card.Id == "BT_008t"));
			Assert.Equal(1, impcaster.AttackDamage);
			Assert.Equal(1, impcaster.Health);
			Assert.Equal(1, impcaster[GameTag.SPELLPOWER]);
		}

		[Fact]
		public void FelfinNavigator_BT_010_ShouldBuffOtherFriendlyMurlocs()
		{
			Game game = CreateGame();
			Minion murloc1 = game.ProcessCard<Minion>("Murloc Raider", asZeroCost: true);
			Minion murloc2 = game.ProcessCard<Minion>("Murloc Raider", asZeroCost: true);

			Minion navigator = game.ProcessCard<Minion>("Felfin Navigator", asZeroCost: true);

			Assert.Equal(3, murloc1.AttackDamage);
			Assert.Equal(2, murloc1.Health);
			Assert.Equal(3, murloc2.AttackDamage);
			Assert.Equal(2, murloc2.Health);
			Assert.Equal(4, navigator.AttackDamage);
			Assert.Equal(4, navigator.Health);
		}

		[Fact]
		public void FrozenShadoweaver_BT_714_ShouldFreezeEnemyTarget()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Frozen Shadoweaver", target, asZeroCost: true);

			Assert.True(target.IsFrozen);
		}

		[Theory]
		[InlineData("Bonechewer Brawler", 4)]
		[InlineData("Bonechewer Vanguard", 6)]
		public void BonechewerMinions_ShouldGainAttackWhenDamaged(string cardName, int expectedAttack)
		{
			Game game = CreateGame();
			Minion minion = game.ProcessCard<Minion>(cardName, asZeroCost: true);

			game.ProcessCard("Moonfire", minion, asZeroCost: true);

			Assert.Equal(1, minion[GameTag.TAUNT]);
			Assert.Equal(expectedAttack, minion.AttackDamage);
		}

		[Fact]
		public void RuststeedRaider_BT_720_ShouldGainTemporaryAttack()
		{
			Game game = CreateGame();

			Minion raider = game.ProcessCard<Minion>("Ruststeed Raider", asZeroCost: true);

			Assert.Equal(1, raider[GameTag.TAUNT]);
			Assert.Equal(1, raider[GameTag.RUSH]);
			Assert.Equal(5, raider.AttackDamage);
			game.EndTurn();
			Assert.Equal(1, game.CurrentOpponent.BoardZone[0].AttackDamage);
		}

		[Fact]
		public void GuardianAugmerchant_BT_722_ShouldDamageMinionAndGiveDivineShield()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);

			game.ProcessCard("Guardian Augmerchant", target, asZeroCost: true);

			Assert.Equal(1, target.Damage);
			Assert.Equal(1, target[GameTag.DIVINE_SHIELD]);
		}

		[Fact]
		public void RocketAugmerchant_BT_723_ShouldDamageMinionAndGiveRush()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);

			game.ProcessCard("Rocket Augmerchant", target, asZeroCost: true);

			Assert.Equal(1, target.Damage);
			Assert.Equal(1, target[GameTag.RUSH]);
			Assert.False(target.IsExhausted);
		}

		[Fact]
		public void EtherealAugmerchant_BT_724_ShouldDamageMinionAndGiveSpellDamage()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);

			game.ProcessCard("Ethereal Augmerchant", target, asZeroCost: true);

			Assert.Equal(1, target.Damage);
			Assert.Equal(1, target[GameTag.SPELLPOWER]);
		}

		[Fact]
		public void SoulboundAshtongue_BT_727_ShouldDamageControllerHeroWhenDamaged()
		{
			Game game = CreateGame();
			Minion ashtongue = game.ProcessCard<Minion>("Soulbound Ashtongue", asZeroCost: true);

			game.ProcessCard("Moonfire", ashtongue, asZeroCost: true);

			Assert.Equal(1, ashtongue.Damage);
			Assert.Equal(1, game.CurrentPlayer.Hero.Damage);
		}

		[Fact]
		public void OverconfidentOrc_BT_730_ShouldLoseAttackWhenDamaged()
		{
			Game game = CreateGame();
			Minion orc = game.ProcessCard<Minion>("Overconfident Orc", asZeroCost: true);

			Assert.Equal(1, orc[GameTag.TAUNT]);
			Assert.Equal(3, orc.AttackDamage);
			game.ProcessCard("Moonfire", orc, asZeroCost: true);
			Assert.Equal(1, orc.AttackDamage);
		}

		[Fact]
		public void ScrapyardColossus_BT_155_ShouldSummonTauntColossusOnDeathrattle()
		{
			Game game = CreateGame();
			Minion colossus = game.ProcessCard<Minion>("Scrapyard Colossus", asZeroCost: true);

			game.ProcessCard("Fireball", colossus, asZeroCost: true);
			game.ProcessCard("Moonfire", colossus, asZeroCost: true);

			Minion token = Assert.Single(game.CurrentPlayer.BoardZone.Where(p => p.Card.Id == "BT_155t"));
			Assert.Equal(7, token.AttackDamage);
			Assert.Equal(7, token.Health);
			Assert.Equal(1, token[GameTag.TAUNT]);
		}

		[Fact]
		public void TerrorguardEscapee_BT_159_ShouldSummonHuntressesForOpponent()
		{
			Game game = CreateGame();

			game.ProcessCard("Terrorguard Escapee", asZeroCost: true);

			Assert.Equal(3, game.CurrentOpponent.BoardZone.Count(p => p.Card.Id == "BT_159t"));
		}

		[Fact]
		public void RustswornCultist_BT_160_ShouldGiveOtherMinionsDeathrattleToSummonDemon()
		{
			Game game = CreateGame();
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard("Rustsworn Cultist", asZeroCost: true);
			game.ProcessCard("Moonfire", wisp, asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.BoardZone, p => p.Card.Id == "BT_160t");
		}

		[Fact]
		public void BurrowingScorpid_BT_717_ShouldGainStealthIfBattlecryKillsTarget()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			game.EndTurn();

			Minion scorpid = game.ProcessCard<Minion>("Burrowing Scorpid", target, asZeroCost: true);

			Assert.True(target.ToBeDestroyed);
			Assert.Equal(1, scorpid[GameTag.STEALTH]);
		}

		[Fact]
		public void BlisteringRot_BT_721_ShouldSummonRotWithSameStatsAtEndOfTurn()
		{
			Game game = CreateGame();
			game.ProcessCard("Blistering Rot", asZeroCost: true);

			game.EndTurn();

			Minion token = Assert.Single(game.CurrentOpponent.BoardZone.Where(p => p.Card.Id == "BT_721t"));
			Assert.Equal(1, token.AttackDamage);
			Assert.Equal(2, token.Health);
		}

		[Fact]
		public void DragonmawSkyStalker_BT_726_ShouldSummonDragonriderOnDeathrattle()
		{
			Game game = CreateGame();
			Minion stalker = game.ProcessCard<Minion>("Dragonmaw Sky Stalker", asZeroCost: true);

			game.ProcessCard("Fireball", stalker, asZeroCost: true);

			Minion token = Assert.Single(game.CurrentPlayer.BoardZone.Where(p => p.Card.Id == "BT_726t"));
			Assert.Equal(3, token.AttackDamage);
			Assert.Equal(4, token.Health);
		}

		[Fact]
		public void DisguisedWanderer_BT_728_ShouldSummonInquisitorOnDeathrattle()
		{
			Game game = CreateGame();
			Minion wanderer = game.ProcessCard<Minion>("Disguised Wanderer", asZeroCost: true);

			game.ProcessCard("Fireball", wanderer, asZeroCost: true);

			Minion token = Assert.Single(game.CurrentPlayer.BoardZone.Where(p => p.Card.Id == "BT_728t"));
			Assert.Equal(9, token.AttackDamage);
			Assert.Equal(1, token.Health);
		}

		[Fact]
		public void WasteWarden_BT_729_ShouldDamageTargetAndSameRaceMinions()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion beast1 = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			Minion beast2 = game.ProcessCard<Minion>("Bloodfen Raptor", asZeroCost: true);
			Minion nonBeast = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Waste Warden", beast1, asZeroCost: true);

			Assert.Equal(3, beast1.Damage);
			Assert.Equal(3, beast2.Damage);
			Assert.Equal(0, nonBeast.Damage);
		}

		[Fact]
		public void InfectiousSporeling_BT_731_ShouldTransformDamagedMinionIntoSporeling()
		{
			Game game = CreateGame();
			Minion sporeling = game.ProcessCard<Minion>("Infectious Sporeling", asZeroCost: true);
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Dalaran Mage", asZeroCost: true);
			game.EndTurn();
			sporeling.IsExhausted = false;

			game.Process(MinionAttackTask.Any(game.CurrentPlayer, sporeling, target));

			Assert.Contains(game.CurrentOpponent.BoardZone, p => p.Card.Id == "BT_731");
			Assert.DoesNotContain(game.CurrentOpponent.BoardZone, p => p.Card.Name == "Dalaran Mage");
		}

		[Fact]
		public void ScavengingShivarra_BT_732_ShouldDealSixRandomlySplitAmongOtherMinions()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion enemy1 = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion enemy2 = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion enemy3 = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Scavenging Shivarra", asZeroCost: true);

			Assert.True(enemy1.ToBeDestroyed);
			Assert.True(enemy2.ToBeDestroyed);
			Assert.True(enemy3.ToBeDestroyed);
		}

		[Fact]
		public void SupremeAbyssal_BT_734_ShouldNotBeAbleToAttackHeroes()
		{
			Game game = CreateGame();
			Minion abyssal = game.ProcessCard<Minion>("Supreme Abyssal", asZeroCost: true);
			abyssal.IsExhausted = false;

			Assert.DoesNotContain(game.CurrentOpponent.Hero, abyssal.ValidAttackTargets);
		}
	}
}
