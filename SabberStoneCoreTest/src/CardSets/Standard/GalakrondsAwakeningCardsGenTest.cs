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
	public class GalakrondsAwakeningCardsGenTest
	{
		private static Game CreateGame(CardClass playerClass = CardClass.MAGE, CardClass opponentClass = CardClass.MAGE)
		{
			var fillerDeck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = playerClass,
				Player2HeroClass = opponentClass,
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

		private static IPlayable AddToDeck(Controller controller, string cardName)
		{
			IPlayable card = Entity.FromCard(controller, Cards.FromName(cardName));
			controller.DeckZone.Add(card);
			return card;
		}

		private static IPlayable AddToHand(Controller controller, string cardName) =>
			Generic.DrawCard(controller, Cards.FromName(cardName));

		[Fact]
		public void RisingWinds_YOD_001_DrawsOrSummonsAndAddsTwinspellCopy()
		{
			Game game = CreateGame(CardClass.DRUID);
			AddToDeck(game.CurrentPlayer, "Wisp");

			game.ProcessCard("Rising Winds", asZeroCost: true, chooseOne: 1);

			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Wisp");
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Id == "YOD_001ts");

			game = CreateGame(CardClass.DRUID);
			game.ProcessCard("Rising Winds", asZeroCost: true, chooseOne: 2);

			Assert.Contains(game.CurrentPlayer.BoardZone, p => p.Card.Id == "YOD_001t");
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Id == "YOD_001ts");
		}

		[Fact]
		public void WingedGuardian_YOD_003_HasKeywordTags()
		{
			Card card = Cards.FromId("YOD_003");
			Assert.Equal(1, card[GameTag.TAUNT]);
			Assert.Equal(1, card[GameTag.REBORN]);
			Assert.Equal(1, card[GameTag.CANT_BE_TARGETED_BY_SPELLS]);
			Assert.Equal(1, card[GameTag.CANT_BE_TARGETED_BY_HERO_POWERS]);
		}

		[Fact]
		public void ChopshopCopter_YOD_004_AddsRandomMechWhenFriendlyMechDies()
		{
			Game game = CreateGame(CardClass.HUNTER);
			game.ProcessCard("Chopshop Copter", asZeroCost: true);
			Minion mech = game.ProcessCard<Minion>("Spider Tank", asZeroCost: true);
			int handCount = game.CurrentPlayer.HandZone.Count;

			mech.Kill();

			Assert.True(game.CurrentPlayer.HandZone.Count > handCount);
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.IsRace(Race.MECHANICAL));
		}

		[Fact]
		public void FreshScent_YOD_005_BuffsBeastAndAddsTwinspellCopy()
		{
			Game game = CreateGame(CardClass.HUNTER);
			Minion beast = game.ProcessCard<Minion>("Bloodfen Raptor", asZeroCost: true);

			game.ProcessCard("Fresh Scent", beast, asZeroCost: true);

			Assert.Equal(5, beast.AttackDamage);
			Assert.Equal(4, beast.Health);
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Id == "YOD_005ts");
		}

		[Fact]
		public void EscapedManasaber_YOD_006_GainsTemporaryManaWhenItAttacks()
		{
			Game game = CreateGame();
			game.CurrentPlayer.BaseMana = 5;
			Minion manasaber = game.ProcessCard<Minion>("Escaped Manasaber", asZeroCost: true);
			manasaber.IsExhausted = false;
			int mana = game.CurrentPlayer.RemainingMana;

			manasaber.Attack(game.CurrentOpponent.Hero);

			Assert.True(game.CurrentPlayer.RemainingMana > mana);
		}

		[Fact]
		public void AnimatedAvalanche_YOD_007_SummonsCopyIfElementalWasPlayedLastTurn()
		{
			Game game = CreateGame(CardClass.MAGE);
			game.CurrentPlayer.NumElementalsPlayedLastTurn = 1;

			game.ProcessCard("Animated Avalanche", asZeroCost: true);

			Assert.Equal(2, game.CurrentPlayer.BoardZone.Count(p => p.Card.Id == "YOD_007"));
		}

		[Fact]
		public void ArcaneAmplifier_YOD_008_IncreasesHeroPowerDamage()
		{
			Game game = CreateGame(CardClass.MAGE);
			game.ProcessCard("Arcane Amplifier", asZeroCost: true);

			game.PlayHeroPower(game.CurrentOpponent.Hero, asZeroCost: true);

			Assert.Equal(3, game.CurrentOpponent.Hero.Damage);
		}

		[Fact]
		public void TheAmazingReno_YOD_009_RemovesAllMinionsAndReplacesHeroPower()
		{
			Game game = CreateGame(CardClass.MAGE);
			game.ProcessCard("Wisp", asZeroCost: true);
			game.EndTurn();
			game.ProcessCard("Wisp", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("The Amazing Reno", asZeroCost: true);

			Assert.Empty(game.CurrentPlayer.BoardZone);
			Assert.Empty(game.CurrentOpponent.BoardZone);
			Assert.Equal("YOD_009h", game.CurrentPlayer.Hero.HeroPower.Card.Id);
		}

		[Fact]
		public void Shotbot_YOD_010_HasReborn()
		{
			Assert.Equal(1, Cards.FromId("YOD_010")[GameTag.REBORN]);
		}

		[Fact]
		public void AirRaid_YOD_012_SummonsTwoTauntRecruitsAndAddsTwinspellCopy()
		{
			Game game = CreateGame(CardClass.PALADIN);

			game.ProcessCard("Air Raid", asZeroCost: true);

			Assert.Equal(2, game.CurrentPlayer.BoardZone.Count);
			Assert.All(game.CurrentPlayer.BoardZone, p => Assert.True(p.HasTaunt));
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Id == "YOD_012ts");
		}

		[Fact]
		public void ClericOfScales_YOD_013_DiscoversSpellFromDeckIfHoldingDragon()
		{
			Game game = CreateGame(CardClass.PRIEST);
			AddToHand(game.CurrentPlayer, "Faerie Dragon");
			AddToDeck(game.CurrentPlayer, "Holy Smite");

			game.ProcessCard("Cleric of Scales", asZeroCost: true);
			game.ChooseNthChoice(1);

			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Holy Smite");
		}

		[Fact]
		public void AeonReaver_YOD_014_DealsDamageEqualToTargetsAttack()
		{
			Game game = CreateGame(CardClass.PRIEST);
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Aeon Reaver", target, asZeroCost: true);

			Assert.Equal(4, target.Damage);
		}

		[Fact]
		public void DarkProphecy_YOD_015_DiscoversSummonsAndBuffsHealth()
		{
			Game game = CreateGame(CardClass.PRIEST);

			game.ProcessCard("Dark Prophecy", asZeroCost: true);
			IPlayable chosen = game.ChooseNthChoice(1);

			Assert.Contains(game.CurrentPlayer.BoardZone, p => p.Id == chosen.Id);
			Assert.True(((Minion)chosen).Health > chosen.Card.Health);
		}

		[Fact]
		public void Skyvateer_YOD_016_DrawsOnDeath()
		{
			Game game = CreateGame(CardClass.ROGUE);
			AddToDeck(game.CurrentPlayer, "Wisp");
			Minion skyvateer = game.ProcessCard<Minion>("Skyvateer", asZeroCost: true);

			skyvateer.TakeDamage(skyvateer, skyvateer.Health);
			game.DeathProcessingAndAuraUpdate();

			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Wisp");
		}

		[Fact]
		public void ShadowSculptor_YOD_017_DrawsForCardsPlayedThisTurnOnCombo()
		{
			Game game = CreateGame(CardClass.ROGUE);
			AddToDeck(game.CurrentPlayer, "Wisp");
			game.ProcessCard("Wisp", asZeroCost: true);

			game.ProcessCard("Shadow Sculptor", asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Name == "Wisp");
		}

		[Fact]
		public void Waxmancy_YOD_018_DiscoversBattlecryMinionAndDiscountsIt()
		{
			Game game = CreateGame(CardClass.ROGUE);

			game.ProcessCard("Waxmancy", asZeroCost: true);
			IPlayable chosen = game.ChooseNthChoice(1);

			Assert.Equal(1, chosen.Card[GameTag.BATTLECRY]);
			Assert.True(chosen.Cost <= chosen.Card.Cost - 2);
		}

		[Fact]
		public void ExplosiveEvolution_YOD_020_TransformsTargetIntoMoreExpensiveMinion()
		{
			Game game = CreateGame(CardClass.SHAMAN);
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard("Explosive Evolution", target, asZeroCost: true);

			Assert.NotEqual("Wisp", game.CurrentPlayer.BoardZone[0].Card.Name);
		}

		[Fact]
		public void RiskySkipper_YOD_022_DamagesAllMinionsAfterFriendlyMinionPlayed()
		{
			Game game = CreateGame(CardClass.WARRIOR);
			Minion skipper = game.ProcessCard<Minion>("Risky Skipper", asZeroCost: true);

			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			Assert.Equal(1, skipper.Damage);
			Assert.Equal(1, wisp.Damage);
		}

		[Fact]
		public void BoomSquad_YOD_023_DiscoversOnlyLackeyMechOrDragon()
		{
			Game game = CreateGame(CardClass.WARRIOR);

			game.ProcessCard("Boom Squad", asZeroCost: true);

			Assert.All(game.GetChoiceCards(), card =>
				Assert.True(card[GameTag.MARK_OF_EVIL] == 1 || card.IsRace(Race.MECHANICAL) || card.IsRace(Race.DRAGON)));
		}

		[Fact]
		public void BombWrangler_YOD_024_SummonsBoomBotWhenDamaged()
		{
			Game game = CreateGame(CardClass.WARRIOR);
			Minion wrangler = game.ProcessCard<Minion>("Bomb Wrangler", asZeroCost: true);

			wrangler.TakeDamage(wrangler, 1);

			Assert.Contains(game.CurrentPlayer.BoardZone, p => p.Card.Id == "GVG_110t");
		}

		[Fact]
		public void TwistedKnowledge_YOD_025_DiscoversTwoWarlockCards()
		{
			Game game = CreateGame(CardClass.WARLOCK);

			game.ProcessCard("Twisted Knowledge", asZeroCost: true);
			IPlayable first = game.ChooseNthChoice(1);
			IPlayable second = game.ChooseNthChoice(1);

			Assert.Equal(CardClass.WARLOCK, first.Card.Class);
			Assert.Equal(CardClass.WARLOCK, second.Card.Class);
		}

		[Fact]
		public void FiendishServant_YOD_026_GivesAttackToRandomFriendlyMinionOnDeath()
		{
			Game game = CreateGame(CardClass.WARLOCK);
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion servant = game.ProcessCard<Minion>("Fiendish Servant", asZeroCost: true);

			servant.Kill();

			Assert.Equal(3, target.AttackDamage);
		}

		[Fact]
		public void ChaosGazer_YOD_027_DiscardsCorruptedPlayableCardAtItsControllersTurnEnd()
		{
			Game game = CreateGame(CardClass.WARLOCK);
			AddToHand(game.CurrentOpponent, "Wisp");

			game.ProcessCard("Chaos Gazer", asZeroCost: true);
			Assert.Single(game.CurrentOpponent.HandZone);

			game.EndTurn();
			game.EndTurn();

			Assert.Empty(game.CurrentPlayer.HandZone);
		}

		[Fact]
		public void SkydivingInstructor_YOD_028_SummonsOneCostMinionFromDeck()
		{
			Game game = CreateGame();
			AddToDeck(game.CurrentPlayer, "Stonetusk Boar");

			game.ProcessCard("Skydiving Instructor", asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.BoardZone, p => p.Card.Name == "Stonetusk Boar");
		}

		[Fact]
		public void Hailbringer_YOD_029_SummonsTwoIceShardsWithFreezeTrigger()
		{
			Game game = CreateGame();

			game.ProcessCard("Hailbringer", asZeroCost: true);

			Assert.Equal(2, game.CurrentPlayer.BoardZone.Count(p => p.Card.Id == "YOD_029t"));
			Assert.All(game.CurrentPlayer.BoardZone.Where(p => p.Card.Id == "YOD_029t"), p => Assert.Equal(1, p.Card[GameTag.FREEZE]));
		}

		[Fact]
		public void LicensedAdventurer_YOD_030_AddsCoinIfQuestControlled()
		{
			Game game = CreateGame();
			game.ProcessCard("Untapped Potential", asZeroCost: true);

			game.ProcessCard("Licensed Adventurer", asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Id == "GAME_005");
		}

		[Fact]
		public void FrenziedFelwing_YOD_032_CostsLessForOpponentHeroDamageThisTurn()
		{
			Game game = CreateGame();
			game.CurrentOpponent.Hero.DamageTakenThisTurn = 2;
			IPlayable felwing = AddToHand(game.CurrentPlayer, "Frenzied Felwing");
			game.AuraUpdate();

			Assert.Equal(2, felwing.Cost);
		}

		[Fact]
		public void BoompistolBully_YOD_033_IncreasesEnemyBattlecryCardCosts()
		{
			Game game = CreateGame();
			IPlayable wisp = AddToHand(game.CurrentOpponent, "Novice Engineer");
			int originalCost = wisp.Cost;

			game.ProcessCard("Boompistol Bully", asZeroCost: true);

			Assert.Equal(originalCost + 5, wisp.Cost);
		}

		[Fact]
		public void GrandLackeyErkh_YOD_035_AddsLackeyAfterFriendlyLackeyPlayed()
		{
			Game game = CreateGame();
			game.ProcessCard("Grand Lackey Erkh", asZeroCost: true);
			int handCount = game.CurrentPlayer.HandZone.Count;

			IPlayable lackey = Generic.DrawCard(game.CurrentPlayer, Cards.FromId("DAL_613"));
			game.ProcessCard(lackey, asZeroCost: true);

			Assert.True(game.CurrentPlayer.HandZone.Count > handCount);
			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card[GameTag.MARK_OF_EVIL] == 1);
		}

		[Fact]
		public void RotnestDrake_YOD_036_DestroysRandomEnemyMinionIfHoldingDragon()
		{
			Game game = CreateGame(CardClass.HUNTER);
			AddToHand(game.CurrentPlayer, "Faerie Dragon");
			game.EndTurn();
			game.ProcessCard("Wisp", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Rotnest Drake", asZeroCost: true);

			Assert.Empty(game.CurrentOpponent.BoardZone);
		}

		[Fact]
		public void SkyGenralKragg_YOD_038_SummonsSharkbaitIfQuestWasPlayed()
		{
			Game game = CreateGame();
			game.ProcessCard("Untapped Potential", asZeroCost: true);

			game.ProcessCard("Sky Gen'ral Kragg", asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.BoardZone, p => p.Card.Id == "YOD_038t");
		}

		[Fact]
		public void SteelBeetle_YOD_040_GainsArmorIfHoldingExpensiveSpell()
		{
			Game game = CreateGame(CardClass.DRUID);
			AddToHand(game.CurrentPlayer, "Starfire");

			game.ProcessCard("Steel Beetle", asZeroCost: true);

			Assert.Equal(5, game.CurrentPlayer.Hero.Armor);
		}

		[Fact]
		public void EyeOfTheStorm_YOD_041_SummonsThreeStormblockersAndOverloads()
		{
			Game game = CreateGame(CardClass.SHAMAN);

			game.ProcessCard("Eye of the Storm", asZeroCost: true);

			Assert.Equal(3, game.CurrentPlayer.BoardZone.Count(p => p.Card.Id == "YOD_041t"));
			Assert.Equal(3, game.CurrentPlayer.OverloadOwed);
		}

		[Fact]
		public void TheFistOfRaden_YOD_042_SummonsLegendaryOfSpellCostAndLosesDurability()
		{
			Game game = CreateGame(CardClass.SHAMAN);
			game.ProcessCard("The Fist of Ra-den", asZeroCost: true);
			int durability = game.CurrentPlayer.Hero.Weapon.Durability;

			game.ProcessCard("Frostbolt", game.CurrentOpponent.Hero);

			Assert.True(game.CurrentPlayer.BoardZone.Any(p => p.Card.Rarity == Rarity.LEGENDARY && p.Card.Cost == 2));
			Assert.Equal(durability - 1, game.CurrentPlayer.Hero.Weapon.Durability);
		}

		[Fact]
		public void Scalelord_YOD_043_GivesFriendlyMurlocsDivineShield()
		{
			Game game = CreateGame(CardClass.PALADIN);
			Minion murloc = game.ProcessCard<Minion>("Murloc Raider", asZeroCost: true);

			game.ProcessCard("Scalelord", asZeroCost: true);

			Assert.Equal(1, murloc[GameTag.DIVINE_SHIELD]);
		}
	}
}
