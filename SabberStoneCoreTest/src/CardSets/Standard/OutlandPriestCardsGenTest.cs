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
	public class OutlandPriestCardsGenTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null)
		{
			var defaultDeck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.PRIEST,
				Player2HeroClass = CardClass.MAGE,
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

		private static IPlayable AddHandCard(Game game, string cardName)
		{
			return Generic.DrawCard(game.CurrentPlayer, Cards.FromName(cardName));
		}

		[Fact]
		public void PsychicConjurer_EX1_193_ShouldCopyCardFromOpponentDeck()
		{
			Game game = CreateGame(
				opponentDeck: Enumerable.Repeat(Cards.FromName("Murloc Raider"), 30));

			game.ProcessCard("Psychic Conjurer", asZeroCost: true);

			Assert.Single(game.CurrentPlayer.HandZone);
			Assert.Equal("Murloc Raider", game.CurrentPlayer.HandZone[0].Card.Name);
			Assert.Equal(26, game.CurrentOpponent.DeckZone.Count);
		}

		[Fact]
		public void PowerInfusion_EX1_194_ShouldGiveMinionTwoAttackAndSixHealth()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard("Power Infusion", target, asZeroCost: true);

			Assert.Equal(3, target.AttackDamage);
			Assert.Equal(7, target.Health);
		}

		[Fact]
		public void KulTiranChaplain_EX1_195_ShouldGiveFriendlyMinionTwoHealth()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard("Kul Tiran Chaplain", target, asZeroCost: true);

			Assert.Equal(3, target.Health);
		}

		[Fact]
		public void ScarletSubjugator_EX1_196_ShouldReduceEnemyAttackUntilYourNextTurn()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Scarlet Subjugator", target, asZeroCost: true);

			Assert.Equal(2, target.AttackDamage);
			game.EndTurn();
			Assert.Equal(2, target.AttackDamage);
			game.EndTurn();
			Assert.Equal(4, target.AttackDamage);
		}

		[Fact]
		public void ShadowWordRuin_EX1_197_ShouldDestroyOnlyMinionsWithAtLeastFiveAttack()
		{
			Game game = CreateGame();
			Minion friendlySmall = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			Minion friendlyLarge = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			game.EndTurn();
			Minion enemyLarge = game.ProcessCard<Minion>("Boulderfist Ogre", asZeroCost: true);
			Minion enemySmall = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Shadow Word: Ruin", asZeroCost: true);

			Assert.False(friendlySmall.ToBeDestroyed);
			Assert.True(friendlyLarge.ToBeDestroyed);
			Assert.True(enemyLarge.ToBeDestroyed);
			Assert.False(enemySmall.ToBeDestroyed);
		}

		[Fact]
		public void Renew_BT_252_ShouldHealAndDiscoverSpell()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.ProcessCard("Fireball", target, asZeroCost: true);

			game.ProcessCard("Renew", target, asZeroCost: true);

			Assert.Equal(3, target.Damage);
			Assert.NotNull(game.CurrentPlayer.Choice);
			Assert.All(game.CurrentPlayer.Choice.Choices, choice =>
				Assert.Equal(CardType.SPELL, game.IdEntityDic[choice].Card.Type));
			int handCount = game.CurrentPlayer.HandZone.Count;
			game.Process(ChooseTask.Pick(game.CurrentPlayer, game.CurrentPlayer.Choice.Choices[0]));
			Assert.Equal(handCount + 1, game.CurrentPlayer.HandZone.Count);
		}

		[Fact]
		public void NatalieSeline_EX1_198_ShouldDestroyMinionAndGainItsHealth()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			target.Damage = 1;
			game.EndTurn();

			Minion natalie = game.ProcessCard<Minion>("Natalie Seline", target, asZeroCost: true);

			Assert.True(target.ToBeDestroyed);
			Assert.Equal(5, natalie.Health);
		}

		[Fact]
		public void ReliquaryOfSouls_BT_197_ShouldShufflePrimeOnDeathrattle()
		{
			Game game = CreateGame();
			Minion reliquary = game.ProcessCard<Minion>("Reliquary of Souls", asZeroCost: true);
			int deckCount = game.CurrentPlayer.DeckZone.Count;

			game.ProcessCard("Fireball", reliquary, asZeroCost: true);

			Assert.Equal(deckCount + 1, game.CurrentPlayer.DeckZone.Count);
			Assert.Contains(game.CurrentPlayer.DeckZone, p => p.Card.Id == "BT_197t");
		}

		[Fact]
		public void SoulMirror_BT_198_ShouldSummonCopiesThatAttackOriginals()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Soul Mirror", asZeroCost: true);

			Minion copy = Assert.Single(game.CurrentPlayer.BoardZone.Where(p => p.Card.Name == "Chillwind Yeti"));
			Assert.Equal(4, copy.Damage);
			Assert.Equal(4, enemy.Damage);
		}

		[Fact]
		public void PsycheSplit_BT_253_ShouldBuffAndSummonCopy()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard("Psyche Split", target, asZeroCost: true);

			Assert.Equal(2, target.AttackDamage);
			Assert.Equal(3, target.Health);
			Assert.Equal(2, game.CurrentPlayer.BoardZone.Count(p => p.Card.Name == "Wisp" && p.AttackDamage == 2 && p.Health == 3));
		}

		[Fact]
		public void SethekkVeilweaver_BT_254_ShouldAddPriestSpellAfterSpellOnMinion()
		{
			Game game = CreateGame();
			game.ProcessCard("Sethekk Veilweaver", asZeroCost: true);
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			IPlayable powerInfusion = AddHandCard(game, "Power Infusion");

			game.ProcessCard(powerInfusion, target, asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.HandZone, p => p.Card.Class == CardClass.PRIEST && p.Card.Type == CardType.SPELL);
		}

		[Fact]
		public void DragonmawOverseer_BT_256_ShouldBuffAnotherFriendlyMinionAtEndOfTurn()
		{
			Game game = CreateGame();
			game.ProcessCard("Dragonmaw Overseer", asZeroCost: true);
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.EndTurn();

			Assert.Equal(3, target.AttackDamage);
			Assert.Equal(3, target.Health);
		}

		[Fact]
		public void Apotheosis_BT_257_ShouldBuffAndGiveLifesteal()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Wisp", asZeroCost: true);

			game.ProcessCard("Apotheosis", target, asZeroCost: true);

			Assert.Equal(3, target.AttackDamage);
			Assert.Equal(4, target.Health);
			Assert.Equal(1, target[GameTag.LIFESTEAL]);
		}

		[Fact]
		public void DragonmawSentinel_BT_262_ShouldGainAttackAndLifestealIfHoldingDragon()
		{
			Game game = CreateGame();
			AddHandCard(game, "Alexstrasza");

			Minion sentinel = game.ProcessCard<Minion>("Dragonmaw Sentinel", asZeroCost: true);

			Assert.Equal(2, sentinel.AttackDamage);
			Assert.Equal(1, sentinel[GameTag.LIFESTEAL]);
		}

		[Fact]
		public void SkeletalDragon_BT_341_ShouldAddDragonAtEndOfTurn()
		{
			Game game = CreateGame();
			game.ProcessCard("Skeletal Dragon", asZeroCost: true);

			game.EndTurn();

			Assert.Contains(game.CurrentOpponent.HandZone, p => p.Card.IsRace(Race.DRAGON));
		}
	}
}
