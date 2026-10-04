using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;

namespace OWSBG.Tests
{
    /// <summary>
    /// The enemies' voices (AUD-10, docs/design/enemy-sounds.md): every family in the game has one and every voice's cues
    /// exist; each sounds like what it is drawn as (a shell a tick, ink wet and low, the Cantor's bell in D, brass bright,
    /// paper dry and quiet, earth low, a reedling's peep high, the wasp's hum at its pitch, every loop meeting itself);
    /// the bank's order of keeping voices is the direction's; a sound stands where its enemy is.
    /// </summary>
    public class EnemySoundsTests
    {
        static IEnumerable<Type> Families => typeof(Enemy).Assembly.GetTypes().Where(t => typeof(Enemy).IsAssignableFrom(t) && !t.IsAbstract);

        [Test]
        public void EveryFamilyHasAVoiceAndEveryVoiceItsCues()
        {
            var families = Families.ToList();
            Assert.GreaterOrEqual(families.Count, 28, "the roster and the bosses");
            foreach (var t in families) Assert.IsTrue(EnemySounds.Has(t.Name), t.Name + " has a voice of its own, not the default's");
            Assert.IsFalse(EnemySounds.Has("Nobody"));
            Assert.AreSame(EnemySounds.Of(EnemySounds.Default), EnemySounds.Of("Nobody"), "a family the table does not know is wet ink");
            Assert.AreSame(EnemySounds.Of(EnemySounds.Default), EnemySounds.Of(null));
            Assert.AreEqual(EnemySounds.Material.Ink, EnemySounds.Of("Nobody").Material);

            foreach (var v in EnemySounds.Voices)
            {
                Assert.IsNotEmpty(v.What, v.Family + " says what it sounds like");
                foreach (var id in v.Cues)
                {
                    var c = InkSounds.Of(id);
                    Assert.IsNotNull(c, v.Family + " names " + id + ", which exists");
                    Assert.AreEqual(InkSounds.Kind.Enemy, c.Kind, id);
                }
                Assert.AreEqual(AudioDirection.Voice.Enemy, InkSounds.Of(v.Hurt).Voice, v.Family + "'s hurt is an enemy hit");
                Assert.AreEqual(AudioDirection.Voice.Enemy, InkSounds.Of(v.Death).Voice, v.Family + "'s death is an enemy death");
                if (v.Blocked != null) Assert.AreEqual(AudioDirection.Voice.Enemy, InkSounds.Of(v.Blocked).Voice);
                foreach (var m in v.Moves.Values) { Assert.AreEqual(AudioDirection.Voice.World, InkSounds.Of(m).Voice, m + " is a world one-shot"); Assert.IsFalse(InkSounds.Of(m).Loop, m + " is played once"); }
                foreach (var l in v.Loops.Values) { Assert.AreEqual(AudioDirection.Voice.World, InkSounds.Of(l).Voice); Assert.IsTrue(InkSounds.Of(l).Loop, l + " loops"); }
                Assert.AreEqual(EnemySounds.CueOf(v.Material, "hurt"), v.Hurt);
                Assert.AreEqual(v.OwnDeath ?? EnemySounds.CueOf(v.Material, "death"), v.Death, "the material's death, unless it dies its own way (AUD-15)");
                foreach (var e in v.Events.Values) { Assert.AreEqual(AudioDirection.Voice.World, InkSounds.Of(e).Voice, e + " is a world one-shot"); Assert.IsFalse(InkSounds.Of(e).Loop, e + " is played once"); }
            }
            // The moves are named by the clips the animator asks for.
            Assert.AreEqual("crab_hop", EnemySounds.MoveCue("MarshCrab", "hop"));
            Assert.AreEqual("crab_scuttle", EnemySounds.LoopCue("MarshCrab", "move"));
            Assert.IsNull(EnemySounds.MoveCue("MarshCrab", "hurt"), "hurt is the hit's, not a move");
            Assert.IsNull(EnemySounds.MoveCue("MarshCrab", null));
            Assert.AreEqual("wasp_buzz", EnemySounds.LoopCue("Pulpwasp", "idle"));
            Assert.AreEqual("wasp_buzz", EnemySounds.LoopCue("Pulpwasp", "spit"), "the hum does not stop to spit");
            Assert.AreEqual("cantor_toll", EnemySounds.MoveCue("Cantor", "recover"), "the toll lands as the ring ends");
            Assert.AreEqual("ember_crackle", EnemySounds.LoopCue("Salamander", "rush"));
            Assert.IsNull(EnemySounds.LoopCue("Salamander", "idle"), "cool, the embers are quiet");
            Assert.AreEqual("shell_block", EnemySounds.Of("MarshCrab").Blocked);
            Assert.IsNull(EnemySounds.Of("Smudge").Blocked, "an undrawn smudge is nothing to hit: silence");
            Assert.IsNull(EnemySounds.Of("Sketch").Blocked, "the fill left out: silence");
            Assert.That(EnemySounds.Of("Tussock").BlockedClips, Is.EquivalentTo(new[] { "breach", "idle" }), "a tussock's shell clacks only when it is up");
            Assert.IsNull(EnemySounds.Of("MarshCrab").BlockedClips, "a crab's shell is always up");
            Assert.AreEqual(EnemySounds.Material.Bell, EnemySounds.Of("Cantor").Material);
            Assert.AreEqual(EnemySounds.Material.Brass, EnemySounds.Of("Warden").Material);
            Assert.AreEqual(EnemySounds.Material.Brass, EnemySounds.Of("Halvard").Material, "a Warden is a Warden");
            Assert.AreEqual(EnemySounds.Material.Ink, EnemySounds.Of("MemorySmudge").Material);
            Assert.AreEqual(EnemySounds.Material.Bell, EnemySounds.Of("Choir").Material);
            Assert.AreEqual(InkSounds.Of("stroke").Voice, AudioDirection.Voice.Wren, "hers are not the enemies'");
        }

