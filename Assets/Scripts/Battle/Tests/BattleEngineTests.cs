using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class BattleEngineTests
    {
        private BattleSettings _settings;
        private MoveData _tackle;
        private MoveData _quickAttack;

        [SetUp] public void Setup()
        {
            _settings = TestFactory.Settings();
            _tackle = TestFactory.Move("Tackle", ElementType.Normal, MoveCategory.Physical, 40);
            _quickAttack = TestFactory.Move("Quick Attack", ElementType.Normal, MoveCategory.Physical, 40, priority: 1);
        }

        private Pokemon Fast() => TestFactory.Mon(
            TestFactory.Species("Fast", ElementType.Normal, 60, 80, 60, 60, 60, 120), _tackle, _quickAttack);

        private Pokemon Slow() => TestFactory.Mon(
            TestFactory.Species("Slow", ElementType.Normal, 60, 80, 60, 60, 60, 20), _tackle, _quickAttack);

        private static Party P(BattleSide side, params Pokemon[] mons) =>
            new Party(side, mons.ToList(), 6);

        [Test] public void FasterMovesFirst()
        {
            var player = P(BattleSide.Player, Fast());
            var enemy = P(BattleSide.Enemy, Slow());
            var engine = new BattleEngine(player, enemy, _settings, new FakeRng());
            var events = engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            var firstMove = events.OfType<MoveUsedEvent>().First();
            Assert.AreSame(player.Active, firstMove.User);
        }

        [Test] public void PriorityBeatsSpeed()
        {
            var player = P(BattleSide.Player, Slow());  // Quick Attack (priority 1)
            var enemy = P(BattleSide.Enemy, Fast());    // Tackle (priority 0)
            var engine = new BattleEngine(player, enemy, _settings, new FakeRng());
            var events = engine.ExecuteTurn(BattleAction.UseMove(1), BattleAction.UseMove(0));
            var firstMove = events.OfType<MoveUsedEvent>().First();
            Assert.AreSame(player.Active, firstMove.User);
        }

        [Test] public void FaintTriggersForcedSwitchThenContinues()
        {
            var glass = TestFactory.Mon(TestFactory.Species("Glass", ElementType.Normal, 1, 10, 10, 10, 10, 10), _tackle);
            var bench = TestFactory.Mon(TestFactory.Species("Bench", ElementType.Normal, 60, 80, 60, 60, 60, 50), _tackle);
            var player = P(BattleSide.Player, Fast());
            var enemy = P(BattleSide.Enemy, glass, bench);
            var engine = new BattleEngine(player, enemy, _settings, new FakeRng());
            engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            Assert.IsTrue(engine.AwaitingForcedSwitch(BattleSide.Enemy));
            var sw = engine.ResolveForcedSwitch(BattleSide.Enemy, 1);
            Assert.IsTrue(sw.OfType<SwitchedInEvent>().Any());
            Assert.IsFalse(engine.IsOver);
        }

        [Test] public void LastFaintEndsBattle()
        {
            var glass = TestFactory.Mon(TestFactory.Species("Glass", ElementType.Normal, 1, 10, 10, 10, 10, 10), _tackle);
            var player = P(BattleSide.Player, Fast());
            var enemy = P(BattleSide.Enemy, glass);
            var engine = new BattleEngine(player, enemy, _settings, new FakeRng());
            var events = engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            Assert.IsTrue(engine.IsOver);
            Assert.AreEqual(BattleResult.PlayerWon, engine.Result);
            Assert.IsTrue(events.OfType<BattleEndedEvent>().Any());
        }

        [Test] public void PoisonTicksAtEndOfTurn()
        {
            var poison = TestFactory.Move("Poison Sting", ElementType.Poison, MoveCategory.Physical, 15);
            poison.InflictsStatus = StatusCondition.Poison;
            poison.StatusChance = 100;
            var attacker = TestFactory.Mon(TestFactory.Species("P", ElementType.Poison, 60, 80, 60, 60, 60, 120), poison);
            var victim = TestFactory.Mon(TestFactory.Species("V", ElementType.Normal, 200, 80, 200, 60, 200, 5), _tackle);
            var player = P(BattleSide.Player, attacker);
            var enemy = P(BattleSide.Enemy, victim);
            var engine = new BattleEngine(player, enemy, _settings, new FakeRng());
            engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            Assert.AreEqual(StatusCondition.Poison, victim.Status);
            Assert.Less(victim.CurrentHp, victim.MaxHp); // hit + poison tick
        }

        [Test] public void SwitchHappensBeforeMove()
        {
            var player = P(BattleSide.Player, Fast(), Slow());
            var enemy = P(BattleSide.Enemy, Fast());
            var engine = new BattleEngine(player, enemy, _settings, new FakeRng());
            var events = new List<BattleEvent>(
                engine.ExecuteTurn(BattleAction.SwitchTo(1), BattleAction.UseMove(0)));
            int switchIdx = events.FindIndex(e => e is SwitchedInEvent);
            int firstMoveIdx = events.FindIndex(e => e is MoveUsedEvent);
            Assert.GreaterOrEqual(firstMoveIdx, 0);
            Assert.Less(switchIdx, firstMoveIdx);
        }
    }
}
