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
	}
}
