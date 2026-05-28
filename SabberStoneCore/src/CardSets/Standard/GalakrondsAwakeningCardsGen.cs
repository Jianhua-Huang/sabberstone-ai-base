using System.Collections.Generic;
using System.Linq;
using SabberStoneCore.Actions;
using SabberStoneCore.Auras;
using SabberStoneCore.Conditions;
using SabberStoneCore.Enchants;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.src.Loader;
using SabberStoneCore.Tasks;
using SabberStoneCore.Tasks.SimpleTasks;
using SabberStoneCore.Triggers;

namespace SabberStoneCore.CardSets.Standard
{
	public class GalakrondsAwakeningCardsGen
	{
		public static void AddAll(Dictionary<string, CardDef> cards)
		{
			Druid(cards);
			Hunter(cards);
			Mage(cards);
			Paladin(cards);
			Priest(cards);
			Rogue(cards);
			Shaman(cards);
			Warlock(cards);
			Warrior(cards);
			Neutral(cards);
			NonCollect(cards);
		}

		private static Card[] CollectibleCards(Game game) =>
			Cards.FormatTypeCards(game.FormatType).Where(p => p.Collectible).ToArray();

		private static void AddRandomCardToHand(Game game, Controller controller, IEntity source, IEnumerable<Card> cards)
		{
			Card[] pool = cards.Where(p => p != null).Distinct().ToArray();
			if (pool.Length == 0)
				return;

			Card pick = pool[game.Random.Next(pool.Length)];
			IPlayable entity = Entity.FromCard(controller, pick);
			entity[GameTag.DISPLAYED_CREATOR] = source.Id;
			Generic.AddHandPhase(controller, entity);
		}

		private static void CreateDiscover(Game game, Controller controller, IEntity source, IEnumerable<Card> cards,
			ChoiceAction action = ChoiceAction.HAND, ISimpleTask afterTask = null)
		{
			Card[] pool = cards.Where(p => p != null).Distinct().ToArray();
			if (pool.Length == 0)
				return;

			Card[] choices = DiscoverTask.GetChoices(new[] { pool }, 3, game.Random);
			Generic.CreateChoiceCards(controller, source, null, ChoiceType.GENERAL, action, choices, afterTask);
		}

		private static bool HasPlayedQuest(Controller controller) =>
			controller.SecretZone.Quest != null ||
			controller.GraveyardZone.Any(p => p.Card.IsQuest) ||
			controller.SetasideZone.Any(p => p.Card.IsQuest);

