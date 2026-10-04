using System;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;

namespace OWSBG.Tests
{
    /// <summary>
    /// The last four main bosses' themes (AUD-14, docs/design/music.md §3e): the Gatekeeper's in the Verdance's key, its
    /// one bowed voice stopping unresolved and then trying to fly; Oriel's, the Guild's motif mirrored; the Bells', which
    /// strike no bell; Corra's Drawing's, her father drawn bigger and a small one beside him. Each opens with its bed,
    /// pulse and lead and adds or changes a layer a phase, and with them every main boss has a theme of its own.
    /// </summary>
    public class MainBossScoreTests
    {
        static readonly (string family, Region region)[] Four =
        {
            (nameof(Gatekeeper), Region.Verdance), (nameof(Oriel), Region.Halden), (nameof(HalfCathedralBells), Region.Greyfold), (nameof(CorrasDrawing), Region.Blank),
        };

        static int[] Degrees(Score.Stem s) => s.Notes.OrderBy(n => n.Start).Select(n => n.Degree).ToArray();
        static int Pc(int degree) => ((degree % 7) + 7) % 7;

        [Test]
        public void EachIsInItsRegionsKeyAndBeatAndAddsALayerAPhase()
        {
            foreach (var (family, region) in Four)
            {
                var t = Score.ThemeOfBoss(family);
                Assert.IsNotNull(t, family + " has a theme of its own (a boss's family is its class's name, which the driver asks by)");
                Assert.AreNotEqual(Score.SharedBoss, t.Boss);
                Assert.AreEqual(region, t.Region, family + " in the key of the region it is fought in");
                Assert.AreEqual(AudioDirection.BeatOf(region), t.Beat, 1e-4f, family + " at its region's beat");
                Assert.AreEqual(0, t.RestBars, t.Id + ": a boss theme never rests");
                Assert.IsFalse(t.KeepsBeat, t.Id + ": not a rhythm boss");
                Assert.AreEqual(0, t.ForPhase);
                CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, t.Stems.Select(s => s.Phase).Distinct(), t.Id + ": a layer enters at each phase");
                foreach (var need in new[] { "bed", "pulse", "lead" }) Assert.AreEqual(1, t.Stem(need).Phase, t.Id + " opens with its " + need);
                Assert.IsNotNull(t.Stem("voices"), t.Id + " has voices (audio-direction 6)");
                Assert.IsTrue(t.Stems.Any(s => s.Until == 3), t.Id + ": a layer changed in the last phase, not only added");
                foreach (var s in t.Stems)
                {
                    Assert.IsNotEmpty(s.Notes, t.Id + "/" + s.Id);
                    Assert.IsFalse(s.Combat);
                    Assert.AreEqual(s.Instrument, Score.InstrumentOf(s.Instrument).Id, t.Id + "/" + s.Id + " plays a known instrument");
                    foreach (var n in s.Notes)
                    {
                        Assert.GreaterOrEqual(n.Start, 0f);
                        Assert.IsTrue(Score.InMode(t.Region, Score.Semitones(t.Region, n.Degree)), t.Id + "/" + s.Id + " " + n + " in the mode");
                        Assert.LessOrEqual(n.Start + n.Beats, t.Bars * Score.BeatsPerBar + 1e-3f, t.Id + "/" + s.Id + " " + n + " within the loop");
                    }
                }
                for (int phase = 1; phase <= 3; phase++)
                    Assert.IsTrue(t.Stems.Any(s => Score.Theme.Sounds(s, phase) && s.Id == "pulse"), t.Id + ": the pulse in phase " + phase);
                Assert.AreSame(t, Score.ThemeOfBoss(family, region, 2), "the fight's lookup finds it in every phase");
                var stems = Score.Render(t);
                int len = (int)Math.Round(t.LoopSeconds * Score.SampleRate);
                var mix = new float[len];
                foreach (var kv in stems) { Assert.AreEqual(len, kv.Value.Length, t.Id + "/" + kv.Key + " is exactly the loop"); for (int i = 0; i < len; i++) mix[i] += kv.Value[i]; }
                Assert.AreEqual(AudioDirection.SfxPeakDbtp, RollCallSong.PeakDb(mix), 0.05f, t.Id + " at the ceiling");
            }
        }

