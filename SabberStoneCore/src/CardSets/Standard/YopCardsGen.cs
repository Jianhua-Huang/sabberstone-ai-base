using System.Linq;
using SabberStoneCore.Actions;
using SabberStoneCore.Auras;
using SabberStoneCore.Conditions;
using SabberStoneCore.Enchants;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks;
using SabberStoneCore.Tasks.SimpleTasks;
using SabberStoneCore.Triggers;
using SabberStoneCore.src.Loader;

namespace SabberStoneCore.CardSets.Standard
{
	public class YopCardsGen
	{
		public static void AddAll(System.Collections.Generic.Dictionary<string, CardDef> cards)
		{
			DemonHunter(cards);
			Druid(cards);
			Hunter(cards);
			Mage(cards);
			Neutral(cards);
			Paladin(cards);
			Priest(cards);
			Rogue(cards);
			Shaman(cards);
			Warlock(cards);
			Warrior(cards);
			NonCollect(cards);
		}

		private static void DemonHunter(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [YOP_002] Felsaber - Can only attack if your hero attacked this turn.
			cards.Add("YOP_002", new CardDef(new Power
			{
				Aura = new AdaptiveEffect(SelfCondition.HasMyHeroNotAttackedThisTurn, GameTag.CANT_ATTACK)
			}));

			// [YOP_030] Felfire Deadeye - Your Hero Power costs (1) less.
			cards.Add("YOP_030", new CardDef(new Power
			{
				Aura = new Aura(AuraType.HEROPOWER, Effects.ReduceCost(1))
			}));
		}

