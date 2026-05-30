using System.Collections.Generic;
using System.Linq;
using SabberStoneCore.Actions;
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
		public static void AddAll(Dictionary<string, CardDef> cards)
		{
			DemonHunter(cards);
			Hunter(cards);
			Neutral(cards);
			Paladin(cards);
			Priest(cards);
			Shaman(cards);
			Warlock(cards);
			NonCollect(cards);
		}

		private static void DemonHunter(IDictionary<string, CardDef> cards)
		{
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
		}

		private static void Neutral(IDictionary<string, CardDef> cards)
		{
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

			// [SCH_313] Wretched Tutor - Spellburst: Deal 2 damage to all other minions.
			cards.Add("SCH_313", new CardDef(new Power
			{
				Trigger = Spellburst(new CustomTask((g, c, s, t, stack) =>
				{
					foreach (Minion minion in c.BoardZone.GetAll().Concat(c.Opponent.BoardZone.GetAll()).Where(p => p != s).ToArray())
						Generic.DamageCharFunc.Invoke(s as IPlayable, minion, 2, false);
				}))
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
		}

		private static void Shaman(IDictionary<string, CardDef> cards)
		{
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
	}
}
