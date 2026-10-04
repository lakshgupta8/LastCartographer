using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A boss part's voice (AUD-16, docs/design/enemy-sounds.md §2b). <see cref="BossPart.Make"/> puts one on every part;
    /// it finds the part's sounds by the name it was made under and plays them where the part is: as it appears, as a
    /// strike lands on it (<see cref="BossPart.TakeHit"/> tells it), as it goes without one, and a loop while it is
    /// there. A part its owner speaks for (a dove, a rope) has none. A part cleared as its fight ends goes quietly.
    /// </summary>
    public sealed class PartVoice : MonoBehaviour
    {
        EnemySounds.Part _voice;
        AudioSource _loop;
        float _loopGain;
        bool _started, _struck;

        /// <summary>The part's sounds, or null for a part with none of its own.</summary>
        public EnemySounds.Part Voice => _voice;
        /// <summary>The cue looping right now, if one is.</summary>
        public string Looping => _loop != null && _loop.isPlaying && _voice != null ? _voice.Loop : null;

        void Start()
        {
            _voice = EnemySounds.PartOf(name);
            _started = true;
            if (_voice == null) { enabled = false; return; }
            if (_voice.Appear != null) InkSoundBank.Play(_voice.Appear, 1f, transform.position);
            StartLoop();
        }

        /// <summary>A strike landed on it: its struck cue, and it will not also be heard going.</summary>
        public void Struck()
        {
            _struck = true;
            if (_voice?.Struck != null) InkSoundBank.Play(_voice.Struck, 1f, transform.position);
        }

        void StartLoop()
        {
            var bank = InkSoundBank.Instance;
            if (_voice.Loop == null || bank == null) return;
            var clip = bank.Clip(_voice.Loop);
            if (clip == null) return;
            _loop = gameObject.AddComponent<AudioSource>();
            _loop.playOnAwake = false;
            _loop.spatialBlend = 0f;   // panned by hand, from where it is
            _loop.loop = true;
            var c = InkSounds.Of(_voice.Loop);
            _loopGain = c != null ? c.Gain : 1f;
            _loop.clip = clip;
            _loop.volume = 0f;
            _loop.Play();
        }

        void LateUpdate()
        {
            var bank = InkSoundBank.Instance;
            if (_loop == null || !_loop.isPlaying || bank == null) return;
            var place = bank.Place(transform.position);
            _loop.panStereo = place.pan;
            _loop.volume = bank.Volume * _loopGain * place.gain;
        }

        void OnDestroy()
        {
            if (!_started || _struck || _voice?.Gone == null) return;
            if (!gameObject.scene.isLoaded || Boss.FightsActive == 0) return;   // the room unloading, or the fight cleared away
            InkSoundBank.Play(_voice.Gone, 1f, transform.position);
        }
    }
}