        static float[] R(string id) => InkSounds.Render(id);

        [Test]
        public void EveryVoiceIsItsMaterial()
        {
            var shell = R("shell_hurt");
            Assert.Less(InkSounds.Seconds(shell), 0.12f, "a shell is a tick");
            Assert.Greater(RollCallSong.EnergyAbove(shell, 800f), 0.6f, "hard and high");
            Assert.Less(InkSounds.Seconds(R("shell_block")), InkSounds.Seconds(shell), "the quill turned away is shorter still");
            Assert.Greater(InkSounds.Seconds(R("shell_death")), 0.3f, "the shell cracking takes a moment");

            var ink = R("ink_hurt");
            Assert.Less(RollCallSong.EnergyAbove(ink, 600f), 0.3f, "ink is wet and low");
            Assert.Less(InkSounds.Seconds(ink), InkSounds.Seconds(R("hurt")), "shorter than her smudge: it is not her");
            Assert.Greater(InkSounds.Seconds(R("ink_death")), 0.4f, "and dissolves slowly");

            var bell = R("bell_hurt");
            Assert.AreEqual(EnemySounds.CantorHz, RollCallSong.PitchOf(bell, 0, 4096, 500f, 2000f), EnemySounds.CantorHz * 0.03f, "the handbell rings D, the coast's tonic");
            Assert.AreEqual(RollCallSong.TonicHz(Region.Saltmarrow) * 16f, EnemySounds.CantorHz, 1f, "four octaves above the drone's D");
            var toll = R("cantor_toll");
            Assert.Greater(InkSounds.Seconds(toll), 0.8f, "the toll rings on");
            Assert.Less(RollCallSong.EnergyAbove(toll, 2000f), 0.3f, "and is the lower bell");

            Assert.Greater(RollCallSong.EnergyAbove(R("brass_hurt"), 2000f), 0.5f, "brass is bright");
            Assert.LessOrEqual(InkSounds.Of("paper_hurt").Gain, 0.5f, "dry paper is quiet");
            Assert.Greater(RollCallSong.EnergyAbove(R("paper_hurt"), 1200f), 0.7f, "and has nothing low in it");
            Assert.Less(RollCallSong.EnergyAbove(R("tussock_heave"), 300f), 0.3f, "earth heaves low");
            Assert.Less(RollCallSong.EnergyAbove(R("earth_hurt"), 300f), 0.5f);
            float peep = RollCallSong.PitchOf(R("reedling_peep"), 0, 2048, 1000f, 4000f);
            Assert.That(peep, Is.InRange(1800f, 3400f), "a reedling peeps high");
            float fluffDeath = RollCallSong.PitchOf(R("fluff_death"), 0, 2048, 1000f, 4000f);
            Assert.Greater(fluffDeath, peep * 0.6f, "its last peep starts high too");
            Assert.Less(InkSounds.Of("moth_pass").Gain, 0.5f, "the quill through a cloud is almost nothing");

            var buzz = R("wasp_buzz");
            Assert.IsTrue(InkSounds.Of("wasp_buzz").Loop);
            Assert.AreEqual(EnemySounds.WaspHz, RollCallSong.PitchOf(buzz, 0, 4096, 40f, 400f), 15f, "the wasp hums at its pitch");
            Assert.AreEqual(1f, InkSounds.Seconds(buzz), 0.001f, "a second round");
            foreach (var c in InkSounds.Cues.Where(c => c.Loop && c.Kind == InkSounds.Kind.Enemy))
            {
                var s = R(c.Id);
                float inside = 0f;
                for (int i = 1; i < s.Length; i++) inside = Math.Max(inside, Math.Abs(s[i] - s[i - 1]));
                Assert.LessOrEqual(Math.Abs(s[0] - s[s.Length - 1]), inside + 0.01f, c.Id + " meets itself");
                Assert.That(InkSounds.Of(c.Id).Gain, Is.LessThanOrEqualTo(0.5f), c.Id + ": a loop sits under the room, not on it");
            }
            Assert.AreEqual(14, InkSounds.Cues.Count(c => c.Loop && c.Kind == InkSounds.Kind.Enemy), "the scuttle, the drift, the crackle, the flutter, the hum, the rumble; and the bosses' "
                + "lamp, furnace, Bind, stone wings, iron feet, burning, crayon and quill (AUD-15)");
        }

