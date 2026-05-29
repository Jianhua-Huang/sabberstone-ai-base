using System.Collections.Generic;
using System.Linq;
using SabberStoneCore.Actions;
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
	public class OutlandCardsGen
	{
		public static void AddAll(Dictionary<string, CardDef> cards)
		{
			DemonHunter(cards);
			Priest(cards);
			Neutral(cards);
			NonCollect(cards);
		}

		private static void DemonHunter(IDictionary<string, CardDef> cards)
		{
			// [BT_035] Chaos Strike - Give your hero +2 Attack this turn. Draw a card.
			cards.Add("BT_035", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new AddEnchantmentTask("BT_035e", EntityType.HERO),
					new DrawTask())
			}));

			// [BT_036] Coordinated Strike - Summon three 1/1 Illidari with Rush.
			cards.Add("BT_036", new CardDef(new Power
			{
				PowerTask = new SummonTask("BT_036t", 3)
			}));

			// [BT_142] Shadowhoof Slayer - Battlecry: Give your hero +1 Attack this turn.
			cards.Add("BT_142", new CardDef(new Power
			{
				PowerTask = new AddEnchantmentTask("BT_142e", EntityType.HERO)
			}));

			// [BT_173] Command the Illidari - Summon six 1/1 Illidari with Rush.
			cards.Add("BT_173", new CardDef(new Power
			{
				PowerTask = new SummonTask("BT_036t", 6)
			}));

			// [BT_175] Twin Slice - Give your hero +1 Attack this turn. Add 'Second Slice' to your hand.
			cards.Add("BT_175", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new AddEnchantmentTask("BT_142e", EntityType.HERO),
					new AddCardTo("BT_175t", EntityType.HAND))
			}));

			// [BT_235] Chaos Nova - Deal 4 damage to all minions.
			cards.Add("BT_235", new CardDef(new Power
			{
				PowerTask = new DamageTask(4, EntityType.ALLMINIONS, true)
			}));

			// [BT_351] Battlefiend - After your hero attacks, gain +1 Attack.
			cards.Add("BT_351", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.HERO,
					SingleTask = new AddEnchantmentTask("BT_351e", EntityType.SOURCE)
				}
			}));

			// [BT_352] Satyr Overseer - After your hero attacks, summon a 2/2 Satyr.
			cards.Add("BT_352", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.HERO,
					SingleTask = new SummonTask("BT_352t")
					}
				}));

			// [BT_354] Blade Dance - Deal your hero's Attack to 3 random enemy minions.
			cards.Add("BT_354", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					var targets = c.Opponent.BoardZone.GetAll().Where(p => !p.ToBeDestroyed).Cast<IPlayable>().ToList();
					if (targets.Count == 0)
						return;
					int amount = c.Hero.AttackDamage;
					IEnumerable<IPlayable> selectedTargets = targets.Count <= 3 ? targets : targets.ChooseNElements(3, g.Random);
					foreach (IPlayable target in selectedTargets)
						Generic.DamageCharFunc.Invoke((IPlayable)s, (ICharacter)target, amount, true);
					if (targets.Count > 3)
						g.OnRandomHappened(true);
				})
			}));

			// [BT_355] Wrathscale Naga - After a friendly minion dies, deal 3 damage to a random enemy.
			cards.Add("BT_355", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.DEATH)
				{
					TriggerSource = TriggerSource.FRIENDLY,
					Condition = SelfCondition.IsMinion,
					SingleTask = ComplexTask.DamageRandomTargets(1, EntityType.ENEMIES, 3)
				}
			}));

			// [BT_407] Ur'zul Horror - Deathrattle: Add a 2/1 Lost Soul to your hand.
			cards.Add("BT_407", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("BT_407t", EntityType.HAND)
			}));

			// [BT_495] Glaivebound Adept - Battlecry: If your hero attacked this turn, deal 4 damage.
			cards.Add("BT_495", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_IF_AVAILABLE_AND_HERO_HAS_ATTACK, 0 },
					{ PlayReq.REQ_ENEMY_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (t is ICharacter character && c.Hero.NumAttacksThisTurn > 0)
							Generic.DamageCharFunc.Invoke((IPlayable)s, character, 4, false);
					})
				}));

			// [BT_512] Inner Demon - Give your hero +8 Attack this turn.
			cards.Add("BT_512", new CardDef(new Power
			{
				PowerTask = new AddEnchantmentTask("BT_512e", EntityType.HERO)
			}));

			// [BT_427] Feast of Souls - Draw a card for each friendly minion that died this turn.
			cards.Add("BT_427", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					for (int i = 0; i < c.NumFriendlyMinionsThatDiedThisTurn; i++)
						Generic.Draw(c);
				})
			}));

			// [BT_488] Soul Split - Choose a friendly Demon. Summon a copy of it.
			cards.Add("BT_488", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 },
					{ PlayReq.REQ_FRIENDLY_TARGET, 0 },
					{ PlayReq.REQ_TARGET_WITH_RACE, (int)Race.DEMON }
				},
				new Power
				{
					PowerTask = new CopyTask(EntityType.TARGET, Zone.PLAY)
				}));

			// [BT_490] Consume Magic - Silence an enemy minion. Outcast draw is handled in a later mechanic pass.
			cards.Add("BT_490", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 },
					{ PlayReq.REQ_ENEMY_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new SilenceTask(EntityType.TARGET)
				}));

			// [BT_493] Priestess of Fury - At the end of your turn, deal 6 damage randomly split among all enemies.
			cards.Add("BT_493", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = ComplexTask.Repeat(ComplexTask.DamageRandomTargets(1, EntityType.ENEMIES, 1), 6)
				}
			}));

			// [BT_496] Furious Felfin - Battlecry: If your hero attacked this turn, gain +1 Attack and Rush.
			cards.Add("BT_496", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (c.Hero.NumAttacksThisTurn <= 0 || !(s is Minion minion))
						return;
					Generic.AddEnchantmentBlock(g, Cards.FromId("BT_496e"), minion, minion, 0, 0, 0);
					minion[GameTag.RUSH] = 1;
					if (minion.IsExhausted)
					{
						minion.IsExhausted = false;
						minion.AttackableByRush = true;
						g.RushMinions.Add(minion.Id);
					}
				})
			}));

			// [BT_486] Pit Commander - At the end of your turn, summon a Demon from your deck.
			cards.Add("BT_486", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = ComplexTask.SummonRandomMinion(EntityType.DECK, RelaCondition.IsOther(SelfCondition.IsRace(Race.DEMON)))
				}
			}));

			// [BT_509] Fel Summoner - Deathrattle: Summon a random Demon from your hand.
			cards.Add("BT_509", new CardDef(new Power
			{
				DeathrattleTask = new CustomTask((g, c, s, t, stack) =>
				{
					var demons = c.HandZone.GetAll().Where(p => p.Card.IsRace(Race.DEMON)).ToList();
					if (demons.Count == 0 || c.BoardZone.IsFull)
						return;
					IPlayable demon = demons.Choose(g.Random);
					Generic.RemoveFromZone.Invoke(c, demon);
					Generic.SummonBlock.Invoke(g, (Minion)demon, -1, s);
					g.OnRandomHappened(true);
				})
			}));

			// [BT_514] Immolation Aura - Deal 1 damage to all minions twice.
			cards.Add("BT_514", new CardDef(new Power
			{
				PowerTask = ComplexTask.Repeat(new DamageTask(1, EntityType.ALLMINIONS, true), 2)
			}));

			// [BT_740] Soul Cleave - Lifesteal. Deal 2 damage to two random enemy minions.
			cards.Add("BT_740", new CardDef(new Power
			{
				PowerTask = ComplexTask.DamageRandomTargets(2, EntityType.OP_MINIONS, 2, true)
			}));

			// [BT_752] Blur - Your hero can't take damage this turn.
			cards.Add("BT_752", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					c.Hero[GameTag.IMMUNE] = 1;
					Generic.AddEnchantmentBlock(g, Cards.FromId("BT_752e"), (IPlayable)s, c.Hero, 0, 0, 0);
				})
			}));

			// [BT_801] Eye Beam - Lifesteal. Deal 3 damage to a minion. Outcast cost is handled later.
			cards.Add("BT_801", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new DamageTask(3, EntityType.TARGET, true)
				}));

			// [BT_922] Umberwing - Battlecry: Summon two 1/1 Felwings.
			cards.Add("BT_922", new CardDef(new Power
			{
				PowerTask = new SummonTask("BT_922t", 2)
			}));

			// [BT_761] Coilfang Warlord - Deathrattle: Summon a 5/9 Warlord with Taunt.
			cards.Add("BT_761", new CardDef(new Power
			{
				DeathrattleTask = new SummonTask("BT_761t", SummonSide.DEATHRATTLE)
			}));
		}

		private static void Priest(IDictionary<string, CardDef> cards)
		{
			// [EX1_193] Psychic Conjurer - Battlecry: Copy a card in your opponent's deck and add it to your hand.
			cards.Add("EX1_193", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					var deck = c.Opponent.DeckZone.GetAll().ToList();
					if (deck.Count == 0)
						return;
					IPlayable sourceCard = deck.Choose(g.Random);
					IPlayable copy = Entity.FromCard(c, sourceCard.Card);
					copy[GameTag.DISPLAYED_CREATOR] = s.Id;
					Generic.AddHandPhase.Invoke(c, copy);
					g.OnRandomHappened(true);
				})
			}));

			// [EX1_194] Power Infusion - Give a minion +2/+6.
			cards.Add("EX1_194", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new AddEnchantmentTask("EX1_194e", EntityType.TARGET)
				}));

			// [EX1_195] Kul Tiran Chaplain - Battlecry: Give a friendly minion +2 Health.
			cards.Add("EX1_195", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_IF_AVAILABLE, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 },
					{ PlayReq.REQ_FRIENDLY_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new AddEnchantmentTask("EX1_195e", EntityType.TARGET)
				}));

			// [EX1_196] Scarlet Subjugator - Battlecry: Give an enemy minion -2 Attack until your next turn.
			cards.Add("EX1_196", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_IF_AVAILABLE, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 },
					{ PlayReq.REQ_ENEMY_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new AddEnchantmentTask("EX1_196e", EntityType.TARGET)
				}));

			// [EX1_197] Shadow Word: Ruin - Destroy all minions with 5 or more Attack.
			cards.Add("EX1_197", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion minion in c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).ToArray())
						if (minion.AttackDamage >= 5)
							minion.Destroy();
				})
			}));

			// [EX1_198] Natalie Seline - Battlecry: Destroy a minion and gain its Health.
			cards.Add("EX1_198", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(s is Minion natalie) || !(t is Minion target))
							return;
						int health = target.Health;
						target.Destroy();
						Generic.AddEnchantmentBlock(g, Cards.FromId("EX1_198e"), natalie, natalie, health, 0, 0);
					})
				}));
		}

		private static void Neutral(IDictionary<string, CardDef> cards)
		{
			// ---------------------------------- MINION - NEUTRAL
			// [BT_255] Kael'thas Sunstrider - COST:6 [ATK:4/HP:7]
			// - Set: black_temple, Rarity: legendary
			// --------------------------------------------------------
			// Text: Every third spell you cast each turn costs (0).
			// --------------------------------------------------------
			// GameTag:
			// - ELITE = 1
			// - AURA = 1
			// --------------------------------------------------------
			cards.Add("BT_255", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_CAST)
				{
					TriggerSource = TriggerSource.FRIENDLY,
					Condition = SelfCondition.IsSpell,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						int spellsPlayed = c.CardsPlayedThisTurn.Count(card => card.Type == CardType.SPELL);
						if (spellsPlayed % 3 != 2)
							return;

						Card enchantment = Cards.FromId("BT_255e");
						foreach (IPlayable spell in c.HandZone.Where(p => p.Card.Type == CardType.SPELL).ToArray())
							Generic.AddEnchantmentBlock(g, enchantment, (IPlayable)s, spell, 0, 0, 0);
					})
				}
			}));
		}

		private static void NonCollect(IDictionary<string, CardDef> cards)
		{
			cards.Add("BT_035e", new CardDef(new Power
			{
				Enchant = Enchants.Enchants.GetAutoEnchantFromText("BT_035e")
			}));

			cards.Add("BT_142e", new CardDef(new Power
			{
				Enchant = Enchants.Enchants.GetAutoEnchantFromText("BT_142e")
			}));

			cards.Add("BT_512e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(8))
				{
					IsOneTurnEffect = true
				}
			}));

			cards.Add("BT_351e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1))
			}));

			cards.Add("BT_752e", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = ComplexTask.Create(
						new CustomTask((g, c, s, t, stack) =>
						{
							if (s is Enchantment enchantment)
								enchantment.Target[GameTag.IMMUNE] = 0;
						}),
						RemoveEnchantmentTask.Task)
				}
			}));

			cards.Add("BT_496e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1))
			}));

			cards.Add("BT_175t", new CardDef(new Power
			{
				PowerTask = new AddEnchantmentTask("BT_142e", EntityType.HERO)
			}));

			cards.Add("EX1_194e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(2), Effects.Health_N(6))
			}));

			cards.Add("EX1_195e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Health_N(2))
			}));

			cards.Add("EX1_196e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(-2)),
				Trigger = new Trigger(TriggerType.TURN_START)
				{
					SingleTask = RemoveEnchantmentTask.Task
				}
			}));

			cards.Add("EX1_198e", new CardDef(new Power
			{
				Enchant = Enchants.Enchants.AddHealthScriptTag
			}));

			// ---------------------------------- ENCHANTMENT - NEUTRAL
			// [BT_255e] Sunstrider
			// --------------------------------------------------------
			// Text: Costs (0).
			// --------------------------------------------------------
			cards.Add("BT_255e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.SetCost(0))
				{
					RemoveWhenPlayed = true
				},
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = RemoveEnchantmentTask.Task
				}
			}));
		}
	}
}
