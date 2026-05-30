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
			DemonHunter(cards);
			Druid(cards);
			Mage(cards);
			Neutral(cards);
			Paladin(cards);
			Shaman(cards);
			Warlock(cards);
			Warrior(cards);
			NonCollect(cards);
		}

		private static void DemonHunter(IDictionary<string, CardDef> cards)
		{
			// [DMF_219] Relentless Pursuit - Give your hero +4 Attack and Immune this turn.
			cards.Add("DMF_219", new CardDef(new Power
			{
				PowerTask = new AddEnchantmentTask("DMF_219e", EntityType.HERO)
			}));

			// [DMF_221] Felscream Blast - Lifesteal. Deal 1 damage to a minion and its neighbors.
			cards.Add("DMF_221", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = ComplexTask.Create(
					new IncludeAdjacentTask(EntityType.TARGET, true),
					new DamageTask(1, EntityType.STACK, true))
			}));

			// [DMF_223] Renowned Performer - Rush. Deathrattle: Summon two 1/1 Assistants with Taunt.
			cards.Add("DMF_223", new CardDef(new Power
			{
				DeathrattleTask = new SummonTask("DMF_223t", 2, SummonSide.DEATHRATTLE)
			}));
		}

		private static void Druid(IDictionary<string, CardDef> cards)
		{
			// [DMF_730] Moontouched Amulet - Give your hero +4 Attack this turn. Corrupt: And gain 6 Armor.
			cards.Add("DMF_730", new CardDef(new Power
			{
				PowerTask = new AddEnchantmentTask("DMF_730e", EntityType.HERO)
			}));

			// [DMF_730t] Moontouched Amulet - Corrupted. Give +4 Attack and gain 6 Armor.
			cards.Add("DMF_730t", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new AddEnchantmentTask("DMF_730e", EntityType.HERO),
					new ArmorTask(6))
			}));

			// [DMF_733] Kiri, Chosen of Elune - Add a Solar Eclipse and Lunar Eclipse to your hand.
			cards.Add("DMF_733", new CardDef(new Power
			{
				PowerTask = ComplexTask.Create(
					new AddCardTo("DMF_058", EntityType.HAND),
					new AddCardTo("DMF_057", EntityType.HAND))
			}));
		}

		private static void Mage(IDictionary<string, CardDef> cards)
		{
			// [DMF_101] Firework Elemental - Battlecry: Deal 3 damage to a minion.
			cards.Add("DMF_101", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = new DamageTask(3, EntityType.TARGET, false)
			}));

			// [DMF_101t] Firework Elemental - Corrupted. Battlecry: Deal 12 damage to a minion.
			cards.Add("DMF_101t", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = new DamageTask(12, EntityType.TARGET, false)
			}));
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

			// [DMF_244] Day at the Faire - Summon 3 Silver Hand Recruits. Corrupt: Summon 5.
			cards.Add("DMF_244", new CardDef(new Power
			{
				PowerTask = new SummonTask("CS2_101t", 3, SummonSide.SPELL)
			}));

			// [DMF_244t] Day at the Faire - Corrupted. Summon 5 Silver Hand Recruits.
			cards.Add("DMF_244t", new CardDef(new Power
			{
				PowerTask = new SummonTask("CS2_101t", 5, SummonSide.SPELL)
			}));
		}

		private static void Shaman(IDictionary<string, CardDef> cards)
		{
			// [DMF_701] Dunk Tank - Deal 4 damage. Corrupt: Then deal 2 damage to all enemy minions.
			cards.Add("DMF_701", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0}
			}, new Power
			{
				PowerTask = new DamageTask(4, EntityType.TARGET, true)
			}));

			// [DMF_701t] Dunk Tank - Corrupted. Deal 4 damage, then 2 to all enemy minions.
			cards.Add("DMF_701t", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0}
			}, new Power
			{
				PowerTask = ComplexTask.Create(
					new DamageTask(4, EntityType.TARGET, true),
					new DamageTask(2, EntityType.OP_MINIONS, true))
			}));

			// [DMF_702] Stormstrike - Deal 3 damage to a minion. Give your hero +3 Attack this turn.
			cards.Add("DMF_702", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0},
				{PlayReq.REQ_MINION_TARGET, 0}
			}, new Power
			{
				PowerTask = ComplexTask.Create(
					new DamageTask(3, EntityType.TARGET, true),
					new AddEnchantmentTask("DMF_702e", EntityType.HERO))
			}));

			// [DMF_703] Pit Master - Battlecry: Summon a 3/2 Duelist. Corrupt: Summon two.
			cards.Add("DMF_703", new CardDef(new Power
			{
				PowerTask = new SummonTask("DMF_703t2")
			}));

			// [DMF_703t] Pit Master - Corrupted. Battlecry: Summon two 3/2 Duelists.
			cards.Add("DMF_703t", new CardDef(new Power
			{
				PowerTask = new SummonTask("DMF_703t2", 2, SummonSide.ALTERNATE)
			}));

			// [DMF_704] Cagematch Custodian - Battlecry: Draw a weapon.
			cards.Add("DMF_704", new CardDef(new Power
			{
				PowerTask = ComplexTask.DrawFromDeck(1, SelfCondition.IsWeapon)
			}));
		}

		private static void Warlock(IDictionary<string, CardDef> cards)
		{
			// [DMF_174] Circus Medic - Battlecry: Restore 4 Health. Corrupt: Deal 4 damage instead.
			cards.Add("DMF_174", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_IF_AVAILABLE, 0}
			}, new Power
			{
				PowerTask = new HealTask(4, EntityType.TARGET)
			}));

			// [DMF_174t] Circus Medic - Corrupted. Battlecry: Deal 4 damage.
			cards.Add("DMF_174t", new CardDef(new Dictionary<PlayReq, int>
			{
				{PlayReq.REQ_TARGET_TO_PLAY, 0}
			}, new Power
			{
				PowerTask = new DamageTask(4, EntityType.TARGET)
			}));

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

			// [DMF_219e] Out for Blood - +4 Attack and Immune this turn.
			cards.Add("DMF_219e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(4), Effects.Immune)
			}));

			// [DMF_702e] Stormstrike - +3 Attack this turn.
			cards.Add("DMF_702e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(3))
			}));

			// [DMF_730e] Moontouched Amulet - +4 Attack this turn.
			cards.Add("DMF_730e", new CardDef(new Power
			{
				Enchant = new Enchant(Effects.Attack_N(4))
			}));
		}
	}
}
