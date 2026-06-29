using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class PartyTests
    {
        private static Pokemon Mon()
        {
            return TestFactory.Mon(TestFactory.Species("M", ElementType.Normal, 45, 49, 49, 65, 65, 45));
        }

        [Test] public void ActiveIsFirstByDefault()
        {
            var party = new Party(BattleSide.Player, new List<Pokemon> { Mon(), Mon() }, 6);
            Assert.AreSame(party.Members[0], party.Active);
        }

        [Test] public void CannotSwitchToActiveOrFainted()
        {
            var a = Mon(); var b = Mon();
            var party = new Party(BattleSide.Player, new List<Pokemon> { a, b }, 6);
            Assert.IsFalse(party.CanSwitchTo(0)); // active
            b.TakeDamage(1000);
            Assert.IsFalse(party.CanSwitchTo(1)); // fainted
        }

        [Test] public void SwitchResetsOutgoingStages()
        {
            var a = Mon(); var b = Mon();
            a.ChangeStage(Stat.Attack, 2);
            var party = new Party(BattleSide.Player, new List<Pokemon> { a, b }, 6);
            party.SwitchTo(1);
            Assert.AreEqual(1, party.ActiveIndex);
            Assert.AreEqual(0, a.GetStage(Stat.Attack));
        }

        [Test] public void HasUsableReflectsFaints()
        {
            var a = Mon(); var b = Mon();
            var party = new Party(BattleSide.Player, new List<Pokemon> { a, b }, 6);
            a.TakeDamage(1000); b.TakeDamage(1000);
            Assert.IsFalse(party.HasUsablePokemon());
        }
    }
}
