using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// An enemy's voice (AUD-10, docs/design/enemy-sounds.md). Every enemy gets one as it wakes. It plays the family's
    /// cues from what the enemy already says about itself: a move's cue as its clip begins (the same names the animator
    /// asks for, <see cref="Enemy.Clip"/>), a loop while a clip runs (a wasp's hum, a tussock's rumble), the material's
    /// hurt on a landed hit, its death with the kill layer, and its block when a strike is turned away. Every sound
    /// stands where the enemy is: panned toward it and quieter past the screen's edge. Nothing happens without the bank.
    /// </summary>
    public sealed class EnemyVoice : MonoBehaviour
    {
        Enemy _enemy;
        EnemySounds.Voice _voice;
        AudioSource _loop;
        string _clip, _loopCue;
        float _loopGain;

        /// <summary>The family's voice: the table's, or the default's (wet ink) for a family it does not know.</summary>
        public EnemySounds.Voice Voice => _voice ??= EnemySounds.Of(_enemy != null ? _enemy.Family : null);
        /// <summary>The cue looping right now, if one is.</summary>
        public string Looping => _loop != null && _loop.isPlaying ? _loopCue : null;
        /// <summary>The last move's cue this voice played.</summary>
        public string LastMove { get; private set; }
        public float LoopVolume => _loop != null ? _loop.volume : 0f;

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
