using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    public class WorldStateTests
    {
        [Test]
        public void FlagsRaiseChangeEventsOnlyWhenValueChanges()
        {
            var w = new WorldState();
            int events = 0;
            w.FlagChanged += (k, v) => events++;
            w.Set("a.b", 1);
            w.Set("a.b", 1);
            w.Set("a.b", 2);
            Assert.AreEqual(2, events);
            Assert.AreEqual(2, w.Get("a.b"));
            Assert.IsTrue(w.Is("a.b"));
            Assert.IsFalse(w.Is("missing"));
            Assert.AreEqual(7, w.Get("missing", 7));
        }

        [Test]
        public void SurveyMarksOnceAndNotifies()
        {
            var w = new WorldState();
            string last = null;
            w.VantageSurveyed += id => last = id;
            Assert.IsTrue(w.MarkSurveyed("Saltmarrow_A/Reedmother"));
            Assert.IsFalse(w.MarkSurveyed("Saltmarrow_A/Reedmother"));
            Assert.AreEqual("Saltmarrow_A/Reedmother", last);
            Assert.IsTrue(w.IsSurveyed("Saltmarrow_A/Reedmother"));
        }

        [Test]
        public void SaveJsonRoundTripsEverything()
        {
            var w = new WorldState();
            w.Set("prologue.started", 1);
            w.Set("$met_sable", true);
            w.Numbers["$iris_seeds"] = 12.5f;
            w.Strings["$last_place"] = "Drowned Quay";
            w.AnchoredPlaces.Add("halden.lowmarket.street");
            w.MarkSurveyed("Saltmarrow_A/Reedmother");
            w.BoundMemories.Add("isolde.first_sight");
            w.RespawnRoom = "Greybox_Saltmarrow_A";
            w.RespawnSpawn = "Desk";

            var json = GameState.ToJson(w);
            var back = GameState.FromJson(json);

            Assert.AreEqual(1, back.Get("prologue.started"));
            Assert.IsTrue(back.Is("$met_sable"));
            Assert.AreEqual(12.5f, back.Numbers["$iris_seeds"], 0.0001f);
            Assert.AreEqual("Drowned Quay", back.Strings["$last_place"]);
            Assert.IsTrue(back.AnchoredPlaces.Contains("halden.lowmarket.street"));
            Assert.IsTrue(back.IsSurveyed("Saltmarrow_A/Reedmother"));
            CollectionAssert.AreEqual(new[] { "isolde.first_sight" }, back.BoundMemories);
            Assert.AreEqual("Greybox_Saltmarrow_A", back.RespawnRoom);
            Assert.AreEqual("Desk", back.RespawnSpawn);
        }

        [Test]
        public void EquipmentAndWaxSealRoundTrip()
        {
            var w = new WorldState();
            var e = w.Equipment;
            e.OwnedCharters.Add(CharterKind.Drifter);
            Assert.IsTrue(e.SetCharter(CharterKind.Drifter));
            Assert.IsFalse(e.SetCharter(CharterKind.Warden), "not owned");
            e.OwnedInstruments.Add(InstrumentKind.PlumbWeight);
            e.OwnedInstruments.Add(InstrumentKind.WaxSeal);
            Assert.IsTrue(e.Equip(2, InstrumentKind.WaxSeal, 1));
            Assert.IsTrue(e.Equip(1, InstrumentKind.PlumbWeight, 6));
            Assert.IsTrue(e.Equip(2, InstrumentKind.PlumbWeight, 6), "moving to another slot swaps");
            Assert.AreEqual(InstrumentKind.WaxSeal, e.Slots[1].Kind, "the displaced Instrument takes the old slot");
            Assert.IsTrue(e.Equip(1, InstrumentKind.PlumbWeight, 6));
            Assert.AreEqual(InstrumentKind.WaxSeal, e.Slots[2].Kind);
            Assert.IsFalse(e.Equip(1, InstrumentKind.PlumbWeight, 6), "already there");
            Assert.IsFalse(e.Equip(0, InstrumentKind.CompassDart, 12), "not owned");
            Assert.IsTrue(e.Equip(2, InstrumentKind.None, 0), "clear a slot");
            Assert.IsTrue(e.Slots[2].IsEmpty);
            e.SetUses(1, 4);
            e.SelectedSlot = 1;
            w.WaxSealRoom = "Greybox_Saltmarrow_B";
            w.WaxSealX = 3.5f; w.WaxSealY = -1f;

            var back = GameState.FromJson(GameState.ToJson(w));
            var be = back.Equipment;
            Assert.AreEqual(CharterKind.Drifter, be.Charter);
            Assert.IsTrue(be.OwnsCharter(CharterKind.Surveyor), "the starting Charter is always owned");
            Assert.IsTrue(be.OwnsCharter(CharterKind.Drifter));
            Assert.IsTrue(be.OwnsInstrument(InstrumentKind.WaxSeal));
            Assert.AreEqual(InstrumentKind.PlumbWeight, be.Slots[1].Kind);
            Assert.AreEqual(4, be.Slots[1].UsesLeft);
            Assert.IsTrue(be.Slots[0].IsEmpty);
            Assert.AreEqual(1, be.SelectedSlot);
            Assert.IsTrue(back.HasWaxSeal);
            Assert.AreEqual("Greybox_Saltmarrow_B", back.WaxSealRoom);
            Assert.AreEqual(3.5f, back.WaxSealX, 0.001f);
        }
    }
}
