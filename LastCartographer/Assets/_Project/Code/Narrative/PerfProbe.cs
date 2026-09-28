using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OWSBG.Core;
using OWSBG.World;
using Unity.Profiling;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The performance probe (PRG-24): <c>LastCartographer.exe -batchmode -perf -perfOut perf.json -logFile perf.log</c>.
    /// With the 60 fps cap lifted, so a frame costs what it costs, it walks a route of rooms from the first one, always
    /// taking the first way on to a room it hasn't seen. In each room it waits for the neighbours' bundles, lets things
    /// settle, then samples frames (each room once; from a dead end it walks back and tries another way): their times, draw batches, SetPass calls, triangles, and the managed memory each
    /// allocates. It times every transition, and writes the lot as JSON against <see cref="PerfBudget"/>, one
    /// "[OWSBG] perf:" log line per room and transition. It quits with 0 when everything is within budget, 1 when
    /// something is over, and 2 when a room never came in.
    /// </summary>
    public sealed class PerfProbe : MonoBehaviour
    {
        public const string Arg = "-perf";
        /// <summary>With it, the first room's quiet-frame allocations are put down to the scripts that make them.</summary>
        public const string AttributeArg = "-perfAttribute";

        [Serializable] public sealed class RoomSample
        {
            public string room;
            public int frames;
            public float avgMs, p50Ms, p95Ms, p99Ms, maxMs, fps;
            public float batches, setPass, triangles;
            public float gcBytesPerFrame = -1f;
            public int gcCollections;
        }

        [Serializable] public sealed class TransitionSample
        {
            public string from, to;
            public float ms;
        }

        [Serializable] public sealed class Report
        {
            public string version, device, cpu, graphics, resolution;
            public int refreshHz;
            public bool rendering;
            public List<RoomSample> rooms = new List<RoomSample>();
            public List<TransitionSample> transitions = new List<TransitionSample>();
            public List<string> overBudget = new List<string>();
            public bool passed;
        }

        public int Rooms { get; set; } = 6;
        public int SampleFrames { get; set; } = 300;
        public int SettleFrames { get; set; } = 60;
        public float RoomTimeout { get; set; } = 60f;
        public bool QuitWhenDone { get; set; }
        public bool Attribute { get; set; }
        public string OutPath { get; set; }
        public Report Result { get; private set; }
        public bool Done { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void FromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains(Arg)) return;
            var go = new GameObject("~PerfProbe");
            DontDestroyOnLoad(go);
            var probe = go.AddComponent<PerfProbe>();
            probe.QuitWhenDone = true;
            probe.Attribute = args.Contains(AttributeArg);
            int i = Array.IndexOf(args, "-perfOut");
            probe.OutPath = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "perf.json");
        }

        static IEnumerator Wait(Func<bool> done, float seconds, Action<bool> result)
        {
            float start = Time.realtimeSinceStartup;
            while (!done() && Time.realtimeSinceStartup - start < seconds) yield return null;
            result(done());
        }

        IEnumerator Start()
        {
            FrameRate.Uncap();
            var report = new Report
            {
                version = BuildInfo.Label,
                device = SystemInfo.deviceModel,
                cpu = SystemInfo.processorType + " × " + SystemInfo.processorCount,
                graphics = SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ")",
                resolution = Screen.width + "x" + Screen.height,
                refreshHz = (int)Math.Round(Screen.currentResolution.refreshRateRatio.value),
                rendering = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null,
            };
            Result = report;

            bool ok = false;
            yield return Wait(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning,
                RoomTimeout, r => ok = r);
            if (!ok) { yield return Finish(report, 2); yield break; }
            var rooms = RoomManager.Instance;
            var seen = new HashSet<string>();
            var back = new Stack<string>();   // the way back, for when a room leads nowhere new
            int hops = 0;

            while (true)
            {
                string here = rooms.CurrentRoom;
                bool sampled = seen.Contains(here);
                seen.Add(here);
                yield return Wait(() => !rooms.IsPreloading, 10f, _ => { });
                if (sampled) goto Onward;
                if (Attribute && report.rooms.Count == 0) yield return AttributeGc(here);
                for (int i = 0; i < SettleFrames; i++) yield return null;
                var sample = new RoomSample { room = here };
                yield return Sample(sample);
                report.rooms.Add(sample);
                Debug.Log("[OWSBG] perf: room " + here + ": " + sample.fps.ToString("0") + " fps, avg " + sample.avgMs.ToString("0.00") + " ms, p95 " + sample.p95Ms.ToString("0.00") +
                          ", p99 " + sample.p99Ms.ToString("0.00") + ", max " + sample.maxMs.ToString("0.00") + "; " + sample.batches.ToString("0") + " batches, " +
                          sample.setPass.ToString("0") + " SetPass, " + sample.triangles.ToString("0") + " tris; " +
                          (sample.gcBytesPerFrame < 0 ? "GC n/a" : sample.gcBytesPerFrame.ToString("0") + " B/frame GC") + ", " + sample.gcCollections + " collections");

                Onward:
                if (report.rooms.Count >= Rooms || ++hops > Rooms * 3) break;
                string next = NextRoom(seen);
                if (next != null) back.Push(here);
                else if (back.Count > 0) next = back.Pop();   // a dead end: walk back and try another way
                else break;
                rooms.Transition(next, SpawnFor(next, here));
                yield return Wait(() => rooms.CurrentRoom == next && !rooms.IsTransitioning, RoomTimeout, r => ok = r);
                if (!ok) { report.overBudget.Add(next + " never came in"); yield return Finish(report, 2); yield break; }
                var t = new TransitionSample { from = here, to = next, ms = rooms.LastTransitionMs };
                report.transitions.Add(t);
                Debug.Log("[OWSBG] perf: transition " + here + " → " + next + ": " + t.ms.ToString("0") + " ms");
            }

            foreach (var r in report.rooms)
            {
                if (report.rendering && r.p95Ms > PerfBudget.FrameMs) report.overBudget.Add(r.room + ": p95 " + r.p95Ms.ToString("0.0") + " ms");
                if (r.gcBytesPerFrame > PerfBudget.GcBytesPerFrame) report.overBudget.Add(r.room + ": " + r.gcBytesPerFrame.ToString("0") + " B/frame GC");
                if (r.gcCollections > 0) report.overBudget.Add(r.room + ": " + r.gcCollections + " collections while standing still");
                if (r.batches > PerfBudget.Batches) report.overBudget.Add(r.room + ": " + r.batches.ToString("0") + " batches");
            }
            foreach (var t in report.transitions)
                if (!PerfBudget.TransitionWithin(t.ms)) report.overBudget.Add(t.from + " → " + t.to + ": " + t.ms.ToString("0") + " ms");
            yield return Finish(report, report.overBudget.Count == 0 ? 0 : 1);
        }

        /// <summary>The first way out of this room to a room not yet seen.</summary>
        static string NextRoom(HashSet<string> seen)
        {
            var room = Room.Current;
            if (room == null) return null;
            foreach (var t in room.GetComponentsInChildren<RoomTransition>(true))
                if (!string.IsNullOrEmpty(t.TargetScene) && !seen.Contains(t.TargetScene)) return t.TargetScene;
            return null;
        }

        static string SpawnFor(string to, string from)
        {
            var room = Room.Current;
            if (room == null) return null;
            foreach (var t in room.GetComponentsInChildren<RoomTransition>(true))
                if (t.TargetScene == to) return t.TargetSpawn;
            return null;
        }

        IEnumerator Sample(RoomSample s)
        {
            var times = new List<float>(SampleFrames);
            var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            var setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            var tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            double b = 0, sp = 0, tr = 0, g = 0;
            yield return null;   // the recorders see a whole frame before the first read
            // Release players have no "GC Allocated In Frame"; the main thread's own allocation count stands in.
            long allocatedBefore = AllocatedBytes();
            int collectionsBefore = GC.CollectionCount(0);
            for (int i = 0; i < SampleFrames; i++)
            {
                yield return null;
                times.Add(Time.unscaledDeltaTime * 1000f);
                b += batches.Valid ? batches.LastValue : 0;
                sp += setPass.Valid ? setPass.LastValue : 0;
                tr += tris.Valid ? tris.LastValue : 0;
                g += gc.Valid ? gc.LastValue : 0;
            }
            var f = PerfBudget.Summarise(times);
            s.frames = f.Count; s.avgMs = f.AvgMs; s.p50Ms = f.P50Ms; s.p95Ms = f.P95Ms; s.p99Ms = f.P99Ms; s.maxMs = f.MaxMs; s.fps = f.Fps;
            int n = Mathf.Max(1, times.Count);
            s.batches = (float)(b / n); s.setPass = (float)(sp / n); s.triangles = (float)(tr / n);
            long allocatedAfter = AllocatedBytes();
            // Mono's thread counter reads 0 when it isn't kept: that is "unknown", not "nothing".
            s.gcBytesPerFrame = gc.Valid ? (float)(g / n) : allocatedBefore > 0 && allocatedAfter > allocatedBefore ? (allocatedAfter - allocatedBefore) / (float)n : -1f;
            s.gcCollections = GC.CollectionCount(0) - collectionsBefore;
            batches.Dispose(); setPass.Dispose(); tris.Dispose(); gc.Dispose();
        }

        /// <summary>
        /// Who allocates on a quiet frame (development builds, where the per-frame GC counter exists): each of our script
        /// types is switched off in turn, and what the frame allocates without it is taken from what it allocates with it.
        /// </summary>
        IEnumerator AttributeGc(string room)
        {
            var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            if (!gc.Valid) { Debug.Log("[OWSBG] perf: attribution needs a development build"); gc.Dispose(); yield break; }
            IEnumerator Measure(int frames, Action<double> result)
            {
                yield return null;
                double sum = 0;
                for (int i = 0; i < frames; i++) { yield return null; sum += gc.LastValue; }
                result(sum / frames);
            }
            double baseline = 0;
            yield return Measure(180, r => baseline = r);
            var groups = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(m => m != this && m.enabled && m.GetType().Namespace != null && m.GetType().Namespace.StartsWith("OWSBG"))
                .GroupBy(m => m.GetType()).ToList();
            var found = new List<(string name, double bytes)>();
            foreach (var g in groups)
            {
                foreach (var m in g) m.enabled = false;
                double without = 0;
                yield return Measure(90, r => without = r);
                foreach (var m in g) if (m != null) m.enabled = true;
                yield return null;
                if (baseline - without >= 8) found.Add((g.Key.Name, baseline - without));
            }
            gc.Dispose();
            Debug.Log("[OWSBG] perf: " + room + " allocates " + baseline.ToString("0") + " B/frame; by script: " +
                      (found.Count == 0 ? "none over 8 B" : string.Join(", ", found.OrderByDescending(f => f.bytes).Select(f => f.name + " " + f.bytes.ToString("0")))));
        }

        static long AllocatedBytes()
        {
            try { return GC.GetAllocatedBytesForCurrentThread(); }
            catch { return -1; }
        }

        IEnumerator Finish(Report report, int code)
        {
            report.passed = code == 0;
            FrameRate.Recap();
            if (!string.IsNullOrEmpty(OutPath))
            {
                try
                {
                    var dir = Path.GetDirectoryName(Path.GetFullPath(OutPath));
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(OutPath, JsonUtility.ToJson(report, true));
                }
                catch (Exception e) { Debug.LogWarning("[OWSBG] perf: couldn't write " + OutPath + ": " + e.Message); }
            }
            Debug.Log("[OWSBG] perf: " + (code == 0 ? "within budget" : code == 1 ? "over budget: " + string.Join("; ", report.overBudget) : "a room never came in") +
                      " (" + report.rooms.Count + " rooms, " + report.transitions.Count + " transitions, " + report.resolution + ", " + report.graphics + ")");
            Done = true;
            yield return null;
            if (QuitWhenDone) Application.Quit(code);
        }
    }
}
