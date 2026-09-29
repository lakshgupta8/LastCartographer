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

        [Test]
        public void AStillMemoryBindsAndHoldsNothing()
        {
            Assert.IsTrue(Memories.IsStill("tam.next_spring"), "bound from the Stillness");
            Assert.IsFalse(Memories.IsStill("dotha.nine_songs"));
            Assert.IsFalse(Memories.IsStill(null));
            var w = new WorldState();
            Assert.IsTrue(Memories.Bind(w, "tam.next_spring"), "it binds");
            Assert.AreEqual("Halden_Hall_2", Memories.HomeOf("tam.next_spring"), "Tam lives in the Hall");
            Assert.IsNull(Memories.BoundFor(w, "Halden_Hall_2"), "but it is no bind to anchor with");
            Memories.Drop(w, "Greybox_X", 0f, 0f);
            CollectionAssert.Contains(w.DroppedMemories, "tam.next_spring", "it drops like any ink");
            Assert.AreEqual(1, Memories.Recover(w));
            Assert.IsTrue(Memories.Has(w, "tam.next_spring"));
        }

        [Test]
        public void AMemoryBelongsToWhereItsGiverLives()
        {
            Assert.AreEqual("Saltmarrow_B", Memories.HomeOf("dotha.nine_songs"), "Dotha lives in Merrow's End");
            Assert.AreEqual("Saltmarrow_A", Memories.HomeOf("sable.boats_back"), "Sable keeps the quay");
            Assert.IsNull(Memories.HomeOf("isolde.first_sight"), "Isolde lives nowhere now");
            Assert.AreEqual("Emberdown_Rest_2", Memories.HomeOf("kettil.count"), "Kettil's square");
            Assert.AreEqual("Verdance_House_2", Memories.HomeOf("teodor.eleven_names"), "Teodor's cloister");
            Assert.AreEqual("Windreach_Camp_1", Memories.HomeOf("idrenne.standing_place"), "the wagon that stays");
            string scenes = System.IO.Path.Combine(UnityEngine.Application.dataPath, "_Project/Scenes/Greybox/Greybox_");
            foreach (var id in Memories.English.Keys)
            {
                var home = Memories.HomeOf(id);
                if (home == null) continue;
                Assert.IsTrue(System.IO.File.Exists(scenes + home + ".unity") || RoomPlans.Find(home) != null, id + "'s home is a built or planned room: " + home);
            }

            var w = new WorldState();
            Assert.IsNull(Memories.BoundFor(w, "Saltmarrow_B"));
            Memories.Bind(w, "isolde.first_sight");
            Assert.IsNull(Memories.BoundFor(w, "Saltmarrow_B"), "a memory from elsewhere doesn't anchor here");
            Memories.Bind(w, "dotha.nine_songs");
            Assert.AreEqual("dotha.nine_songs", Memories.BoundFor(w, "Saltmarrow_B"));
            Assert.IsNull(Memories.BoundFor(w, "Saltmarrow_A"));
            Memories.Drop(w, "Greybox_Saltmarrow_B", 0f, 0f);
            Assert.IsNull(Memories.BoundFor(w, "Saltmarrow_B"), "a dropped memory isn't carried");
            Memories.Recover(w);
            Assert.AreEqual("dotha.nine_songs", Memories.BoundFor(w, "Saltmarrow_B"), "recovered, it is");
        }
    }
}
