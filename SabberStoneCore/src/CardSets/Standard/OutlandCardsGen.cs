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
			Neutral(cards);
			NonCollect(cards);
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