        [Test]
        public void EveryMainBossHasAThemeOfItsOwnAndOnlyTheOptionalsShare()
        {
            foreach (var sheet in Bosses.All)
            {
                string family = sheet.Id == "bells" ? "HalfCathedralBells"
                    : string.Concat(sheet.Id.Split('_').Where(w => !int.TryParse(w, out _)).Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1)));
                if (!sheet.Optional) Assert.IsNotNull(Score.ThemeOfBoss(family), sheet.Name + " (" + family + ") has a theme of its own");
            }
            foreach (var (id, family) in new[] { ("choir", "Choir"), ("hale", "Hale"), ("fallen_star", "FallenStar") })
            {
                Assert.IsTrue(Bosses.Find(id).Optional, id + " is an optional");
                Assert.IsNull(Score.ThemeOfBoss(family), family + " still fights to its region's motif");
            }
        }

        [Test]
        public void TheGatekeepersVoiceStopsUnresolvedAndThenTriesToFly()
        {
            var t = Score.ThemeOfBoss("Gatekeeper");
            foreach (var s in t.Stems) CollectionAssert.Contains(Score.BandOf(Region.Verdance), s.Instrument, s.Id + " from the Verdance's band");
            var lead = t.Stem("lead");
            Assert.AreEqual("gamba", lead.Instrument, "the Verdance's one bowed voice");
            Assert.AreEqual(1, Pc(lead.Notes.OrderBy(n => n.Start).Last().Degree), "it stops on the flat second, unresolved");
            Assert.GreaterOrEqual(lead.Notes.Average(n => n.Beats), 2f, "slow as stone");
            Assert.AreEqual(3, lead.Until, "the voice that stood gives way to the flight");
            Assert.AreEqual("organ", t.Stem("bed").Instrument);
            Assert.AreEqual(3, t.Stem("bed").Until, "the roots tear free: the pedal leaves");
            var voices = t.Stem("voices");
            Assert.AreEqual(2, voices.Phase, "it rises to the top of the gate");
            var climb = Degrees(voices);
            for (int i = 1; i < climb.Length; i++) Assert.Greater(climb[i], climb[i - 1], "the glass a step higher every bar");
            var flight = t.Stem("flight");
            Assert.AreEqual(3, flight.Phase);
            for (int bar = 0; bar < t.Bars; bar++)
            {
                var beat = flight.Notes.Where(n => n.Start >= bar * 4f && n.Start < bar * 4f + 4f).OrderBy(n => n.Start).ToList();
                int top = beat.Max(n => n.Degree);
                Assert.Less(beat.Last().Degree, top - 3, "bar " + bar + ": every climb comes down hard");
            }
            var peaks = Enumerable.Range(0, t.Bars).Select(bar => flight.Notes.Where(n => n.Start >= bar * 4f && n.Start < bar * 4f + 4f).Max(n => n.Degree)).ToArray();
            Assert.Less(peaks.Last(), peaks.First(), "badly: it climbs lower as it tires");
        }

        [Test]
        public void OrielMirrorsTheGuildsMotifAndHoldsTheCadenceOpen()
        {
            var t = Score.ThemeOfBoss("Oriel");
            foreach (var s in t.Stems) CollectionAssert.Contains(Score.BandOf(Region.Halden), s.Instrument, s.Id + " from Halden's band");
            var lead = t.Stem("lead");
            Assert.AreEqual("brass", lead.Instrument, "a Warden: the Guild's brass");
            var mirror = Score.GuildMotif.Select(m => m.degree).Reverse().ToArray();
            CollectionAssert.AreEqual(mirror, Degrees(lead).Take(mirror.Length), "the Guild's motif played backwards: Wren's combo reversed");
            CollectionAssert.AreEqual(Score.GuildMotif.Select(m => m.beats).Reverse(), lead.Notes.OrderBy(n => n.Start).Take(mirror.Length).Select(n => n.Beats), "its lengths mirrored with it");
            var voices = t.Stem("voices");
            Assert.AreEqual(2, voices.Phase, "her own stance, the right way up, in the second phase");
            CollectionAssert.AreEqual(Score.GuildMotif.Select(m => m.degree + 7), Degrees(voices).Take(Score.GuildMotif.Length), "the motif forwards, an octave above her mirror");
            var flourish = t.Stem("flourish");
            Assert.AreEqual(2, flourish.Phase); Assert.AreEqual("harpsichord", flourish.Instrument);
            Assert.IsTrue(flourish.Notes.All(n => n.Beats <= 0.25f + 1e-4f), "the pen's run: quick");
            Assert.AreEqual(3, t.Stem("bed").Until, "the clockwork gives way when she Binds");
            var bind = t.Stem("bind");
            Assert.AreEqual(3, bind.Phase);
            Assert.IsTrue(bind.Notes.All(n => Pc(n.Degree) == 4 || Pc(n.Degree) == 6), "the dominant and its leading tone, held");
            Assert.IsFalse(bind.Notes.Any(n => Pc(n.Degree) == 0), "and never let home");
            var bass = t.Stem("bed").Notes.OrderBy(n => n.Start).Select(n => Pc(n.Degree)).ToList();
            for (int i = 0; i + 1 < bass.Count; i++) if (bass[i] == 4) Assert.AreEqual(5, bass[i + 1], "Halden's V goes to vi, never to I");
        }

        [Test]
        public void TheBellsStrikeNoBellAndTheGreatOneSingsTheAnswer()
        {
            var t = Score.ThemeOfBoss("HalfCathedralBells");
            Assert.IsFalse(t.Stems.Any(s => s.Instrument == "bell" || s.Instrument == "handbell" || s.Instrument == "anvil"), "every ring in the nave is the fight's own tell");
            foreach (var s in t.Stems) CollectionAssert.Contains(new[] { "heldtone", "cymbal" }, s.Instrument, s.Id + ": the Greyfold's held sine and bowed cymbal");
            Assert.AreEqual(3, t.Stem("bed").Until, "the held tone gives way to the white");
            var lead = t.Stem("lead");
            CollectionAssert.AreEqual(Score.BellsNorth, Degrees(lead), "\"For the flock, going north.\": rising, a bar a note");
            var voices = t.Stem("voices");
            Assert.AreEqual(2, voices.Phase, "phase 2: two bells, in canon");
            var first = lead.Notes.OrderBy(n => n.Start).ToList();
            var second = voices.Notes.OrderBy(n => n.Start).ToList();
            Assert.AreEqual(first.Count, second.Count);
            for (int bar = 0; bar < t.Bars; bar++)
            {
                var follows = first[(bar + t.Bars - 1) % t.Bars];
                Assert.AreEqual(follows.Degree - 3, second[bar].Degree, "bar " + bar + ": the second bell a bar behind the first, a fourth under");
                Assert.AreEqual(bar * 4f, second[bar].Start, 1e-4f);
            }
            var great = t.Stem("great");
            Assert.AreEqual(3, great.Phase, "the great bell");
            CollectionAssert.AreEqual(Score.AnswerDegrees(Region.Greyfold), Degrees(great), "\"We did not forget you.\": the roll-call's answer");
            Assert.IsTrue(great.Notes.All(n => n.Beats >= Score.BeatsPerBar), "a bar a note");
            Assert.Less(great.Notes.Max(n => n.Degree), lead.Notes.Min(n => n.Degree), "under everything");
        }

        [Test]
        public void CorrasDrawingDrawsHimBiggerAndFightsInOutline()
        {
            var t = Score.ThemeOfBoss("CorrasDrawing");
            foreach (var s in t.Stems) CollectionAssert.Contains(Score.BandOf(Region.Blank), s.Instrument, s.Id + " from the Blank's band");
            var lead = t.Stem("lead");
            Assert.AreEqual("celesta", lead.Instrument, "a child's song");
            Assert.IsTrue(lead.Notes.All(n => new[] { 1, 2, 4, 5 }.Contains(Pc(n.Degree))), "sing-song on the fifth and the third");
            Assert.IsFalse(Score.ComesHome(lead), "it does not come home");
            for (int b = 0; b < t.Bars * Score.BeatsPerBar; b++)
                Assert.IsTrue(t.Stem("pulse").Notes.Any(n => Math.Abs(n.Start - b) < 1e-4f), "a child counting, on beat " + b);
            var motif = Score.GuildMotif;
            var voices = t.Stem("voices").Notes.OrderBy(n => n.Start).ToList();
            Assert.AreEqual(2, t.Stem("voices").Phase, "phase 2: it draws a second Voss");
            CollectionAssert.AreEqual(motif.Select(m => m.degree), voices.Select(n => n.Degree), "her father's motif, the Guild's");
            CollectionAssert.AreEqual(motif.Select(m => m.beats * 2f), voices.Select(n => n.Beats), "\"He was bigger.\": drawn at twice its length");
            var small = t.Stem("small").Notes.OrderBy(n => n.Start).ToList();
            Assert.AreEqual(2, t.Stem("small").Phase);
            CollectionAssert.AreEqual(motif.Select(m => m.beats), small.Take(motif.Length).Select(n => n.Beats), "the small one at its own size");
            Assert.AreEqual(voices[0].Start, small[0].Start, "holding its hand: they start together");
            Assert.Greater(Score.InstrumentOf("celesta").Transpose, Score.InstrumentOf("reversedpiano").Transpose, "the small one above the big one");
            Assert.AreEqual(3, t.Stem("bed").Until, "the room's colour goes");
            Assert.AreEqual(3, lead.Until, "the crayon runs out");
            var outline = t.Stem("outline");
            Assert.AreEqual(3, outline.Phase);
            var onBeat = lead.Notes.Where(n => n.Start % 1f == 0f).OrderBy(n => n.Start).ToList();
            Assert.AreEqual(onBeat.Count * 2, outline.Notes.Count, "the outline: only the notes on the beat, twice a loop");
            var firstPass = outline.Notes.OrderBy(n => n.Start).Take(onBeat.Count).ToList();
            for (int i = 0; i < onBeat.Count; i++)
            {
                Assert.AreEqual(onBeat[i].Degree, firstPass[i].Degree);
                Assert.AreEqual(onBeat[i].Start / 2f, firstPass[i].Start, 1e-4f, "faster: at twice the pace");
            }
        }
    }
}
