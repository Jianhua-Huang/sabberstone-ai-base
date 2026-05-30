using System.Collections.Generic;
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
			Neutral(cards);
			NonCollect(cards);
		}

		private static void Neutral(IDictionary<string, CardDef> cards)
		{
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
		}

		private static void NonCollect(IDictionary<string, CardDef> cards)
		{
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
	}
}