		private static void Druid(IDictionary<string, CardDef> cards)
		{
			cards.Add("YOD_001", new CardDef(new Power()));
			cards.Add("YOD_003", new CardDef());

			cards.Add("YOD_040", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (c.HandZone.Any(p => p.Card.Type == CardType.SPELL && p.Cost >= 5))
						c.Hero.Armor += 5;
				})
			}));
		}

		private static void Hunter(IDictionary<string, CardDef> cards)
		{
			cards.Add("YOD_004", new CardDef(new Power
			{
				Trigger = TriggerBuilder.Type(TriggerType.DEATH)
					.SetTask(new CustomTask((g, c, s, t, stack) =>
						AddRandomCardToHand(g, c, s, CollectibleCards(g).Where(p => p.IsRace(Race.MECHANICAL)))))
					.SetSource(TriggerSource.MINIONS)
					.SetCondition(SelfCondition.IsRace(Race.MECHANICAL))
					.GetTrigger()
			}));

			cards.Add("YOD_005", new CardDef(new Dictionary<PlayReq, int>
			{
				{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
				{ PlayReq.REQ_MINION_TARGET, 0 },
				{ PlayReq.REQ_FRIENDLY_TARGET, 0 },
				{ PlayReq.REQ_TARGET_WITH_RACE, (int)Race.BEAST }
			}, new Power
			{
				PowerTask = new AddEnchantmentTask("YOD_005e", EntityType.TARGET)
			}));

			cards.Add("YOD_036", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new ConditionTask(EntityType.SOURCE, SelfCondition.IsDragonInHand),
					new FlagTask(true, ComplexTask.DestroyRandomTargets(1, EntityType.OP_MINIONS)))
			}));
		}

		private static void Mage(IDictionary<string, CardDef> cards)
		{
			cards.Add("YOD_007", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (c.NumElementalsPlayedLastTurn > 0 && !c.BoardZone.IsFull)
						Entity.FromCard(c, s.Card, zone: c.BoardZone, creator: s);
				})
			}));

			cards.Add("YOD_008", new CardDef(new Power
			{
				Aura = new Aura(AuraType.HERO, new Effect(GameTag.HEROPOWER_DAMAGE, EffectOperator.ADD, 2))
			}));

			cards.Add("YOD_009", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion minion in c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).ToArray())
					{
						Generic.RemoveFromZone(minion.Controller, minion);
						minion.Controller.SetasideZone.Add(minion);
					}
				})
			}));
		}

		private static void Paladin(IDictionary<string, CardDef> cards)
		{
			cards.Add("YOD_010", new CardDef());

			cards.Add("YOD_012", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new SummonTask("CS2_101t", addToStack: true),
					new SummonTask("CS2_101t", addToStack: true),
					new SetGameTagTask(GameTag.TAUNT, 1, EntityType.STACK))
			}));

			cards.Add("YOD_043", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion minion in c.BoardZone.GetAll(p => p.IsRace(Race.MURLOC)))
						minion[GameTag.DIVINE_SHIELD] = 1;
				})
			}));
		}

		private static void Priest(IDictionary<string, CardDef> cards)
		{
			cards.Add("YOD_013", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (c.DragonInHand)
						CreateDiscover(g, c, s, c.DeckZone.Where(p => p.Card.Type == CardType.SPELL).Select(p => p.Card));
				})
			}));

			cards.Add("YOD_014", new CardDef(new Dictionary<PlayReq, int>
			{
				{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
				{ PlayReq.REQ_MINION_TARGET, 0 },
				{ PlayReq.REQ_ENEMY_TARGET, 0 }
			}, new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (t is ICharacter character)
						Generic.DamageCharFunc((IPlayable)s, character, character.AttackDamage, false);
				})
			}));

			cards.Add("YOD_015", new CardDef(new Power
			{
				PowerTask = new DiscoverTask(CardType.MINION, CardClass.INVALID,
					(GameTag.COST, RelaSign.EQ, 2), ChoiceAction.SUMMON,
					new AddEnchantmentTask("YOD_015e", EntityType.TARGET))
			}));
		}

		private static void Rogue(IDictionary<string, CardDef> cards)
		{
			cards.Add("YOD_016", new CardDef(new Power
			{
				PowerTask = new SetGameTagTask(GameTag.DEATHRATTLE, 1, EntityType.SOURCE),
				DeathrattleTask = new DrawTask()
			}));

			cards.Add("YOD_017", new CardDef(new Power
			{
				ComboTask = new CustomTask((g, c, s, t, stack) =>
				{
					for (int i = 0; i < c.NumCardsPlayedThisTurn - 1; i++)
						Generic.Draw(c);
				})
			}));

			cards.Add("YOD_018", new CardDef(new Power
			{
				PowerTask = new DiscoverTask(DiscoverType.BATTLECRY,
					new AddEnchantmentTask("YOD_018e", EntityType.TARGET))
			}));
		}

		private static void Shaman(IDictionary<string, CardDef> cards)
		{
			cards.Add("YOD_020", new CardDef(new Dictionary<PlayReq, int>
			{
				{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
				{ PlayReq.REQ_MINION_TARGET, 0 }
			}, new Power
			{
				PowerTask = new TransformMinionTask(EntityType.TARGET, 3)
			}));

			cards.Add("YOD_041", new CardDef(new Power
			{
				PowerTask = new SummonTask("YOD_041t", 3)
			}));

			cards.Add("YOD_042", new CardDef(new Power
			{
				Trigger = TriggerBuilder.Type(TriggerType.AFTER_CAST)
					.SetTask(new CustomTask((g, c, s, t, stack) =>
					{
						if (c.BoardZone.IsFull)
							return;

						int cost = t?[GameTag.TAG_LAST_KNOWN_COST_IN_HAND] ?? t?.Cost ?? 0;
						Card[] pool = CollectibleCards(g)
							.Where(p => p.Type == CardType.MINION && p.Rarity == Rarity.LEGENDARY && p.Cost == cost)
							.ToArray();
						if (pool.Length > 0)
							Entity.FromCard(c, pool[g.Random.Next(pool.Length)], zone: c.BoardZone, creator: s);

						new DamageWeaponTask(false).Process(g, c, s, t, stack);
					}))
					.SetSource(TriggerSource.FRIENDLY)
					.GetTrigger()
			}));
		}

		private static void Warlock(IDictionary<string, CardDef> cards)
		{
			cards.Add("YOD_025", new CardDef(new Power
			{
				PowerTask = new DiscoverTask(CardType.INVALID, CardClass.WARLOCK,
					choiceAction: ChoiceAction.HAND, repeat: 2)
			}));

			cards.Add("YOD_026", new CardDef(new Power
			{
				DeathrattleTask = new CustomTask((g, c, s, t, stack) =>
				{
					Minion[] minions = c.BoardZone.GetAll();
					if (minions.Length == 0)
						return;

					Minion target = minions[g.Random.Next(minions.Length)];
					Generic.AddEnchantmentBlock(g, Cards.FromId("Yod_026e"), (IPlayable)s, target,
						((Minion)s).AttackDamage, 0, 0);
				})
			}));

			cards.Add("YOD_027", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					IPlayable[] playableCards = c.Opponent.HandZone.Where(p => p.Card.Type != CardType.INVALID && p.Cost <= c.Opponent.RemainingMana).ToArray();
					if (playableCards.Length == 0)
						return;

					IPlayable card = playableCards[g.Random.Next(playableCards.Length)];
					Generic.AddEnchantmentBlock(g, Cards.FromId("YOD_027e"), (IPlayable)s, card, 0, 0, 0);
				})
			}));
		}

		private static void Warrior(IDictionary<string, CardDef> cards)
		{
			cards.Add("YOD_022", new CardDef(new Power
			{
				Trigger = TriggerBuilder.Type(TriggerType.AFTER_PLAY_MINION)
					.SetTask(new DamageTask(1, EntityType.ALLMINIONS))
					.SetSource(TriggerSource.FRIENDLY)
					.GetTrigger()
			}));

			cards.Add("YOD_023", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
					CreateDiscover(g, c, s, Cards.FormatTypeCards(g.FormatType).Where(p =>
						p[GameTag.MARK_OF_EVIL] == 1 ||
						(p.Collectible && (p.IsRace(Race.MECHANICAL) || p.IsRace(Race.DRAGON))))))
			}));

			cards.Add("YOD_024", new CardDef(new Power
			{
				Trigger = TriggerBuilder.Type(TriggerType.TAKE_DAMAGE)
					.SetTask(new SummonTask("GVG_110t"))
					.SetSource(TriggerSource.SELF)
					.GetTrigger()
			}));
		}

		private static void Neutral(IDictionary<string, CardDef> cards)
		{
			cards.Add("YOD_006", new CardDef(new Power
			{
				Trigger = TriggerBuilder.Type(TriggerType.AFTER_ATTACK)
					.SetTask(new TempManaTask(1))
					.SetSource(TriggerSource.SELF)
					.GetTrigger()
			}));

			cards.Add("YOD_028", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					IPlayable[] minions = c.DeckZone.Where(p => p.Card.Type == CardType.MINION && p.Cost == 1).ToArray();
					if (minions.Length == 0 || c.BoardZone.IsFull)
						return;

					IPlayable minion = minions[g.Random.Next(minions.Length)];
					Generic.RemoveFromZone(c, minion);
					Generic.SummonBlock(g, (Minion)minion, -1, s);
				})
			}));

			cards.Add("YOD_029", new CardDef(new Power
			{
				PowerTask = new SummonTask("YOD_029t", 2)
			}));

			cards.Add("YOD_030", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (c.SecretZone.Quest != null)
						new AddCardTo("GAME_005", EntityType.HAND).Process(g, c, s, t, stack);
				})
			}));

			cards.Add("YOD_032", new CardDef(new Power
			{
				Aura = new AdaptiveCostEffect(p => p.Controller.Opponent.Hero.DamageTakenThisTurn)
			}));

			cards.Add("YOD_033", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (IPlayable card in c.Opponent.HandZone.Where(p => p.Card[GameTag.BATTLECRY] == 1))
						Generic.AddEnchantmentBlock(g, Cards.FromId("YOD_033e"), (IPlayable)s, card, 0, 0, 0);
				})
			}));

			cards.Add("YOD_035", new CardDef(new Power
			{
				Trigger = TriggerBuilder.Type(TriggerType.AFTER_PLAY_CARD)
					.SetTask(new AddLackeyTask(1))
					.SetSource(TriggerSource.FRIENDLY)
					.SetCondition(SelfCondition.IsTagValue(GameTag.MARK_OF_EVIL, 1))
					.GetTrigger()
			}));

			cards.Add("YOD_038", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (HasPlayedQuest(c) && !c.BoardZone.IsFull)
						Entity.FromCard(c, Cards.FromId("YOD_038t"), zone: c.BoardZone, creator: s);
				})
			}));
		}

		private static void NonCollect(IDictionary<string, CardDef> cards)
		{
			cards.Add("YOD_001b", new CardDef(new Power { PowerTask = new DrawTask() }));
			cards.Add("YOD_001c", new CardDef(new Power { PowerTask = new SummonTask("YOD_001t") }));
			cards.Add("YOD_001ts", new CardDef(new Power()));
			cards.Add("YOD_005ts", new CardDef(new Power { PowerTask = new AddEnchantmentTask("YOD_005e", EntityType.TARGET) }));
			cards.Add("YOD_012ts", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new SummonTask("CS2_101t", addToStack: true),
					new SummonTask("CS2_101t", addToStack: true),
					new SetGameTagTask(GameTag.TAUNT, 1, EntityType.STACK))
			}));
			cards.Add("YOD_009h", new CardDef(new Power
			{
				Trigger = TriggerBuilder.Type(TriggerType.TURN_START)
					.SetTask(new CastRandomSpellTask())
					.GetTrigger()
			}));
			cards.Add("YOD_005e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(2))
			}));
			cards.Add("YOD_015e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Health_N(3))
			}));
			cards.Add("YOD_018e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.ReduceCost(2))
			}));
			cards.Add("Yod_026e", new CardDef(new Power
			{
				Enchant = Enchants.Enchants.AddAttackScriptTag
			}));
			cards.Add("YOD_027e", new CardDef(new Power
			{
				Enchant = new Enchant(),
				Trigger = TriggerBuilder.Type(TriggerType.TURN_END)
					.SetTask(new CustomTask((g, c, s, t, stack) =>
					{
						if (s is Enchantment enchantment &&
						    enchantment.Target is IPlayable playable &&
						    playable.Zone == playable.Controller.HandZone &&
						    g.CurrentPlayer == playable.Controller)
							Generic.DiscardBlock(playable.Controller, playable);
					}))
					.SetSource(TriggerSource.ALL)
					.GetTrigger()
			}));
			cards.Add("YOD_029t", new CardDef(new Power
			{
				Trigger = TriggerBuilder.Type(TriggerType.DEAL_DAMAGE)
					.SetTask(new SetGameTagTask(GameTag.FROZEN, 1, EntityType.EVENT_TARGET))
					.SetSource(TriggerSource.SELF)
					.GetTrigger()
			}));
			cards.Add("YOD_033e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AddCost(5))
				{
					IsOneTurnEffect = true
				}
			}));
			cards.Add("YOD_041t", new CardDef());
			cards.Add("YOD_038t", new CardDef());
		}
	}
}
