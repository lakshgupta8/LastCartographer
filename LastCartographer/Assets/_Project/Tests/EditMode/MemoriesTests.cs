using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>The rules of the drop (GDD 6, PRG-17): bind once, death drops everything, a second death folds forward, recovery returns it all, and the save keeps the drop.</summary>
    public class MemoriesTests
    {
        [Test]
        public void DeathDropsAndRecoveryReturnsEverything()
        {
            var w = new WorldState();
            Assert.IsFalse(Memories.Drop(w, "Room_A", 1f, 2f), "nothing bound, nothing dropped");
            Assert.IsTrue(Memories.Bind(w, "isolde.first_sight"));
            Assert.IsFalse(Memories.Bind(w, "isolde.first_sight"), "bound once");
            Assert.IsTrue(Memories.Has(w, "isolde.first_sight"));
            string droppedRoom = null; int dropped = 0;
            void OnDrop(string r, int n) { droppedRoom = r; dropped = n; }
            Memories.Dropped += OnDrop;
            try
            {
                Assert.IsTrue(Memories.Drop(w, "Room_A", 3f, 0.5f));
            }
            finally { Memories.Dropped -= OnDrop; }
            Assert.AreEqual("Room_A", droppedRoom); Assert.AreEqual(1, dropped);
            Assert.IsTrue(Memories.HasDrop(w));
            Assert.AreEqual(0, Memories.Count(w), "out of reach");
            Assert.AreEqual(3f, w.DropX); Assert.AreEqual(0.5f, w.DropY);

            // A second death, with something new bound meanwhile, folds the old drop forward.
            Memories.Bind(w, "sable.boats_back");
            Assert.IsTrue(Memories.Drop(w, "Room_B", -4f, 0f));
            Assert.AreEqual("Room_B", w.DropRoom);
            CollectionAssert.AreEquivalent(new[] { "isolde.first_sight", "sable.boats_back" }, w.DroppedMemories);

            Assert.AreEqual(2, Memories.Recover(w));
            Assert.IsFalse(Memories.HasDrop(w));
            Assert.IsNull(w.DropRoom);
            CollectionAssert.AreEquivalent(new[] { "isolde.first_sight", "sable.boats_back" }, w.BoundMemories);
            Assert.AreEqual(0, Memories.Recover(w), "nothing twice");
        }

        [Test]
        public void TheSaveKeepsTheDrop()
        {
            var w = new WorldState();
            Memories.Bind(w, "isolde.first_sight");
            Memories.Drop(w, "Greybox_Saltmarrow_B", 12.5f, 0f);
            var back = GameState.FromJson(GameState.ToJson(w));
            Assert.AreEqual("Greybox_Saltmarrow_B", back.DropRoom);
            Assert.AreEqual(12.5f, back.DropX);
            CollectionAssert.AreEqual(new[] { "isolde.first_sight" }, back.DroppedMemories);
            Assert.IsEmpty(back.BoundMemories);
            StringAssert.Contains("\"version\": 4", GameState.ToJson(w));
        }

        [Test]
        public void NamesRead()
        {
            Assert.AreEqual("the first time she saw you", Memories.Name("isolde.first_sight"));
            Assert.AreEqual("x.y", Memories.Name("x.y"), "unknown ids read as themselves");
            Assert.AreEqual("", Memories.Describe(new string[0]));
            Assert.AreEqual("the first time she saw you and the count of boats that came back", Memories.Describe(new[] { "isolde.first_sight", "sable.boats_back" }));
        }
    }
}
