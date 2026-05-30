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
	public class OutlandDataPatch47374Test
	{
		private const GameTag GalakrondRelatedTag = (GameTag)676;

		private static Game CreateGame(CardClass playerClass = CardClass.WARRIOR, CardClass opponentClass = CardClass.MAGE)
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
		public void Build47374_ShouldUseRenamedCoreHeroPowerEntities()
		{
			Assert.Equal("Armor Up!", Cards.FromId("HERO_01bp").Name);
			Assert.Equal("Tank Up!", Cards.FromId("HERO_01bp2").Name);
			Assert.Equal("Demon Claws", Cards.FromId("HERO_10bp").Name);
			Assert.Equal("Demon's Bite", Cards.FromId("HERO_10bp2").Name);
			Assert.Equal(CardType.ENCHANTMENT, Cards.FromId("HERO_10bpe").Type);
			Assert.Equal(CardType.ENCHANTMENT, Cards.FromId("HERO_10pe2").Type);

			Assert.DoesNotContain(Cards.All, p => p.Id == "CS2_102");
			Assert.DoesNotContain(Cards.All, p => p.Id == "DS1h_292");
			Assert.DoesNotContain(Cards.All, p => p.Id == "HERO_10p");
		}

		[Fact]
		public void BasicHeroPowers_ShouldKeepWorkingAfterEntityRename()
		{
			Game warrior = CreateGame();
			Assert.Equal("HERO_01bp", warrior.CurrentPlayer.Hero.HeroPower.Card.Id);
			warrior.PlayHeroPower(asZeroCost: true);
			Assert.Equal(2, warrior.CurrentPlayer.Hero.Armor);

			Game demonHunter = CreateGame(CardClass.DEMONHUNTER);
			Assert.Equal("HERO_10bp", demonHunter.CurrentPlayer.Hero.HeroPower.Card.Id);
			demonHunter.PlayHeroPower(asZeroCost: true);
			Assert.Equal(1, demonHunter.CurrentPlayer.Hero.AttackDamage);
		}

		[Fact]
		public void JusticarTrueheart_ShouldUpgradeToRenamedHeroPowerEntities()
		{
			Game warrior = CreateGame();

			warrior.ProcessCard("Justicar Trueheart", asZeroCost: true);

			Assert.Equal("HERO_01bp2", warrior.CurrentPlayer.Hero.HeroPower.Card.Id);
			warrior.PlayHeroPower(asZeroCost: true);
			Assert.Equal(4, warrior.CurrentPlayer.Hero.Armor);
		}

		[Fact]
		public void TeronGorefiend_ShouldDestroyOnlyOtherFriendlyMinions()
		{
			Game game = CreateGame();
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion teron = game.ProcessCard<Minion>("Teron Gorefiend", asZeroCost: true);

			Assert.Contains(teron, game.CurrentPlayer.BoardZone);
			Assert.DoesNotContain(wisp, game.CurrentPlayer.BoardZone);
			Assert.Contains(wisp, game.CurrentPlayer.GraveyardZone);
		}

		[Theory]
		[InlineData("DRG_019")]
		[InlineData("DRG_021")]
		[InlineData("DRG_027")]
		[InlineData("DRG_050")]
		[InlineData("DRG_202")]
		[InlineData("DRG_203")]
		[InlineData("DRG_217")]
		[InlineData("DRG_218")]
		[InlineData("DRG_242")]
		[InlineData("DRG_248")]
		[InlineData("DRG_249")]
		[InlineData("DRG_250")]
		[InlineData("DRG_300")]
		[InlineData("DRG_303")]
		public void Build68600_ShouldKeepGalakrondRelatedTagOnInvokeCards(string cardId)
		{
			Assert.Equal(1, Cards.FromId(cardId)[GalakrondRelatedTag]);
		}
	}
}
