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

			// [SCH_279] Trueaim Crescent - After your Hero attacks a minion, your minions attack it too.
			cards.Add("SCH_279", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.HERO,
					Condition = SelfCondition.IsEventTargetIs(CardType.MINION),
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(g.CurrentEventData?.EventTarget is Minion target))
							return;

						foreach (Minion minion in c.BoardZone.GetAll().ToArray())
						{
							if (target.ToBeDestroyed || target.Zone != c.Opponent.BoardZone)
								break;
							if (!minion.ToBeDestroyed && minion.Zone == c.BoardZone)
							{
								EventMetaData currentEvent = g.CurrentEventData;
								Generic.AttackBlock.Invoke(c, minion, target, true, false);
								g.CurrentEventData = currentEvent;
							}
						}
					})
				}
			}));

			// [SCH_253] Cycle of Hatred - Deal 3 damage to all minions. Summon a 3/3 Spirit for every minion killed.
			cards.Add("SCH_253", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					Minion[] minions = c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).ToArray();
					foreach (Minion minion in minions)
						Generic.DamageCharFunc.Invoke(s as IPlayable, minion, 3, false);

					int killed = minions.Count(minion => minion.ToBeDestroyed);
					if (killed == 0)
						return;

					g.DeathProcessingAndAuraUpdate();

					for (int i = 0; i < killed && !c.BoardZone.IsFull; i++)
						Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, Cards.FromId("SCH_253t")), -1, s);
				})
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

			// [SCH_422] Double Jump - Draw an Outcast card from your deck.
			cards.Add("SCH_422", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					IPlayable outcastCard = c.DeckZone.FirstOrDefault(p => p.Card[GameTag.OUTCAST] == 1);
					if (outcastCard != null)
						Generic.Draw(c, outcastCard);
				})
			}));

			// [SCH_705] Vilefiend Trainer - Outcast: Summon two 1/1 Demons.
			cards.Add("SCH_705", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (WasPlayedFromOutcastPosition(s))
						new SummonTask("SCH_705t", 2, SummonSide.SPELL).Process(g, c, s, t, stack);
				})
			}));

			// [SCH_354] Ancient Void Hound - At the end of your turn, steal 1 Attack and Health from all enemy minions.
			cards.Add("SCH_354", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						int stolen = 0;
						foreach (Minion minion in c.Opponent.BoardZone.GetAll().ToArray())
						{
							Generic.AddEnchantmentBlock(g, Cards.FromId("SCH_354e"), s as IPlayable, minion, 0, 0, 0);
							stolen++;
						}

						for (int i = 0; i < stolen; i++)
							Generic.AddEnchantmentBlock(g, Cards.FromId("SCH_354e2"), s as IPlayable, s, 0, 0, 0);
					})
				}
			}));

			// [SCH_357] Fel Guardians - Summon three 1/2 Demons with Taunt. Costs (1) less whenever a friendly minion dies.
			cards.Add("SCH_357", new CardDef(new Power
			{
				PowerTask = new SummonTask("SCH_357t", 3),
				Trigger = new Trigger(TriggerType.DEATH)
				{
					TriggerActivation = TriggerActivation.HAND,
					TriggerSource = TriggerSource.FRIENDLY,
					Condition = SelfCondition.IsMinion,
					SingleTask = new AddEnchantmentTask("SCH_357e", EntityType.SOURCE)
				}
			}));

			// [SCH_276] Magehunter - Rush. Whenever this attacks a minion, Silence it.
			cards.Add("SCH_276", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.ATTACK)
				{
					TriggerSource = TriggerSource.SELF,
					Condition = SelfCondition.IsEventTargetIs(CardType.MINION),
					SingleTask = new SilenceTask(EntityType.EVENT_TARGET)
				}
			}));
		}

		private static void Druid(IDictionary<string, CardDef> cards)
		{
			// [SCH_242] Gibberling - Spellburst: Summon a Gibberling.
			cards.Add("SCH_242", new CardDef(new Power
			{
				Trigger = Spellburst(new SummonTask("SCH_242", SummonSide.SPELL))
			}));

			// [SCH_182] Speaker Gidra - Rush, Windfury. Spellburst: Gain Attack and Health equal to the spell's Cost.
			cards.Add("SCH_182", new CardDef(new Power
			{
				Trigger = Spellburst(new CustomTask((g, c, s, t, stack) =>
				{
					if (s is IPlayable gidra && t is Spell spell)
						Generic.AddEnchantmentBlock(g, Cards.FromId("SCH_182e"), gidra, gidra, spell.Card.Cost, spell.Card.Cost);
				}))
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

			// [SCH_333] Nature Studies - Discover a spell. Your next one costs (1) less.
			cards.Add("SCH_333", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new AddEnchantmentTask("SCH_333e", EntityType.CONTROLLER),
					new DiscoverTask(CardType.SPELL))
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
			// [SCH_235] Devolving Missiles - Shoot three missiles at random enemy minions that transform them into ones that cost (1) less.
			cards.Add("SCH_235", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					for (int i = 0; i < 3; i++)
					{
						Minion[] enemies = c.Opponent.BoardZone.GetAll();
						if (enemies.Length == 0)
							return;

						new TransformMinionTask(EntityType.TARGET, -1)
							.Process(g, c, s, enemies.Choose(g.Random), stack);
					}
				})
			}));

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

			// [SCH_348] Combustion - Deal 4 damage to a minion. Any excess damages both neighbors.
			cards.Add("SCH_348", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!(t is Minion target))
						return;

					Minion[] neighbors = target.Controller.BoardZone
						.GetAll(p => p.ZonePosition == target.ZonePosition - 1 || p.ZonePosition == target.ZonePosition + 1);
					int health = target.Health;
					int damage = Generic.DamageCharFunc.Invoke(s as IPlayable, target, 4, true);
					int excess = damage - health;
					if (excess <= 0)
						return;

					foreach (Minion neighbor in neighbors)
						Generic.DamageCharFunc.Invoke(s as IPlayable, neighbor, excess, false);
				})
			}));

			// [SCH_353] Cram Session - Draw 1 card. Improved by Spell Damage.
			cards.Add("SCH_353", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					int amount = 1 + c.CurrentSpellPower;
					for (int i = 0; i < amount; i++)
						Generic.Draw(c);
				})
			}));

			// [SCH_352] Potion of Illusion - Add 1/1 copies of your minions to your hand. They cost (1).
			cards.Add("SCH_352", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion minion in c.BoardZone.GetAll().ToArray())
					{
						if (c.HandZone.IsFull)
							return;

						IPlayable copy = Entity.FromCard(c, minion.Card, zone: c.HandZone);
						copy[GameTag.ATK] = 1;
						copy[GameTag.HEALTH] = 1;
						copy[GameTag.COST] = 1;
					}
				})
			}));

			// [SCH_400] Mozaki, Master Duelist - After you cast a spell, gain Spell Damage +1.
			cards.Add("SCH_400", new CardDef(new Power
			{
				Trigger = TriggerBuilder.Type(TriggerType.AFTER_CAST)
					.SetTask(new AddEnchantmentTask("SCH_400e2", EntityType.SOURCE))
					.SetSource(TriggerSource.FRIENDLY)
					.SetCondition(SelfCondition.IsSpell)
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
			// [SCH_126] Disciplinarian Gandling - After you play a minion, destroy it and summon a 4/4 Failed Student.
			cards.Add("SCH_126", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_PLAY_MINION)
				{
					TriggerSource = TriggerSource.FRIENDLY,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(s is Minion gandling) || !(t is Minion minion) || minion.Id == gandling.Id || minion.Zone != c.BoardZone)
							return;

						minion.Destroy();
						g.DeathProcessingAndAuraUpdate();
						if (!c.BoardZone.IsFull)
							Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, Cards.FromId("SCH_126t")), -1, s);
					})
				}
			}));

			// [SCH_120] Cabal Acolyte - Taunt. Spellburst: Gain control of a random enemy minion with 2 or less Attack.
			cards.Add("SCH_120", new CardDef(new Power
			{
				Trigger = Spellburst(new CustomTask((g, c, s, t, stack) =>
					GainControlOfRandomEnemyMinion(g, c, minion => minion.AttackDamage <= 2)))
			}));

			// [SCH_224] Headmaster Kel'Thuzad - Spellburst: If the spell destroys any minions, summon them.
			cards.Add("SCH_224", new CardDef(new Power
			{
				Trigger = HeadmasterKelThuzadSpellburst()
			}));

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

			// [SCH_425] Doctor Krastinov - Rush. Whenever this attacks, give your weapon +1/+1.
			cards.Add("SCH_425", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.ATTACK)
				{
					TriggerSource = TriggerSource.SELF,
					SingleTask = new AddEnchantmentTask("SCH_425e", EntityType.WEAPON)
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

			// [SCH_162] Vectus - Battlecry: Summon two 1/1 Whelps. Each gains a Deathrattle from your minions that died this game.
			cards.Add("SCH_162", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					for (int i = 0; i < 2 && !c.BoardZone.IsFull; i++)
					{
						Minion whelp = (Minion)Entity.FromCard(c, Cards.FromId("SCH_162t"));
						Generic.SummonBlock.Invoke(g, whelp, -1, s);
						CopyRandomDeadFriendlyDeathrattle(g, c, s, whelp);
					}
				})
			}));

			// [SCH_142] Voracious Reader - At the end of your turn, draw until you have 3 cards.
			cards.Add("SCH_142", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						while (c.HandZone.Count < 3 && !c.DeckZone.IsEmpty)
							Generic.Draw(c);
					})
				}
			}));

			// [SCH_530] Sorcerous Substitute - Battlecry: If you have Spell Damage, summon a copy of this.
			cards.Add("SCH_530", new CardDef(new Power
			{
				PowerTask = ComplexTask.Conditional(SelfCondition.IsSpellDmgOnHero,
					new SummonCopyTask(EntityType.SOURCE))
			}));

			// [SCH_157] Enchanted Cauldron - Spellburst: Cast a random spell of the same Cost.
			cards.Add("SCH_157", new CardDef(new Power
			{
				Trigger = Spellburst(new CustomTask((g, c, s, t, stack) =>
				{
					if (t is Spell spell)
						CastRandomSpellOfCost(g, c, spell.Card.Cost);
				}))
			}));

			// [SCH_230] Onyx Magescribe - Spellburst: Add 2 random spells from your class to your hand.
			cards.Add("SCH_230", new CardDef(new Power
			{
				Trigger = Spellburst(new CustomTask((g, c, s, t, stack) =>
				{
					AddRandomSpellToHand(g, c, s, card => card.Class == c.HeroClass);
					AddRandomSpellToHand(g, c, s, card => card.Class == c.HeroClass);
				}))
			}));

			// [SCH_231] Intrepid Initiate - Spellburst: Gain +2 Attack.
			cards.Add("SCH_231", new CardDef(new Power
			{
				Trigger = Spellburst(new AddEnchantmentTask("SCH_231e", EntityType.SOURCE))
			}));

			// [SCH_232] Crimson Hothead - Spellburst: Gain +1 Attack and Taunt.
			cards.Add("SCH_232", new CardDef(new Power
			{
				Trigger = Spellburst(new AddEnchantmentTask("SCH_232e", EntityType.SOURCE))
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

			// [SCH_350] Wand Thief - Combo: Discover a Mage spell.
			cards.Add("SCH_350", new CardDef(new Power
			{
				ComboTask = new DiscoverTask(CardType.SPELL, CardClass.MAGE)
			}));

			// [SCH_351] Jandice Barov - Battlecry: Summon two random 5-Cost minions. Secretly pick one that dies when it takes damage.
			cards.Add("SCH_351", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (!Cards.CostMinionCards(g.FormatType).TryGetValue(5, out List<Card> minions) || minions.Count == 0)
						return;

					var summoned = new List<Minion>();
					for (int i = 0; i < 2 && !c.BoardZone.IsFull; i++)
					{
						var minion = (Minion)Entity.FromCard(c, minions.Choose(g.Random));
						Generic.SummonBlock.Invoke(g, minion, -1, s);
						if (minion.Zone == c.BoardZone)
							summoned.Add(minion);
					}

					if (summoned.Count == 0)
						return;

					if (summoned.Count == 1)
					{
						Generic.AddEnchantmentBlock(g, Cards.FromId("SCH_351e"), s as IPlayable, summoned[0], 0, 0, 0);
						return;
					}

					if (!Generic.CreateChoice(c, s, ChoiceType.GENERAL, ChoiceAction.STACK, summoned.Select(p => p.Id).ToList()))
						return;

					c.Choice.AfterChooseTask = new CustomTask((game, controller, source, target, choiceStack) =>
					{
						if (target is Minion illusion)
							Generic.AddEnchantmentBlock(game, Cards.FromId("SCH_351e"), source as IPlayable, illusion, 0, 0, 0);
					});
				})
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

			// [SCH_711] Plagued Protodrake - Deathrattle: Summon a random 7-Cost minion.
			cards.Add("SCH_711", new CardDef(new Power
			{
				DeathrattleTask = ComplexTask.SummonRandomMinion(GameTag.COST, 7)
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

			// [SCH_245] Steward of Scrolls - Spell Damage +1. Battlecry: Discover a spell.
			cards.Add("SCH_245", new CardDef(new Power
			{
				PowerTask = new DiscoverTask(CardType.SPELL)
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

			// [SCH_259] Sphere of Sapience - At the start of your turn, look at your top card. You can put it on the bottom and lose 1 Durability.
			cards.Add("SCH_259", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_START)
				{
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (c.DeckZone.IsEmpty || c.Hero.Weapon == null || c.Hero.Weapon.Id != s.Id)
							return;

						IPlayable topCard = c.DeckZone.TopCard;
						IPlayable newFate = Entity.FromCard(c, Cards.FromId("SCH_259t"), null, c.SetasideZone);
						Generic.RevealCardBlock(s as IPlayable, topCard);
						if (!Generic.CreateChoice(c, s, ChoiceType.GENERAL, ChoiceAction.STACK, new List<int> { topCard.Id, newFate.Id }))
							return;

						c.Choice.AfterChooseTask = new CustomTask((game, controller, source, target, choiceStack) =>
						{
							if (target?.Card.Id != "SCH_259t" || controller.DeckZone.IsEmpty)
								return;

							IPlayable movedCard = controller.DeckZone.TopCard;
							controller.DeckZone.Remove(movedCard);
							controller.DeckZone.Add(movedCard, 0);
							controller.Hero.Weapon.Damage += 1;
							game.DeathProcessingAndAuraUpdate();
						});
					})
				}
			}));
		}

		private static void Paladin(IDictionary<string, CardDef> cards)
		{
			// [SCH_139] Devout Pupil - Divine Shield, Taunt. Costs (1) less for each spell you've cast on friendly characters this game.
			cards.Add("SCH_139", new CardDef(new Power
			{
				Aura = new AdaptiveCostEffect(
					initialisationFunction: p => -SpellsCastOnFriendlyCharacters(p.Controller),
					triggerValueFunction: p => WasCastOnFriendlyCharacter(p) ? -1 : 0,
					trigger: TriggerType.AFTER_CAST,
					triggerSource: TriggerSource.FRIENDLY)
			}));

			// [SCH_141] High Abbess Alura - Spellburst: Cast a spell from your deck (targets this if possible).
			cards.Add("SCH_141", new CardDef(new Power
			{
				Trigger = Spellburst(new CustomTask((g, c, s, t, stack) => CastRandomSpellFromDeckTargetingSource(g, c, s)))
			}));

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

			// [SCH_247] First Day of School - Add 2 random 1-Cost minions to your hand.
			cards.Add("SCH_247", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					AddRandomMinionToHand(g, c, s, card => card.Cost == 1);
					AddRandomMinionToHand(g, c, s, card => card.Cost == 1);
				})
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

			// [SCH_523] Ceremonial Maul - Spellburst: Summon a Student with Taunt and stats equal to the spell's Cost.
			cards.Add("SCH_523", new CardDef(new Power
			{
				Trigger = Spellburst(new CustomTask((g, c, s, t, stack) =>
				{
					if (c.BoardZone.IsFull || !(t is Spell spell))
						return;

					Minion student = (Minion)Entity.FromCard(c, Cards.FromId("SCH_523t"));
					student[GameTag.ATK] = spell.Cost;
					student[GameTag.HEALTH] = spell.Cost;
					student.HasTaunt = true;
					Generic.SummonBlock.Invoke(g, student, -1, s);
				}))
			}));

			// [SCH_532] Goody Two-Shields - Divine Shield. Spellburst: Gain Divine Shield.
			cards.Add("SCH_532", new CardDef(new Power
			{
				Trigger = Spellburst(ComplexTask.DivineShield(EntityType.SOURCE))
			}));

			// [SCH_533] Commencement - Summon a minion from your deck. Give it Taunt and Divine Shield.
			cards.Add("SCH_533", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (c.BoardZone.IsFull)
						return;

					List<IPlayable> minions = c.DeckZone.Where(p => p is Minion).ToList();
					if (minions.Count == 0)
						return;

					Minion minion = (Minion)minions.Choose(g.Random);
					Generic.RemoveFromZone.Invoke(c, minion);
					minion.HasTaunt = true;
					minion.HasDivineShield = true;
					Generic.SummonBlock.Invoke(g, minion, -1, s);

					if (minions.Count > 1)
						g.OnRandomHappened(true);
				})
			}));
		}

		private static void Priest(IDictionary<string, CardDef> cards)
		{
			// [SCH_140] Flesh Giant - Costs (1) less for each time your hero's Health changed during your turns.
			cards.Add("SCH_140", new CardDef(new Power
			{
				Aura = new AdaptiveCostEffect(p => p.Controller.NumHeroHealthChangesOnOwnTurnsThisGame)
			}));

			// [SCH_136] Power Word: Feast - Give a minion +2/+2. Restore it to full Health at the end of this turn.
			cards.Add("SCH_136", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = new AddEnchantmentTask("SCH_136e", EntityType.TARGET)
			}));

			// [SCH_514] Raise Dead - Deal 3 damage to your hero. Return two friendly minions that died this game to your hand.
			cards.Add("SCH_514", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					Generic.DamageCharFunc.Invoke(s as IPlayable, c.Hero, 3, false);

					List<IPlayable> deadMinions = c.GraveyardZone
						.Where(p => p is Minion)
						.ToList();
					if (deadMinions.Count == 0)
						return;

					int amount = System.Math.Min(2, deadMinions.Count);
					IPlayable[] chosen = deadMinions.Count > amount
						? deadMinions.ChooseNElements(amount, g.Random)
						: deadMinions.ToArray();

					foreach (IPlayable deadMinion in chosen)
					{
						IPlayable copy = Entity.FromCard(c, deadMinion.Card);
						copy[GameTag.DISPLAYED_CREATOR] = s.Id;
						Generic.AddHandPhase.Invoke(c, copy);
					}

					if (deadMinions.Count > amount)
						g.OnRandomHappened(true);
				})
			}));

			// [SCH_513] Brittlebone Destroyer - Battlecry: If your hero's Health changed this turn, destroy a minion.
			cards.Add("SCH_513", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_IF_AVAILABLE, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (c.Hero.HealthChangedThisTurn > 0 && t is Minion target)
						target.Destroy();
				})
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

			// [SCH_233] Draconic Studies - Discover a Dragon. Your next one costs (1) less.
			cards.Add("SCH_233", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new AddEnchantmentTask("SCH_233e", EntityType.CONTROLLER),
					new DiscoverTask(DiscoverType.DRAGON))
			}));
		}

		private static void Rogue(IDictionary<string, CardDef> cards)
		{
			// [SCH_300] Carrion Studies - Discover a Deathrattle minion. Your next one costs (1) less.
			cards.Add("SCH_300", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new AddEnchantmentTask("SCH_300e", EntityType.CONTROLLER),
					new DiscoverTask(DiscoverType.DEATHRATTLE_MINIONS))
			}));

			// [SCH_426] Infiltrator Lilian - Stealth. Deathrattle: Summon a 4/2 Forsaken Lilian that attacks a random enemy.
			cards.Add("SCH_426", new CardDef(new Power
			{
				DeathrattleTask = new CustomTask((g, c, s, t, stack) =>
				{
					var lilian = (Minion)Entity.FromCard(c, Cards.FromId("SCH_426t"));
					Generic.SummonBlock.Invoke(g, lilian, -1, s);
					if (lilian.Zone != c.BoardZone)
						return;

					List<ICharacter> enemies = c.Opponent.BoardZone.GetAll().Cast<ICharacter>().ToList();
					enemies.Add(c.Opponent.Hero);
					Generic.AttackBlock.Invoke(c, lilian, enemies.Choose(g.Random), true, false);
				})
			}));

			// [SCH_234] Shifty Sophomore - Stealth. Spellburst: Add a Combo card to your hand.
			cards.Add("SCH_234", new CardDef(new Power
			{
				Trigger = Spellburst(new CustomTask((g, c, s, t, stack) =>
					AddRandomCardToHand(g, c, s, card => card.Combo)))
			}));

			// [SCH_305] Secret Passage - Replace your hand with 4 cards from your deck. Swap back next turn.
			cards.Add("SCH_305", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) => SecretPassage(g, c, s as IPlayable))
			}));

			// [SCH_519] Vulpera Toxinblade - Your weapon has +2 Attack.
			cards.Add("SCH_519", new CardDef(new Power
			{
				Aura = new Aura(AuraType.WEAPON, "SCH_519e")
			}));

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

			// [SCH_522] Steeldancer - Battlecry: Summon a random minion with Cost equal to your weapon's Attack.
			cards.Add("SCH_522", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					if (c.BoardZone.IsFull || c.Hero.Weapon == null)
						return;

					if (!Cards.CostMinionCards(g.FormatType).TryGetValue(c.Hero.Weapon.AttackDamage, out List<Card> minions)
						|| minions.Count == 0)
						return;

					Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, minions.Choose(g.Random)), -1, s);
					g.OnRandomHappened(true);
				})
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

			// [SCH_623] Cutting Class - Draw 2 cards. Costs (1) less per Attack of your weapon.
			cards.Add("SCH_623", new CardDef(new Power
			{
				Aura = new AdaptiveCostEffect(p => p.Controller.Hero.Weapon?.AttackDamage ?? 0),
				PowerTask = new DrawTask(2)
			}));
		}

		private static void Shaman(IDictionary<string, CardDef> cards)
		{
			// [SCH_236] Diligent Notetaker - Spellburst: Return the spell to your hand.
			cards.Add("SCH_236", new CardDef(new Power
			{
				Trigger = Spellburst(new CopyTask(EntityType.EVENT_SOURCE, Zone.HAND))
			}));

			// [SCH_270] Primordial Studies - Discover a Spell Damage minion. Your next one costs (1) less.
			cards.Add("SCH_270", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new AddEnchantmentTask("SCH_270e", EntityType.CONTROLLER),
					new DiscoverTask(CardType.MINION, tagValueCriteria: (GameTag.SPELLPOWER, RelaSign.GEQ, 1)))
			}));

			// [SCH_301] Rune Dagger - After your hero attacks, gain Spell Damage +1 this turn.
			cards.Add("SCH_301", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.HERO,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						int spellPower = c.NativeTags.ContainsKey(GameTag.SPELLPOWER) ? c.NativeTags[GameTag.SPELLPOWER] : 0;
						c.NativeTags[GameTag.SPELLPOWER] = spellPower + 1;
						Generic.AddEnchantmentBlock(g, Cards.FromId("SCH_301e"), s as IPlayable, c, 0, 0, 0);
					})
				}
			}));

			// [SCH_427] Lightning Bloom - Gain 2 Mana Crystals this turn only. Overload: (2)
			cards.Add("SCH_427", new CardDef(new Power
			{
				PowerTask = new TempManaTask(2)
			}));

			// [SCH_535] Tidal Wave - Lifesteal. Deal 3 damage to all minions.
			cards.Add("SCH_535", new CardDef(new Power
			{
				PowerTask = new DamageTask(3, EntityType.ALLMINIONS, true)
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

			// [SCH_273] Ras Frostwhisper - At the end of your turn, deal 1 damage to all enemies (improved by Spell Damage).
			cards.Add("SCH_273", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						int amount = 1 + c.CurrentSpellPower;
						Generic.DamageCharFunc.Invoke(s as IPlayable, c.Opponent.Hero, amount, false);
						foreach (Minion minion in c.Opponent.BoardZone.GetAll().ToArray())
							Generic.DamageCharFunc.Invoke(s as IPlayable, minion, amount, false);
					})
				}
			}));
		}

		private static void Warrior(IDictionary<string, CardDef> cards)
		{
			// [SCH_237] Athletic Studies - Discover a Rush minion. Your next one costs (1) less.
			cards.Add("SCH_237", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new AddEnchantmentTask("SCH_237e", EntityType.CONTROLLER),
					new DiscoverTask(CardType.MINION, tagValueCriteria: (GameTag.RUSH, RelaSign.GEQ, 1)))
			}));

			// [SCH_238] Reaper's Scythe - Spellburst: Also damages adjacent minions this turn.
			cards.Add("SCH_238", new CardDef(new Power
			{
				Trigger = Spellburst(new AddEnchantmentTask("SCH_238e", EntityType.CONTROLLER))
			}));

			// [SCH_317] Playmaker - After you play a Rush minion, summon a copy with 1 Health remaining.
			cards.Add("SCH_317", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_PLAY_MINION)
				{
					TriggerSource = TriggerSource.FRIENDLY,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(t is Minion minion) || !minion.IsRush || minion.Zone != c.BoardZone || c.BoardZone.IsFull)
							return;

						var tags = new EntityData((EntityData)minion.NativeTags);
						var copy = (Minion)Entity.FromCard(c, minion.Card, tags, c.BoardZone, zonePos: minion.ZonePosition + 1, creator: s);
						minion.CopyInternalAttributes(copy);
						copy.Damage = copy.BaseHealth - 1;
					})
				}
			}));

			// [SCH_337] Troublemaker - At the end of your turn, summon two 3/3 Ruffians that attack random enemies.
			cards.Add("SCH_337", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						var ruffians = new List<Minion>();
						for (int i = 0; i < 2 && !c.BoardZone.IsFull; i++)
						{
							var ruffian = (Minion)Entity.FromCard(c, Cards.FromId("SCH_337t"));
							Generic.SummonBlock.Invoke(g, ruffian, -1, s);
							ruffians.Add(ruffian);
						}

						foreach (Minion ruffian in ruffians)
						{
							if (ruffian.Zone != c.BoardZone)
								continue;

							List<ICharacter> enemies = c.Opponent.BoardZone.GetAll().Cast<ICharacter>().ToList();
							enemies.Add(c.Opponent.Hero);
							Generic.AttackBlock.Invoke(c, ruffian, enemies.Choose(g.Random), true, false);
						}
					})
				}
			}));

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
			// [SCH_158] Demonic Studies - Discover a Demon. Your next one costs (1) less.
			cards.Add("SCH_158", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new AddEnchantmentTask("SCH_158e", EntityType.CONTROLLER),
					new DiscoverTask(DiscoverType.DEMON))
			}));

			// [SCH_159] Mindrender Illucia - Battlecry: Swap hands and decks with your opponent until your next turn.
			cards.Add("SCH_159", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new CustomTask((g, c, s, t, stack) => SwapHandsAndDecks(c)),
					new AddEnchantmentTask("SCH_159e", EntityType.CONTROLLER))
			}));

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

			// [SCH_702] Felosophy - Copy the lowest Cost Demon in your hand. Outcast: Give both +1/+1.
			cards.Add("SCH_702", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					List<IPlayable> lowestCostDemons = c.HandZone
						.Where(p => p is Minion && p.Card.IsRace(Race.DEMON))
						.GroupBy(p => p.Cost)
						.OrderBy(gp => gp.Key)
						.FirstOrDefault()
						?.ToList();
					if (lowestCostDemons == null || lowestCostDemons.Count == 0)
						return;

					IPlayable demon = lowestCostDemons.Choose(g.Random);
					IPlayable copy = Generic.Copy(c, s, demon, Zone.HAND);

					if (lowestCostDemons.Count > 1)
						g.OnRandomHappened(true);

					if (WasPlayedFromOutcastPosition(s))
					{
						Generic.AddEnchantmentBlock(g, Cards.FromId("SCH_702e"), s as IPlayable, demon, 0, 0, 0);
						Generic.AddEnchantmentBlock(g, Cards.FromId("SCH_702e"), s as IPlayable, copy, 0, 0, 0);
					}
				})
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

			// [SCH_181] Archwitch Willow - Battlecry: Summon a random Demon from your hand and deck.
			cards.Add("SCH_181", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					SummonRandomDemonFromZone(g, c, s, c.HandZone.GetAll());
					SummonRandomDemonFromZone(g, c, s, c.DeckZone.GetAll());
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

			// [SCH_354e] Siphoned - Reduced Attack and Health.
			cards.Add("SCH_354e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(-1))
			}));

			// [SCH_354e2] Void Siphon - Increased Attack and Health.
			cards.Add("SCH_354e2", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(1))
			}));

			// [SCH_357e] Soul Infused - Costs (1) less.
			cards.Add("SCH_357e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.ReduceCost(1))
			}));

			// [SCH_351e] Illusion - Dies when it takes damage.
			cards.Add("SCH_351e", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TAKE_DAMAGE)
				{
					TriggerSource = TriggerSource.ENCHANTMENT_TARGET,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (s is Enchantment enchantment && enchantment.Target is IPlayable illusion)
							illusion.Destroy();
					})
				}
			}));

			// [SCH_351e2] Illusion - Dies when it takes damage.
			cards.Add("SCH_351e2", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TAKE_DAMAGE)
				{
					TriggerSource = TriggerSource.ENCHANTMENT_TARGET,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (s is Enchantment enchantment && enchantment.Target is IPlayable illusion)
							illusion.Destroy();
					})
				}
			}));

			// [SCH_158e] Demonic Studies - Your next Demon costs (1) less.
			cards.Add("SCH_158e", new CardDef(new Power
			{
				Aura = new Aura(AuraType.HAND, Effects.ReduceCost(1))
				{
					Condition = SelfCondition.IsRace(Race.DEMON),
					RemoveTrigger = (TriggerType.PLAY_MINION, SelfCondition.IsRace(Race.DEMON))
				}
			}));

			// [SCH_233e] Draconic Studies - Your next Dragon costs (1) less.
			cards.Add("SCH_233e", new CardDef(new Power
			{
				Aura = new Aura(AuraType.HAND, Effects.ReduceCost(1))
				{
					Condition = SelfCondition.IsRace(Race.DRAGON),
					RemoveTrigger = (TriggerType.PLAY_MINION, SelfCondition.IsRace(Race.DRAGON))
				}
			}));

			// [SCH_237e] Athletic Studies - Your next Rush minion costs (1) less.
			cards.Add("SCH_237e", new CardDef(new Power
			{
				Aura = new Aura(AuraType.HAND, Effects.ReduceCost(1))
				{
					Condition = SelfCondition.IsTagValue(GameTag.RUSH, 1, RelaSign.GEQ),
					RemoveTrigger = (TriggerType.PLAY_MINION, SelfCondition.IsTagValue(GameTag.RUSH, 1, RelaSign.GEQ))
				}
			}));

			// [SCH_270e] Runic Studies - Your next Spell Damage minion costs (1) less.
			cards.Add("SCH_270e", new CardDef(new Power
			{
				Aura = new Aura(AuraType.HAND, Effects.ReduceCost(1))
				{
					Condition = SelfCondition.IsTagValue(GameTag.SPELLPOWER, 1, RelaSign.GEQ),
					RemoveTrigger = (TriggerType.PLAY_MINION, SelfCondition.IsTagValue(GameTag.SPELLPOWER, 1, RelaSign.GEQ))
				}
			}));

			// [SCH_300e] Carrion Studies - Your next Deathrattle minion costs (1) less.
			cards.Add("SCH_300e", new CardDef(new Power
			{
				Aura = new Aura(AuraType.HAND, Effects.ReduceCost(1))
				{
					Condition = SelfCondition.IsDeathrattleCard,
					RemoveTrigger = (TriggerType.PLAY_MINION, SelfCondition.IsDeathrattleMinion)
				}
			}));

			// [SCH_301e] Runic Power - You have Spell Damage +1 this turn.
			cards.Add("SCH_301e", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_END)
				{
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						int spellPower = c.NativeTags.ContainsKey(GameTag.SPELLPOWER) ? c.NativeTags[GameTag.SPELLPOWER] : 0;
						c.NativeTags[GameTag.SPELLPOWER] = spellPower > 0 ? spellPower - 1 : 0;
					}),
					RemoveAfterTriggered = true
				}
			}));

			// [SCH_400e2] Magic Master - Spell Damage +1.
			cards.Add("SCH_400e2", new CardDef(new Power
			{
				Enchant = new Enchant(GameTag.SPELLPOWER, EffectOperator.ADD, 1)
			}));

			// [SCH_333e] Nature Studies - Your next spell costs (1) less.
			cards.Add("SCH_333e", new CardDef(new Power
			{
				Aura = new Aura(AuraType.HAND, Effects.ReduceCost(1))
				{
					Condition = SelfCondition.IsSpell
				},
				Trigger = new Trigger(TriggerType.AFTER_CAST)
				{
					Condition = SelfCondition.IsSpell,
					SingleTask = RemoveEnchantmentTask.Task,
					RemoveAfterTriggered = true
				}
			}));

			// [SCH_238e] Reaper's Scythe - Damages minions next to whomever your hero attacks.
			cards.Add("SCH_238e", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.AFTER_ATTACK)
				{
					TriggerSource = TriggerSource.HERO,
					Condition = SelfCondition.IsEventTargetIs(CardType.MINION),
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						if (!(g.CurrentEventData?.EventTarget is Minion target))
							return;

						int damage = c.Hero.AttackDamage;
						foreach (Minion adjacent in target.GetAdjacentMinions().ToArray())
							Generic.DamageCharFunc.Invoke(c.Hero, adjacent, damage, false);
					})
				}
			}));

			// [SCH_159e] Mind Swap - At the start of your turn, swap both players hands and decks.
			cards.Add("SCH_159e", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_START)
				{
					SingleTask = ComplexTask.Create(
						new CustomTask((g, c, s, t, stack) => SwapHandsAndDecks(c)),
						RemoveEnchantmentTask.Task)
				}
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

			// [SCH_702e] Felosophically Inclined - +1/+1.
			cards.Add("SCH_702e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(1))
			}));

			// [SCH_231e] Ready for School - +2 Attack.
			cards.Add("SCH_231e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(2))
			}));

			// [SCH_232e] Fired Up - +1 Attack and Taunt.
			cards.Add("SCH_232e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1), new Effect(GameTag.TAUNT, EffectOperator.SET, 1))
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

			// [SCH_352e] Potion of Illusion - Set Attack and Health to 1.
			cards.Add("SCH_352e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.SetAttackHealth(1))
			}));

			// [SCH_352e2] Potion of Illusion - Costs (1).
			cards.Add("SCH_352e2", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.SetCost(1))
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

			// [SCH_425e] Sharpened - +1/+1.
			cards.Add("SCH_425e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(1), new Effect(GameTag.DURABILITY, EffectOperator.ADD, 1))
			}));

			// [SCH_519e] Akunda's Bite - +2 Attack.
			cards.Add("SCH_519e", new CardDef(new Power
			{
				Enchant = new Enchant(new Effect(GameTag.ATK, EffectOperator.ADD, 2))
			}));

			// [SCH_162e] Experimental Plague - Copied Deathrattle from a friendly minion that died this game.
			cards.Add("SCH_162e", new CardDef(new Power
			{
				DeathrattleTask = ActivateCapturedDeathrattleTask.Task
			}));

			// [SCH_182e] Outspoken - Increased stats.
			cards.Add("SCH_182e", new CardDef(new Power
			{
				Enchant = new Enchant(
					Effects.Attack_N(0),
					Effects.Health_N(0))
				{
					UseScriptTag = true
				}
			}));

			// [SCH_305e3] Secret Passage Player Enchantment - Swap back next turn.
			cards.Add("SCH_305e3", new CardDef(new Power
			{
				Trigger = new Trigger(TriggerType.TURN_START)
				{
					SingleTask = new CustomTask((g, c, s, t, stack) => RestoreSecretPassage(c, s)),
					RemoveAfterTriggered = true
				}
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

		private static bool WasPlayedFromOutcastPosition(IEntity source)
		{
			return source is Playable playable && playable.WasPlayedFromOutcastPosition;
		}

		private static Trigger HeadmasterKelThuzadSpellburst()
		{
			return new MultiTrigger(
				new Trigger(TriggerType.CAST_SPELL)
				{
					TriggerSource = TriggerSource.FRIENDLY,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						s[GameTag.TAG_SCRIPT_DATA_NUM_1] = c.GraveyardZone.Count;
						s[GameTag.TAG_SCRIPT_DATA_NUM_2] = c.Opponent.GraveyardZone.Count;
					})
				},
				new Trigger(TriggerType.AFTER_CAST)
				{
					TriggerSource = TriggerSource.FRIENDLY,
					SingleTask = new CustomTask((g, c, s, t, stack) =>
					{
						int friendlyGraveyardCount = s[GameTag.TAG_SCRIPT_DATA_NUM_1];
						int opponentGraveyardCount = s[GameTag.TAG_SCRIPT_DATA_NUM_2];
						Minion[] destroyedBySpell = c.GraveyardZone
							.Skip(friendlyGraveyardCount)
							.Concat(c.Opponent.GraveyardZone.Skip(opponentGraveyardCount))
							.OfType<Minion>()
							.ToArray();

						foreach (Minion minion in destroyedBySpell)
						{
							if (c.BoardZone.IsFull)
								break;

							Generic.SummonBlock.Invoke(g, (Minion)Entity.FromCard(c, minion.Card), -1, s);
						}

						(s as IPlayable)?.ActivatedTrigger?.Remove();
					}),
					RemoveAfterTriggered = true
				});
		}

		private static void SecretPassage(Game game, Controller controller, IPlayable source)
		{
			if (source == null)
				return;

			int passageId = source.Id;
			foreach (IPlayable playable in controller.HandZone.GetAll().ToArray())
			{
				playable[GameTag.TAG_SCRIPT_DATA_NUM_1] = passageId;
				controller.SetasideZone.Add(controller.HandZone.Remove(playable));
			}

			for (int i = 0; i < 4 && !controller.DeckZone.IsEmpty && !controller.HandZone.IsFull; i++)
			{
				IPlayable playable = controller.DeckZone.Draw();
				playable[GameTag.TAG_SCRIPT_DATA_NUM_2] = passageId;
				controller.HandZone.Add(playable);
			}

			Generic.AddEnchantmentBlock(game, Cards.FromId("SCH_305e3"), source, controller, passageId);
		}

		private static void RestoreSecretPassage(Controller controller, IEntity source)
		{
			int passageId = source[GameTag.TAG_SCRIPT_DATA_NUM_1];
			foreach (IPlayable playable in controller.HandZone.GetAll()
				.Where(p => p[GameTag.TAG_SCRIPT_DATA_NUM_2] == passageId)
				.ToArray())
			{
				playable[GameTag.TAG_SCRIPT_DATA_NUM_2] = 0;
				controller.DeckZone.Add(controller.HandZone.Remove(playable), 0);
			}

			foreach (IPlayable playable in controller.SetasideZone
				.Where(p => p[GameTag.TAG_SCRIPT_DATA_NUM_1] == passageId)
				.ToArray())
			{
				if (controller.HandZone.IsFull)
					break;

				playable[GameTag.TAG_SCRIPT_DATA_NUM_1] = 0;
				controller.HandZone.Add(controller.SetasideZone.Remove(playable));
			}

			(source as Enchantment)?.Remove();
		}

		private static bool DestroySoulFragment(Controller controller)
		{
			IPlayable fragment = controller.DeckZone.FirstOrDefault(p => p.Card.Id == "SCH_307t");
			if (fragment == null)
				return false;

			controller.SetasideZone.Add(controller.DeckZone.Remove(fragment));
			return true;
		}

		private static int SpellsCastOnFriendlyCharacters(Controller controller)
		{
			return controller.PlayHistory.Count(h =>
				h.SourceCard.Type == CardType.SPELL &&
				h.TargetController == controller.PlayerId &&
				h.TargetCard != null);
		}

		private static bool WasCastOnFriendlyCharacter(IPlayable spell)
		{
			return spell.Controller.PlayHistory.Any(h =>
				h.SourceId == spell.Id &&
				h.SourceCard.Type == CardType.SPELL &&
				h.TargetController == spell.Controller.PlayerId &&
				h.TargetCard != null);
		}

		private static void SwapHandsAndDecks(Controller controller)
		{
			Controller opponent = controller.Opponent;

			IPlayable[] controllerHand = controller.HandZone.GetAll();
			IPlayable[] opponentHand = opponent.HandZone.GetAll();

			foreach (IPlayable playable in controllerHand)
				controller.HandZone.Remove(playable);
			foreach (IPlayable playable in opponentHand)
				opponent.HandZone.Remove(playable);

			foreach (IPlayable playable in controllerHand)
			{
				SetController(playable, opponent);
				opponent.HandZone.Add(playable);
			}

			foreach (IPlayable playable in opponentHand)
			{
				SetController(playable, controller);
				controller.HandZone.Add(playable);
			}

			controller.DeckZone.ForEach(playable => SetController(playable, opponent));
			opponent.DeckZone.ForEach(playable => SetController(playable, controller));

			var deck = controller.DeckZone;
			controller.DeckZone = opponent.DeckZone;
			controller.DeckZone.Controller = controller;
			opponent.DeckZone = deck;
			opponent.DeckZone.Controller = opponent;
		}

		private static void SetController(IPlayable playable, Controller controller)
		{
			playable.Controller = controller;
			playable[GameTag.CONTROLLER] = controller.PlayerId;
		}

		private static void GainControlOfRandomEnemyMinion(Game game, Controller controller, System.Predicate<Minion> predicate)
		{
			List<Minion> targets = controller.Opponent.BoardZone
				.Where(minion => predicate(minion))
				.ToList();
			if (targets.Count == 0)
				return;

			Minion target = targets.Choose(game.Random);
			if (targets.Count > 1)
				game.OnRandomHappened(true);

			new ControlTask(EntityType.TARGET).Process(game, controller, target, target);
		}

		private static void SummonRandomDemonFromZone(Game game, Controller controller, IEntity source, IEnumerable<IPlayable> zone)
		{
			if (controller.BoardZone.IsFull)
				return;

			List<IPlayable> demons = zone.Where(p => p is Minion && p.Card.IsRace(Race.DEMON)).ToList();
			if (demons.Count == 0)
				return;

			IPlayable demon = demons.Choose(game.Random);
			Generic.RemoveFromZone.Invoke(controller, demon);
			Generic.SummonBlock.Invoke(game, (Minion)demon, -1, source);

			if (demons.Count > 1)
				game.OnRandomHappened(true);
		}

		private static void CastRandomSpellFromDeckTargetingSource(Game game, Controller controller, IEntity source)
		{
			List<IPlayable> spells = controller.DeckZone
				.Where(p => p is Spell && p.Card.IsPlayableByCardReq(in controller))
				.ToList();
			if (spells.Count == 0)
				return;

			IPlayable spellPlayable = spells.Choose(game.Random);
			Generic.RemoveFromZone.Invoke(controller, spellPlayable);

			var spell = (Spell)spellPlayable;
			List<ICharacter> validTargets = spell.Card.GetValidPlayTargets(in controller);
			ICharacter target = null;

			if (source is ICharacter sourceCharacter && validTargets.Contains(sourceCharacter))
				target = sourceCharacter;
			else if (spell.Card.MustHaveTargetToPlay)
			{
				if (validTargets.Count == 0)
				{
					controller.GraveyardZone.Add(spell);
					return;
				}

				target = validTargets.Choose(game.Random);
			}

			int chooseOne = spell.Card.ChooseOne ? game.Random.Next(1, 3) : -1;
			Generic.CastSpell.Invoke(controller, game, spell, target, chooseOne);

			while (controller.Choice != null)
				Generic.ChoicePick.Invoke(controller, game, controller.Choice.Choices.Choose(game.Random));

			if (spells.Count > 1 || spell.Card.ChooseOne)
				game.OnRandomHappened(true);
		}

		private static void CastRandomSpellOfCost(Game game, Controller controller, int cost)
		{
			List<Card> spells = Cards.FormatTypeCards(game.FormatType)
				.Where(card => card.Type == CardType.SPELL
				               && card.Cost == cost
				               && card.Implemented
				               && !card.HideStat
				               && card.IsPlayableByCardReq(in controller))
				.ToList();
			if (spells.Count == 0)
				return;

			Card card = spells.Choose(game.Random);
			var spell = (Spell)Entity.FromCard(controller, card);
			ICharacter target = spell.GetRandomValidTarget();
			if (spell.Card.MustHaveTargetToPlay && target == null)
			{
				controller.GraveyardZone.Add(spell);
				return;
			}

			int chooseOne = spell.Card.ChooseOne ? game.Random.Next(1, 3) : -1;
			Generic.CastSpell.Invoke(controller, game, spell, target, chooseOne);

			while (controller.Choice != null)
				Generic.ChoicePick.Invoke(controller, game, controller.Choice.Choices.Choose(game.Random));

			game.OnRandomHappened(true);
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

		private static void AddRandomCardToHand(Game game, Controller controller, IEntity source, System.Func<Card, bool> predicate)
		{
			List<Card> cards = Cards.FormatTypeCards(game.FormatType)
				.Where(card => card.Collectible && predicate(card))
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

		private static void CopyRandomDeadFriendlyDeathrattle(Game game, Controller controller, IEntity source, Minion target)
		{
			List<IPlayable> deadDeathrattleMinions = controller.GraveyardZone
				.Where(p => p is Minion && p.Card.Power?.DeathrattleTask != null)
				.ToList();

			if (deadDeathrattleMinions.Count == 0)
				return;

			IPlayable captured = deadDeathrattleMinions.Choose(game.Random);
			Generic.AddEnchantmentBlock(game, Cards.FromId("SCH_162e"), (IPlayable)source, target, 0, 0, captured.Id);
		}
	}
}
