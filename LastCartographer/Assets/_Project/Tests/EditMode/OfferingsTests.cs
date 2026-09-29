using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// Memory as currency (bible 10, docs/design/offerings.md): what asks, what it takes and gives, a given memory gone
    /// for good, and the cost the bible names: offering a place's memory weakens that place's anchor.
    /// </summary>
    public class OfferingsTests
    {
        static string ProjectDir => Path.Combine(Application.dataPath, "_Project");

        static WorldState Carrying(params string[] memories)
        {
            var w = new WorldState();
            foreach (var m in memories) Memories.Bind(w, m);
            return w;
        }

        [Test]
        public void EveryAskerStandsSomewhereAndAsksInYarn()
        {
            var yarn = string.Join("\n", Directory.GetFiles(Path.Combine(ProjectDir, "Dialogue"), "*.yarn", SearchOption.AllDirectories).Select(File.ReadAllText));
            Assert.GreaterOrEqual(Offerings.All.Count, 2);
            Assert.AreEqual(Offerings.All.Count, Offerings.All.Select(a => a.Id).Distinct().Count());
            foreach (var a in Offerings.All)
            {
                Assert.IsTrue(RoomPlans.Find(a.Room) != null || File.Exists(Path.Combine(ProjectDir, "Scenes/Greybox/Greybox_" + a.Room + ".unity")), a.Id + " stands in " + a.Room);
                StringAssert.Contains("title: " + a.Node, yarn, a.Id + " asks in " + a.Node);
                Assert.IsNotEmpty(a.Brief, a.Id);
                Assert.IsTrue(a.Scraps > 0 || a.Seeds > 0 || a.Opens != null, a.Id + " gives something for it");
                foreach (var m in a.Wants)
                {
                    Assert.IsTrue(Memories.English.ContainsKey(m), a.Id + " wants a memory that exists: " + m);
                    StringAssert.Contains("<<offer " + a.Id + " " + m + ">>", yarn, a.Id + " takes " + m + " in its scene");
                }
            }
            foreach (Match m in Regex.Matches(yarn, @"<<offer (\S+) (\S+)>>"))
                Assert.IsTrue(Offerings.Find(m.Groups[1].Value)?.Accepts(m.Groups[2].Value) == true, m.Value + " is an asker taking what it wants");
            CollectionAssert.IsSubsetOf(Offerings.All.Select(a => a.Kind).Distinct(), new[] { AskerKind.Door, AskerKind.Bird, AskerKind.Keystone });
            Assert.IsTrue(Offerings.All.Any(a => a.Kind == AskerKind.Door) && Offerings.All.Any(a => a.Kind == AskerKind.Bird), "a door and a bird");
        }

        [Test]
        public void AGivenMemoryIsGoneForGood()
        {
            var w = Carrying("dotha.nine_songs", "isolde.first_sight");
            Assert.IsFalse(Offerings.CanOffer(w, "chapel_door", "isolde.first_sight"), "the door wants a song");
            Assert.IsFalse(Offerings.CanOffer(w, "nobody", "dotha.nine_songs"));
            Assert.IsTrue(Offerings.CanOfferAny(w, "chapel_door"));

            int scraps = Commissions.Scraps(w);
            Assert.IsTrue(Offerings.Offer(w, "chapel_door", "dotha.nine_songs"));
            Assert.IsFalse(Memories.Has(w, "dotha.nine_songs"), "given");
            Assert.IsTrue(Offerings.IsGiven(w, "dotha.nine_songs"));
            Assert.IsTrue(Offerings.IsDone(w, "chapel_door"));
            Assert.IsTrue(w.Is("saltmarrow.chapel.door_open"), "the door opens");
            Assert.AreEqual(scraps + 2, Commissions.Scraps(w), "the reliquary's two scraps");
            Assert.IsFalse(Memories.Bind(w, "dotha.nine_songs"), "a given memory can't be bound again");
            Assert.IsFalse(Offerings.Offer(w, "chapel_door", "dotha.nine_songs"), "once");
            Assert.IsNull(Memories.BoundFor(w, "Saltmarrow_B"), "and it can't anchor Merrow's End now");

            // What she doesn't carry, she can't give: the smudge holds a dropped memory.
            var d = Carrying("sable.boats_back");
            Memories.Drop(d, "Greybox_Saltmarrow_Chain_3", 0f, 0f);
            Assert.IsFalse(Offerings.CanOffer(d, "chain_gannet", "sable.boats_back"), "dropped");
            Memories.Recover(d);
            int seeds = Economy.Seeds(d);
            Assert.IsTrue(Offerings.Offer(d, "chain_gannet", "sable.boats_back"), "recovered");
            Assert.AreEqual(seeds + 6, Economy.Seeds(d), "seed, for the number");
            Memories.Drop(d, "Greybox_Saltmarrow_Chain_3", 0f, 0f);
            Assert.IsFalse(d.DroppedMemories.Contains("sable.boats_back"), "a given memory isn't in the next smudge");
        }

        [Test]
        public void AStillMemoryIsOnlyCarriedAndTheArchivistAsksForIsoldes()
        {
            var w = Carrying("tam.next_spring", "isolde.first_sight");
            foreach (var a in Offerings.All)
            {
                Assert.IsFalse(Offerings.CanOffer(w, a.Id, "tam.next_spring"), a.Id + " won't take a still memory");
                Assert.IsFalse(Offerings.Offer(w, a.Id, "tam.next_spring"));
            }
            Assert.IsTrue(Memories.Has(w, "tam.next_spring"), "still carried");

            var corvin = Offerings.Find("archivist");
            Assert.AreEqual(AskerKind.Bird, corvin.Kind, "an owl, not a keystone: the seventh is given whatever she does");
            Assert.IsTrue(Offerings.CanOffer(w, "archivist", "isolde.first_sight"));
            Assert.IsTrue(Offerings.Offer(w, "archivist", "isolde.first_sight"));
            Assert.IsTrue(w.Is("corvin.saw_her"));
            Assert.IsTrue(Offerings.IsGiven(w, "isolde.first_sight"), "the first thing ever bound, gone");
            Assert.IsNull(Memories.HomeOf("isolde.first_sight"), "no home: it costs only itself");
            Assert.AreEqual(0, w.Flags.Count(kv => kv.Key.EndsWith(".weakened")), "and loosens nothing");
        }

        [Test]
        public void OfferingAPlacesMemoryWeakensItsAnchor()
        {
            var w = Carrying("dotha.nine_songs");
            Places.Anchor(w, "Saltmarrow_B");
            Assert.AreEqual(0, FadeStages.Get(w, "Saltmarrow_B"));
            Assert.IsFalse(FadeStages.Advance(w, "Saltmarrow_B", 1), "an anchor holds against the story's fade");
            Offerings.Offer(w, "chapel_door", "dotha.nine_songs");
            Assert.AreEqual(1, Offerings.Weakened(w, "Saltmarrow_B"), "bible 10: offering one weakens that place's anchor");
            Assert.AreEqual(1, FadeStages.Get(w, "Saltmarrow_B"), "the ink thins a stage");
            Assert.AreEqual(PlaceFate.Anchored, Places.FateOf(w, "Saltmarrow_B"), "the fate stays: it is final");

            var held = Carrying("dotha.nine_songs");
            Places.Hold(held, "Saltmarrow_B");
            Offerings.Offer(held, "chapel_door", "dotha.nine_songs");
            Assert.AreEqual(0, Offerings.Weakened(held, "Saltmarrow_B"), "a held place is held by its people, not by ink");
            Assert.AreEqual(0, FadeStages.Get(held, "Saltmarrow_B"));

            var loose = Carrying("dotha.nine_songs");
            Offerings.Offer(loose, "chapel_door", "dotha.nine_songs");
            Assert.AreEqual(0, Offerings.Weakened(loose, "Saltmarrow_B"), "not yet anchored: nothing to weaken, but nothing to anchor with");
            Assert.AreEqual(0, FadeStages.Get(loose, "Saltmarrow_B"));

            // A loosened seal thins, but never to blank.
            var worn = new WorldState();
            Places.Anchor(worn, "Saltmarrow_B");
            for (int i = 0; i < 6; i++) FadeStages.Loosen(worn, "Saltmarrow_B");
            Assert.AreEqual(FadeStages.Max - 1, FadeStages.Get(worn, "Saltmarrow_B"));
            Assert.IsFalse(FadeStages.IsErased(worn, "Saltmarrow_B"));
        }
    }
}
