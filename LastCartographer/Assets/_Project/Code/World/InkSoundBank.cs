using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The ink sounds in the game (AUD-03): wakes with the game, turns <see cref="InkSounds"/>' cues into clips the
    /// first time they are asked for, and plays them at the Sfx bus's gain (the mix's snapshots and the player's
    /// Sounds volume apply; no duck touches the Sfx bus, so the tells are never lowered). The world's own sounds hook
    /// here: an enemy's death is the kill layer, and every telegraph (<see cref="Enemy.Telegraphed"/>) is the tell
    /// for its kind, on the telegraph's first frame. Wren's are <see cref="WrenSounds"/>'.
    /// </summary>
    public sealed class InkSoundBank : MonoBehaviour
    {
        public static InkSoundBank Instance { get; private set; }

        AudioSource _shots, _loop;
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

        public int Played { get; private set; }
        public string Last { get; private set; }
        public string Looping => _loop != null && _loop.isPlaying && _loop.clip != null ? _loop.clip.name : null;
        public int Cached => _clips.Count;
        public float Volume => _shots != null ? _shots.volume : 0f;
        /// <summary>The last tell played, and for whom.</summary>
        public AudioDirection.Tell? LastTell { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("~Sounds");
            DontDestroyOnLoad(go);
            go.AddComponent<InkSoundBank>();
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            _shots = Source("Shots", false);
            _loop = Source("Loop", true);
        }

        AudioSource Source(string name, bool loop)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.loop = loop;
            return src;
        }

        void OnEnable()
        {
            Enemy.AnyDied += OnEnemyDied;
            Enemy.Telegraphed += OnTelegraphed;
        }

        void OnDisable()
        {
            Enemy.AnyDied -= OnEnemyDied;
            Enemy.Telegraphed -= OnTelegraphed;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            float gain = Mix.Live != null ? Mix.Live.Gain(Mix.Bus.Sfx) : Options.Get(Options.Volume.Master) * Options.Get(Options.Volume.Sound);
            _shots.volume = gain;
            _loop.volume = gain;
        }

        public AudioClip Clip(string id)
        {
            if (_clips.TryGetValue(id, out var clip) && clip != null) return clip;
            var samples = InkSounds.Render(id);
            if (samples == null) return null;
            clip = AudioClip.Create(id, samples.Length, 1, InkSounds.SampleRate, false);
            clip.SetData(samples, 0);
            _clips[id] = clip;
            return clip;
        }

        /// <summary>Play a cue once; a cue that does not exist is a no-op. Returns the clip played.</summary>
        public static AudioClip Play(string id, float volume = 1f)
        {
            var bank = Instance;
            if (bank == null) return null;
            var cue = InkSounds.Of(id);
            var clip = bank.Clip(id);
            if (clip == null) return null;
            bank._shots.PlayOneShot(clip, volume * (cue != null ? cue.Gain : 1f));
            bank.Played++;
            bank.Last = id;
            return clip;
        }

        /// <summary>Loop a cue while something is held (the survey); null stops it.</summary>
        public static void Loop(string id)
        {
            var bank = Instance;
            if (bank == null) return;
            if (id == null) { if (bank._loop.isPlaying) bank._loop.Stop(); bank._loop.clip = null; return; }
            var clip = bank.Clip(id);
            if (clip == null) return;
            if (bank._loop.clip == clip && bank._loop.isPlaying) return;
            bank._loop.clip = clip;
            bank._loop.Play();
            bank.Played++;
            bank.Last = id;
        }

        /// <summary>The tell for an attack kind, by the direction's name for it.</summary>
        public static AudioClip Tell(AttackKind kind)
        {
            var t = AudioDirection.TellOf(kind.ToString());
            if (Instance != null) Instance.LastTell = t;
            return Play(InkSounds.TellCue(t));
        }

        void OnEnemyDied(Enemy e) => Play("kill");
        void OnTelegraphed(Enemy e, AttackKind kind) => Tell(kind);

        /// <summary>Stop everything (a new game, the tests).</summary>
        public void Hush()
        {
            if (_shots != null) _shots.Stop();
            if (_loop != null) { _loop.Stop(); _loop.clip = null; }
            LastTell = null;
        }
    }
}
