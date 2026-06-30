using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class InventoryTests
    {
        [Test] public void NewRunSeedsStartingItems()
        {
            RunState.NewRun(3);
            Assert.AreEqual(3, RunState.ItemCount("MonsterCatcher"));
            Assert.AreEqual(2, RunState.ItemCount("Potion"));
            Assert.AreEqual(1, RunState.ItemCount("Antidote"));
            Assert.AreEqual(0, RunState.Gold);
        }

        [Test] public void AddRemoveItemsDoNotUnderflow()
        {
            RunState.NewRun(3);
            RunState.AddItem("Potion", 2);
            Assert.AreEqual(4, RunState.ItemCount("Potion"));
            Assert.IsTrue(RunState.RemoveItem("Potion", 4));
            Assert.AreEqual(0, RunState.ItemCount("Potion"));
            Assert.IsFalse(RunState.RemoveItem("Potion", 1));
            Assert.AreEqual(0, RunState.ItemCount("Potion"));
        }

        [Test] public void GoldAddAndSpend()
        {
            RunState.NewRun(3);
            RunState.AddGold(50);
            Assert.AreEqual(50, RunState.Gold);
            Assert.IsFalse(RunState.TrySpendGold(60));
            Assert.AreEqual(50, RunState.Gold);
            Assert.IsTrue(RunState.TrySpendGold(30));
            Assert.AreEqual(20, RunState.Gold);
        }

        [Test] public void NextTierKeepsInventoryAndGold()
        {
            RunState.NewRun(3);
            RunState.AddGold(40);
            RunState.AddItem("Potion", 1);
            RunState.NextTier(7);
            Assert.AreEqual(40, RunState.Gold);
            Assert.AreEqual(3, RunState.ItemCount("Potion"));
            Assert.AreEqual(3, RunState.ItemCount("MonsterCatcher"));
        }

        [Test] public void ItemCatalogHasTheStartingItems()
        {
            Assert.IsNotNull(ItemCatalog.ById("MonsterCatcher"));
            Assert.IsNotNull(ItemCatalog.ById("Potion"));
            Assert.IsNotNull(ItemCatalog.ById("Revive"));
            Assert.Greater(ItemCatalog.All.Count, 0);
        }
    }
}
