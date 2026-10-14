using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A thing thrown at Wren, heard (AUD-19, docs/design/enemy-sounds.md §2c). <see cref="EnemyProjectile.Spawn"/> puts
    /// one on everything thrown; it finds the shot's sounds by the name it was thrown under, plays its leaving where it
    /// leaves (unless the thrower's own move is that sound) and loops its flight from where it is, panned and quieter
    /// past the screen. How it ends is <see cref="WorldSoundHooks"/>' to play, from where it was spent.
    /// </summary>
    public sealed class ProjectileVoice : MonoBehaviour
    {
        EnemySounds.Shot _shot;
        AudioSource _loop;
        float _loopGain;

        /// <summary>The shot's sounds.</summary>
        public EnemySounds.Shot Shot => _shot;
        /// <summary>The cue looping right now, if one is.</summary>
        public string Looping => _loop != null && _loop.isPlaying && _shot != null ? _shot.Fly : null;

        void Start()
        {
            _shot = EnemySounds.ShotOf(name);
            if (_shot.Launch != null) InkSoundBank.Play(_shot.Launch, 1f, transform.position);
            var bank = InkSoundBank.Instance;
            if (_shot.Fly == null || bank == null) return;
            var clip = bank.Clip(_shot.Fly);
            if (clip == null) return;
            _loop = gameObject.AddComponent<AudioSource>();
            _loop.playOnAwake = false;
            _loop.spatialBlend = 0f;   // panned by hand, from where it is
            _loop.loop = true;
            var c = InkSounds.Of(_shot.Fly);
            _loopGain = c != null ? c.Gain : 1f;
            _loop.clip = clip;
            _loop.volume = 0f;
            _loop.Play();
            LateUpdate();
        }

        void LateUpdate()
        {
            var bank = InkSoundBank.Instance;
            if (_loop == null || !_loop.isPlaying || bank == null) return;
            var place = bank.Place(transform.position);
            _loop.panStereo = place.pan;
            _loop.volume = bank.Volume * _loopGain * place.gain;
        }
    }
}
