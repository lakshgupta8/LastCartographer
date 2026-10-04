using System.Reflection;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// An enemy's voice (AUD-10, docs/design/enemy-sounds.md). Every enemy gets one as it wakes. It plays the family's
    /// cues from what the enemy already says about itself: a move's cue as its clip begins (the same names the animator
    /// asks for, <see cref="Enemy.Clip"/>), a loop while a clip runs (a wasp's hum, a tussock's rumble), the material's
    /// hurt on a landed hit, its death with the kill layer, and its block when a strike is turned away. A boss's events
    /// (AUD-15) are counts it already keeps, heard each time one goes up: a bell tolled, a rope cut, a limb redrawn, in
    /// the phase's own take where there is one. Every sound
    /// stands where the enemy is: panned toward it and quieter past the screen's edge. Nothing happens without the bank.
    /// </summary>
    public sealed class EnemyVoice : MonoBehaviour
    {
        Enemy _enemy;
        EnemySounds.Voice _voice;
        AudioSource _loop;
        string _clip, _loopCue;
        float _loopGain;
        (PropertyInfo count, string cue, int last)[] _events;

        /// <summary>The family's voice: the table's, or the default's (wet ink) for a family it does not know.</summary>
        public EnemySounds.Voice Voice => _voice ??= EnemySounds.Of(_enemy != null ? _enemy.Family : null);
        /// <summary>The cue looping right now, if one is.</summary>
        public string Looping => _loop != null && _loop.isPlaying ? _loopCue : null;
        /// <summary>The last move's cue this voice played.</summary>
        public string LastMove { get; private set; }
        public float LoopVolume => _loop != null ? _loop.volume : 0f;
        /// <summary>The last event's cue this voice played.</summary>
        public string LastEvent { get; private set; }

        void Awake() { _enemy = GetComponent<Enemy>(); }

        void OnEnable()
        {
            if (_enemy == null) return;
            _enemy.WasHit += OnHit;
            _enemy.Died += OnDied;
            _enemy.HitBlocked += OnBlocked;
        }

        void OnDisable()
        {
            if (_enemy != null) { _enemy.WasHit -= OnHit; _enemy.Died -= OnDied; _enemy.HitBlocked -= OnBlocked; }
            StopLoop();
            _clip = null;
            _events = null;   // counted afresh when it wakes again
        }

        void LateUpdate()
        {
            if (_enemy == null) return;
            var bank = InkSoundBank.Instance;
            if (bank == null) return;
            var clip = _enemy.Clip;
            if (!string.Equals(clip, _clip))
            {
                bool first = _clip == null;   // the clip it was stood in the room with is not a move it made
                _clip = clip;
                if (!first && clip != null && !_enemy.IsDying && Voice.Moves.TryGetValue(clip, out var cue))
                {
                    InkSoundBank.Play(cue, 1f, transform.position);
                    LastMove = cue;
                }
            }
            Count();
            string loop = clip != null && !_enemy.IsDead && !_enemy.IsDying && Voice.Loops.TryGetValue(clip, out var l) ? l : null;
            if (!string.Equals(loop, _loopCue))
            {
                StopLoop();
                if (loop != null) StartLoop(bank, loop);
            }
            if (_loop != null && _loop.isPlaying)
            {
                var place = bank.Place(transform.position);
                _loop.panStereo = place.pan;
                _loop.volume = bank.Volume * _loopGain * place.gain;
            }
        }

        /// <summary>The boss's counts, read each frame: a count gone up is its event's cue. The first read only takes them as they stand.</summary>
        void Count()
        {
            var events = Voice.Events;
            if (events.Count == 0) return;
            if (_events == null)
            {
                _events = new (PropertyInfo, string, int)[events.Count];
                int i = 0;
                foreach (var kv in events)
                {
                    var p = _enemy.GetType().GetProperty(kv.Key, BindingFlags.Public | BindingFlags.Instance);
                    if (p != null && p.PropertyType != typeof(int)) p = null;
                    _events[i++] = (p, kv.Value, p != null ? (int)p.GetValue(_enemy) : 0);
                }
                return;
            }
            for (int i = 0; i < _events.Length; i++)
            {
                var e = _events[i];
                if (e.count == null) continue;
                int n = (int)e.count.GetValue(_enemy);
                if (n > e.last)
                {
                    var cue = EnemySounds.PhaseCue(e.cue, _enemy is Boss b ? b.Phase : 0);
                    InkSoundBank.Play(cue, 1f, transform.position);
                    LastEvent = cue;
                }
                _events[i].last = n;
            }
        }

        void StartLoop(InkSoundBank bank, string cue)
        {
            var clip = bank.Clip(cue);
            if (clip == null) return;
            if (_loop == null)
            {
                _loop = gameObject.AddComponent<AudioSource>();
                _loop.playOnAwake = false;
                _loop.spatialBlend = 0f;   // the room is a plane: panned by hand, from where it stands
                _loop.loop = true;
            }
            var c = InkSounds.Of(cue);
            _loopGain = c != null ? c.Gain : 1f;
            _loopCue = cue;
            _loop.clip = clip;
            _loop.volume = 0f;
            _loop.Play();
        }

        void StopLoop()
        {
            _loopCue = null;
            if (_loop != null && _loop.isPlaying) _loop.Stop();
        }

        void OnHit(Enemy e, HitInfo hit)
        {
            if (e.Health <= 0) return;   // the death speaks for it
            InkSoundBank.Play(Voice.Hurt, 1f, transform.position);
        }

        void OnDied(Enemy e)
        {
            StopLoop();
            InkSoundBank.Play(Voice.Death, 1f, transform.position);
        }

        void OnBlocked(Enemy e)
        {
            var v = Voice;
            if (v.Blocked == null) return;
            if (v.BlockedClips != null && (_enemy.Clip == null || System.Array.IndexOf(v.BlockedClips, _enemy.Clip) < 0)) return;
            InkSoundBank.Play(v.Blocked, 1f, transform.position);
        }
    }
}
