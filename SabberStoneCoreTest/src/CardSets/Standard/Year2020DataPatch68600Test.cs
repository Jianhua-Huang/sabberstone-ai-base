using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using Xunit;

namespace SabberStoneCoreTest.CardSets.Standard
{
	public class Year2020DataPatch68600Test
	{
		[Fact]
		public void Build68600_ShouldLoadScholomanceAndDarkmoonAsStandardSets()
		{
			Assert.Equal(CardSet.SCHOLOMANCE, Cards.FromId("SCH_231").Set);
			Assert.Equal(CardSet.DARKMOON_FAIRE, Cards.FromId("DMF_083").Set);
			Assert.Contains(Cards.FromId("SCH_231"), Cards.AllStandard);
			Assert.Contains(Cards.FromId("DMF_083"), Cards.AllStandard);
		}

		[Fact]
		public void Build68600_ShouldLoadNewMechanicTags()
		{
			Assert.Equal(1, Cards.FromId("SCH_231")[GameTag.SPELLBURST]);
			Assert.Equal(1, Cards.FromId("DMF_083")[GameTag.CORRUPT]);
			Assert.Equal(1, Cards.FromId("DMF_083t")[GameTag.CORRUPTEDCARD]);
		}
	}
}
