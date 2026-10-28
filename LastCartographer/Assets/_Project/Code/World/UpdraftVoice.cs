using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Rising air, heard (AUD-25, docs/design/world-sounds.md): an <see cref="Updraft"/> puts one on itself. It loops the
    /// air's rush from where the column stands, panned and quieter past the screen: a hiss under the room while it is only
    /// there, and the full rush while it carries her (its lifts counting up), easing between the two.
    /// </summary>
    public sealed class UpdraftVoice : MonoBehaviour
    {
        /// <summary>How much of the rush is heard while the column carries no one.</summary>
        public const float Idle = 0.35f;

        Updraft _draft;
        AudioSource _loop;
        float _gain, _level = Idle;
        int _lifts;

        /// <summary>The cue looping, while it does.</summary>
        public string Looping => _loop != null && _loop.isPlaying ? WorldSounds.UpdraftCue : null;
        /// <summary>How much of the rush is heard now, 0..1 of the cue's level.</summary>
        public float Level => _level;

        void Start()
        {
            _draft = GetComponent<Updraft>();
            var bank = InkSoundBank.Instance;
            var clip = bank != null ? bank.Clip(WorldSounds.UpdraftCue) : null;
            if (clip == null) { enabled = false; return; }
            _loop = gameObject.AddComponent<AudioSource>();
            _loop.playOnAwake = false;
            _loop.spatialBlend = 0f;   // panned by hand, from where it stands
            _loop.loop = true;
            _loop.clip = clip;
            var c = InkSounds.Of(WorldSounds.UpdraftCue);
            _gain = c != null ? c.Gain : 1f;
            _loop.volume = 0f;
            _loop.Play();
        }

        void LateUpdate()
        {
            var bank = InkSoundBank.Instance;
            if (_loop == null || bank == null) return;
            int lifts = _draft != null ? _draft.Lifts : 0;
            bool carrying = lifts != _lifts;
            _lifts = lifts;
            _level = Mathf.MoveTowards(_level, carrying ? 1f : Idle, Time.deltaTime * (carrying ? 4f : 1.5f));
            var place = bank.Place(transform.position);
            _loop.panStereo = place.pan;
            _loop.volume = bank.Volume * _gain * _level * place.gain;
        }
    }
}
