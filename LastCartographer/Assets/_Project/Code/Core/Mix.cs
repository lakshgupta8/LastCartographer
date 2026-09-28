using System;
using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// The mix (AUD-09, docs/design/audio-mix.md): six buses, a snapshot for each state the game can be in, ducks for
    /// the moments inside a state, the fade-stage filter, and the player's volumes. <see cref="Mixer"/> runs it; this
    /// class is the numbers, so a test can read them and a build can't drift from them. Gains are linear (1 is as
    /// designed); cutoffs are Hz on a low-pass filter (<see cref="Open"/> is no filter).
    /// </summary>
    public static class Mix
    {
        public enum Bus { Master, Music, Ambience, Sfx, Dialogue, Ui }

        /// <summary>The state the mix follows, most pressing first: the driver picks the first that holds.</summary>
        public enum Snapshot { Paused, Boss, Dialogue, Blank, Combat, Explore }

        /// <summary>A moment inside a state that pulls the others down while it lasts.</summary>
        public enum Duck { Line, Impact, Hurt, Bind }

        public const float Open = 22000f;
        public const int BusCount = 6;
        public static readonly Bus[] Buses = (Bus[])Enum.GetValues(typeof(Bus));
        public static readonly Snapshot[] Snapshots = (Snapshot[])Enum.GetValues(typeof(Snapshot));
        public static readonly Duck[] Ducks = (Duck[])Enum.GetValues(typeof(Duck));

        public sealed class SnapshotSpec
        {
            public Snapshot Name;
            /// <summary>Per bus, indexed by <see cref="Bus"/>; Master is always 1 here (the player owns it).</summary>
            public float[] Gain = new float[BusCount];
            /// <summary>Low-pass cutoff per bus; only Music and Ambience are filtered by the driver.</summary>
            public float[] Cutoff = new float[BusCount];
            /// <summary>Seconds the move into this snapshot takes.</summary>
            public float In;
            /// <summary>Seconds the move out of it takes.</summary>
            public float Out;
        }

        public sealed class DuckSpec
        {
            public Duck Name;
            /// <summary>The bus the moment is heard on; the ducked buses make room for it.</summary>
            public Bus Voice;
            /// <summary>What is left of each ducked bus while the duck holds (1 is untouched).</summary>
            public float[] Floor = new float[BusCount];
            public float Attack, Hold, Release;
        }

        static readonly Dictionary<Snapshot, SnapshotSpec> _snapshots = new Dictionary<Snapshot, SnapshotSpec>();
        static readonly Dictionary<Duck, DuckSpec> _ducks = new Dictionary<Duck, DuckSpec>();

        static SnapshotSpec Snap(Snapshot s, float @in, float @out, float music, float ambience, float sfx, float dialogue, float ui,
            float musicCutoff = Open, float ambienceCutoff = Open)
        {
            var spec = new SnapshotSpec { Name = s, In = @in, Out = @out };
            spec.Gain[(int)Bus.Master] = 1f;
            spec.Gain[(int)Bus.Music] = music; spec.Gain[(int)Bus.Ambience] = ambience; spec.Gain[(int)Bus.Sfx] = sfx;
            spec.Gain[(int)Bus.Dialogue] = dialogue; spec.Gain[(int)Bus.Ui] = ui;
            for (int i = 0; i < BusCount; i++) spec.Cutoff[i] = Open;
            spec.Cutoff[(int)Bus.Music] = musicCutoff; spec.Cutoff[(int)Bus.Ambience] = ambienceCutoff;
            _snapshots[s] = spec;
            return spec;
        }

        static DuckSpec DuckOf(Duck d, Bus voice, float attack, float hold, float release, float music, float ambience, float sfx = 1f, float dialogue = 1f, float ui = 1f)
        {
            var spec = new DuckSpec { Name = d, Voice = voice, Attack = attack, Hold = hold, Release = release };
            for (int i = 0; i < BusCount; i++) spec.Floor[i] = 1f;
            spec.Floor[(int)Bus.Music] = music; spec.Floor[(int)Bus.Ambience] = ambience; spec.Floor[(int)Bus.Sfx] = sfx;
            spec.Floor[(int)Bus.Dialogue] = dialogue; spec.Floor[(int)Bus.Ui] = ui;
            _ducks[d] = spec;
            return spec;
        }

        static Mix()
        {
            // Explore is the room as designed: everything at 1, nothing filtered.
            Snap(Snapshot.Explore, 0.6f, 0.6f, 1f, 1f, 1f, 1f, 1f);
            // Combat: the ambience steps back so the reads are heard; the theme's combat layer is the music row's.
            Snap(Snapshot.Combat, 0.4f, 2.0f, 1f, 0.7f, 1f, 1f, 1f);
            // A conversation: the room stays present but under the voice.
            Snap(Snapshot.Dialogue, 0.3f, 0.8f, 0.55f, 0.65f, 0.8f, 1f, 1f);
            // A boss: the theme owns the room; the ambience is nearly gone; the way out is slow, like the aftermath.
            Snap(Snapshot.Boss, 1.0f, 2.5f, 1f, 0.3f, 1f, 1f, 1f);
            // The Blank and the Greyfold's end: reversed motifs, under a low-pass, the room mostly gone.
            Snap(Snapshot.Blank, 2.0f, 2.0f, 0.7f, 0.4f, 0.9f, 1f, 1f, 2500f, 1800f);
            // Paused: the world through a wall; nothing in the room plays.
            Snap(Snapshot.Paused, 0.15f, 0.25f, 0.4f, 0.3f, 0f, 0f, 1f, 1200f, 1200f);

            // A line spoken: music and ambience make room for the voice while it lasts.
            DuckOf(Duck.Line, Bus.Dialogue, 0.08f, 0.0f, 0.6f, 0.6f, 0.7f);
            // A hit landing: the ambience blinks so the impact is clean (the combat doc's clarity rule).
            DuckOf(Duck.Impact, Bus.Sfx, 0.01f, 0.12f, 0.25f, 0.85f, 0.5f);
            // Wren hurt: the music dips for a breath.
            DuckOf(Duck.Hurt, Bus.Sfx, 0.02f, 0.3f, 0.8f, 0.5f, 0.6f);
            // A Bind: the room holds its breath while the ink takes.
            DuckOf(Duck.Bind, Bus.Sfx, 0.05f, 0.5f, 1.2f, 0.6f, 0.3f);
        }

        public static SnapshotSpec Of(Snapshot s) => _snapshots[s];
        public static DuckSpec Of(Duck d) => _ducks[d];

        /// <summary>The mixer the game is running, set by its driver; null in a bare test scene.</summary>
        public static Mixer Live { get; set; }

        /// <summary>A moment happened: the live mix ducks for it, and a blow counts as combat. Nothing when no mix runs.</summary>
        public static void Note(Duck d)
        {
            var m = Live;
            if (m == null) return;
            m.Trigger(d);
            if (d == Duck.Impact || d == Duck.Hurt) m.NoteCombat();
        }

        /// <summary>The ambience's low-pass by a place's fade stage (AUD-05's filtering): 0 is open, 3 is nearly gone.</summary>
        public static float StageCutoff(int stage) => Mathf.Clamp(stage, 0, 3) switch { 0 => Open, 1 => 9000f, 2 => 3500f, _ => 1200f };

        /// <summary>Seconds the combat snapshot outlasts the last blow.</summary>
        public const float CombatHold = 6f;

        /// <summary>Linear gain as decibels for a mixer parameter; silence is -80.</summary>
        public static float ToDb(float linear) => linear <= 0.0001f ? -80f : Mathf.Clamp(20f * Mathf.Log10(linear), -80f, 20f);
        public static float FromDb(float db) => db <= -80f ? 0f : Mathf.Pow(10f, db / 20f);

        /// <summary>The player's option that scales a bus: Ambience and Ui follow Sound with Sfx.</summary>
        public static Options.Volume VolumeOf(Bus b) => b switch
        {
            Bus.Master => Options.Volume.Master,
            Bus.Music => Options.Volume.Music,
            Bus.Dialogue => Options.Volume.Voices,
            _ => Options.Volume.Sound,
        };

        /// <summary>The region a room belongs to, from its plan or its name ("Greybox_Saltmarrow_A"); null if it says nothing.</summary>
        public static Region? RegionOf(string room)
        {
            if (string.IsNullOrEmpty(room)) return null;
            string id = room.StartsWith(WorldGraph.GreyboxPrefix, StringComparison.Ordinal) ? room.Substring(WorldGraph.GreyboxPrefix.Length) : room;
            var plan = RoomPlans.Find(id);
            string head = plan != null ? plan.Zone : id;
            int cut = head.IndexOfAny(new[] { '.', '_', '/' });
            if (cut > 0) head = head.Substring(0, cut);
            return Enum.TryParse<Region>(head, out var r) ? r : (Region?)null;
        }
    }

    /// <summary>
    /// The mix running: a snapshot moving to the one the state asks for, ducks with their envelopes, the stage filter,
    /// and the player's volumes, combined into one gain and one cutoff per bus. Pure: it is ticked with seconds and
    /// asked, so the tests can run it as fast as they like and the driver feeds it unscaled time.
    /// </summary>
    public sealed class Mixer
    {
        readonly float[] _gain = new float[Mix.BusCount];
        readonly float[] _cutoff = new float[Mix.BusCount];
        readonly float[] _from = new float[Mix.BusCount], _fromCut = new float[Mix.BusCount];
        readonly float[] _duckT = new float[Mix.Ducks.Length];   // seconds since the duck was triggered; negative is idle
        float _moveT, _moveSeconds;
        float _combatLeft;

        public Mix.Snapshot Current { get; private set; } = Mix.Snapshot.Explore;
        /// <summary>The snapshot the state last asked for; <see cref="Current"/> once the move is done.</summary>
        public Mix.Snapshot Target { get; private set; } = Mix.Snapshot.Explore;
        public bool Moving => _moveT < _moveSeconds;
        public int Stage { get; set; }
        public bool InCombat => _combatLeft > 0f;
        /// <summary>The player's volumes; the tests give it one that doesn't touch PlayerPrefs.</summary>
        public Func<Options.Volume, float> Volume { get; set; } = v => Options.Get(v);

        public Mixer()
        {
            var spec = Mix.Of(Mix.Snapshot.Explore);
            for (int i = 0; i < Mix.BusCount; i++) { _gain[i] = spec.Gain[i]; _cutoff[i] = spec.Cutoff[i]; }
            for (int i = 0; i < _duckT.Length; i++) _duckT[i] = -1f;
            _moveT = _moveSeconds = 0f;
        }

        /// <summary>Ask for a snapshot. Moving to it takes its In seconds, or the old one's Out when the new one is Explore.</summary>
        public void Go(Mix.Snapshot s)
        {
            if (s == Target) return;
            var outgoing = Mix.Of(Target);
            var incoming = Mix.Of(s);
            for (int i = 0; i < Mix.BusCount; i++) { _from[i] = _gain[i]; _fromCut[i] = _cutoff[i]; }
            Target = s;
            _moveSeconds = s == Mix.Snapshot.Explore ? outgoing.Out : incoming.In;
            _moveT = 0f;
            if (_moveSeconds <= 0f) Settle();
        }

        /// <summary>Jump there now; the driver uses it on a room load, the tests to start somewhere.</summary>
        public void Snap(Mix.Snapshot s)
        {
            Target = s;
            _moveSeconds = 0f; _moveT = 0f;
            Settle();
        }

        void Settle()
        {
            var spec = Mix.Of(Target);
            for (int i = 0; i < Mix.BusCount; i++) { _gain[i] = spec.Gain[i]; _cutoff[i] = spec.Cutoff[i]; }
            Current = Target;
        }

        public void Trigger(Mix.Duck d) => _duckT[(int)d] = 0f;

        /// <summary>A blow landed or was taken: combat holds for <see cref="Mix.CombatHold"/> more seconds.</summary>
        public void NoteCombat() => _combatLeft = Mix.CombatHold;

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            if (_moveT < _moveSeconds)
            {
                _moveT = Mathf.Min(_moveSeconds, _moveT + dt);
                float t = Mathf.SmoothStep(0f, 1f, _moveT / _moveSeconds);
                var spec = Mix.Of(Target);
                for (int i = 0; i < Mix.BusCount; i++)
                {
                    _gain[i] = Mathf.Lerp(_from[i], spec.Gain[i], t);
                    _cutoff[i] = Mathf.Lerp(_fromCut[i], spec.Cutoff[i], t);
                }
                if (_moveT >= _moveSeconds) Current = Target;
            }
            for (int i = 0; i < _duckT.Length; i++)
            {
                if (_duckT[i] < 0f) continue;
                var d = Mix.Of((Mix.Duck)i);
                _duckT[i] += dt;
                if (_duckT[i] > d.Attack + d.Hold + d.Release) _duckT[i] = -1f;
            }
            if (_combatLeft > 0f) _combatLeft = Mathf.Max(0f, _combatLeft - dt);
        }

        /// <summary>How far a duck is pressing now, 0 (idle) to 1 (holding).</summary>
        public float DuckLevel(Mix.Duck duck)
        {
            float t = _duckT[(int)duck];
            if (t < 0f) return 0f;
            var d = Mix.Of(duck);
            if (t < d.Attack) return d.Attack <= 0f ? 1f : t / d.Attack;
            t -= d.Attack;
            if (t < d.Hold) return 1f;
            t -= d.Hold;
            return d.Release <= 0f ? 0f : Mathf.Clamp01(1f - t / d.Release);
        }

        /// <summary>What the ducks leave of a bus, 1 when none is pressing.</summary>
        public float DuckGain(Mix.Bus bus)
        {
            float g = 1f;
            for (int i = 0; i < _duckT.Length; i++)
            {
                float level = DuckLevel((Mix.Duck)i);
                if (level <= 0f) continue;
                float floor = Mix.Of((Mix.Duck)i).Floor[(int)bus];
                g = Mathf.Min(g, Mathf.Lerp(1f, floor, level));
            }
            return g;
        }

        /// <summary>The snapshot's gain for a bus as it is now, mid-move included.</summary>
        public float SnapshotGain(Mix.Bus bus) => _gain[(int)bus];
        public float SnapshotCutoff(Mix.Bus bus) => _cutoff[(int)bus];

        /// <summary>Everything together: the player's master and bus volumes, the snapshot, the ducks. What a source plays at.</summary>
        public float Gain(Mix.Bus bus)
        {
            float master = Mathf.Clamp01(Volume(Options.Volume.Master));
            if (bus == Mix.Bus.Master) return master;
            return master * Mathf.Clamp01(Volume(Mix.VolumeOf(bus))) * _gain[(int)bus] * DuckGain(bus);
        }

        /// <summary>The low-pass a bus sits under: the snapshot's, and for the ambience the place's fade stage too.</summary>
        public float Cutoff(Mix.Bus bus)
        {
            float c = _cutoff[(int)bus];
            if (bus == Mix.Bus.Ambience) c = Mathf.Min(c, Mix.StageCutoff(Stage));
            return c;
        }
    }
}
