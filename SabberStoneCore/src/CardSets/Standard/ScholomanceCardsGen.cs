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
	public class ScholomanceCardsGen
	{
		private static readonly Dictionary<int, List<string>> RememberedElekkSpellIds = new Dictionary<int, List<string>>();

		public static void AddAll(Dictionary<string, CardDef> cards)
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
			Warrior(cards);
			Warlock(cards);
			NonCollect(cards);
		}

		private static void DemonHunter(IDictionary<string, CardDef> cards)
		{
			// [SCH_618] Blood Herald - Whenever a friendly minion dies while this is in your hand, gain +1/+1.
			cards.Add("SCH_618", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.DEATH)
				{
					TriggerActivation = TriggerActivation.HAND,
					TriggerSource = TriggerSource.FRIENDLY,
					Condition = SelfCondition.IsMinion,
					SingleTask = new AddEnchantmentTask("SCH_618e", EntityType.SOURCE)
				}
			}));

			// [SCH_600] Demon Companion - Summon a random Demon Companion.
			cards.Add("SCH_600", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					string[] companions = {"SCH_600t1", "SCH_600t2", "SCH_600t3"};
					Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, Cards.FromId(companions[g.Random.Next(companions.Length)])), -1, s);
				})
			}));

			// [SCH_252] Marrowslicer - Battlecry: Shuffle 2 Soul Fragments into your deck.
			cards.Add("SCH_252", new CardDef(new Power
			{
				PowerTask = new AddCardTo("SCH_307t", EntityType.DECK, 2)
			}));

			// [SCH_355] Shardshatter Mystic - Battlecry: Destroy a Soul Fragment in your deck to deal 3 damage to all other minions.
			cards.Add("SCH_355", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!DestroySoulFragment(c))
						return;

					foreach (Minion minion in c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).Where(p => p != s).ToArray())
						Generic.DamageCharFunc.Invoke(s as IPlayable, minion, 3, false);
				})
			}));

			// [SCH_704] Soulshard Lapidary - Battlecry: Destroy a Soul Fragment in your deck to give your hero +5 Attack this turn.
			cards.Add("SCH_704", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (DestroySoulFragment(c))
						Generic.AddEnchantmentBlock(g, Cards.FromId("SCH_704e"), s as IPlayable, c.Hero, 0, 0, 0);
				})
			}));
		}

		private static void Druid(IDictionary<string, CardDef> cards)
		{
			// [SCH_242] Gibberling - Spellburst: Summon a Gibberling.
			cards.Add("SCH_242", new CardDef(new Power
			{
				Trigger = Spellburst(new SummonTask("SCH_242", SummonSide.SPELL))
			}));

			// [SCH_609] Survival of the Fittest - Give +4/+4 to all minions in your hand, deck, and battlefield.
			cards.Add("SCH_609", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (IPlayable playable in c.HandZone.GetAll()
						.Concat(c.DeckZone.GetAll())
						.Concat(c.BoardZone.GetAll())
						.Where(p => p.Card.Type == CardType.MINION)
						.ToArray())
						Generic.AddEnchantmentBlock(g, Cards.FromId("SCH_609e"), s as IPlayable, playable, 0, 0, 0);
				})
			}));

			// [SCH_613] Groundskeeper - Taunt. Battlecry: If you're holding a spell that costs (5) or more, restore 5 Health.
			cards.Add("SCH_613", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (c.HandZone.Any(p => p.Card.Type == CardType.SPELL && p.Card.Cost >= 5))
						c.Hero.TakeHeal(s as IPlayable, 5);
				})
			}));

			// [SCH_606] Partner Assignment - Add a random 2-Cost and 3-Cost Beast to your hand.
			cards.Add("SCH_606", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					AddRandomMinionToHand(g, c, s, card => card.Cost == 2 && card.IsRace(Race.BEAST));
					AddRandomMinionToHand(g, c, s, card => card.Cost == 3 && card.IsRace(Race.BEAST));
				})
			}));

			// [SCH_616] Twilight Runner - Stealth. Whenever this attacks, draw 2 cards.
			cards.Add("SCH_616", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.SELF,
					SingleTask = new DrawTask(2)
				}
			}));
		}

		private static void Hunter(IDictionary<string, CardDef> cards)
		{
			// [SCH_604] Overwhelm - Deal 2 damage to a minion, plus one more for each Beast you control.
			cards.Add("SCH_604", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					int beasts = c.BoardZone.GetAll(p => p.Card.IsRace(Race.BEAST)).Length;
					Generic.DamageCharFunc.Invoke(s as IPlayable, t as ICharacter, 2 + beasts + c.CurrentSpellPower, false);
				})
			}));

			// [SCH_617] Adorable Infestation - Give a minion +1/+1. Summon a 1/1 Cub. Add a Cub to your hand.
			cards.Add("SCH_617", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = ComplexTask.Create(
					new AddEnchantmentTask("SCH_617e", EntityType.TARGET),
					new SummonTask("SCH_617t", SummonSide.SPELL),
					new AddCardTo("SCH_617t", EntityType.HAND))
			}));

			// [SCH_239] Krolusk Barkstripper - Spellburst: Destroy a random enemy minion.
			cards.Add("SCH_239", new CardDef(new Power
			{
				Trigger = Spellburst(new CustomTask((g, c, s, t, stack) =>
				{
					Minion[] enemies = c.Opponent.BoardZone.GetAll();
					if (enemies.Length > 0)
						enemies.Choose(g.Random).Destroy();
				}))
			}));
		}

		private static void Mage(IDictionary<string, CardDef> cards)
		{
			// [SCH_241] Firebrand - Spellburst: Deal 4 damage randomly split among all enemy minions.
			cards.Add("SCH_241", new CardDef(new Power
			{
				Trigger = Spellburst(new CustomTask((g, c, s, t, stack) =>
				{
					for (int i = 0; i < 4; i++)
					{
						Minion[] enemies = c.Opponent.BoardZone.GetAll();
						if (enemies.Length == 0)
							return;

						Generic.DamageCharFunc.Invoke(s as IPlayable, enemies.Choose(g.Random), 1, false);
					}
				}))
			}));

			// [SCH_243] Wyrm Weaver - Spellburst: Summon two 1/3 Mana Wyrms.
			cards.Add("SCH_243", new CardDef(new Power
			{
				Trigger = Spellburst(new SummonTask("NEW1_012", 2, SummonSide.SPELL))
			}));

			// [SCH_509] Brain Freeze - Freeze a minion. Combo: Also deal 3 damage to it.
			cards.Add("SCH_509", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = ComplexTask.Freeze(EntityType.TARGET),
				ComboTask = ComplexTask.Create(
					ComplexTask.Freeze(EntityType.TARGET),
					new DamageTask(3, EntityType.TARGET, true))
			}));
		}

		private static void Neutral(IDictionary<string, CardDef> cards)
		{
			// [SCH_135] Turalyon, the Tenured - Rush. Whenever this attacks a minion, set the defender's Attack and Health to 3.
			cards.Add("SCH_135", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.ATTACK)
				{
					TriggerSource = TriggerSource.SELF,
					Condition = SelfCondition.IsEventTargetIs(CardType.MINION),
					SingleTask = new AddEnchantmentTask("SCH_135e", EntityType.EVENT_TARGET)
				}
			}));

			// [SCH_133] Wolpertinger - Battlecry: Summon a copy of this.
			cards.Add("SCH_133", new CardDef(new Power
			{
				PowerTask = new SummonCopyTask(EntityType.SOURCE)
			}));

			// [SCH_147] Boneweb Egg - Deathrattle: Summon two 2/1 Spiders. If discarded, trigger its Deathrattle.
			cards.Add("SCH_147", new CardDef(new Power
			{
				DeathrattleTask = new SummonTask("SCH_147t", 2, SummonSide.DEATHRATTLE),
				Trigger = new Trigger(TriggerType.DISCARD)
				{
					TriggerActivation = TriggerActivation.HAND,
					TriggerSource = TriggerSource.SELF,
					SingleTask = new SummonTask("SCH_147t", 2, SummonSide.DEATHRATTLE)
				}
			}));

			// [SCH_146] Robes of Protection - Your minions have "Can't be targeted by spells or Hero Powers."
			cards.Add("SCH_146", new CardDef(new Power
			{
				Aura = new Aura(AuraType.BOARD, Effects.CantBeTargetedBySpellsAndHeroPowers)
			}));

			// [SCH_231] Intrepid Initiate - Spellburst: Gain +2 Attack.
			cards.Add("SCH_231", new CardDef(new Power
			{
				Trigger = Spellburst(new AddEnchantmentTask("SCH_231e", EntityType.SOURCE))
			}));

			// [SCH_311] Animated Broomstick - Rush. Battlecry: Give your other minions Rush.
			cards.Add("SCH_311", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion minion in c.BoardZone.GetAll(p => p != s))
						minion.IsRush = true;
				})
			}));

			// [SCH_312] Tour Guide - Battlecry: Your next Hero Power costs (0).
			cards.Add("SCH_312", new CardDef(new Power
			{
				PowerTask = new AddEnchantmentTask("SCH_312e", EntityType.CONTROLLER)
			}));

			// [SCH_340] Bloated Python - Deathrattle: Summon a 4/4 Hapless Handler.
			cards.Add("SCH_340", new CardDef(new Power
			{
				DeathrattleTask = new SummonTask("SCH_340t", SummonSide.DEATHRATTLE)
			}));

			// [SCH_707] Fishy Flyer - Rush. Deathrattle: Add a 4/3 Ghost with Rush to your hand.
			cards.Add("SCH_707", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("SCH_707t", EntityType.HAND)
			}));

			// [SCH_708] Sneaky Delinquent - Stealth. Deathrattle: Add a 3/1 Ghost with Stealth to your hand.
			cards.Add("SCH_708", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("SCH_708t", EntityType.HAND)
			}));

			// [SCH_709] Smug Senior - Taunt. Deathrattle: Add a 5/7 Ghost with Taunt to your hand.
			cards.Add("SCH_709", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("SCH_709t", EntityType.HAND)
			}));

			// [SCH_710] Ogremancer - Whenever your opponent casts a spell, summon a 2/2 Skeleton with Taunt.
			cards.Add("SCH_710", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_CAST)
				{
					TriggerSource = TriggerSource.ENEMY,
					SingleTask = new SummonTask("SCH_710t", SummonSide.SPELL)
				}
			}));

			// [SCH_713] Cult Neophyte - Battlecry: Your opponent's spells cost (1) more next turn.
			cards.Add("SCH_713", new CardDef(new Power
			{
				PowerTask = new AddEnchantmentTask("SCH_713e", EntityType.OP_CONTROLLER)
			}));

			// [SCH_714] Educated Elekk - Whenever a spell is played, this minion remembers it. Deathrattle: Shuffle the spells into your deck.
			cards.Add("SCH_714", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					RememberedElekkSpellIds[s.Id] = new List<string>();
				}),
				Trigger = new Trigger(TriggerType.AFTER_CAST)
				{
					TriggerSource = TriggerSource.ALL,
					Condition = SelfCondition.IsSpell,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						RememberElekkSpell(s, t);
					})
				},
				DeathrattleTask = new CustomTask((g, c, s, t, stack) =>
				{
					ShuffleRememberedElekkSpells(c, s);
				})
			}));

			// [SCH_717] Keymaster Alabaster - Whenever your opponent draws a card, add a copy to your hand that costs (1).
			cards.Add("SCH_717", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.DRAW)
				{
					TriggerSource = TriggerSource.ENEMY,
					SingleTask = ComplexTask.Create(
						new CopyTask(EntityType.TARGET, Zone.HAND, addToStack: true),
						new AddAuraEffect(Effects.SetCost(1), EntityType.STACK))
				}
			}));

			// [SCH_313] Wretched Tutor - Spellburst: Deal 2 damage to all other minions.
			cards.Add("SCH_313", new CardDef(new Power
			{
				Trigger = Spellburst(new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion minion in c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).Where(p => p != s).ToArray())
						Generic.DamageCharFunc.Invoke(s as IPlayable, minion, 2, false);
				}))
			}));

			// [SCH_160] Wandmaker - Battlecry: Add a 1-Cost spell from your class to your hand.
			cards.Add("SCH_160", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					AddRandomSpellToHand(g, c, s, card => card.Cost == 1 && card.Class == c.HeroClass);
				})
			}));

			// [SCH_283] Manafeeder Panthara - Battlecry: If you've used your Hero Power this turn, draw a card.
			cards.Add("SCH_283", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (c.HeroPowerActivationsThisTurn > 0)
						Generic.Draw(c);
				})
			}));

			// [SCH_605] Lake Thresher - Also damages the minions next to whomever this attacks.
			cards.Add("SCH_605", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.SELF,
					Condition = SelfCondition.IsEventTargetIs(CardType.MINION),
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(s is Minion attacker) || !(g.CurrentEventData?.EventTarget is Minion target))
							return;

						foreach (Minion adjacent in target.GetAdjacentMinions().ToArray())
							Generic.DamageCharFunc.Invoke(attacker, adjacent, attacker.AttackDamage, false);
					})
				}
			}));

			// [SCH_428] Lorekeeper Polkelt - Battlecry: Reorder your deck from the highest Cost card to the lowest Cost card.
			cards.Add("SCH_428", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					List<IPlayable> cardsInDeck = c.DeckZone.GetAll()
						.OrderBy(p => p.Card.Cost)
						.ThenBy(p => p.Card.AssetId)
						.ToList();

					foreach (IPlayable playable in c.DeckZone.GetAll())
						c.DeckZone.Remove(playable);

					foreach (IPlayable playable in cardsInDeck)
						c.DeckZone.Add(playable);
				})
			}));

			// [SCH_244] Teacher's Pet - Taunt. Deathrattle: Summon a random 3-Cost Beast.
			cards.Add("SCH_244", new CardDef(new Power
			{
				DeathrattleTask = new CustomTask((g, c, s, t, stack) =>
				{
					List<Card> beasts = Cards.FormatTypeCards(g.FormatType)
						.Where(card => card.Collectible && card.Type == CardType.MINION && card.Cost == 3 && card.IsRace(Race.BEAST))
						.ToList();

					if (beasts.Count == 0 || c.BoardZone.IsFull)
						return;

					Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, beasts.Choose(g.Random)), -1, s);
				})
			}));

			// [SCH_248] Pen Flinger - Battlecry: Deal 1 damage. Spellburst: Return this to your hand.
			cards.Add("SCH_248", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_IF_AVAILABLE, 0}
			}, new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (t is ICharacter target)
						Generic.DamageCharFunc.Invoke(s as IPlayable, target, 1, false);
				}),
				Trigger = Spellburst(new ReturnHandTask(EntityType.SOURCE))
			}));
		}

		private static void Paladin(IDictionary<string, CardDef> cards)
		{
			// [SCH_138] Blessing of Authority - Give a minion +8/+8. It can't attack heroes this turn.
			cards.Add("SCH_138", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = ComplexTask.Create(
					new AddEnchantmentTask("SCH_138e", EntityType.TARGET),
					new AddEnchantmentTask("SCH_138e2", EntityType.TARGET))
			}));

			// [SCH_250] Wave of Apathy - Set the Attack of all enemy minions to 1 until your next turn.
			cards.Add("SCH_250", new CardDef(new Power
			{
				PowerTask = new AddEnchantmentTask("SCH_250e", EntityType.OP_MINIONS)
			}));

			// [SCH_149] Argent Braggart - Battlecry: Gain Attack and Health to match the highest in the battlefield.
			cards.Add("SCH_149", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(s is Minion braggart))
						return;

					Minion[] minions = c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).ToArray();
					int maxAttack = minions.Max(p => p.AttackDamage);
					int maxHealth = minions.Max(p => p.Health);
					Generic.AddEnchantmentBlock(g, Cards.FromId("SCH_149e"), braggart, braggart,
						maxAttack - braggart.AttackDamage, maxHealth - braggart.Health, 0);
				})
			}));

			// [SCH_526] Lord Barov - Battlecry: Set the Health of all other minions to 1. Deathrattle: Deal 1 damage to all minions.
			cards.Add("SCH_526", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion minion in c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).Where(p => p != s).ToArray())
						Generic.AddEnchantmentBlock(g, Cards.FromId("SCH_526e"), s as IPlayable, minion, 0, 0, 0);
				}),
				DeathrattleTask = new DamageTask(1, EntityType.ALLMINIONS, false)
			}));

			// [SCH_524] Shield of Honor - Give a damaged minion +3 Attack and Divine Shield.
			cards.Add("SCH_524", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0},
				{PlayReq.REQ_DAMAGED_TARGET, 0}
			}, new Power
			{
				PowerTask = new AddEnchantmentTask("SCH_524e", EntityType.TARGET)
			}));
		}

		private static void Priest(IDictionary<string, CardDef> cards)
		{
			// [SCH_136] Power Word: Feast - Give a minion +2/+2. Restore it to full Health at the end of this turn.
			cards.Add("SCH_136", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = new AddEnchantmentTask("SCH_136e", EntityType.TARGET)
			}));

			// [SCH_512] Initiation - Deal 4 damage to a minion. If that kills it, summon a new copy.
			cards.Add("SCH_512", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(t is Minion target))
						return;

					Generic.DamageCharFunc.Invoke(s as IPlayable, target, 4, true);
					if (target.ToBeDestroyed)
						Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, target.Card), -1, s);
				})
			}));

			// [SCH_302] Gift of Luminance - Give a minion Divine Shield, then summon a 1/1 copy of it.
			cards.Add("SCH_302", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(t is Minion target))
						return;

					target.HasDivineShield = true;
					var copy = (Minion)Entity.FromCard(c, target.Card);
					Generic.SummonBlock.Invoke(g, copy, -1, s);
					Generic.AddEnchantmentBlock(g, Cards.FromId("SCH_302e"), s as IPlayable, copy, 0, 0, 0);
					copy.HasDivineShield = true;
				})
			}));
		}

		private static void Rogue(IDictionary<string, CardDef> cards)
		{
			// [SCH_521] Coerce - Destroy a damaged minion. Combo: Destroy any minion.
			cards.Add("SCH_521", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (t is Minion target && target.Damage > 0)
						target.Destroy();
				}),
				ComboTask = new DestroyTask(EntityType.TARGET)
			}));

			// [SCH_622] Self-Sharpening Sword - After your hero attacks, gain +1 Attack.
			cards.Add("SCH_622", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.HERO,
					SingleTask = new AddEnchantmentTask("SCH_622e", EntityType.SOURCE)
				}
			}));
		}

		private static void Shaman(IDictionary<string, CardDef> cards)
		{
			// [SCH_236] Diligent Notetaker - Spellburst: Return the spell to your hand.
			cards.Add("SCH_236", new CardDef(new Power
			{
				Trigger = Spellburst(new CopyTask(EntityType.EVENT_SOURCE, Zone.HAND))
			}));

			// [SCH_615] Totem Goliath - Deathrattle: Summon all four basic Totems. Overload: (1)
			cards.Add("SCH_615", new CardDef(new Power
			{
				DeathrattleTask = ComplexTask.Create(
					new SummonTask("CS2_050", SummonSide.DEATHRATTLE),
					new SummonTask("CS2_051", SummonSide.DEATHRATTLE),
					new SummonTask("CS2_052", SummonSide.DEATHRATTLE),
					new SummonTask("NEW1_009", SummonSide.DEATHRATTLE))
			}));

			// [SCH_271] Molten Blast - Deal 2 damage. Summon that many 1/1 Elementals.
			cards.Add("SCH_271", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0}
			}, new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					int amount = 2 + c.CurrentSpellPower;
					Generic.DamageCharFunc.Invoke(s as IPlayable, t as ICharacter, amount, false);
					for (int i = 0; i < amount && !c.BoardZone.IsFull; i++)
						Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, Cards.FromId("SCH_271t")), -1, s);
				})
			}));
		}

		private static void Warrior(IDictionary<string, CardDef> cards)
		{
			// [SCH_525] In Formation! - Add 2 random Taunt minions to your hand.
			cards.Add("SCH_525", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					AddRandomMinionToHand(g, c, s, card => card[GameTag.TAUNT] == 1);
					AddRandomMinionToHand(g, c, s, card => card[GameTag.TAUNT] == 1);
				})
			}));
		}

		private static void Warlock(IDictionary<string, CardDef> cards)
		{
			// [SCH_307] School Spirits - Deal 2 damage to all minions. Shuffle 2 Soul Fragments into your deck.
			cards.Add("SCH_307", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new DamageTask(2, EntityType.ALLMINIONS, true),
					new AddCardTo("SCH_307t", EntityType.DECK, 2))
			}));

			// [SCH_343] Void Drinker - Taunt. Battlecry: Destroy a Soul Fragment in your deck to gain +3/+3.
			cards.Add("SCH_343", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (DestroySoulFragment(c))
						Generic.AddEnchantmentBlock(g, Cards.FromId("SCH_343e"), s as IPlayable, s, 0, 0, 0);
				})
			}));

			// [SCH_700] Spirit Jailer - Battlecry: Shuffle 2 Soul Fragments into your deck.
			cards.Add("SCH_700", new CardDef(new Power
			{
				PowerTask = new AddCardTo("SCH_307t", EntityType.DECK, 2)
			}));

			// [SCH_701] Soul Shear - Deal 3 damage to a minion. Shuffle 2 Soul Fragments into your deck.
			cards.Add("SCH_701", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = ComplexTask.Create(
					new DamageTask(3, EntityType.TARGET, true),
					new AddCardTo("SCH_307t", EntityType.DECK, 2))
			}));

			// [SCH_517] Shadowlight Scholar - Battlecry: Destroy a Soul Fragment in your deck to deal 3 damage.
			cards.Add("SCH_517", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_IF_AVAILABLE, 0}
			}, new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (t is ICharacter target && DestroySoulFragment(c))
						Generic.DamageCharFunc.Invoke(s as IPlayable, target, 3, false);
				})
			}));

			// [SCH_703] Soulciologist Malicia - Battlecry: For each Soul Fragment in your deck, summon a 3/3 Soul with Rush.
			cards.Add("SCH_703", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					int count = c.DeckZone.Count(p => p.Card.Id == "SCH_307t");
					for (int i = 0; i < count && !c.BoardZone.IsFull; i++)
						Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, Cards.FromId("SCH_703t")), -1, s);
				})
			}));
		}

		private static void NonCollect(IDictionary<string, CardDef> cards)
		{
			// [SCH_307t] Soul Fragment - Casts When Drawn: Restore 2 Health to your hero.
			cards.Add("SCH_307t", new CardDef(new Power
			{
				TopdeckTask = new HealTask(2, EntityType.HERO)
			}));

			// [SCH_617e] Adorable - +1/+1.
			cards.Add("SCH_617e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(1))
			}));

			// [SCH_135e] Schooled - 3/3.
			cards.Add("SCH_135e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.SetAttackHealth(3))
			}));

			// [SCH_136e] Power Word: Feast - +2/+2 and fully heal at the end of this turn.
			cards.Add("SCH_136e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(2)),
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = new HealTask(999, EntityType.TARGET),
					RemoveAfterTriggered = true
				}
			}));

			// [SCH_138e] Blessing of Authority - +8/+8.
			cards.Add("SCH_138e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(8))
			}));

			// [SCH_138e2] Honorable Intentions - Can't attack heroes this turn.
			cards.Add("SCH_138e2", new CardDef(new Power
			{
				Enchant = new Enchant(new Effect(GameTag.CANNOT_ATTACK_HEROES, EffectOperator.SET, 1)),
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = new SetGameTagTask(GameTag.CANNOT_ATTACK_HEROES, 0, EntityType.TARGET),
					RemoveAfterTriggered = true
				}
			}));

			// [SCH_250e] Apathetic - Attack reduced to 1 until next turn.
			cards.Add("SCH_250e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.SetAttack(1)),
				Trigger = new Trigger(TriggerType.TURN_START)
				{
					SingleTask = RemoveEnchantmentTask.Task
				}
			}));

			// [SCH_149e] Boastful - Increased Attack and Health.
			cards.Add("SCH_149e", new CardDef(new Power
			{
				Enchant = Enchants.Enchants.AddAttackHealthScriptTag
			}));

			// [SCH_526e] Humility - Health set to 1.
			cards.Add("SCH_526e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.SetMaxHealth(1))
			}));

			// [SCH_524e] Shield of Honor - +3 Attack and Divine Shield.
			cards.Add("SCH_524e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(3), new Effect(GameTag.DIVINE_SHIELD, EffectOperator.SET, 1))
			}));

			// [SCH_343e] Soul Powered - +3/+3.
			cards.Add("SCH_343e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(3))
			}));

			// [SCH_704e] Soul Rage - +5 Attack this turn.
			cards.Add("SCH_704e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(5))
			}));

			// [SCH_231e] Ready for School - +2 Attack.
			cards.Add("SCH_231e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(2))
			}));

			// [SCH_312e] School Tour - Your Hero Power costs (0).
			cards.Add("SCH_312e", new CardDef(new Power
			{
				Aura = new Aura(AuraType.HEROPOWER, Effects.SetCost(0))
				{
					RemoveTrigger = (TriggerType.INSPIRE, null)
				}
			}));

			// [SCH_713e] Spoiled! - Your spells cost (1) more this turn.
			cards.Add("SCH_713e", new CardDef(new Power
			{
				Aura = new Aura(AuraType.OP_HAND, Effects.AddCost(1))
				{
					Condition = SelfCondition.IsSpell,
					RemoveTrigger = (TriggerType.TURN_END, SelfCondition.IsOpTurn)
				}
			}));

			// [SCH_618e] Blood of Innocents - +1/+1.
			cards.Add("SCH_618e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(1))
			}));

			// [SCH_600t3] Kolek - Your other minions have +1 Attack.
			cards.Add("SCH_600t3", new CardDef(new Power
			{
				Aura = new Aura(AuraType.BOARD_EXCEPT_SOURCE, "SCH_600t3e")
			}));

			// [SCH_600t3e] Kolek's Call - +1 Attack.
			cards.Add("SCH_600t3e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1))
			}));

			// [SCH_302e] Gift of Luminance - Set Attack and Health to 1.
			cards.Add("SCH_302e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.SetAttackHealth(1))
			}));

			// [SCH_609e] Survival of the Fittest - +4/+4.
			cards.Add("SCH_609e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(4))
			}));

			// [SCH_622e] Honed Edge - +1 Attack.
			cards.Add("SCH_622e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1))
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

		private static bool DestroySoulFragment(Controller controller)
		{
			IPlayable fragment = controller.DeckZone.FirstOrDefault(p => p.Card.Id == "SCH_307t");
			if (fragment == null)
				return false;

			controller.SetasideZone.Add(controller.DeckZone.Remove(fragment));
			return true;
		}

		private static void AddRandomMinionToHand(Game game, Controller controller, IEntity source, System.Func<Card, bool> predicate)
		{
			List<Card> cards = Cards.FormatTypeCards(game.FormatType)
				.Where(card => card.Collectible && card.Type == CardType.MINION && predicate(card))
				.ToList();

			if (cards.Count == 0)
				return;

			IPlayable entity = Entity.FromCard(controller, cards.Choose(game.Random));
			entity[GameTag.DISPLAYED_CREATOR] = source.Id;
			Generic.AddHandPhase.Invoke(controller, entity);
		}

		private static void AddRandomSpellToHand(Game game, Controller controller, IEntity source, System.Func<Card, bool> predicate)
		{
			List<Card> cards = Cards.FormatTypeCards(game.FormatType)
				.Where(card => card.Collectible && card.Type == CardType.SPELL && predicate(card))
				.ToList();

			if (cards.Count == 0)
				return;

			IPlayable entity = Entity.FromCard(controller, cards.Choose(game.Random));
			entity[GameTag.DISPLAYED_CREATOR] = source.Id;
			Generic.AddHandPhase.Invoke(controller, entity);
		}

		private static void RememberElekkSpell(IEntity source, IPlayable target)
		{
			if (!(source is IPlayable elekk) || !(target is Spell spell))
				return;

			if (!RememberedElekkSpellIds.TryGetValue(elekk.Id, out List<string> remembered))
			{
				remembered = new List<string>();
				RememberedElekkSpellIds[elekk.Id] = remembered;
			}

			remembered.Add(spell.Card.Id);
		}

		private static void ShuffleRememberedElekkSpells(Controller controller, IEntity source)
		{
			if (!(source is IPlayable elekk) || !RememberedElekkSpellIds.TryGetValue(elekk.Id, out List<string> remembered))
				return;

			foreach (string cardId in remembered)
			{
				IPlayable spell = Entity.FromCard(controller, Cards.FromId(cardId));
				spell[GameTag.DISPLAYED_CREATOR] = source.Id;
				Generic.ShuffleIntoDeck.Invoke(controller, source, spell);
			}

			RememberedElekkSpellIds.Remove(elekk.Id);
		}
	}
}
