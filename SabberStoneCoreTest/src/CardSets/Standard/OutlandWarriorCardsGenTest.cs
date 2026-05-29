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
	public class OutlandWarriorCardsGenTest
	{
		private static Game CreateGame(IEnumerable<Card> playerDeck = null, IEnumerable<Card> opponentDeck = null)
		{
			var deck = Enumerable.Repeat(Cards.FromName("Wisp"), 30).ToList();
			var game = new Game(new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.WARRIOR,
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
		public void Bladestorm_BT_117_ShouldRepeatUntilAMinionDies()
		{
			Game game = CreateGame();
			Minion wisp = game.ProcessCard<Minion>("Wisp", asZeroCost: true);
			Minion yeti = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);

			game.ProcessCard("Bladestorm", asZeroCost: true);

			Assert.True(wisp.ToBeDestroyed || !game.CurrentPlayer.BoardZone.Contains(wisp));
			Assert.Equal(1, yeti.Damage);
		}

		[Fact]
		public void WarmaulChallenger_BT_120_ShouldBattleEnemyMinionToDeath()
		{
			Game game = CreateGame();
			game.EndTurn();
			Minion target = game.ProcessCard<Minion>("River Crocolisk", asZeroCost: true);
			game.EndTurn();

			Minion challenger = game.ProcessCard<Minion>("Warmaul Challenger", target, asZeroCost: true);

			Assert.True(target.ToBeDestroyed || !game.CurrentOpponent.BoardZone.Contains(target));
			Assert.Equal(6, challenger.Damage);
		}

		[Fact]
		public void ImprisonedGanarg_BT_121_ShouldAwakenAndEquipFieryWarAxe()
		{
			Game game = CreateGame();

			Minion ganarg = game.ProcessCard<Minion>("Imprisoned Gan'arg", asZeroCost: true);

			Assert.True(ganarg.Untouchable);
			Assert.Equal(0, game.CurrentPlayer.BoardZone.CountExceptUntouchables);
			AdvanceTwoOwnerTurns(game);

			Assert.False(ganarg.Untouchable);
			Assert.Equal(1, game.CurrentPlayer.BoardZone.CountExceptUntouchables);
			Assert.NotNull(game.CurrentPlayer.Hero.Weapon);
			Assert.Equal(3, game.CurrentPlayer.Hero.Weapon.AttackDamage);
			Assert.Equal(2, game.CurrentPlayer.Hero.Weapon.Durability);
		}

		[Fact]
		public void KargathBladefist_BT_123_ShouldShufflePrimeOnDeathrattle()
		{
			Game game = CreateGame();
			Minion kargath = game.ProcessCard<Minion>("Kargath Bladefist", asZeroCost: true);
			int deckCount = game.CurrentPlayer.DeckZone.Count;

			game.ProcessCard("Fireball", kargath, asZeroCost: true);

			Assert.Equal(deckCount + 1, game.CurrentPlayer.DeckZone.Count);
			Assert.Contains(game.CurrentPlayer.DeckZone, p => p.Card.Id == "BT_123t");
		}

		[Fact]
		public void CorsairCache_BT_124_ShouldDrawWeaponAndBuffIt()
		{
			Game game = CreateGame();
			SetDeck(game, "Fiery War Axe", "Wisp");

			game.ProcessCard("Corsair Cache", asZeroCost: true);

			Weapon weapon = Assert.IsType<Weapon>(game.CurrentPlayer.HandZone.Single(p => p.Card.Name == "Fiery War Axe"));
			Assert.Equal(4, weapon.AttackDamage);
			Assert.Equal(3, weapon.Durability);
			Assert.Single(game.CurrentPlayer.DeckZone);
		}

		[Fact]
		public void BloodboilBrute_BT_138_ShouldCostLessForEachDamagedMinion()
		{
			Game game = CreateGame();
			Minion first = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			Minion second = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			IPlayable brute = AddHandCard(game, "Bloodboil Brute");

			game.ProcessCard("Moonfire", first, asZeroCost: true);
			game.ProcessCard("Moonfire", second, asZeroCost: true);

			Assert.Equal(5, brute.Cost);
		}

		[Fact]
		public void BonechewerRaider_BT_140_ShouldGainStatsAndRushIfAnyMinionDamaged()
		{
			Game game = CreateGame();
			Minion damaged = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);
			game.ProcessCard("Moonfire", damaged, asZeroCost: true);

			Minion raider = game.ProcessCard<Minion>("Bonechewer Raider", asZeroCost: true);

			Assert.Equal(4, raider.AttackDamage);
			Assert.Equal(4, raider.Health);
			Assert.Equal(1, raider[GameTag.RUSH]);
			Assert.False(raider.IsExhausted);
		}

		[Fact]
		public void SwordAndBoard_BT_233_ShouldDamageMinionAndGainArmor()
		{
			Game game = CreateGame();
			Minion target = game.ProcessCard<Minion>("Chillwind Yeti", asZeroCost: true);

			game.ProcessCard("Sword and Board", target, asZeroCost: true);

			Assert.Equal(2, target.Damage);
			Assert.Equal(2, game.CurrentPlayer.Hero.Armor);
		}

		[Fact]
		public void ScrapGolem_BT_249_ShouldGainArmorEqualToAttackOnDeathrattle()
		{
			Game game = CreateGame();
			Minion golem = game.ProcessCard<Minion>("Scrap Golem", asZeroCost: true);

			game.ProcessCard("Fireball", golem, asZeroCost: true);
			game.ProcessCard("Moonfire", golem, asZeroCost: true);

			Assert.Equal(4, game.CurrentPlayer.Hero.Armor);
		}
	}
}
