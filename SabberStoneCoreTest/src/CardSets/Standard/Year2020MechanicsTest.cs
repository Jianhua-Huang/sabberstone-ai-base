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
	public class Year2020MechanicsTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null)
		{
			var deck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.HUNTER,
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
		public void Corrupt_ShouldTransformLowerCostCorruptCardInHand()
		{
			Game game = CreateGame();
			IPlayable cobra = Generic.DrawCard(game.Player1, Cards.FromId("DMF_083"));

			game.ProcessCard("Chillwind Yeti");

			Assert.Equal("DMF_083t", game.Player1.HandZone[0].Card.Id);
			Assert.Equal(cobra.Id, game.Player1.HandZone[0].Id);
			Assert.Equal(1, game.Player1.HandZone[0][GameTag.POISONOUS]);
			Assert.Equal(1, game.Player1.HandZone[0].Card[GameTag.CORRUPTEDCARD]);
		}

		[Theory]
		[InlineData("DMF_064", "DMF_064t", 8, 8, 1, 0, 1, 0, 0)]
		[InlineData("DMF_073", "DMF_073t", 3, 2, 0, 1, 1, 0, 0)]
		[InlineData("DMF_080", "DMF_080t", 8, 8, 0, 1, 0, 0, 0)]
		[InlineData("DMF_083", "DMF_083t", 1, 5, 0, 0, 0, 1, 0)]
		[InlineData("DMF_184", "DMF_184t", 4, 7, 1, 0, 0, 0, 0)]
		[InlineData("DMF_517", "DMF_517a", 5, 2, 0, 0, 0, 0, 1)]
		public void Corrupt_ShouldApplyStaticCorruptedMinionKeywords(
			string originalId,
			string corruptedId,
			int attack,
			int health,
			int taunt,
			int rush,
			int divineShield,
			int poisonous,
			int stealth)
		{
			Game game = CreateGame();
			Generic.DrawCard(game.Player1, Cards.FromId(originalId));

			game.ProcessCard("Boulderfist Ogre");

			IPlayable corrupted = game.Player1.HandZone[0];
			Assert.Equal(corruptedId, corrupted.Card.Id);
			Assert.Equal(attack, corrupted.Card[GameTag.ATK]);
			Assert.Equal(health, corrupted.Card[GameTag.HEALTH]);
			Assert.Equal(taunt, corrupted.Card[GameTag.TAUNT]);
			Assert.Equal(rush, corrupted.Card[GameTag.RUSH]);
			Assert.Equal(divineShield, corrupted.Card[GameTag.DIVINE_SHIELD]);
			Assert.Equal(poisonous, corrupted.Card[GameTag.POISONOUS]);
			Assert.Equal(stealth, corrupted.Card[GameTag.STEALTH]);
		}

		[Fact]
		public void Spellburst_ShouldTriggerOnceAfterFriendlySpell()
		{
			Game game = CreateGame();
			Minion initiate = game.ProcessCard<Minion>("Intrepid Initiate");

			game.ProcessCard("Moonfire", game.Player2.Hero);
			game.ProcessCard("Moonfire", game.Player2.Hero);

			Assert.Equal(3, initiate.AttackDamage);
		}
	}
}
