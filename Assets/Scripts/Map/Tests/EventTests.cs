using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class EventTests
    {
        // ---- RunState roster / level helpers ------------------------------------

        [Test] public void NewRunResetsMaxRosterAndExpandPersists()
        {
            RunState.NewRun(3);
            Assert.AreEqual(6, RunState.MaxRoster);
            RunState.ExpandRoster(1);
            Assert.AreEqual(7, RunState.MaxRoster);
            RunState.NextTier(1);
            Assert.AreEqual(7, RunState.MaxRoster, "ExpandRoster persists across tiers");
        }

        [Test] public void AddLevelsRaisesLowersAndClampsAtOne()
        {
            RunState.NewRun(3);
            int start = RunState.PlayerRoster[0].Level;
            RunState.AddLevels(0, +4);
            Assert.AreEqual(start + 4, RunState.PlayerRoster[0].Level);
            RunState.AddLevels(0, -100);
            Assert.AreEqual(1, RunState.PlayerRoster[0].Level, "clamped to >= 1");
        }

        [Test] public void AddLevelsAllRaisesAndClampsAtOne()
        {
            RunState.NewRun(3);
            RunState.PlayerRoster.Add(new MonsterSave("Briarstag", 3));
            RunState.AddLevelsAll(+2);
            Assert.AreEqual(RunState.StarterLevel + 2, RunState.PlayerRoster[0].Level);
            Assert.AreEqual(5, RunState.PlayerRoster[1].Level);
            RunState.AddLevelsAll(-100);
            Assert.AreEqual(1, RunState.PlayerRoster[0].Level);
            Assert.AreEqual(1, RunState.PlayerRoster[1].Level);
        }

        [Test] public void GrantRandomAbilityAddsADistinctAbility()
        {
            RunState.NewRun(3);
            Assert.AreEqual(1, RunState.PlayerRoster[0].AbilityIds.Count);
            RunState.GrantRandomAbility(0, 1234);
            var ids = RunState.PlayerRoster[0].AbilityIds;
            Assert.AreEqual(2, ids.Count);
            Assert.AreEqual(ids.Count, new HashSet<string>(ids).Count, "no duplicate ability ids");
        }

        // ---- inventory / gold helpers -------------------------------------------

        [Test] public void LoseRandomItemTypeRemovesOneTypeAndFailsWhenEmpty()
        {
            RunState.NewRun(3);
            int before = RunState.Inventory.Count;
            Assert.Greater(before, 0);
            Assert.IsTrue(RunState.LoseRandomItemType(7));
            Assert.AreEqual(before - 1, RunState.Inventory.Count, "exactly one item type removed");

            RunState.Inventory.Clear();
            Assert.IsFalse(RunState.LoseRandomItemType(7), "false on empty inventory");
        }

        [Test] public void SpendGoldClampedFloorsAtZero()
        {
            RunState.NewRun(3);
            RunState.AddGold(10);
            RunState.SpendGoldClamped(50);
            Assert.AreEqual(0, RunState.Gold);
        }

        // ---- VisitEvent ----------------------------------------------------------

        [Test] public void VisitEventClearsAndAdvances()
        {
            RunState.NewRun(3);
            int node = RunState.Available()[0];
            Assert.IsTrue(RunState.VisitEvent(node));
            Assert.AreEqual(node, RunState.CurrentNodeId);
            Assert.IsTrue(RunState.Cleared.Contains(node));
        }

        [Test] public void VisitEventRejectsUnavailableNode()
        {
            RunState.NewRun(3);
            Assert.IsFalse(RunState.VisitEvent(RunState.Map.BossId));
        }

        // ---- EventCatalog --------------------------------------------------------

        [Test] public void CatalogHas24UniqueIdsThatRoundTrip()
        {
            Assert.AreEqual(24, EventCatalog.All.Count);
            var seen = new HashSet<string>();
            foreach (var e in EventCatalog.All)
            {
                Assert.IsTrue(seen.Add(e.Id), "duplicate id " + e.Id);
                Assert.AreSame(e, EventCatalog.ById(e.Id), "ById round-trip for " + e.Id);
            }
        }

        [Test] public void NeedsMonsterTargetMatchesSpec()
        {
            var needTarget = new HashSet<string>
            {
                "AncientAwakening", "TrainingGrounds", "EvolutionCatalyst", "MentorsGift",
                "ForbiddenTome", "SacrificialRite", "RecklessEvolution", "GlassCannonBrew",
                "SoulTax", "MysteryBox",
            };
            foreach (var e in EventCatalog.All)
                Assert.AreEqual(needTarget.Contains(e.Id), e.NeedsMonsterTarget, "NeedsMonsterTarget for " + e.Id);
        }

        [Test] public void ConditionsMatchSpec()
        {
            Assert.AreEqual(EventCondition.TeamAboveOne, EventCatalog.ById("SacrificialRite").Condition);
            Assert.AreEqual(EventCondition.HasItems, EventCatalog.ById("PawnEverything").Condition);
            Assert.AreEqual(EventCondition.HasEvolvableNow, EventCatalog.ById("EvolutionCatalyst").Condition);
            Assert.AreEqual(EventCondition.HasAnyEvolution, EventCatalog.ById("RecklessEvolution").Condition);

            var conditioned = new HashSet<string>
            {
                "SacrificialRite", "PawnEverything", "EvolutionCatalyst", "RecklessEvolution",
            };
            foreach (var e in EventCatalog.All)
                if (!conditioned.Contains(e.Id))
                    Assert.AreEqual(EventCondition.None, e.Condition, "expected None for " + e.Id);
        }

        [Test] public void RandomOfferIsDistinctSubsetAndDeterministic()
        {
            var allIds = new List<string>();
            foreach (var e in EventCatalog.All) allIds.Add(e.Id);
            var pool = new HashSet<string>(allIds);

            var offer = EventCatalog.RandomOffer(5, 3, allIds);
            Assert.AreEqual(3, offer.Count);
            Assert.AreEqual(3, new HashSet<string>(offer).Count, "distinct");
            foreach (var id in offer) Assert.IsTrue(pool.Contains(id), "offer id from input " + id);

            CollectionAssert.AreEqual(offer, EventCatalog.RandomOffer(5, 3, allIds), "deterministic per seed");
        }
    }
}
