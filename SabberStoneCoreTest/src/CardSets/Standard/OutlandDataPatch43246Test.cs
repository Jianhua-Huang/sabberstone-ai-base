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
	public class OutlandDataPatch43246Test
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

		private static IPlayable AddToHand(Controller controller, string cardId) =>
			Generic.DrawCard(controller, Cards.FromId(cardId));

		[Fact]
		public void Build43246_ShouldLoadTraditionalOutlandCards()
		{
			Card kaelthas = Cards.FromId("BT_255");
			Card enchantment = Cards.FromId("BT_255e");

			Assert.Equal("Kael'thas Sunstrider", kaelthas.Name);
			Assert.Equal(CardSet.BLACK_TEMPLE, kaelthas.Set);
			Assert.Equal(CardType.MINION, kaelthas.Type);
			Assert.True(kaelthas.Collectible);
			Assert.Equal(7, kaelthas.Cost);
			Assert.Equal(4, kaelthas[GameTag.ATK]);
			Assert.Equal(7, kaelthas[GameTag.HEALTH]);
			Assert.Equal(CardType.ENCHANTMENT, enchantment.Type);
			Assert.Contains(kaelthas, Cards.AllStandard);
		}

		[Fact]
		public void Build43246_ShouldApplyTraditionalBalanceChanges()
		{
			Assert.Contains("(0)", Cards.FromId("CFM_020").Text);
			Assert.Contains("(0)", Cards.FromId("CFM_020e").Text);
			Assert.Equal(2, Cards.FromId("DAL_433")[GameTag.ATK]);
			Assert.Equal(5, Cards.FromId("LOOT_080").Cost);
			Assert.Equal(5, Cards.FromId("LOOT_080t2").Cost);
			Assert.Equal(5, Cards.FromId("LOOT_080t3").Cost);
			Assert.Equal(6, Cards.FromId("LOOT_539").Cost);
			Assert.Equal(8, Cards.FromId("OG_211").Cost);
			Assert.Equal(0, Cards.FromId("DRG_099t1")[GameTag.ImmuneToSpellpower]);
		}

		[Fact]
		public void RazaTheChained_ShouldSetHeroPowerCostToZero()
		{
			Game game = CreateGame(CardClass.PRIEST);

			game.ProcessCard("Raza the Chained", asZeroCost: true);

			Assert.Equal(0, game.CurrentPlayer.Hero.HeroPower.Cost);
		}

		[Fact]
		public void KaelthasSunstrider_ShouldDiscountEveryThirdSpellEachTurn()
		{
			Game game = CreateGame(CardClass.MAGE);
			game.ProcessCard("Kael'thas Sunstrider", asZeroCost: true);
			IPlayable coin1 = AddToHand(game.CurrentPlayer, "GAME_005");
			IPlayable coin2 = AddToHand(game.CurrentPlayer, "GAME_005");
			IPlayable fireball = AddToHand(game.CurrentPlayer, "CS2_029");

			game.ProcessCard(coin1);
			Assert.Equal(4, fireball.Cost);

			game.ProcessCard(coin2);

			Assert.Equal(0, fireball.Cost);
			game.ProcessCard(fireball, game.CurrentOpponent.Hero);
			Assert.Equal(6, game.CurrentOpponent.Hero.Damage);

			IPlayable coin3 = AddToHand(game.CurrentPlayer, "GAME_005");
			IPlayable coin4 = AddToHand(game.CurrentPlayer, "GAME_005");
			IPlayable frostbolt = AddToHand(game.CurrentPlayer, "CS2_024");

			game.ProcessCard(coin3);
			Assert.Equal(2, frostbolt.Cost);

			game.ProcessCard(coin4);
			Assert.Equal(0, frostbolt.Cost);
		}
	}
}
