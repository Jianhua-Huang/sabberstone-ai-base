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
	public class BarrensCardsGen
	{
		public static void AddAll(System.Collections.Generic.Dictionary<string, CardDef> cards)
		{
			CoreSet2021(cards);
			Neutral(cards);
			Rogue(cards);
			Warrior(cards);
			NonCollect(cards);
		}

		private static void CoreSet2021(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			AddCoreAlias(cards, "CORE_CS2_029", "CS2_029");
			AddCoreAlias(cards, "CORE_CS2_062", "CS2_062");
			AddCoreAlias(cards, "CORE_CS2_089", "CS2_089");
			AddCoreAlias(cards, "CORE_CS2_189", "CS2_189");
			AddCoreAlias(cards, "CORE_DS1_185", "DS1_185");

			// [CORE_EX1_096] Loot Hoarder - Deathrattle: Draw a card.
			cards.Add("CORE_EX1_096", new CardDef(new Power
			{
				DeathrattleTask = new DrawTask()
			}));
		}

		private static void AddCoreAlias(System.Collections.Generic.IDictionary<string, CardDef> cards,
			string coreId, string baseId)
		{
			if (cards.ContainsKey(baseId) && !cards.ContainsKey(coreId))
				cards.Add(coreId, cards[baseId]);
		}

		private static void Neutral(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [BAR_020] Razormane Raider - Frenzy: Attack a random enemy.
			cards.Add("BAR_020", new CardDef(new Power
			{
				Trigger = Frenzy(ComplexTask.Create(
					new RandomTask(1, EntityType.ENEMIES),
					new AttackTask(EntityType.SOURCE, EntityType.STACK)))
			}));

			// [BAR_024] Oasis Thrasher - Frenzy: Deal 3 damage to the enemy Hero.
			cards.Add("BAR_024", new CardDef(new Power
			{
				Trigger = Frenzy(new DamageTask(3, EntityType.OP_HERO))
			}));
		}

		private static void Rogue(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [BAR_320] Efficient Octo-bot - Frenzy: Reduce the cost of cards in your hand by (1).
			cards.Add("BAR_320", new CardDef(new Power
			{
				Trigger = Frenzy(new AddEnchantmentTask("BAR_320e", EntityType.HAND))
			}));
		}

		private static void Warrior(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [BAR_843] Warsong Envoy - Frenzy: Gain +1 Attack for each damaged character.
			cards.Add("BAR_843", new CardDef(new Power
			{
				Trigger = Frenzy(new CustomTask((g, c, s, t, stack) =>
				{
					if (!(s is Minion minion))
						return;

					int damagedCharacters = c.BoardZone.Count(p => p.Damage > 0)
						+ c.Opponent.BoardZone.Count(p => p.Damage > 0)
						+ (c.Hero.Damage > 0 ? 1 : 0)
						+ (c.Opponent.Hero.Damage > 0 ? 1 : 0);

					if (damagedCharacters > 0)
						minion.AttackDamage += damagedCharacters;
				}))
			}));

			// [BAR_896] Stonemaul Anchorman - Rush. Frenzy: Draw a card.
			cards.Add("BAR_896", new CardDef(new Power
			{
				Trigger = Frenzy(new DrawTask())
			}));
		}

		private static void NonCollect(System.Collections.Generic.IDictionary<string, CardDef> cards)
		{
			// [BAR_320e] Training - Costs (1) less.
			cards.Add("BAR_320e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.ReduceCost(1))
			}));

			// [BAR_843e] Incensed - Increased Attack.
			cards.Add("BAR_843e", new CardDef(new Power()));
		}

		private static Trigger Frenzy(ISimpleTask task)
		{
			return new Trigger(TriggerType.TAKE_DAMAGE)
			{
				TriggerSource = TriggerSource.SELF,
				Condition = new SelfCondition(p => p is Minion minion && !minion.ToBeDestroyed && minion.Health > 0),
				SingleTask = task,
				RemoveAfterTriggered = true
			};
		}
	}
}
