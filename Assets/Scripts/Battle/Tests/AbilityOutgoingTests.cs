using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using MonsterCatcher.Map;

namespace MonsterCatcher.Battle.Tests
{
    public class AbilityOutgoingTests
    {
        private static SpeciesData Sp() =>
            TestFactory.Species("A", ElementType.Normal, 100, 100, 100, 100, 100, 100);

        private static MoveData FireSpecial() =>
            TestFactory.Move("Ember", ElementType.Fire, MoveCategory.Special, 60);

        private static Pokemon Make(string ability, MoveData move)
        {
            var moves = new List<MoveData> { move };
            return new Pokemon(Sp(), 50, moves, ability == null ? null : new[] { ability });
        }

        [Test]
        public void Bruiser_AddsFlatTenPercent()
        {
            var move = FireSpecial();
            var plainAtk = Make(null, move);
            var plainDef = Make(null, move);
            var buffedAtk = Make("Bruiser", move);

            float plain = AbilityApplier.OutgoingMultiplier(plainAtk, plainDef, move, 1.0, 0, false);
            float buffed = AbilityApplier.OutgoingMultiplier(buffedAtk, plainDef, move, 1.0, 0, false);

            Assert.AreEqual(1.0f, plain, 1e-4f);
            Assert.AreEqual(1.10f, buffed, 1e-4f);
        }

        [Test]
        public void Berserk_OnlyWhenAtOrBelowThirdHp()
        {
            var move = FireSpecial();
            var atk = Make("Berserk", move);
            var def = Make(null, move);

            float high = AbilityApplier.OutgoingMultiplier(atk, def, move, 1.0, 0, false);
            Assert.AreEqual(1.0f, high, 1e-4f);

            atk.SetCurrentHp(atk.MaxHp / 3);
            float low = AbilityApplier.OutgoingMultiplier(atk, def, move, 1.0, 0, false);
            Assert.AreEqual(1.5f, low, 1e-4f);
        }

        [Test]
        public void Pyromaniac_BoostsFireOnly()
        {
            var fire = FireSpecial();
            var normal = TestFactory.Move("Hit", ElementType.Normal, MoveCategory.Physical, 60);
            var atk = Make("Pyromaniac", fire);
            var def = Make(null, fire);

            float onNormal = AbilityApplier.OutgoingMultiplier(atk, def, normal, 1.0, 0, false);
            Assert.AreEqual(1.0f, onNormal, 1e-4f);

            float onFire = AbilityApplier.OutgoingMultiplier(atk, def, fire, 1.0, 0, false);
            Assert.AreEqual(1.20f, onFire, 1e-4f);
        }
    }
}
