using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class ShopTests
    {
        [Test] public void VisitShopClearsAndAdvances()
        {
            RunState.NewRun(3);
            int node = RunState.Available()[0];
            Assert.IsTrue(RunState.VisitShop(node));
            Assert.AreEqual(node, RunState.CurrentNodeId);
            Assert.IsTrue(RunState.Cleared.Contains(node));
        }

        [Test] public void BuyingSpendsGoldAndAddsItem()
        {
            RunState.NewRun(3);
            RunState.AddGold(100);
            var potion = ItemCatalog.ById("Potion");
            int before = RunState.ItemCount("Potion");
            Assert.IsTrue(RunState.TrySpendGold(potion.Price));
            RunState.AddItem("Potion", 1);
            Assert.AreEqual(before + 1, RunState.ItemCount("Potion"));
            Assert.AreEqual(100 - potion.Price, RunState.Gold);
        }

        [Test] public void ShopOffersThreeDistinctRandomItems()
        {
            var offer = ItemCatalog.RandomOffer(5, 3);
            Assert.AreEqual(3, offer.Count);
            Assert.AreEqual(3, new System.Collections.Generic.HashSet<string>(offer).Count);   // distinct
            foreach (var id in offer) Assert.IsNotNull(ItemCatalog.ById(id));
            CollectionAssert.AreEqual(offer, ItemCatalog.RandomOffer(5, 3));                    // deterministic per seed
        }

        [Test] public void ShopNodesAppearAcrossSeeds()
        {
            bool seen = false;
            for (int s = 0; s < 80 && !seen; s++)
            {
                var map = MapGenerator.Generate(s);
                foreach (var n in map.Nodes) if (n.Type == NodeType.Shop) { seen = true; break; }
            }
            Assert.IsTrue(seen, "shop nodes should appear across seeds");
        }
    }
}
