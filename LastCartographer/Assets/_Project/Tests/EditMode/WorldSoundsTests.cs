using System;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// The world's sounds and the pages' (AUD-11, docs/design/world-sounds.md): every Instrument, decision and commission
    /// step has its cue; the pages ride the Ui bus and the world the Sfx bus, both ranked as world one-shots and quieter
    /// than her strikes; each sounds like what it is (the cursor a tick, a room a page turned, a lamp a whump with glass
    /// in it, a stamp low); a slider's tick and a seed's drop carry their values in their pitch.
    /// </summary>
    public class WorldSoundsTests
    {
        static float[] R(string id) => InkSounds.Render(id);

        [Test]
        public void EveryInstrumentDecisionAndStepHasItsCue()
        {
            foreach (InstrumentKind k in Enum.GetValues(typeof(InstrumentKind)))
            {
                var id = WorldSounds.InstrumentCue(k);
                if (k == InstrumentKind.None) { Assert.IsNull(id, "an empty slot makes no sound of its own"); continue; }
                Assert.IsNotNull(InkSounds.Of(id), k + " has a sound");
                Assert.AreEqual(InkSounds.Kind.Wren, InkSounds.Of(id).Kind, k + " is hers: it rides with her strikes");
            }
            Assert.IsNull(WorldSounds.FateCue(PlaceFate.Unwritten));
            Assert.AreEqual("anchor", WorldSounds.FateCue(PlaceFate.Anchored));
            Assert.AreEqual("held", WorldSounds.FateCue(PlaceFate.Held));
            Assert.AreEqual("released", WorldSounds.FateCue(PlaceFate.Released));
            Assert.IsNull(WorldSounds.CommissionCue(CommissionState.Posted), "posting is the ledger's silence");
            Assert.AreEqual("ledger_take", WorldSounds.CommissionCue(CommissionState.Taken));
            Assert.AreEqual("commission_done", WorldSounds.CommissionCue(CommissionState.Fulfilled));
            Assert.AreEqual("stamp", WorldSounds.CommissionCue(CommissionState.Closed));
            Assert.AreEqual("commission_failed", WorldSounds.CommissionCue(CommissionState.Failed));
            foreach (var id in new[] { "anchor", "held", "released", "ledger_take", "commission_done", "stamp", "commission_failed" })
                Assert.IsTrue(InkSounds.Has(id), id);
        }

        [Test]
        public void ThePagesRideTheUiBusAndTheWorldTheSfxBus()
        {
            var pages = InkSounds.Cues.Where(c => c.Kind == InkSounds.Kind.Ui).ToList();
            var world = InkSounds.Cues.Where(c => c.Kind == InkSounds.Kind.World).ToList();
            Assert.GreaterOrEqual(pages.Count, 9, "open, close, back, move, tick, select, denied, line, note");
            Assert.GreaterOrEqual(world.Count, 25);
            foreach (var c in pages)
            {
                Assert.AreEqual(Mix.Bus.Ui, c.Bus, c.Id + ": heard while the game is paused");
                Assert.AreEqual(AudioDirection.Voice.World, c.Voice, c.Id);
                Assert.LessOrEqual(c.Gain, 0.5f, c.Id + ": a page is quieter than a strike");
                Assert.That(InkSounds.FileName(c), Does.StartWith("ui_sfx_"));
                Assert.IsFalse(c.Loop);
            }
            foreach (var c in world)
            {
                Assert.AreEqual(Mix.Bus.Sfx, c.Bus, c.Id + ": in the room");
                Assert.AreEqual(AudioDirection.Voice.World, c.Voice, c.Id + ": kept after the fight's sounds");
                Assert.That(InkSounds.FileName(c), Does.StartWith("world_sfx_"));
                Assert.IsFalse(c.Loop);
            }
            foreach (var c in InkSounds.Cues.Where(c => c.Kind != InkSounds.Kind.Ui)) Assert.AreEqual(Mix.Bus.Sfx, c.Bus, c.Id);
            // The mix keeps the pages whole while the room is paused (audio-mix.md).
            Assert.AreEqual(1f, Mix.Of(Mix.Snapshot.Paused).Gain[(int)Mix.Bus.Ui], 0.001f, "paused, the pages are heard");
            Assert.AreEqual(0f, Mix.Of(Mix.Snapshot.Paused).Gain[(int)Mix.Bus.Sfx], 0.001f, "and the room is not");
        }

        [Test]
        public void EachSoundsLikeWhatItIs()
        {
            Assert.Less(InkSounds.Seconds(R("ui_move")), 0.05f, "the cursor is a tick");
            Assert.Less(InkSounds.Seconds(R("ui_line")), 0.08f, "a line shown is a quick scratch, never in the way of the words");
            Assert.Less(InkSounds.Seconds(R("ui_open")), InkSounds.Seconds(R("page_turn")), "a page lifted is quicker than a room's page turned");
            Assert.Greater(InkSounds.Seconds(R("page_turn")), 0.3f, "a room turns slowly");
            var lamp = R("lamp_lit");
            Assert.Greater(RollCallSong.EnergyAbove(lamp, 2000f), 0.05f, "a lamp has glass in it");
            Assert.Less(RollCallSong.EnergyAbove(lamp, 2000f), 0.8f, "under its whump");
            Assert.Less(RollCallSong.EnergyAbove(R("stamp"), 600f), 0.5f, "a stamp is low");
            Assert.Less(RollCallSong.EnergyAbove(R("anchor"), 600f), 0.6f, "a stake driven is low");
            Assert.Greater(RollCallSong.EnergyAbove(R("ui_move"), 1500f), 0.6f, "the nib's tick is high");
            Assert.Greater(InkSounds.Seconds(R("erased")), InkSounds.Seconds(R("fade_step")), "erasure is the eraser dragged twice; a stage is one soft pass");
            Assert.Less(InkSounds.Of("fade_step").Gain, InkSounds.Of("erased").Gain, "and quieter");
            Assert.Greater(RollCallSong.EnergyAbove(R("parry"), 2500f), 0.5f, "a parry rings bright");
            Assert.AreEqual(1f, InkSounds.Of("parry").Gain, 0.001f, "and full: it is the answer to a tell");
        }

        [Test]
        public void ASlidersTickAndASeedsDropCarryTheirValues()
        {
            Assert.AreEqual(1f, WorldSounds.TickPitch(-1f), 0.001f, "a switch ticks plain");
            Assert.AreEqual(0.75f, WorldSounds.TickPitch(0f), 0.001f, "silent is low");
            Assert.AreEqual(1.5f, WorldSounds.TickPitch(1f), 0.001f, "full is an octave up");
            for (int i = 1; i <= 10; i++) Assert.Greater(WorldSounds.TickPitch(i / 10f), WorldSounds.TickPitch((i - 1) / 10f), "every tenth is higher than the last");
            Assert.AreEqual(1f, WorldSounds.SeedPitch(1), 0.001f);
            Assert.Greater(WorldSounds.SeedPitch(3), WorldSounds.SeedPitch(1), "a richer seed drops higher");
            Assert.AreEqual(WorldSounds.SeedPitch(9), WorldSounds.SeedPitch(50), 0.001f, "up to a point");
            Assert.LessOrEqual(WorldSounds.SeedPitch(50), 1.5f, "never past a fifth");
            Assert.AreEqual(1f, WorldSounds.SeedPitch(0), 0.001f);
        }
    }
}
