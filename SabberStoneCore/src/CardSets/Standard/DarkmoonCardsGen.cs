using System.Collections.Generic;
using SabberStoneCore.Actions;
using SabberStoneCore.Conditions;
using SabberStoneCore.Enchants;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Tasks;
using SabberStoneCore.Tasks.SimpleTasks;
using SabberStoneCore.src.Loader;

namespace SabberStoneCore.CardSets.Standard
{
	public class DarkmoonCardsGen
	{
		public static void AddAll(Dictionary<string, CardDef> cards)
		{
			Neutral(cards);
			Paladin(cards);
			Warlock(cards);
			Warrior(cards);
			NonCollect(cards);
		}

		private static void Neutral(IDictionary<string, CardDef> cards)
		{
			// [DMF_065] Banana Vendor - Battlecry: Add 2 Bananas to each player's hand.
			cards.Add("DMF_065", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new AddCardTo("DMF_065t", EntityType.HAND, 2),
					new AddCardTo("DMF_065t", EntityType.OP_HAND, 2))
			}));

			// [DMF_066] Knife Vendor - Battlecry: Deal 4 damage to each hero.
			cards.Add("DMF_066", new CardDef(new Power
			{
				PowerTask = new DamageTask(4, EntityType.HEROES)
			}));

			// [DMF_067] Prize Vendor - Battlecry: Both players draw a card.
			cards.Add("DMF_067", new CardDef(new Power
			{
				PowerTask = new CustomTask((g, c, s, t, stack) =>
				{
					Generic.Draw(c);
					Generic.Draw(c.Opponent);
				})
			}));

			// [DMF_091] Wriggling Horror - Battlecry: Give adjacent minions +1/+1.
			cards.Add("DMF_091", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new IncludeAdjacentTask(EntityType.SOURCE),
					new AddEnchantmentTask("DMF_091e2", EntityType.STACK))
			}));

			// [DMF_100] Confection Cyclone - Battlecry: Add two 1/2 Sugar Elementals to your hand.
			cards.Add("DMF_100", new CardDef(new Power
			{
				PowerTask = new AddCardTo("DMF_100t", EntityType.HAND, 2)
			}));

			// [DMF_110] Fire Breather - Battlecry: Deal 2 damage to all minions except Demons.
			cards.Add("DMF_110", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new IncludeTask(EntityType.ALLMINIONS),
					new FilterStackTask(SelfCondition.IsNotRace(Race.DEMON)),
					new DamageTask(2, EntityType.STACK))
			}));

			// [DMF_189] Costumed Entertainer - Battlecry: Give a random minion in your hand +2/+2.
			cards.Add("DMF_189", new CardDef(new Power
			{
				PowerTask = ComplexTask.BuffRandomMinion(EntityType.HAND, "DMF_189e")
			}));
		}

		private static void Paladin(IDictionary<string, CardDef> cards)
		{
			// [DMF_238] Hammer of the Naaru - Battlecry: Summon a 6/6 Holy Elemental with Taunt.
			cards.Add("DMF_238", new CardDef(new Power
			{
				PowerTask = new SummonTask("DMF_238t")
			}));
		}

		private static void Warlock(IDictionary<string, CardDef> cards)
		{
			// [DMF_115] Revenant Rascal - Battlecry: Destroy a Mana Crystal for both players.
			cards.Add("DMF_115", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new ManaCrystalEmptyTask(-1),
					new ManaCrystalEmptyTask(-1, true))
			}));

			// [DMF_533] Ring Matron - Deathrattle: Summon two 3/2 Imps.
			cards.Add("DMF_533", new CardDef(new Power
			{
				DeathrattleTask = new SummonTask("DMF_533t", 2, SummonSide.DEATHRATTLE)
			}));
		}

		private static void Warrior(IDictionary<string, CardDef> cards)
		{
			// [DMF_521] Sword Eater - Taunt. Battlecry: Equip a 3/2 Sword.
			cards.Add("DMF_521", new CardDef(new Power
			{
				PowerTask = new WeaponTask("DMF_521t")
			}));

			// [DMF_523] Bumper Car - Rush. Deathrattle: Add two 1/1 Riders with Rush to your hand.
			cards.Add("DMF_523", new CardDef(new Power
			{
				DeathrattleTask = new AddCardTo("DMF_523t", EntityType.HAND, 2)
			}));
		}

		private static void NonCollect(IDictionary<string, CardDef> cards)
		{
			// [DMF_065t] Banana - Give a minion +1/+1.
			cards.Add("DMF_065t", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = new AddEnchantmentTask("DMF_065e", EntityType.TARGET)
			}));

			// [DMF_065e] Bananas - +1/+1.
			cards.Add("DMF_065e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(1))
			}));

			// [DMF_091e2] Wriggling - +1/+1.
			cards.Add("DMF_091e2", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(1))
			}));

			// [DMF_189e] You're a Star! - +2/+2.
			cards.Add("DMF_189e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.AttackHealth_N(2))
			}));
		}
	}
}
