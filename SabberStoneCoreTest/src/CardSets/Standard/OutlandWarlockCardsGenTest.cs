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
	public class OutlandWarlockCardsGenTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null)
		{
			var deck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.WARLOCK,
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

		private static IPlayable AddHandCard(Game game, string cardName)
		{
			return Generic.DrawCard(game.CurrentPlayer, Cards.FromName(cardName));
		}

		private static void SetDeck(Game game, params string[] cardNames)
		{
			EmptyZone(game.CurrentPlayer.DeckZone.GetAll());
			foreach (string cardName in cardNames)
				game.CurrentPlayer.DeckZone.Add(Entity.FromCard(game.CurrentPlayer, Cards.FromName(cardName)));
		}

		private static void AdvanceTwoOwnerTurns(Game game)
		{
			game.EndTurn();
			game.EndTurn();
			game.EndTurn();
			game.EndTurn();
		}

		[Fact]
		public void KelidanTheBreaker_BT_196_ShouldDestroyTargetAndAllOtherMinionsIfDrawnThisTurn()
		{
			Game game = CreateGame();
			SetDeck(game, "Keli'dan the Breaker");
			Minion friendly = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			Minion otherEnemy = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();
			IPlayable kelidan = Assert.Single(game.CurrentPlayer.HandZone.Where(p => p.Card.Id == "BT_196"));

			game.ProcessCard(kelidan, target, asZeroCost: true);

			Assert.True(target.ToBeDestroyed);
			Assert.True(friendly.ToBeDestroyed);
			Assert.True(otherEnemy.ToBeDestroyed);
			Assert.Contains(game.CurrentPlayer.BoardZone, p => p.Card.Id == "BT_196");
		}

		[Fact]
		public void UnstableFelbolt_BT_199_ShouldDamageEnemyMinionAndFriendlyMinion()
		{
			Game game = CreateGame();
			Minion friendly = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();
			Minion enemy = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.EndTurn();

			game.ProcessCard("Unstable Felbolt", enemy, asZeroCost: true);

			Assert.Equal(3, enemy.Damage);
			Assert.Equal(3, friendly.Damage);
		}

		[Fact]
		public void HandOfGuldan_BT_300_ShouldDrawThreeWhenPlayedOrDiscarded()
		{
			Game game = CreateGame();
			SetDeck(game, "Wisp", "River Crocolisk", "Chillwind Yeti");

			game.ProcessCard("Hand of Gul'dan", asZeroCost: true);

			Assert.Equal(3, game.CurrentPlayer.HandZone.Count);

			game = CreateGame();
			SetDeck(game, "Wisp", "River Crocolisk", "Chillwind Yeti");
			AddHandCard(game, "Hand of Gul'dan");

			game.ProcessCard("Nightshade Matron", asZeroCost: true);

			Assert.Contains(game.CurrentPlayer.DiscardedEntities, id => game.IdEntityDic[id].Card.Id == "BT_300");
			Assert.Equal(3, game.CurrentPlayer.HandZone.Count);
		}

		[Fact]
		public void TheDarkPortal_BT_302_ShouldDrawMinionAndReduceIfHandHasEightCards()
		{
			Game game = CreateGame();
			SetDeck(game, "River Crocolisk", "Fireball");
			for (int i = 0; i < 8; i++)
				AddHandCard(game, "Wisp");
			IPlayable portal = AddHandCard(game, "The Dark Portal");

			game.ProcessCard(portal, asZeroCost: true);

			Minion crocolisk = Assert.IsType<Minion>(game.CurrentPlayer.HandZone.Single(p => p.Card.Name == "River Crocolisk"));
			Assert.Equal(0, crocolisk.Cost);
		}

		[Fact]
		public void EnhancedDreadlord_BT_304_ShouldSummonLifestealDreadlordOnDeathrattle()
		{
			Game game = CreateGame();
			Minion dreadlord = game.ProcessCard<Minion>("Enhanced Dreadlord", asZeroCost: true);

			game.ProcessCard("Fireball", dreadlord, asZeroCost: true);
			game.ProcessCard("Moonfire", dreadlord, asZeroCost: true);

			Minion token = Assert.Single(game.CurrentPlayer.BoardZone.Where(p => p.Card.Id == "BT_304t"));
			Assert.Equal(5, token.AttackDamage);
			Assert.Equal(5, token.Health);
			Assert.Equal(1, token[GameTag.LIFESTEAL]);
		}

		[Fact]
		public void ImprisonedScrapImp_BT_305_ShouldAwakenAndBuffMinionsInHand()
		{
			Game game = CreateGame();
			Minion handMinion = (Minion)AddHandCard(game, "Wisp");

			Minion scrapImp = game.ProcessCard<Minion>("Imprisoned Scrap Imp", asZeroCost: true);

			Assert.True(scrapImp.Untouchable);
			AdvanceTwoOwnerTurns(game);

			Assert.False(scrapImp.Untouchable);
			Assert.Equal(3, handMinion.AttackDamage);
			Assert.Equal(3, handMinion.Health);
		}

		[Fact]
		public void ShadowCouncil_BT_306_ShouldReplaceHandWithBuffedDemons()
		{
			Game game = CreateGame();
			AddHandCard(game, "Wisp");
			AddHandCard(game, "River Crocolisk");

			game.ProcessCard("Shadow Council", asZeroCost: true);

			Assert.Equal(2, game.CurrentPlayer.HandZone.Count);
			Assert.All(game.CurrentPlayer.HandZone, p =>
			{
				Assert.True(p.Card.IsRace(Race.DEMON));
				Assert.True(((Minion)p).AttackDamage >= p.Card[GameTag.ATK] + 2);
				Assert.True(((Minion)p).Health >= p.Card[GameTag.HEALTH] + 2);
			});
		}

		[Fact]
		public void Darkglare_BT_307_ShouldRefreshManaAfterHeroTakesDamage()
		{
			Game game = CreateGame();
			game.ProcessCard("Darkglare", asZeroCost: true);
			game.CurrentPlayer.UsedMana = 5;

			game.ProcessCard("Moonfire", game.CurrentPlayer.Hero, asZeroCost: true);

			Assert.Equal(3, game.CurrentPlayer.UsedMana);
		}

		[Fact]
		public void KanrethadEbonlocke_BT_309_ShouldReduceDemonsAndShufflePrimeOnDeathrattle()
		{
			Game game = CreateGame();
			IPlayable demon = AddHandCard(game, "Flame Imp");

			Minion kanrethad = game.ProcessCard<Minion>("Kanrethad Ebonlocke", asZeroCost: true);

			Assert.Equal(0, demon.Cost);
			game.ProcessCard("Fireball", kanrethad, asZeroCost: true);
			Assert.Contains(game.CurrentPlayer.DeckZone, p => p.Card.Id == "BT_309t");
		}
	}
}