        [Test]
        public void TheDirectionsOrderIsTheBanks()
        {
            var order = (AudioDirection.Voice[])Enum.GetValues(typeof(AudioDirection.Voice));
            Assert.AreEqual(AudioDirection.Priority.Length, order.Length, "a rank for every line of the direction's order");
            for (int i = 1; i < order.Length; i++) Assert.Less((int)order[i - 1], (int)order[i], "kept in this order");
            Assert.That(AudioDirection.PriorityOf(AudioDirection.Voice.Tell), Does.Contain("tells"));
            Assert.That(AudioDirection.PriorityOf(AudioDirection.Voice.WrenHurt), Does.Contain("Wren hurt"));
            Assert.That(AudioDirection.PriorityOf(AudioDirection.Voice.Dialogue), Does.Contain("dialogue"));
            Assert.That(AudioDirection.PriorityOf(AudioDirection.Voice.Wren), Does.Contain("strikes"));
            Assert.That(AudioDirection.PriorityOf(AudioDirection.Voice.Enemy), Does.Contain("enemy"));
            Assert.That(AudioDirection.PriorityOf(AudioDirection.Voice.World), Does.Contain("world"));
            Assert.That(AudioDirection.PriorityOf(AudioDirection.Voice.Ambience), Does.Contain("ambience"));
            Assert.That(AudioDirection.PriorityOf(AudioDirection.Voice.Music), Does.Contain("music"));
            // Every cue knows its rank.
            foreach (var t in Enum.GetValues(typeof(AudioDirection.Tell)).Cast<AudioDirection.Tell>())
                Assert.AreEqual(AudioDirection.Voice.Tell, InkSounds.Of(InkSounds.TellCue(t)).Voice, t + "'s tell is kept first");
            Assert.AreEqual(AudioDirection.Voice.WrenHurt, InkSounds.Of("hurt").Voice);
            Assert.AreEqual(AudioDirection.Voice.WrenHurt, InkSounds.Of("died").Voice);
            foreach (var id in new[] { "stroke", "hit", "kill", "bind", "pogo", "crosshatch" }) Assert.AreEqual(AudioDirection.Voice.Wren, InkSounds.Of(id).Voice, id);
            Assert.AreEqual(AudioDirection.Voice.Enemy, InkSounds.Of("ink_hurt").Voice);
            Assert.AreEqual(AudioDirection.Voice.World, InkSounds.Of("crab_hop").Voice);
            Assert.Less((int)AudioDirection.Voice.Wren, (int)AudioDirection.Voice.Enemy, "her strikes before their hits");
            Assert.Less((int)AudioDirection.Voice.Enemy, (int)AudioDirection.Voice.World, "their hits before their steps");
            Assert.AreEqual(24, AudioDirection.VoiceLimit);
        }

        [Test]
        public void ASoundStandsWhereItsEnemyIs()
        {
            Assert.AreEqual(0f, InkSounds.Pan(0f, 9f), 0.001f, "under the listener: centred");
            Assert.AreEqual(0.35f, InkSounds.Pan(4.5f, 9f), 0.01f, "halfway to the edge: halfway right");
            Assert.AreEqual(0.7f, InkSounds.Pan(9f, 9f), 0.01f, "at the edge");
            Assert.AreEqual(-0.7f, InkSounds.Pan(-30f, 9f), 0.01f, "and never harder than that: a room is a plane, not a headphone test");
            Assert.AreEqual(1f, InkSounds.Falloff(0f, 9f), 0.001f);
            Assert.AreEqual(1f, InkSounds.Falloff(-9f, 9f), 0.001f, "on screen is whole");
            Assert.AreEqual(0.5f, InkSounds.Falloff(18f, 9f), 0.01f, "a screen past the edge: half");
            Assert.AreEqual(0f, InkSounds.Falloff(27f, 9f), 0.001f, "three screens out: gone");
            Assert.AreEqual(0f, InkSounds.Falloff(-40f, 9f), 0.001f);
            Assert.Greater(InkSounds.Falloff(10f, 9f), InkSounds.Falloff(20f, 9f), "further is quieter");
        }
    }
}
