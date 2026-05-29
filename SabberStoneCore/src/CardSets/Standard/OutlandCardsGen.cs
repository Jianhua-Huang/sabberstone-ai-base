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
	public class OutlandCardsGen
	{
		public static void AddAll(Dictionary<string, CardDef> cards)
		{
			Warrior(cards);
			Druid(cards);
			Hunter(cards);
			Mage(cards);
			DemonHunter(cards);
			Priest(cards);
			Neutral(cards);
			NonCollect(cards);
		}

		private static void Mage(IDictionary<string, CardDef> cards)
		{
			// [BT_002] Incanter's Flow - Reduce the Cost of spells in your deck by (1).
			cards.Add("BT_002", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (IPlayable spell in c.DeckZone.GetAll().Where(p => p.Card.Type == CardType.SPELL))
						Generic.AddEnchantmentBlock(g, Cards.FromId("BT_002e"), (IPlayable)s, spell, 0, 0, 0);
				})
			}));

			// [BT_014] Starscryer - Deathrattle: Draw a spell.
			cards.Add("BT_014", new CardDef(new Power
			{
				DeathrattleTask = DrawCardTypeFromDeck(CardType.SPELL)
			}));

			// [BT_028] Astromancer Solarian - Spell Damage +1. Deathrattle: Shuffle Solarian Prime into your deck.
			cards.Add("BT_028", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("BT_028t", EntityType.DECK)
			}));

			// [BT_072] Deep Freeze - Freeze an enemy. Summon two 3/6 Water Elementals.
			cards.Add("BT_072", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_ENEMY_TARGET, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						ComplexTask.Freeze(EntityType.TARGET),
						new SummonTask("CS2_033", 2))
				}));

			// [BT_291] Apexis Blast - Deal 5 damage. If your deck has no minions, summon a random 5-Cost minion.
			cards.Add("BT_291", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new DamageTask(5, EntityType.TARGET, true),
						new CustomTask((g, c, s, t, stack) =>
						{
							if (c.DeckZone.Any(p => p.Card.Type == CardType.MINION) || c.BoardZone.IsFull)
								return;
							var minions = Cards.AllStandard.Where(card => card.Type == CardType.MINION && card.Cost == 5).ToList();
							if (minions.Count == 0)
								return;
							Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, minions.Choose(g.Random)), -1, s);
							g.OnRandomHappened(true);
						}))
				}));
		}

		private static void Hunter(IDictionary<string, CardDef> cards)
		{
			// [BT_163] Nagrand Slam - Summon four 3/5 Clefthoofs that attack random enemies.
			cards.Add("BT_163", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					for (int i = 0; i < 4 && !c.BoardZone.IsFull; i++)
					{
						var clefthoof = (Minion)Entity.FromCard(c, Cards.FromId("BT_163t"));
						Generic.SummonBlock.Invoke(g, clefthoof, -1, s);
						var enemies = c.Opponent.BoardZone.GetAll().Cast<ICharacter>().Concat(new[] { c.Opponent.Hero }).Where(p => !p.ToBeDestroyed).ToList();
						if (enemies.Count == 0)
							continue;
						ICharacter target = enemies.Choose(g.Random);
						Generic.AttackBlock.Invoke(c, clefthoof, target, true, false);
						g.OnRandomHappened(true);
					}
				})
			}));

			// [BT_201] Augmented Porcupine - Deathrattle: Deal this minion's Attack damage randomly split among all enemies.
			cards.Add("BT_201", new CardDef(new Power
			{
				DeathrattleTask = new CustomTask((g, c, s, t, stack) =>
				{
					int amount = s is Minion minion ? minion.AttackDamage : 0;
					for (int i = 0; i < amount; i++)
						ComplexTask.DamageRandomTargets(1, EntityType.ENEMIES, 1).Process(g, c, s, t, stack);
				})
			}));

			// [BT_202] Helboar - Deathrattle: Give a random Beast in your hand +1/+1.
			cards.Add("BT_202", new CardDef(new Power
			{
				DeathrattleTask = BuffRandomBeastInHand("BT_202e")
			}));

			// [BT_205] Scrap Shot - Deal 3 damage. Give a random Beast in your hand +3/+3.
			cards.Add("BT_205", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new DamageTask(3, EntityType.TARGET, true),
						BuffRandomBeastInHand("BT_205e"))
				}));

			// [BT_210] Zixor, Apex Predator - Rush. Deathrattle: Shuffle Zixor Prime into your deck.
			cards.Add("BT_210", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("BT_210t", EntityType.DECK)
			}));

			// [BT_210t] Zixor Prime - Rush. Battlecry: Summon 3 copies of this minion.
			cards.Add("BT_210t", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					for (int i = 0; i < 3 && !c.BoardZone.IsFull; i++)
						Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, ((IPlayable)s).Card), -1, s);
				})
			}));

			// [BT_213] Scavenger's Ingenuity - Draw a Beast. Give it +3/+3.
			cards.Add("BT_213", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					var beasts = c.DeckZone.GetAll().Where(p => p.Card.IsRace(Race.BEAST)).ToList();
					if (beasts.Count == 0)
						return;
					IPlayable beast = beasts.Choose(g.Random);
					Generic.RemoveFromZone.Invoke(c, beast);
					Generic.AddHandPhase.Invoke(c, beast);
					Generic.AddEnchantmentBlock(g, Cards.FromId("BT_213e"), (IPlayable)s, beast, 0, 0, 0);
					if (beasts.Count > 1)
						g.OnRandomHappened(true);
				})
			}));

			// [BT_214] Beastmaster Leoroxx - Battlecry: Summon 3 Beasts from your hand.
			cards.Add("BT_214", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (IPlayable beast in c.HandZone.GetAll().Where(p => p.Card.IsRace(Race.BEAST)).Take(3).ToArray())
					{
						if (c.BoardZone.IsFull)
							break;
						Generic.RemoveFromZone.Invoke(c, beast);
						Generic.SummonBlock.Invoke(g, (Minion)beast, -1, s);
					}
				})
			}));
		}

		private static void Druid(IDictionary<string, CardDef> cards)
		{
			// [BT_128] Fungal Fortunes - Draw 3 cards. Discard any minions drawn.
			cards.Add("BT_128", new CardDef(new Power
			{
				PowerTask = new EnqueueTask(3, ComplexTask.Create(
					new DrawTask(true),
					new ConditionTask(EntityType.STACK, SelfCondition.IsMinion, SelfCondition.IsInZone(Zone.HAND)),
					new FlagTask(true, new DiscardTask(EntityType.STACK))))
			}));

			// [BT_129] Germination - Summon a copy of a friendly minion. Give the copy Taunt.
			cards.Add("BT_129", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 },
					{ PlayReq.REQ_FRIENDLY_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(t is Minion target) || c.BoardZone.IsFull)
							return;
						var tags = new EntityData((EntityData)target.NativeTags);
						var copy = (Minion)Entity.FromCard(c, target.Card, tags, c.BoardZone, creator: s);
						target.CopyInternalAttributes(copy);
						copy[GameTag.TAUNT] = 1;
					})
				}));

			// [BT_130] Overgrowth - Gain two empty Mana Crystals.
			cards.Add("BT_130", new CardDef(new Power
			{
				PowerTask = new ManaCrystalEmptyTask(2)
			}));

			// [BT_132] Ironbark - Give a minion +1/+3 and Taunt. Costs (0) if you have at least 7 Mana Crystals.
			cards.Add("BT_132", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					Aura = new AdaptiveCostEffect(p => p.Controller.BaseMana >= 7 ? p.Card.Cost : 0),
					PowerTask = new AddEnchantmentTask("BT_132e", EntityType.TARGET)
				}));

			// [BT_133] Marsh Hydra - Rush. After this attacks, add a random 8-Cost minion to your hand.
			cards.Add("BT_133", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.SELF,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						var minions = Cards.AllStandard.Where(card => card.Type == CardType.MINION && card.Cost == 8).ToList();
						if (minions.Count == 0)
							return;
						Generic.DrawCard(c, minions.Choose(g.Random));
						g.OnRandomHappened(true);
					})
				}
			}));

			// [BT_134] Bogbeam - Deal 3 damage to a minion. Costs (0) if you have at least 7 Mana Crystals.
			cards.Add("BT_134", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					Aura = new AdaptiveCostEffect(p => p.Controller.BaseMana >= 7 ? p.Card.Cost : 0),
					PowerTask = new DamageTask(3, EntityType.TARGET, true)
				}));

			// [BT_135] Glowfly Swarm - Summon a 2/2 Glowfly for each spell in your hand.
			cards.Add("BT_135", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					int amount = c.HandZone.Count(p => p.Card.Type == CardType.SPELL);
					for (int i = 0; i < amount && !c.BoardZone.IsFull; i++)
						Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, Cards.FromId("BT_135t")), -1, s);
				})
			}));

			// [BT_136] Archspore Msshi'fn - Taunt. Deathrattle: Shuffle Msshi'fn Prime into your deck.
			cards.Add("BT_136", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("BT_136t", EntityType.DECK)
			}));
		}

		private static void Warrior(IDictionary<string, CardDef> cards)
		{
			// [BT_117] Bladestorm - Deal 1 damage to all minions. Repeat until one dies.
			cards.Add("BT_117", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					while (true)
					{
						var minions = c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).Where(p => !p.ToBeDestroyed).ToArray();
						if (minions.Length == 0)
							return;
						foreach (Minion minion in minions)
							Generic.DamageCharFunc.Invoke((IPlayable)s, minion, 1, true);
						if (minions.Any(p => p.ToBeDestroyed))
							return;
					}
				})
			}));

			// [BT_120] Warmaul Challenger - Battlecry: Choose an enemy minion. Battle it to the death!
			cards.Add("BT_120", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_IF_AVAILABLE, 0 },
					{ PlayReq.REQ_ENEMY_TARGET, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(s is Minion challenger) || !(t is Minion target))
							return;
						while (!challenger.ToBeDestroyed && !target.ToBeDestroyed)
						{
							target.TakeDamage(challenger, challenger.AttackDamage);
							if (target.AttackDamage > 0)
								challenger.TakeDamage(target, target.AttackDamage);
						}
					})
				}));

			// [BT_123] Kargath Bladefist - Rush. Deathrattle: Shuffle Kargath Prime into your deck.
			cards.Add("BT_123", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("BT_123t", EntityType.DECK)
			}));

			// [BT_124] Corsair Cache - Draw a weapon. Give it +1/+1.
			cards.Add("BT_124", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					var weapons = c.DeckZone.GetAll().Where(p => p.Card.Type == CardType.WEAPON).ToList();
					if (weapons.Count == 0)
						return;
					IPlayable weapon = weapons.Choose(g.Random);
					Generic.RemoveFromZone.Invoke(c, weapon);
					Generic.AddHandPhase.Invoke(c, weapon);
					Generic.AddEnchantmentBlock(g, Cards.FromId("BT_124e"), (IPlayable)s, weapon, 0, 0, 0);
					if (weapons.Count > 1)
						g.OnRandomHappened(true);
				})
			}));

			// [BT_138] Bloodboil Brute - Rush. Costs (1) less for each damaged minion.
			cards.Add("BT_138", new CardDef(new Power
			{
				Aura = new AdaptiveCostEffect(p => p.Controller.BoardZone.GetAll().Concat(p.Controller.Opponent.BoardZone.GetAll()).Count(m => m.Damage > 0))
			}));

			// [BT_140] Bonechewer Raider - Battlecry: If there is a damaged minion, gain +1/+1 and Rush.
			cards.Add("BT_140", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(s is Minion raider) || !c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).Any(p => p.Damage > 0))
						return;
					Generic.AddEnchantmentBlock(g, Cards.FromId("BT_140e"), raider, raider, 0, 0, 0);
					raider[GameTag.RUSH] = 1;
					if (raider.IsExhausted)
					{
						raider.IsExhausted = false;
						raider.AttackableByRush = true;
						g.RushMinions.Add(raider.Id);
					}
				})
			}));

			// [BT_233] Sword and Board - Deal 2 damage to a minion. Gain 2 Armor.
			cards.Add("BT_233", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new DamageTask(2, EntityType.TARGET, true),
						new ArmorTask(2))
				}));

			// [BT_249] Scrap Golem - Taunt. Deathrattle: Gain Armor equal to this minion's Attack.
			cards.Add("BT_249", new CardDef(new Power
			{
				DeathrattleTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (s is Minion golem)
						c.Hero.GainArmor(golem, golem.AttackDamage);
				})
			}));
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

			// [BT_187] Kayn Sunfury - Charge. All friendly attacks ignore Taunt.
			cards.Add("BT_187", new CardDef(new Power()));

			// [BT_235] Chaos Nova - Deal 4 damage to all minions.
			cards.Add("BT_235", new CardDef(new Power
			{
				PowerTask = new DamageTask(4, EntityType.ALLMINIONS, true)
			}));

			// [BT_271] Flamereaper - Also damages the minions next to whomever your hero attacks.
			cards.Add("BT_271", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.HERO,
					Condition = SelfCondition.IsProposedDefender(CardType.MINION),
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(g.CurrentEventData?.EventTarget is Minion target))
							return;
						int amount = c.Hero.AttackDamage;
						foreach (Minion adjacent in target.GetAdjacentMinions())
							Generic.DamageCharFunc.Invoke((IPlayable)s, adjacent, amount, false);
					})
				}
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

			// [BT_423] Ashtongue Battlelord - Taunt. Lifesteal.
			cards.Add("BT_423", new CardDef(new Power()));

			// [BT_430] Warglaives of Azzinoth - After attacking a minion, your hero may attack again.
			cards.Add("BT_430", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.HERO,
					Condition = SelfCondition.IsProposedDefender(CardType.MINION),
					SingleTask = new SetGameTagTask(GameTag.EXHAUSTED, 0, EntityType.HERO)
				}
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

			// [BT_480] Crimson Sigil Runner - Outcast: Draw a card.
			cards.Add("BT_480", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (WasPlayedFromOutcastPosition(s))
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

			// [BT_490] Consume Magic - Silence an enemy minion. Outcast: Draw a card.
			cards.Add("BT_490", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 },
					{ PlayReq.REQ_ENEMY_TARGET, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new SilenceTask(EntityType.TARGET),
						new CustomTask((g, c, s, t, stack) =>
						{
							if (WasPlayedFromOutcastPosition(s))
								Generic.Draw(c);
						}))
				}));

			// [BT_491] Spectral Sight - Draw a card. Outcast: Draw another.
			cards.Add("BT_491", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					Generic.Draw(c);
					if (WasPlayedFromOutcastPosition(s))
						Generic.Draw(c);
				})
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

			// [BT_487] Hulking Overfiend - Rush. After this attacks and kills a minion, it may attack again.
			cards.Add("BT_487", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.SELF,
					Condition = SelfCondition.IsDefenderDead,
					SingleTask = new SetGameTagTask(GameTag.EXHAUSTED, 0, EntityType.SOURCE)
				}
			}));

			// [BT_601] Skull of Gul'dan - Draw 3 cards. Outcast: Reduce their Cost by (3).
			cards.Add("BT_601", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					bool outcast = WasPlayedFromOutcastPosition(s);
					Card enchantment = Cards.FromId("BT_601e");
					for (int i = 0; i < 3; i++)
					{
						IPlayable drawn = Generic.Draw(c);
						if (outcast && drawn != null && drawn.Zone?.Type == Zone.HAND)
							Generic.AddEnchantmentBlock(g, enchantment, (IPlayable)s, drawn, 0, 0, 0);
					}
				})
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

			// [BT_510] Wrathspike Brute - Taunt. After this is attacked, deal 1 damage to all enemies.
			cards.Add("BT_510", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TAKE_DAMAGE)
				{
					TriggerSource = TriggerSource.SELF,
					Condition = new SelfCondition(p => p.Game.CurrentEventData?.EventSource is ICharacter),
					SingleTask = new DamageTask(1, EntityType.ENEMIES, false)
				}
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

			// [BT_753] Mana Burn - Your opponent has 2 fewer Mana Crystals next turn.
			cards.Add("BT_753", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					c.Opponent.OverloadOwed += 2;
					c.Opponent.OverloadThisGame += 2;
				})
			}));

			// [BT_801] Eye Beam - Lifesteal. Deal 3 damage to a minion. Outcast: This costs (0).
			cards.Add("BT_801", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					Aura = new AdaptiveCostEffect(p => IsOutcastPosition(p) ? p.Card.Cost : 0),
					PowerTask = new DamageTask(3, EntityType.TARGET, true)
				}));

			// [BT_814] Illidari Felblade - Rush. Outcast: Gain Immune this turn.
			cards.Add("BT_814", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(s is Minion minion) || !WasPlayedFromOutcastPosition(s))
						return;
					minion[GameTag.IMMUNE] = 1;
					Generic.AddEnchantmentBlock(g, Cards.FromId("BT_814e"), minion, minion, 0, 0, 0);
				})
			}));

			// [BT_921] Aldrachi Warblades - Lifesteal.
			cards.Add("BT_921", new CardDef(new Power()));

			// [BT_922] Umberwing - Battlecry: Summon two 1/1 Felwings.
			cards.Add("BT_922", new CardDef(new Power
			{
				PowerTask = new SummonTask("BT_922t", 2)
			}));

			// [BT_937] Altruis the Outcast - After you play the left- or right-most card in your hand, deal 1 damage to all enemies.
			cards.Add("BT_937", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.PLAY_CARD)
				{
					TriggerSource = TriggerSource.FRIENDLY,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (t is Playable played && played.WasPlayedFromOutcastPosition)
							new DamageTask(1, EntityType.ENEMIES, false).Process(g, c, (IPlayable)s, null);
					})
				}
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
			// [BT_008] Rustsworn Initiate - Deathrattle: Summon a 1/1 Impcaster with Spell Damage +1.
			cards.Add("BT_008", new CardDef(new Power
			{
				DeathrattleTask = new SummonTask("BT_008t", SummonSide.DEATHRATTLE)
			}));

			// [BT_010] Felfin Navigator - Battlecry: Give your other Murlocs +1/+1.
			cards.Add("BT_010", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion minion in c.BoardZone.GetAll().Where(p => p != s && p.Card.IsRace(Race.MURLOC)))
						Generic.AddEnchantmentBlock(g, Cards.FromId("BT_010e"), (IPlayable)s, minion, 0, 0, 0);
				})
			}));

			// [BT_255] Kael'thas Sunstrider - Every third spell you cast each turn costs (0).
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

			// [BT_714] Frozen Shadoweaver - Battlecry: Freeze an enemy.
			cards.Add("BT_714", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_IF_AVAILABLE, 0 },
					{ PlayReq.REQ_ENEMY_TARGET, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Freeze(EntityType.TARGET)
				}));

			// [BT_715] Bonechewer Brawler - Taunt. Whenever this minion takes damage, gain +2 Attack.
			cards.Add("BT_715", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TAKE_DAMAGE)
				{
					TriggerSource = TriggerSource.SELF,
					SingleTask = new AddEnchantmentTask("BT_715e", EntityType.SOURCE)
				}
			}));

			// [BT_716] Bonechewer Vanguard - Taunt. Whenever this minion takes damage, gain +2 Attack.
			cards.Add("BT_716", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TAKE_DAMAGE)
				{
					TriggerSource = TriggerSource.SELF,
					SingleTask = new AddEnchantmentTask("BT_716e", EntityType.SOURCE)
				}
			}));

			// [BT_720] Ruststeed Raider - Taunt, Rush. Battlecry: Gain +4 Attack this turn.
			cards.Add("BT_720", new CardDef(new Power
			{
				PowerTask = new AddEnchantmentTask("BT_720e", EntityType.SOURCE)
			}));

			// [BT_722] Guardian Augmerchant - Battlecry: Deal 1 damage to a minion and give it Divine Shield.
			cards.Add("BT_722", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_IF_AVAILABLE, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new DamageTask(1, EntityType.TARGET, false),
						new SetGameTagTask(GameTag.DIVINE_SHIELD, 1, EntityType.TARGET))
				}));

			// [BT_723] Rocket Augmerchant - Battlecry: Deal 1 damage to a minion and give it Rush.
			cards.Add("BT_723", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_IF_AVAILABLE, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new DamageTask(1, EntityType.TARGET, false),
						new CustomTask((g, c, s, t, stack) =>
						{
							if (!(t is Minion minion))
								return;
							minion[GameTag.RUSH] = 1;
							if (minion.IsExhausted)
							{
								minion.IsExhausted = false;
								minion.AttackableByRush = true;
								g.RushMinions.Add(minion.Id);
							}
						}))
				}));

			// [BT_724] Ethereal Augmerchant - Battlecry: Deal 1 damage to a minion and give it Spell Damage +1.
			cards.Add("BT_724", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_IF_AVAILABLE, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new DamageTask(1, EntityType.TARGET, false),
						new AddEnchantmentTask("BT_724e", EntityType.TARGET))
				}));

			// [BT_727] Soulbound Ashtongue - Whenever this minion takes damage, also deal that amount to your hero.
			cards.Add("BT_727", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TAKE_DAMAGE)
				{
					TriggerSource = TriggerSource.SELF,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						Generic.DamageCharFunc.Invoke((IPlayable)s, c.Hero, g.CurrentEventData?.EventNumber ?? 0, false);
					})
				}
			}));

			// [BT_730] Overconfident Orc - Taunt. While at full Health, this has +2 Attack.
			cards.Add("BT_730", new CardDef(new Power
			{
				Aura = new AdaptiveEffect(GameTag.ATK, EffectOperator.ADD, p => p is ICharacter character && character.Damage == 0 ? 2 : 0)
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

			cards.Add("BT_010e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1), Effects.Health_N(1))
			}));

			cards.Add("BT_124e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1), new Effect(GameTag.DURABILITY, EffectOperator.ADD, 1))
			}));

			cards.Add("BT_140e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1), Effects.Health_N(1))
			}));

			cards.Add("BT_132e", new CardDef(new Power
			{
				Enchant = new Enchant(
					Effects.Attack_N(1),
					Effects.Health_N(3),
					new Effect(GameTag.TAUNT, EffectOperator.SET, 1))
			}));

			cards.Add("BT_202e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1), Effects.Health_N(1))
			}));

			cards.Add("BT_205e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(3), Effects.Health_N(3))
			}));

			cards.Add("BT_213e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(3), Effects.Health_N(3))
			}));

			cards.Add("BT_002e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.ReduceCost(1))
			}));

			cards.Add("BT_715e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(2))
			}));

			cards.Add("BT_716e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(2))
			}));

			cards.Add("BT_720e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(4))
				{
					IsOneTurnEffect = true
				}
			}));

			cards.Add("BT_724e", new CardDef(new Power
			{
				Enchant = new Enchant(new Effect(GameTag.SPELLPOWER, EffectOperator.ADD, 1))
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

			cards.Add("BT_601e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.ReduceCost(3))
				{
					RemoveWhenPlayed = true
				}
			}));

			cards.Add("BT_814e", new CardDef(new Power
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

		private static ISimpleTask BuffRandomBeastInHand(string enchantmentId)
		{
			return new CustomTask((g, c, s, t, stack) =>
			{
				var beasts = c.HandZone.GetAll().Where(p => p.Card.IsRace(Race.BEAST)).ToList();
				if (beasts.Count == 0)
					return;
				IPlayable beast = beasts.Choose(g.Random);
				Generic.AddEnchantmentBlock(g, Cards.FromId(enchantmentId), (IPlayable)s, beast, 0, 0, 0);
				if (beasts.Count > 1)
					g.OnRandomHappened(true);
			});
		}

		private static ISimpleTask DrawCardTypeFromDeck(CardType cardType)
		{
			return new CustomTask((g, c, s, t, stack) =>
			{
				var cards = c.DeckZone.GetAll().Where(p => p.Card.Type == cardType).ToList();
				if (cards.Count == 0)
					return;
				IPlayable card = cards.Choose(g.Random);
				Generic.RemoveFromZone.Invoke(c, card);
				Generic.AddHandPhase.Invoke(c, card);
				if (cards.Count > 1)
					g.OnRandomHappened(true);
			});
		}

		private static bool IsOutcastPosition(IPlayable playable)
		{
			return playable.Zone?.Type == Zone.HAND &&
				(playable.ZonePosition == 0 || playable.ZonePosition == playable.Controller.HandZone.Count - 1);
		}

		private static bool WasPlayedFromOutcastPosition(IEntity source)
		{
			return source is Playable playable && playable.WasPlayedFromOutcastPosition;
		}
	}
}
