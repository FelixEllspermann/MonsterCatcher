using System.Linq;
using NUnit.Framework;
using MonsterCatcher.Map;

namespace MonsterCatcher.Battle.Tests
{
    public class AbilityTests
    {
        [Test] public void EveryCatalogAbilityHasNonNullEffect()
        {
            foreach (var a in AbilityCatalog.All) Assert.IsNotNull(a.Effect, a.Id);
        }

        [Test] public void IntConstantsMatchBattleEnums()
        {
            Assert.AreEqual((int)ElementType.Fire, T.Fire);
            Assert.AreEqual((int)ElementType.Dark, T.Dark);
            Assert.AreEqual((int)ElementType.Fairy, T.Fairy);
            Assert.AreEqual((int)Stat.Attack, S.Attack);
            Assert.AreEqual((int)Stat.Speed, S.Speed);
            Assert.AreEqual((int)MoveCategory.Special, C.Special);
            Assert.AreEqual((int)StatusCondition.Paralysis, Sc.Paralysis);
            Assert.AreEqual((int)StatusCondition.Sleep, Sc.Sleep);
        }

        [Test] public void PokemonExposesItsAbilityEffects()
        {
            var sp = TestFactory.Species("A", ElementType.Normal, 100, 80, 60, 60, 60, 60);
            var mon = new Pokemon(sp, 50, TestFactory.OneMove(), new[] { "Brawler" });
            Assert.IsTrue(mon.HasAbility("Brawler"));
            Assert.AreEqual(1, mon.AbilityEffects.Count());
        }

        [Test] public void StatMultDefaultsToOne()
        {
            var sp = TestFactory.Species("A", ElementType.Normal, 100, 80, 60, 60, 60, 60);
            var mon = new Pokemon(sp, 50, TestFactory.OneMove(), new string[0]);
            Assert.AreEqual(1f, AbilityApplier.StatMult(mon, Stat.Attack), 1e-6);
        }

        [Test] public void BrawlerRaisesEffectiveAttack()
        {
            var sp = TestFactory.Species("A", ElementType.Normal, 100, 100, 60, 60, 60, 60);
            var baseMon = new Pokemon(sp, 50, TestFactory.OneMove(), new string[0]);
            var buffed  = new Pokemon(sp, 50, TestFactory.OneMove(), new[] { "Brawler" });
            Assert.Greater(buffed.EffectiveStat(Stat.Attack), baseMon.EffectiveStat(Stat.Attack));
        }

        [Test] public void GiantRaisesMaxHp()
        {
            var sp = TestFactory.Species("A", ElementType.Normal, 100, 80, 60, 60, 60, 60);
            var baseMon = new Pokemon(sp, 50, TestFactory.OneMove(), new string[0]);
            var hpMon   = new Pokemon(sp, 50, TestFactory.OneMove(), new[] { "Giant" });
            Assert.Greater(hpMon.MaxHp, baseMon.MaxHp);
        }
    }
}
