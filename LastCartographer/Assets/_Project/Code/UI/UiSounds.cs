using OWSBG.Core;
using OWSBG.World;

namespace OWSBG.UI
{
    /// <summary>
    /// The pages' sounds (AUD-11, docs/design/world-sounds.md): paper lifted and set down, the nib's tick for the cursor,
    /// a short stroke for a choice, a dry dot for one refused, a quick scratch for a line of dialogue, two taps for a
    /// note. They ride the Ui bus through the bank, so they are heard while the game is paused and the room is not.
    /// Every view calls these at the moment it does the thing; without the bank nothing happens.
    /// </summary>
    public static class UiSounds
    {
        public static string Last { get; private set; }

        static void Play(string id, float pitch = 1f) { InkSoundBank.Play(id, 1f, null, pitch); Last = id; }

        public static void Open() => Play("ui_open");
        public static void Close() => Play("ui_close");
        public static void Back() => Play("ui_back");
        public static void Move() => Play("ui_move");
        /// <summary>A setting changed; a slider's value (0..1) pitches the tick, anything else ticks plain.</summary>
        public static void Tick(float value01 = -1f) => Play("ui_tick", WorldSounds.TickPitch(value01));
        public static void Select() => Play("ui_select");
        public static void Denied() => Play("ui_denied");
        public static void Line() => Play("ui_line");
        public static void Toast() => Play("ui_toast");
        /// <summary>Chosen or refused, as the view says.</summary>
        public static bool Did(bool ok) { if (ok) Select(); else Denied(); return ok; }
    }
}
