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
			Paladin(cards);
			Priest(cards);
			Rogue(cards);
			Shaman(cards);
			Warlock(cards);
			Neutral(cards);
			NonCollect(cards);
		}

		private static void Shaman(IDictionary<string, CardDef> cards)
		{
			// [BT_100] Serpentshrine Portal - Deal 3 damage. Summon a random 3-Cost minion. Overload: (1)
			cards.Add("BT_100", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new DamageTask(3, EntityType.TARGET, true),
						SummonRandomCostMinion(3))
				}));

			// [BT_101] Vivid Spores - Give your minions "Deathrattle: Resummon this minion."
			cards.Add("BT_101", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion minion in c.BoardZone.GetAll())
						Generic.AddEnchantmentBlock(g, Cards.FromId("BT_101e"), (IPlayable)s, minion, 0, 0, 0);
				})
			}));

			// [BT_102] Boggspine Knuckles - After your hero attacks, transform your minions into random ones that cost (1) more.
			cards.Add("BT_102", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.HERO,
					SingleTask = new TransformMinionTask(EntityType.MINIONS, 1)
				}
			}));

			// [BT_106] Bogstrok Clacker - Battlecry: Transform adjacent minions into random minions that cost (1) more.
			cards.Add("BT_106", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(s is Minion clacker))
						return;
					foreach (Minion adjacent in clacker.GetAdjacentMinions())
						new TransformMinionTask(EntityType.TARGET, 1).Process(g, c, s, adjacent, stack);
				})
			}));

			// [BT_109] Lady Vashj - Spell Damage +1. Deathrattle: Shuffle Vashj Prime into your deck.
			cards.Add("BT_109", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("BT_109t", EntityType.DECK)
			}));

			// [BT_110] Torrent - Deal 8 damage to a minion. Costs (3) less if you cast a spell last turn.
			cards.Add("BT_110", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					Aura = new AdaptiveCostEffect(p => HasCastSpellRecently(p.Controller) ? 3 : 0),
					PowerTask = new DamageTask(8, EntityType.TARGET, true)
				}));

			// [BT_113] Totemic Reflection - Give a minion +2/+2. If it's a Totem, summon a copy of it.
			cards.Add("BT_113", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new AddEnchantmentTask("BT_113e", EntityType.TARGET),
						new CustomTask((g, c, s, t, stack) =>
						{
							if (t is Minion target && target.Card.IsRace(Race.TOTEM) && !c.BoardZone.IsFull)
								new SummonCopyTask(EntityType.TARGET).Process(g, c, s, t, stack);
						}))
				}));

			// [BT_114] Shattered Rumbler - Battlecry: If you cast a spell last turn, deal 2 damage to all other minions.
			cards.Add("BT_114", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!HasCastSpellRecently(c))
						return;
					foreach (Minion minion in c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).Where(p => p != s).ToArray())
						Generic.DamageCharFunc.Invoke((IPlayable)s, minion, 2, false);
				})
			}));

			// [BT_115] Marshspawn - Battlecry: If you cast a spell last turn, Discover a spell.
			cards.Add("BT_115", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (HasCastSpellRecently(c))
						new DiscoverTask(DiscoverType.SPELL).Process(g, c, s, t, stack);
				})
			}));

			// [BT_230] The Lurker Below - Battlecry: Deal 3 damage to an enemy minion. If it dies, repeat on one of its neighbors.
			cards.Add("BT_230", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 },
					{ PlayReq.REQ_ENEMY_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new CustomTask((g, c, s, t, stack) =>
					{
						Minion target = t as Minion;
						while (target != null && !target.ToBeDestroyed)
						{
							Generic.DamageCharFunc.Invoke((IPlayable)s, target, 3, false);
							if (!target.ToBeDestroyed)
								break;
							target = target.GetAdjacentMinions().FirstOrDefault(p => !p.ToBeDestroyed);
						}
					})
				}));
		}

		private static void Warlock(IDictionary<string, CardDef> cards)
		{
			// [BT_196] Keli'dan the Breaker - Battlecry: Destroy a minion. If drawn this turn, destroy all other minions.
			cards.Add("BT_196", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new DestroyTask(EntityType.TARGET),
						new CustomTask((g, c, s, t, stack) =>
						{
							if (c.NumCardsDrawnThisTurn <= 0 || c.LastCardDrawn != s.Id)
								return;
							foreach (Minion minion in c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).Where(p => p != s && p != t).ToArray())
								minion.Destroy();
						}))
				}));

			// [BT_199] Unstable Felbolt - Deal 3 damage to an enemy minion and a random friendly one.
			cards.Add("BT_199", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 },
					{ PlayReq.REQ_ENEMY_TARGET, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new DamageTask(3, EntityType.TARGET, true),
						ComplexTask.DamageRandomTargets(1, EntityType.MINIONS, 3, true))
				}));

			// [BT_300] Hand of Gul'dan - When you play or discard this, draw 3 cards.
			cards.Add("BT_300", new CardDef(new Power
			{
				PowerTask = new DrawTask(3),
				Trigger = new Trigger(TriggerType.DISCARD)
				{
					TriggerActivation = TriggerActivation.HAND,
					TriggerSource = TriggerSource.SELF,
					SingleTask = new DrawTask(3)
				}
			}));

			// [BT_301] Nightshade Matron - Rush. Battlecry: Discard your highest Cost card.
			cards.Add("BT_301", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					IPlayable card = c.HandZone.GetAll().OrderByDescending(p => p.Cost).FirstOrDefault();
					if (card != null)
						Generic.DiscardBlock.Invoke(c, card);
				})
			}));

			// [BT_302] The Dark Portal - Draw a minion. If you have at least 8 cards in hand, it costs (5) less.
			cards.Add("BT_302", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					bool reduce = c.HandZone.Count >= 8;
					var minions = c.DeckZone.GetAll().Where(p => p.Card.Type == CardType.MINION).ToList();
					if (minions.Count == 0)
						return;
					IPlayable minion = minions.Choose(g.Random);
					Generic.RemoveFromZone.Invoke(c, minion);
					Generic.AddHandPhase.Invoke(c, minion);
					if (reduce)
						Generic.AddEnchantmentBlock(g, Cards.FromId("BT_302e"), (IPlayable)s, minion, 0, 0, 0);
					if (minions.Count > 1)
						g.OnRandomHappened(true);
				})
			}));

			// [BT_304] Enhanced Dreadlord - Taunt. Deathrattle: Summon a 5/5 Dreadlord with Lifesteal.
			cards.Add("BT_304", new CardDef(new Power
			{
				DeathrattleTask = new SummonTask("BT_304t", SummonSide.DEATHRATTLE)
			}));

			// [BT_305] Imprisoned Scrap Imp - Dormant for 2 turns. When this awakens, give all minions in your hand +2/+2.
			cards.Add("BT_305", new CardDef(new Power
			{
				PowerTask = StartDormant(2),
				Trigger = DormantAwakenTrigger(new CustomTask((g, c, s, t, stack) =>
				{
					foreach (IPlayable minion in c.HandZone.GetAll().Where(p => p.Card.Type == CardType.MINION))
						Generic.AddEnchantmentBlock(g, Cards.FromId("BT_305e"), (IPlayable)s, minion, 0, 0, 0);
				}))
			}));

			// [BT_306] Shadow Council - Replace your hand with random Demons. Give them +2/+2.
			cards.Add("BT_306", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					var demons = Cards.AllStandard.Where(card => card.Collectible && card.Type == CardType.MINION && card.IsRace(Race.DEMON)).ToList();
					int count = c.HandZone.Count;
					foreach (IPlayable card in c.HandZone.GetAll().ToArray())
						Generic.RemoveFromZone.Invoke(c, card);
					for (int i = 0; i < count && demons.Count > 0; i++)
					{
						IPlayable demon = Generic.DrawCard(c, demons.Choose(g.Random));
						Generic.AddEnchantmentBlock(g, Cards.FromId("BT_306e"), (IPlayable)s, demon, 0, 0, 0);
					}
					if (count > 0)
						g.OnRandomHappened(true);
				})
			}));

			// [BT_307] Darkglare - After your hero takes damage, refresh 2 Mana Crystals.
			cards.Add("BT_307", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TAKE_DAMAGE)
				{
					TriggerSource = TriggerSource.HERO,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						c.UsedMana = c.UsedMana > 2 ? c.UsedMana - 2 : 0;
					})
				}
			}));

			// [BT_309] Kanrethad Ebonlocke - Your Demons cost (1) less. Deathrattle: Shuffle Kanrethad Prime into your deck.
			cards.Add("BT_309", new CardDef(new Power
			{
				Aura = new Aura(AuraType.HAND, Effects.ReduceCost(1))
				{
					Condition = SelfCondition.IsRace(Race.DEMON)
				},
				DeathrattleTask = new AddCardTo("BT_309t", EntityType.DECK)
			}));
		}

		private static void Rogue(IDictionary<string, CardDef> cards)
		{
			// [BT_042] Bamboozle - Secret: When one of your minions is attacked, transform it into a random one that costs (3) more.
			cards.Add("BT_042", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.ATTACK)
				{
					TriggerSource = TriggerSource.ENEMY,
					Condition = SelfCondition.IsEventTargetIs(CardType.MINION),
					SingleTask = ComplexTask.Secret(new TransformMinionTask(EntityType.EVENT_TARGET, 3))
				}
			}));

			// [BT_188] Shadowjeweler Hanar - After you play a Secret, Discover a Secret from a different class.
			cards.Add("BT_188", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.PLAY_CARD)
				{
					TriggerSource = TriggerSource.FRIENDLY,
					Condition = SelfCondition.IsSecret,
					SingleTask = new DiscoverTask(DiscoverType.SECRET)
				}
			}));

			// [BT_701] Spymistress - Stealth.
			cards.Add("BT_701", new CardDef(new Power()));

			// [BT_702] Ashtongue Slayer - Battlecry: Give a Stealthed minion +3 Attack and Immune this turn.
			cards.Add("BT_702", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_IF_AVAILABLE, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 },
					{ PlayReq.REQ_FRIENDLY_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (t is Minion target && target.HasStealth)
							Generic.AddEnchantmentBlock(g, Cards.FromId("BT_702e"), (IPlayable)s, target, 0, 0, 0);
					})
				}));

			// [BT_703] Cursed Vagrant - Deathrattle: Summon a 7/5 Shadow with Stealth.
			cards.Add("BT_703", new CardDef(new Power
			{
				DeathrattleTask = new SummonTask("BT_703t", SummonSide.DEATHRATTLE)
			}));

			// [BT_710] Greyheart Sage - Battlecry: If you control a Stealthed minion, draw 2 cards.
			cards.Add("BT_710", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!c.BoardZone.Any(p => p.HasStealth))
						return;
					Generic.Draw(c);
					Generic.Draw(c);
				})
			}));

			// [BT_707] Ambush - Secret: After your opponent plays a minion, summon a 2/3 Ambusher with Poisonous.
			cards.Add("BT_707", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_PLAY_MINION)
				{
					SingleTask = ComplexTask.Secret(new SummonTask("BT_707t"))
				}
			}));

			// [BT_709] Dirty Tricks - Secret: After your opponent casts a spell, draw 2 cards.
			cards.Add("BT_709", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_CAST)
				{
					SingleTask = ComplexTask.Secret(new DrawTask(2))
				}
			}));

			// [BT_711] Blackjack Stunner - Battlecry: If you control a Secret, return a minion to its owner's hand. It costs (2) more.
			cards.Add("BT_711", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_IF_AVAILABLE_AND_MINIMUM_FRIENDLY_SECRETS, 1 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new ReturnHandTask(EntityType.TARGET),
						new AddEnchantmentTask("BT_711e", EntityType.TARGET))
				}));

			// [BT_713] Akama - Stealth. Deathrattle: Shuffle Akama Prime into your deck.
			cards.Add("BT_713", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("BT_713t", EntityType.DECK)
			}));

			// [BT_713t] Akama Prime - Permanently Stealthed.
			cards.Add("BT_713t", new CardDef(new Power()));
		}

		private static void Paladin(IDictionary<string, CardDef> cards)
		{
			// [BT_009] Imprisoned Sungill - Dormant for 2 turns. When this awakens, summon two 1/1 Murlocs.
			cards.Add("BT_009", new CardDef(new Power
			{
				PowerTask = StartDormant(2),
				Trigger = DormantAwakenTrigger(new SummonTask("BT_009t", 2))
			}));

			// [BT_011] Libram of Justice - Equip a 1/4 weapon. Change the Health of all enemy minions to 1.
			cards.Add("BT_011", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new WeaponTask("BT_011t"),
					new CustomTask((g, c, s, t, stack) =>
					{
						foreach (Minion minion in c.Opponent.BoardZone.GetAll())
							minion.BaseHealth = minion.Damage + 1;
					}))
			}));

			// [BT_018] Underlight Angling Rod - After your Hero attacks, add a random Murloc to your hand.
			cards.Add("BT_018", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.HERO,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						var murlocs = Cards.AllStandard.Where(card => card.Collectible && card.IsRace(Race.MURLOC)).ToList();
						if (murlocs.Count == 0)
							return;
						Generic.DrawCard(c, murlocs.Choose(g.Random));
						g.OnRandomHappened(true);
					})
				}
			}));

			// [BT_019] Murgur Murgurgle - Divine Shield. Deathrattle: Shuffle Murgurgle Prime into your deck.
			cards.Add("BT_019", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("BT_019t", EntityType.DECK)
			}));

			// [BT_019t] Murgurgle Prime - Divine Shield. Battlecry: Summon 4 random Murlocs. Give them Divine Shield.
			cards.Add("BT_019t", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					var murlocs = Cards.AllStandard.Where(card => card.Collectible && card.Type == CardType.MINION && card.IsRace(Race.MURLOC)).ToList();
					for (int i = 0; i < 4 && !c.BoardZone.IsFull && murlocs.Count > 0; i++)
					{
						var murloc = (Minion)Entity.FromCard(c, murlocs.Choose(g.Random));
						Generic.SummonBlock.Invoke(g, murloc, -1, s);
						murloc[GameTag.DIVINE_SHIELD] = 1;
					}
					g.OnRandomHappened(true);
				})
			}));

			// [BT_020] Aldor Attendant - Battlecry: Reduce the Cost of your Librams by (1) this game.
			cards.Add("BT_020", new CardDef(new Power
			{
				PowerTask = ReduceLibrams("BT_020e")
			}));

			// [BT_024] Libram of Hope - Restore 8 Health. Summon an 8/8 Guardian with Taunt and Divine Shield.
			cards.Add("BT_024", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new HealTask(8, EntityType.TARGET),
						new SummonTask("BT_024t"))
				}));

			// [BT_025] Libram of Wisdom - Give a minion +1/+1 and Deathrattle: Add Libram of Wisdom to your hand.
			cards.Add("BT_025", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new AddEnchantmentTask("BT_025e", EntityType.TARGET)
				}));

			// [BT_026] Aldor Truthseeker - Taunt. Battlecry: Reduce the Cost of your Librams by (2) this game.
			cards.Add("BT_026", new CardDef(new Power
			{
				PowerTask = ReduceLibrams("BT_026e")
			}));

			// [BT_292] Hand of A'dal - Give a minion +2/+2. Draw a card.
			cards.Add("BT_292", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new AddEnchantmentTask("BT_292e", EntityType.TARGET),
						new DrawTask())
				}));

			// [BT_334] Lady Liadrin - Battlecry: Add a copy of each spell you cast on friendly characters this game to your hand.
			cards.Add("BT_334", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (PlayHistoryEntry entry in c.PlayHistory.Where(h =>
						h.SourceCard.Type == CardType.SPELL &&
						h.TargetController == c.PlayerId &&
						h.TargetCard != null))
					{
						if (c.HandZone.IsFull)
							break;
						IPlayable copy = Entity.FromCard(c, entry.SourceCard);
						copy[GameTag.DISPLAYED_CREATOR] = s.Id;
						Generic.AddHandPhase.Invoke(c, copy);
					}
				})
			}));
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

			// [BT_003] Netherwind Portal - Secret: After your opponent casts a spell, summon a random 4-Cost minion.
			cards.Add("BT_003", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_CAST)
				{
					SingleTask = ComplexTask.Secret(SummonRandomCostMinion(4))
				}
			}));

			// [BT_004] Imprisoned Observer - Dormant for 2 turns. When this awakens, deal 2 damage to all enemy minions.
			cards.Add("BT_004", new CardDef(new Power
			{
				PowerTask = StartDormant(2),
				Trigger = DormantAwakenTrigger(new DamageTask(2, EntityType.OP_MINIONS, false))
			}));

			// [BT_006] Evocation - Fill your hand with random Mage spells. At the end of your turn, discard them.
			cards.Add("BT_006", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					var spells = Cards.AllStandard.Where(card => card.Collectible && card.Type == CardType.SPELL && card.Class == CardClass.MAGE).ToList();
					while (!c.HandZone.IsFull && spells.Count > 0)
					{
						IPlayable spell = Entity.FromCard(c, spells.Choose(g.Random));
						if (Generic.AddHandPhase.Invoke(c, spell))
							g.GhostlyCards.Add(spell.Id);
						g.OnRandomHappened(true);
					}
				})
			}));

			// [BT_014] Starscryer - Deathrattle: Draw a spell.
			cards.Add("BT_014", new CardDef(new Power
			{
				DeathrattleTask = DrawCardTypeFromDeck(CardType.SPELL)
			}));

			// [BT_021] Font of Power - Discover a Mage minion. If your deck has no minions, keep all 3.
			cards.Add("BT_021", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (c.DeckZone.Any(p => p.Card.Type == CardType.MINION))
					{
						new DiscoverTask(CardType.MINION, CardClass.MAGE).Process(g, c, s, t, stack);
						return;
					}

					var minions = Cards.AllStandard.Where(card => card.Collectible && card.Type == CardType.MINION && card.Class == CardClass.MAGE).ToArray();
					Card[] choices = DiscoverTask.GetChoices(new[] { minions }, 3, g.Random);
					foreach (Card card in choices)
						if (!c.HandZone.IsFull)
							Generic.DrawCard(c, card);
					if (choices.Length > 0)
						g.OnRandomHappened(true);
				})
			}));

			// [BT_022] Apexis Smuggler - After you play a Secret, Discover a spell.
			cards.Add("BT_022", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.PLAY_CARD)
				{
					TriggerSource = TriggerSource.FRIENDLY,
					Condition = SelfCondition.IsSecret,
					SingleTask = new DiscoverTask(DiscoverType.SPELL)
				}
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

			// [BT_203] Pack Tactics - Secret: When a friendly minion is attacked, summon a 3/3 copy.
			cards.Add("BT_203", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.ATTACK)
				{
					Condition = SelfCondition.IsProposedDefender(CardType.MINION),
					SingleTask = ComplexTask.Secret(
						new SummonCopyTask(EntityType.EVENT_TARGET),
						new CustomTask((g, c, s, t, stack) =>
						{
							Minion copy = c.BoardZone.LastOrDefault();
							if (copy == null)
								return;
							copy.AttackDamage = 3;
							copy.BaseHealth = copy.Damage + 3;
						}))
				}
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

			// [BT_211] Imprisoned Felmaw - Dormant for 2 turns. When this awakens, attack a random enemy.
			cards.Add("BT_211", new CardDef(new Power
			{
				PowerTask = StartDormant(2),
				Trigger = DormantAwakenTrigger(new CustomTask((g, c, s, t, stack) =>
				{
					if (!(s is Minion felmaw))
						return;
					var enemies = c.Opponent.BoardZone.GetAll().Cast<ICharacter>().Concat(new[] { c.Opponent.Hero }).Where(p => !p.ToBeDestroyed).ToList();
					if (enemies.Count == 0)
						return;
					Generic.AttackBlock.Invoke(c, felmaw, enemies.Choose(g.Random), true, false);
					g.OnRandomHappened(true);
				}))
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

			// [BT_212] Mok'Nathal Lion - Rush. Battlecry: Choose a friendly minion. Gain a copy of its Deathrattle.
			cards.Add("BT_212", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_IF_AVAILABLE, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 },
					{ PlayReq.REQ_FRIENDLY_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(s is Minion lion) || !(t is Minion target) || !target.HasDeathrattle)
							return;
						Generic.AddEnchantmentBlock(g, Cards.FromId("BT_212e"), lion, lion, 0, 0, target.Id);
						lion.HasDeathrattle = true;
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

			// [BT_127] Imprisoned Satyr - Dormant for 2 turns. When this awakens, reduce a random minion in your hand by (5).
			cards.Add("BT_127", new CardDef(new Power
			{
				PowerTask = StartDormant(2),
				Trigger = DormantAwakenTrigger(new CustomTask((g, c, s, t, stack) =>
				{
					var minions = c.HandZone.GetAll().Where(p => p.Card.Type == CardType.MINION).ToList();
					if (minions.Count == 0)
						return;
					Generic.AddEnchantmentBlock(g, Cards.FromId("BT_127e"), (IPlayable)s, minions.Choose(g.Random), 0, 0, 0);
					if (minions.Count > 1)
						g.OnRandomHappened(true);
				}))
			}));

			// [BT_131] Ysiel Windsinger - Your spells cost (1).
			cards.Add("BT_131", new CardDef(new Power
			{
				Aura = new Aura(AuraType.HAND, Effects.SetCost(1))
				{
					Condition = SelfCondition.IsSpell
				}
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

			// [BT_126] Teron Gorefiend - Battlecry: Destroy all other friendly minions. Deathrattle: Resummon them with +1/+1.
			cards.Add("BT_126", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(s is Minion teron))
						return;
					foreach (Minion minion in c.BoardZone.GetAll().Where(p => p != teron).ToArray())
					{
						Generic.AddEnchantmentBlock(g, Cards.FromId("BT_126e"), teron, teron, 0, 0, minion.Id);
						minion.Destroy();
					}
					teron.HasDeathrattle = true;
				})
			}));

			// [BT_123] Kargath Bladefist - Rush. Deathrattle: Shuffle Kargath Prime into your deck.
			cards.Add("BT_123", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("BT_123t", EntityType.DECK)
			}));

			// [BT_121] Imprisoned Gan'arg - Dormant for 2 turns. When this awakens, equip a 3/2 Axe.
			cards.Add("BT_121", new CardDef(new Power
			{
				PowerTask = StartDormant(2),
				Trigger = DormantAwakenTrigger(new WeaponTask("CS2_106"))
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

			// [BT_781] Bulwark of Azzinoth - Whenever your hero would take damage, this loses 1 Durability instead.
			cards.Add("BT_781", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.PREDAMAGE)
				{
					TriggerSource = TriggerSource.HERO,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						Weapon weapon = c.Hero.Weapon;
						if (weapon == null || weapon.Card.Id != "BT_781")
							return;
						weapon.Damage += 1;
						g.CurrentEventData.EventNumber = 0;
						if (weapon.ToBeDestroyed)
							c.Hero.RemoveWeapon();
					})
				}
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

			// [BT_321] Netherwalker - Battlecry: Discover a Demon.
			cards.Add("BT_321", new CardDef(new Power
			{
				PowerTask = new DiscoverTask(DiscoverType.DEMON)
			}));

			// [BT_323] Sightless Watcher - Battlecry: Look at 3 cards in your deck. Choose one to put on top.
			cards.Add("BT_323", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					var choices = c.DeckZone.GetAll().Take(3).ToList();
					if (choices.Count <= 1)
						return;
					IPlayable choice = choices.OrderByDescending(p => p.Cost).First();
					Generic.RemoveFromZone.Invoke(c, choice);
					c.DeckZone.Add(choice, 0);
				})
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

			// [BT_416] Raging Felscreamer - Battlecry: The next Demon you play costs (2) less.
			cards.Add("BT_416", new CardDef(new Power
			{
				PowerTask = new AddEnchantmentTask("BT_416e", EntityType.SOURCE)
			}));

			// [BT_429] Metamorphosis - Swap your Hero Power to "Deal 5 damage." After 2 uses, swap it back.
			cards.Add("BT_429", new CardDef(new Power
			{
				PowerTask = new ReplaceHeroPower(Cards.FromId("BT_429p"))
			}));

			// [BT_429p] Demonic Blast - Deal 5 damage. Two uses left.
			cards.Add("BT_429p", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new DamageTask(5, EntityType.TARGET, false),
						new ReplaceHeroPower(Cards.FromId("BT_429p2")))
				}));

			// [BT_429p2] Demonic Blast - Deal 5 damage. Last use.
			cards.Add("BT_429p2", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new DamageTask(5, EntityType.TARGET, false),
						new ReplaceHeroPower(Cards.FromId("HERO_10p")))
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

			// [BT_481] Nethrandamus - Battlecry: Summon two random upgraded-cost minions. Upgrades when friendly minions die.
			cards.Add("BT_481", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (s[GameTag.TAG_SCRIPT_DATA_NUM_2] > 0)
						return;
					s[GameTag.TAG_SCRIPT_DATA_NUM_2] = 1;
					int cost = System.Math.Min(10, s[GameTag.TAG_SCRIPT_DATA_NUM_1]);
					var minions = Cards.AllStandard.Where(card => card.Type == CardType.MINION && card.Cost == cost).ToList();
					for (int i = 0; i < 2 && !c.BoardZone.IsFull && minions.Count > 0; i++)
					{
						Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, minions.Choose(g.Random)), -1, s);
						g.OnRandomHappened(true);
					}
				}),
				Trigger = new Trigger(TriggerType.DEATH)
				{
					TriggerActivation = TriggerActivation.HAND,
					TriggerSource = TriggerSource.FRIENDLY,
					Condition = SelfCondition.IsMinion,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						s[GameTag.TAG_SCRIPT_DATA_NUM_1] = System.Math.Min(10, s[GameTag.TAG_SCRIPT_DATA_NUM_1] + 1);
					})
				}
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
			// [BT_197] Reliquary of Souls - Lifesteal. Deathrattle: Shuffle Reliquary Prime into your deck.
			cards.Add("BT_197", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("BT_197t", EntityType.DECK)
			}));

			// [BT_197t] Reliquary Prime - Taunt, Lifesteal. Only you can target this with spells and Hero Powers.
			cards.Add("BT_197t", new CardDef(new Power()));

			// [BT_252] Renew - Restore 3 Health. Discover a spell.
			cards.Add("BT_252", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new HealTask(3, EntityType.TARGET),
						new DiscoverTask(DiscoverType.SPELL))
				}));

			// [BT_258] Imprisoned Homunculus - Dormant for 2 turns. Taunt.
			cards.Add("BT_258", new CardDef(new Power
			{
				PowerTask = StartDormant(2),
				Trigger = DormantAwakenTrigger(null)
			}));

			// [BT_198] Soul Mirror - Summon copies of enemy minions. They attack their copies.
			cards.Add("BT_198", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion original in c.Opponent.BoardZone.GetAll().ToArray())
					{
						if (c.BoardZone.IsFull)
							break;
						var copy = (Minion)Entity.FromCard(c, original.Card, new EntityData((EntityData)original.NativeTags), c.BoardZone, creator: s);
						original.CopyInternalAttributes(copy);
						Generic.AttackBlock.Invoke(c, copy, original, true, false);
					}
				})
			}));

			// [BT_253] Psyche Split - Give a minion +1/+2. Summon a copy of it.
			cards.Add("BT_253", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = ComplexTask.Create(
						new AddEnchantmentTask("BT_253e", EntityType.TARGET),
						new SummonCopyTask(EntityType.TARGET))
				}));

			// [BT_254] Sethekk Veilweaver - After you cast a spell on a minion, add a Priest spell to your hand.
			cards.Add("BT_254", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_CAST)
				{
					TriggerSource = TriggerSource.FRIENDLY,
					Condition = new SelfCondition(p => p.Game.CurrentEventData?.EventTarget?.Card.Type == CardType.MINION),
					SingleTask = AddRandomClassCardToHand(CardClass.PRIEST, CardType.SPELL)
				}
			}));

			// [BT_256] Dragonmaw Overseer - At the end of your turn, give another friendly minion +2/+2.
			cards.Add("BT_256", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						var targets = c.BoardZone.GetAll().Where(p => p != s).ToList();
						if (targets.Count == 0)
							return;
						Minion target = targets.Choose(g.Random);
						Generic.AddEnchantmentBlock(g, Cards.FromId("BT_256e"), (IPlayable)s, target, 0, 0, 0);
						if (targets.Count > 1)
							g.OnRandomHappened(true);
					})
				}
			}));

			// [BT_257] Apotheosis - Give a minion +2/+3 and Lifesteal.
			cards.Add("BT_257", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new AddEnchantmentTask("BT_257e", EntityType.TARGET)
				}));

			// [BT_262] Dragonmaw Sentinel - Battlecry: If you're holding a Dragon, gain +1 Attack and Lifesteal.
			cards.Add("BT_262", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(s is Minion sentinel) || !c.HandZone.Any(p => p.Card.IsRace(Race.DRAGON)))
						return;
					Generic.AddEnchantmentBlock(g, Cards.FromId("BT_262e"), sentinel, sentinel, 0, 0, 0);
					sentinel[GameTag.LIFESTEAL] = 1;
				})
			}));

			// [BT_341] Skeletal Dragon - Taunt. At the end of your turn, add a Dragon to your hand.
			cards.Add("BT_341", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = AddRandomRaceCardToHand(Race.DRAGON)
				}
			}));

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

			// [BT_155] Scrapyard Colossus - Taunt. Deathrattle: Summon a 7/7 Felcracked Colossus with Taunt.
			cards.Add("BT_155", new CardDef(new Power
			{
				DeathrattleTask = new SummonTask("BT_155t", SummonSide.DEATHRATTLE)
			}));

			// [BT_156] Imprisoned Vilefiend - Dormant for 2 turns. Rush.
			cards.Add("BT_156", new CardDef(new Power
			{
				PowerTask = StartDormant(2),
				Trigger = DormantAwakenTrigger(null)
			}));

			// [BT_159] Terrorguard Escapee - Battlecry: Summon three 1/1 Huntresses for your opponent.
			cards.Add("BT_159", new CardDef(new Power
			{
				PowerTask = new SummonOpTask("BT_159t", 3)
			}));

			// [BT_160] Rustsworn Cultist - Battlecry: Give your other minions "Deathrattle: Summon a 1/1 Demon."
			cards.Add("BT_160", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion minion in c.BoardZone.GetAll().Where(p => p != s))
						Generic.AddEnchantmentBlock(g, Cards.FromId("BT_160e"), (IPlayable)s, minion, 0, 0, 0);
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

			// [BT_717] Burrowing Scorpid - Battlecry: Deal 2 damage. If that kills the target, gain Stealth.
			cards.Add("BT_717", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_IF_AVAILABLE, 0 }
				},
				new Power
				{
					PowerTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(s is Minion scorpid) || !(t is ICharacter target))
							return;
						Generic.DamageCharFunc.Invoke(scorpid, target, 2, false);
						if (target.ToBeDestroyed)
							scorpid[GameTag.STEALTH] = 1;
					})
				}));

			// [BT_720] Ruststeed Raider - Taunt, Rush. Battlecry: Gain +4 Attack this turn.
			cards.Add("BT_720", new CardDef(new Power
			{
				PowerTask = new AddEnchantmentTask("BT_720e", EntityType.SOURCE)
			}));

			// [BT_721] Blistering Rot - At the end of your turn, summon a Rot with stats equal to this minion's.
			cards.Add("BT_721", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(s is Minion source) || c.BoardZone.IsFull)
							return;
						var rot = (Minion)Entity.FromCard(c, Cards.FromId("BT_721t"));
						rot[GameTag.ATK] = source.AttackDamage;
						rot[GameTag.HEALTH] = source.Health;
						Generic.SummonBlock.Invoke(g, rot, -1, s);
					})
				}
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

			// [BT_726] Dragonmaw Sky Stalker - Deathrattle: Summon a 3/4 Dragonrider.
			cards.Add("BT_726", new CardDef(new Power
			{
				DeathrattleTask = new SummonTask("BT_726t", SummonSide.DEATHRATTLE)
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

			// [BT_728] Disguised Wanderer - Deathrattle: Summon a 9/1 Inquisitor.
			cards.Add("BT_728", new CardDef(new Power
			{
				DeathrattleTask = new SummonTask("BT_728t", SummonSide.DEATHRATTLE)
			}));

			// [BT_729] Waste Warden - Battlecry: Deal 3 damage to a minion and all others of the same minion type.
			cards.Add("BT_729", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_TO_PLAY, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(t is Minion target))
							return;
						Race race = target.Card.GetRawRace();
						foreach (Minion minion in c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).Where(p => p.Card.IsRace(race)).ToArray())
							Generic.DamageCharFunc.Invoke((IPlayable)s, minion, 3, false);
					})
				}));

			// [BT_730] Overconfident Orc - Taunt. While at full Health, this has +2 Attack.
			cards.Add("BT_730", new CardDef(new Power
			{
				Aura = new AdaptiveEffect(GameTag.ATK, EffectOperator.ADD, p => p is ICharacter character && character.Damage == 0 ? 2 : 0)
			}));

			// [BT_731] Infectious Sporeling - After this damages a minion, turn it into an Infectious Sporeling.
			cards.Add("BT_731", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.SELF,
					Condition = new SelfCondition(p => p.Game.CurrentEventData?.EventTarget?.Card.Type == CardType.MINION),
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (g.CurrentEventData?.EventTarget is Minion target && target.Damage > 0 && !target.ToBeDestroyed)
							Generic.TransformBlock.Invoke(target.Controller, Cards.FromId("BT_731"), target);
					})
				}
			}));

			// [BT_732] Scavenging Shivarra - Battlecry: Deal 6 damage randomly split among all other minions.
			cards.Add("BT_732", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					for (int i = 0; i < 6; i++)
					{
						var targets = c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).Where(p => p != s && !p.ToBeDestroyed).Cast<IPlayable>().ToList();
						if (targets.Count == 0)
							return;
						Generic.DamageCharFunc.Invoke((IPlayable)s, (ICharacter)targets.Choose(g.Random), 1, false);
						g.OnRandomHappened(true);
					}
				})
			}));

			// [BT_190] Replicat-o-tron - At the end of your turn, transform a neighbor into a copy of this.
			cards.Add("BT_190", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(s is Minion replicator))
							return;
						var adjacent = replicator.GetAdjacentMinions().ToList();
						if (adjacent.Count == 0)
							return;
						Generic.TransformBlock.Invoke(c, Cards.FromId("BT_190"), adjacent.Choose(g.Random));
						if (adjacent.Count > 1)
							g.OnRandomHappened(true);
					})
				}
			}));

			// [BT_733] Mo'arg Artificer - All minions take double damage from spells.
			cards.Add("BT_733", new CardDef(new Power()));

			// [BT_735] Al'ar - Deathrattle: Summon 0/3 Ashes of Al'ar that resurrects this minion on your next turn.
			cards.Add("BT_735", new CardDef(new Power
			{
				DeathrattleTask = new SummonTask("BT_735t", SummonSide.DEATHRATTLE)
			}));

			// [BT_735t] Ashes of Al'ar - At the start of your turn, transform this into Al'ar.
			cards.Add("BT_735t", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_START)
				{
					SingleTask = new ChangeEntityTask("BT_735")
				}
			}));

			// [BT_934] Imprisoned Antaen - Dormant for 2 turns. When this awakens, deal 10 damage randomly split among all enemies.
			cards.Add("BT_934", new CardDef(new Power
			{
				PowerTask = StartDormant(2),
				Trigger = DormantAwakenTrigger(ComplexTask.Repeat(ComplexTask.DamageRandomTargets(1, EntityType.ENEMIES, 1), 10))
			}));

			// [BT_734] Supreme Abyssal - Can't attack heroes.
			cards.Add("BT_734", new CardDef(new Power
			{
				PowerTask = new SetGameTagTask(GameTag.CANNOT_ATTACK_HEROES, 1, EntityType.SOURCE)
			}));

			// [BT_737] Maiev Shadowsong - Battlecry: Choose a minion. It goes Dormant for 2 turns.
			cards.Add("BT_737", new CardDef(
				new Dictionary<PlayReq, int>
				{
					{ PlayReq.REQ_TARGET_IF_AVAILABLE, 0 },
					{ PlayReq.REQ_MINION_TARGET, 0 }
				},
				new Power
				{
					PowerTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(t is Minion target))
							return;
						target[GameTag.DORMANT] = 2;
						target[GameTag.UNTOUCHABLE] = 1;
						target.IsExhausted = true;
						Generic.AddEnchantmentBlock(g, Cards.FromId("BT_737e"), (IPlayable)s, target, 2, 0, 0);
					})
				}));

			// [BT_850] Magtheridon - Dormant. Battlecry: Summon three enemy Warders. When they die, destroy all minions and awaken.
			cards.Add("BT_850", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(s is Minion magtheridon))
						return;
					magtheridon[GameTag.DORMANT] = 3;
					magtheridon[GameTag.UNTOUCHABLE] = 1;
					magtheridon.IsExhausted = true;
					for (int i = 0; i < 3 && !c.Opponent.BoardZone.IsFull; i++)
					{
						var warder = (Minion)Entity.FromCard(c.Opponent, Cards.FromId("BT_850t"));
						warder.HasDeathrattle = true;
						Generic.SummonBlock.Invoke(g, warder, -1, s);
					}
				}),
				Trigger = new Trigger(TriggerType.DEATH)
				{
					TriggerSource = TriggerSource.OP_MINIONS,
					Condition = new SelfCondition(p => p.Card.Id == "BT_850t"),
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(s is Minion magtheridon) || magtheridon[GameTag.DORMANT] <= 0)
							return;
						magtheridon[GameTag.DORMANT] -= 1;
						if (magtheridon[GameTag.DORMANT] > 0)
							return;
						foreach (Minion minion in c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).Where(p => p != magtheridon).ToArray())
							minion.Destroy();
						magtheridon[GameTag.UNTOUCHABLE] = 0;
						magtheridon.IsExhausted = false;
					})
				}
			}));

			// [BT_850t] Hellfire Warder - Counts toward Magtheridon's awakening.
			cards.Add("BT_850t", new CardDef(new Power
			{
				DeathrattleTask = new CustomTask((g, c, s, t, stack) =>
				{
					Minion magtheridon = c.Opponent.BoardZone.GetAll().FirstOrDefault(p => p.Card.Id == "BT_850" && p[GameTag.DORMANT] > 0);
					if (magtheridon == null)
						return;
					magtheridon[GameTag.DORMANT] -= 1;
					if (magtheridon[GameTag.DORMANT] > 0)
						return;
					foreach (Minion minion in c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).Where(p => p != magtheridon).ToArray())
						minion.Destroy();
					magtheridon[GameTag.UNTOUCHABLE] = 0;
					magtheridon.IsExhausted = false;
				})
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

			cards.Add("BT_126e", new CardDef(new Power
			{
				DeathrattleTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(t is Enchantment enchantment) || enchantment.CapturedCard == null || c.BoardZone.IsFull)
						return;
					Minion minion = (Minion)Entity.FromCard(c, enchantment.CapturedCard);
					Generic.SummonBlock.Invoke(g, minion, -1, s);
					Generic.AddEnchantmentBlock(g, Cards.FromId("BT_126e2"), (IPlayable)s, minion, 0, 0, 0);
				})
			}));

			cards.Add("BT_126e2", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1), Effects.Health_N(1))
			}));

			cards.Add("BT_212e", new CardDef(new Power
			{
				DeathrattleTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(t is Enchantment enchantment))
						return;
					ISimpleTask copiedDeathrattle = enchantment.CapturedCard?.Power?.DeathrattleTask;
					copiedDeathrattle?.Process(g, c, s, null, stack);
				})
			}));

			cards.Add("BT_002e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.ReduceCost(1))
			}));

			cards.Add("BT_020e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.ReduceCost(1))
			}));

			cards.Add("BT_026e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.ReduceCost(2))
			}));

			cards.Add("BT_025e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1), Effects.Health_N(1)),
				DeathrattleTask = new AddCardTo("BT_025", EntityType.HAND)
			}));

			cards.Add("BT_292e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(2), Effects.Health_N(2))
			}));

			cards.Add("BT_253e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1), Effects.Health_N(2))
			}));

			cards.Add("BT_256e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(2), Effects.Health_N(2))
			}));

			cards.Add("BT_257e", new CardDef(new Power
			{
				Enchant = new Enchant(
					Effects.Attack_N(2),
					Effects.Health_N(3),
					new Effect(GameTag.LIFESTEAL, EffectOperator.SET, 1))
			}));

			cards.Add("BT_262e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1))
			}));

			cards.Add("BT_702e", new CardDef(new Power
			{
				Enchant = new Enchant(
					Effects.Attack_N(3),
					new Effect(GameTag.IMMUNE, EffectOperator.SET, 1))
				{
					IsOneTurnEffect = true
				}
			}));

			cards.Add("BT_302e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.ReduceCost(5))
			}));

			cards.Add("BT_306e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(2), Effects.Health_N(2))
			}));

			cards.Add("BT_127e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.ReduceCost(5))
			}));

			cards.Add("BT_305e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(2), Effects.Health_N(2))
			}));

			cards.Add("BT_416e", new CardDef(new Power
			{
				Aura = new Aura(AuraType.HAND, Effects.ReduceCost(2))
				{
					Condition = SelfCondition.IsRace(Race.DEMON),
					RemoveTrigger = (TriggerType.PLAY_MINION, SelfCondition.IsRace(Race.DEMON))
				}
			}));

			cards.Add("BT_711e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AddCost(2))
			}));

			cards.Add("BT_737e", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_START)
				{
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(s is Enchantment enchantment) || !(enchantment.Target is Minion minion))
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
						enchantment.Remove();
					})
				}
			}));

			cards.Add("BT_101e", new CardDef(new Power
			{
				DeathrattleTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (s is Minion minion && !c.BoardZone.IsFull)
						Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, minion.Card), -1, s);
				})
			}));

			cards.Add("BT_113e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(2), Effects.Health_N(2))
			}));

			cards.Add("BT_160e", new CardDef(new Power
			{
				DeathrattleTask = new SummonTask("BT_160t", SummonSide.DEATHRATTLE)
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

		private static ISimpleTask SummonRandomCostMinion(int cost)
		{
			return new CustomTask((g, c, s, t, stack) =>
			{
				if (c.BoardZone.IsFull)
					return;
				var minions = Cards.AllStandard.Where(card => card.Type == CardType.MINION && card.Cost == cost).ToList();
				if (minions.Count == 0)
					return;
				Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, minions.Choose(g.Random)), -1, s);
				g.OnRandomHappened(true);
			});
		}

		private static ISimpleTask AddRandomClassCardToHand(CardClass cardClass, CardType cardType)
		{
			return new CustomTask((g, c, s, t, stack) =>
			{
				var cards = Cards.AllStandard.Where(card => card.Collectible && card.Class == cardClass && card.Type == cardType).ToList();
				if (cards.Count == 0)
					return;
				Generic.DrawCard(c, cards.Choose(g.Random));
				g.OnRandomHappened(true);
			});
		}

		private static ISimpleTask AddRandomRaceCardToHand(Race race)
		{
			return new CustomTask((g, c, s, t, stack) =>
			{
				var cards = Cards.AllStandard.Where(card => card.Collectible && card.Type == CardType.MINION && card.IsRace(race)).ToList();
				if (cards.Count == 0)
					return;
				Generic.DrawCard(c, cards.Choose(g.Random));
				g.OnRandomHappened(true);
			});
		}

		private static ISimpleTask ReduceLibrams(string enchantmentId)
		{
			return new CustomTask((g, c, s, t, stack) =>
			{
				foreach (IPlayable card in c.HandZone.GetAll().Concat(c.DeckZone.GetAll()).Where(p => IsLibram(p.Card)))
					Generic.AddEnchantmentBlock(g, Cards.FromId(enchantmentId), (IPlayable)s, card, 0, 0, 0);
			});
		}

		private static ISimpleTask StartDormant(int turns)
		{
			return new CustomTask((g, c, s, t, stack) =>
			{
				if (!(s is Minion minion))
					return;
				minion[GameTag.DORMANT] = turns;
				minion[GameTag.UNTOUCHABLE] = 1;
				minion.IsExhausted = true;
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

		private static bool IsLibram(Card card)
		{
			return card.Id == "BT_011" || card.Id == "BT_024" || card.Id == "BT_025";
		}

		private static bool HasCastSpellRecently(Controller controller)
		{
			return controller.NumSpellsPlayedLastTurn > 0 || controller.NumSpellsPlayedThisGame > 0;
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