		private static void Druid(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [YOP_025t] Dreaming Drake - Corrupted. Taunt. Gain +2/+2.
			cards.Add("YOP_025t", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (s is Minion minion && minion.AttackDamage == 3 && minion.Health == 4)
					{
						minion.AttackDamage = 5;
						minion.Health = 6;
					}
				})
			}));

			// [YOP_026] Arbor Up - Summon two 2/2 Treants. Give your minions +2/+1.
			cards.Add("YOP_026", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new SummonTask("EX1_158t", 2, SummonSide.SPELL),
					new AddEnchantmentTask("YOP_026e", EntityType.MINIONS))
			}));
		}

		private static void Hunter(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [YOP_027] Bola Shot - Deal 1 damage to a minion and 2 damage to its neighbors.
			cards.Add("YOP_027", new CardDef(new System.Collections.Generic.Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(t is Minion target))
						return;

					foreach (Minion adjacent in target.GetAdjacentMinions().ToArray())
						Generic.DamageCharFunc.Invoke(s as IPlayable, adjacent, 2, true);

					Generic.DamageCharFunc.Invoke(s as IPlayable, target, 1, true);
				})
			}));

			// [YOP_028] Saddlemaster - After you play a Beast, add a random Beast to your hand.
			cards.Add("YOP_028", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_PLAY_MINION)
				{
					TriggerSource = TriggerSource.FRIENDLY,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(t is Minion played) || !played.Card.IsRace(Race.BEAST))
							return;

						AddRandomMinionToHand(g, c, s, card => card.IsRace(Race.BEAST));
					})
				}
			}));
		}

		private static void Mage(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [YOP_019] Conjure Mana Biscuit - Add a Biscuit to your hand that refreshes 2 Mana Crystals.
			cards.Add("YOP_019", new CardDef(new Power
			{
				PowerTask = new AddCardTo("YOP_019t", EntityType.HAND)
			}));

			// [YOP_020] Glacier Racer - Spellburst: Deal 3 damage to all Frozen enemies.
			cards.Add("YOP_020", new CardDef(new Power
			{
				Trigger = Spellburst(new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion minion in c.Opponent.BoardZone.GetAll().Where(p => p.IsFrozen).ToArray())
						Generic.DamageCharFunc.Invoke(s as IPlayable, minion, 3, false);

					if (c.Opponent.Hero.IsFrozen)
						Generic.DamageCharFunc.Invoke(s as IPlayable, c.Opponent.Hero, 3, false);
				}))
			}));

			// [YOP_021] Imprisoned Phoenix - Dormant for 2 turns. Spell Damage +2.
			cards.Add("YOP_021", new CardDef(new Power
			{
				PowerTask = StartDormant(2, true),
				Trigger = DormantAwakenTrigger(new CustomTask((g, c, s, t, stack) =>
				{
					if (s is Minion minion)
						minion.SpellPower = minion.Card.SpellPower;
				}))
			}));
		}

		private static void Neutral(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [YOP_031] Crabrider - Rush. Battlecry: Gain Windfury this turn only.
			cards.Add("YOP_031", new CardDef(new Power
			{
				PowerTask = new SetGameTagTask(GameTag.WINDFURY, 1, EntityType.SOURCE),
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = new SetGameTagTask(GameTag.WINDFURY, 0, EntityType.SOURCE),
					RemoveAfterTriggered = true
				}
			}));

			// [YOP_005] Barricade - Summon a 2/4 Guard with Taunt. If it's your only minion, summon another.
			cards.Add("YOP_005", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					bool emptyBoard = c.BoardZone.IsEmpty;
					new SummonTask("YOP_005t", SummonSide.SPELL).Process(g, c, s, t, stack);
					if (emptyBoard)
						new SummonTask("YOP_005t", SummonSide.SPELL).Process(g, c, s, t, stack);
				})
			}));

			// [YOP_012] Deathwarden - Deathrattles can't trigger.
			// Deathrattle suppression is applied from Game.DeathProcessingAndAuraUpdate while this minion is active.
			cards.Add("YOP_012", new CardDef(new Power()));

			// [YOP_032] Armor Vendor - Battlecry: Give 4 Armor to each hero.
			cards.Add("YOP_032", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new ArmorTask(4),
					new ArmorTask(4, true))
			}));

			// [YOP_034] Runaway Blackwing - At the end of your turn, deal 9 damage to a random enemy minion.
			cards.Add("YOP_034", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = ComplexTask.DamageRandomTargets(1, EntityType.OP_MINIONS, 9)
				}
			}));

			// [YOP_035] Moonfang - Can only take 1 damage at a time.
			cards.Add("YOP_035", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.PREDAMAGE)
				{
					TriggerSource = TriggerSource.SELF,
					FastExecution = true,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (g.CurrentEventData.EventNumber > 1)
							g.CurrentEventData.EventNumber = 1;
					})
				}
			}));
		}

		private static void Paladin(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [YOP_010] Imprisoned Celestial - Dormant for 2 turns. Spellburst: Give your minions Divine Shield.
			cards.Add("YOP_010", new CardDef(new Power
			{
				PowerTask = StartDormant(2),
				Trigger = new MultiTrigger(
					DormantAwakenTrigger(null),
					SpellburstAfterAwake(new CustomTask((g, c, s, t, stack) =>
					{
						foreach (Minion minion in c.BoardZone.GetAll().ToArray())
							minion.HasDivineShield = true;
					})))
			}));
		}

		private static void Priest(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [YOP_007] Dark Inquisitor Xanesh - Battlecry: Reduce the Cost of all Corrupt cards in your hand and deck by (2).
			cards.Add("YOP_007", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (IPlayable playable in c.HandZone.GetAll().Concat(c.DeckZone.GetAll()).Where(p => IsCorruptCardDefinition(p.Card)).ToArray())
						Generic.AddEnchantmentBlock(g, Cards.FromId("YOP_007e"), s as IPlayable, playable, 0, 0, 0);
				})
			}));

			// [YOP_009] Rally! - Resurrect a friendly 1-Cost, 2-Cost, and 3-Cost minion.
			cards.Add("YOP_009", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (int cost in new[] {1, 2, 3})
					{
						if (c.BoardZone.IsFull)
							return;

						IPlayable deadMinion = c.GraveyardZone
							.Where(p => p.Card.Type == CardType.MINION && p.Card.Cost == cost)
							.OrderBy(p => p.Id)
							.FirstOrDefault();

						if (deadMinion == null)
							continue;

						Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, deadMinion.Card), -1, s);
					}
				})
			}));
		}

		private static void Rogue(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [YOP_015] Nitroboost Poison - Give a minion +2 Attack. Corrupt: And your weapon.
			cards.Add("YOP_015", new CardDef(new System.Collections.Generic.Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = new AddEnchantmentTask("YOP_015e", EntityType.TARGET)
			}));

			// [YOP_015t] Nitroboost Poison - Corrupted. Give a minion and your weapon +2 Attack.
			cards.Add("YOP_015t", new CardDef(new System.Collections.Generic.Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = ComplexTask.Create(
					new AddEnchantmentTask("YOP_015e", EntityType.TARGET),
					new CustomTask((g, c, s, t, stack) =>
					{
						if (c.Hero.Weapon != null)
							Generic.AddEnchantmentBlock(g, Cards.FromId("YOP_015e"), s as IPlayable, c.Hero.Weapon, 0, 0, 0);
					}))
			}));
		}

		private static void Shaman(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [YOP_023] Landslide - Deal 1 damage to all enemy minions. If you're Overloaded, deal 1 damage again.
			cards.Add("YOP_023", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion minion in c.Opponent.BoardZone.GetAll().ToArray())
						Generic.DamageCharFunc.Invoke(s as IPlayable, minion, 1, true);

					if (c.OverloadLocked <= 0 && c.OverloadOwed <= 0)
						return;

					foreach (Minion minion in c.Opponent.BoardZone.GetAll().ToArray())
						Generic.DamageCharFunc.Invoke(s as IPlayable, minion, 1, true);
				})
			}));

			// [YOP_022] Mistrunner - Battlecry: Give a friendly minion +3/+3. Overload: (1)
			cards.Add("YOP_022", new CardDef(new System.Collections.Generic.Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_FOR_COMBO, 0},
				{PlayReq.REQ_MINION_TARGET, 0},
				{PlayReq.REQ_FRIENDLY_TARGET, 0}
			}, new Power
			{
				PowerTask = new AddEnchantmentTask("YOP_022e", EntityType.TARGET)
			}));
		}

		private static void Warlock(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [YOP_004] Envoy Rustwix - Deathrattle: Shuffle 3 random Prime Legendary minions into your deck.
			cards.Add("YOP_004", new CardDef(new Power
			{
				DeathrattleTask = new CustomTask((g, c, s, t, stack) =>
				{
					for (int i = 0; i < 3; i++)
					{
						if (c.DeckZone.IsFull)
							return;

						Card prime = PrimeLegendaryMinionCards.Choose(g.Random);
						IPlayable entity = Entity.FromCard(c, prime);
						entity[GameTag.DISPLAYED_CREATOR] = s.Id;
						Generic.ShuffleIntoDeck.Invoke(c, s, entity);
					}
				})
			}));

			// [YOP_003] Luckysoul Hoarder - Battlecry: Shuffle 2 Soul Fragments into your deck. Corrupt: Draw a card.
			cards.Add("YOP_003", new CardDef(new Power
			{
				PowerTask = new AddCardTo("SCH_307t", EntityType.DECK, 2)
			}));

			// [YOP_003t] Luckysoul Hoarder - Corrupted. Battlecry: Draw a card.
			cards.Add("YOP_003t", new CardDef(new Power
			{
				PowerTask = new DrawTask()
			}));

			// [YOP_033] Backfire - Draw 3 cards. Deal 3 damage to your hero.
			cards.Add("YOP_033", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new DrawTask(3),
					new DamageTask(3, EntityType.HERO))
			}));
		}

		private static void Warrior(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [YOP_013] Spiked Wheel - Has +3 Attack while your hero has Armor.
			cards.Add("YOP_013", new CardDef(new Power
			{
				Aura = new Aura(AuraType.WEAPON, "YOP_013e")
				{
					Condition = new SelfCondition(p => p.Controller.Hero.Armor > 0),
					Restless = true
				}
			}));

			// [YOP_014] Ironclad - Battlecry: If your hero has Armor, gain +2/+2.
			cards.Add("YOP_014", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (c.Hero.Armor > 0)
						Generic.AddEnchantmentBlock(g, Cards.FromId("YOP_014e"), s as IPlayable, s, 0, 0, 0);
				})
			}));
		}

		private static void NonCollect(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [YOP_019t] Mana Biscuit - Refresh 2 Mana Crystals.
			cards.Add("YOP_019t", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					c.UsedMana = System.Math.Max(0, c.UsedMana - 2);
				})
			}));

			// [YOP_022e] Windstrider - +3/+3.
			cards.Add("YOP_022e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(3), Effects.Health_N(3))
			}));

			// [YOP_026e] Arbor Up - +2/+1.
			cards.Add("YOP_026e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(2), Effects.Health_N(1))
			}));

			// [YOP_014e] Reinforced - +2/+2.
			cards.Add("YOP_014e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(2))
			}));

			// [YOP_013e] Spiked Wheel - +3 Attack.
			cards.Add("YOP_013e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(3))
			}));

			// [YOP_015e] Nitroboost Poison - +2 Attack.
			cards.Add("YOP_015e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(2))
			}));

			// [YOP_007e] Inquisitor's Teachings - Costs (2) less.
			cards.Add("YOP_007e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.ReduceCost(2))
			}));

		}

		private static Trigger Spellburst(ISimpleTask task)
		{
			return new Trigger(TriggerType.AFTER_CAST)
			{
				TriggerSource = TriggerSource.FRIENDLY,
				SingleTask = task,
				RemoveAfterTriggered = true
			};
		}

		private static ISimpleTask StartDormant(int turns, bool suppressSpellPower = false)
		{
			return new CustomTask((g, c, s, t, stack) =>
			{
				if (!(s is Minion minion))
					return;

				minion[GameTag.DORMANT] = turns;
				minion[GameTag.UNTOUCHABLE] = 1;
				minion.IsExhausted = true;
				if (suppressSpellPower)
					minion.SpellPower = 0;
			});
		}

		private static Trigger DormantAwakenTrigger(ISimpleTask awakenTask)
		{
			return new Trigger(TriggerType.TURN_START)
			{
				SingleTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(s is Minion minion) || minion[GameTag.DORMANT] <= 0)
						return;

					int turnsLeft = minion[GameTag.DORMANT] - 1;
					if (turnsLeft > 0)
					{
						minion[GameTag.DORMANT] = turnsLeft;
						return;
					}

					minion[GameTag.DORMANT] = 0;
					minion[GameTag.UNTOUCHABLE] = 0;
					minion.IsExhausted = false;
					awakenTask?.Process(g, minion.Controller, s, t, stack);
				})
			};
		}

		private static Trigger SpellburstAfterAwake(ISimpleTask task)
		{
			return new Trigger(TriggerType.AFTER_CAST)
			{
				TriggerSource = TriggerSource.FRIENDLY,
				SingleTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (s[GameTag.DORMANT] > 0)
						return;

					task.Process(g, c, s, t, stack);
					(s as IPlayable)?.ActivatedTrigger?.Remove();
				})
			};
		}

		private static bool IsCorruptCardDefinition(Card card)
		{
			return card[GameTag.CORRUPT] == 1;
		}

		private static readonly Card[] PrimeLegendaryMinionCards =
		{
			Cards.FromId("BT_019t"),
			Cards.FromId("BT_028t"),
			Cards.FromId("BT_109t"),
			Cards.FromId("BT_123t"),
			Cards.FromId("BT_136t"),
			Cards.FromId("BT_197t"),
			Cards.FromId("BT_210t"),
			Cards.FromId("BT_309t"),
			Cards.FromId("BT_713t")
		};

		private static void AddRandomMinionToHand(Game game, Controller controller, IEntity source, System.Func<Card, bool> predicate)
		{
			var cards = Cards.FormatTypeCards(game.FormatType)
				.Where(card => card.Collectible && card.Type == CardType.MINION && predicate(card))
				.ToList();

			if (cards.Count == 0)
				return;

			IPlayable entity = Entity.FromCard(controller, cards.Choose(game.Random));
			entity[GameTag.DISPLAYED_CREATOR] = source.Id;
			Generic.AddHandPhase.Invoke(controller, entity);
		}
	}
}
