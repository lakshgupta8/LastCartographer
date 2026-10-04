namespace OWSBG.World
{
    /// <summary>
    /// A fight that keeps a beat (the rhythm bosses, audio-direction 4; AUD-13): the music driver holds the boss's theme
    /// to this clock, so its pulse lands where the fight's beat does. The beat does not stop for hitstop: what a hit
    /// froze is added back (<see cref="OWSBG.Core.Hitstop.FrozenSeconds"/>), so it keeps real time with the music.
    /// </summary>
    public interface IKeepsBeat
    {
        /// <summary>Seconds a beat lasts now.</summary>
        float BeatSeconds { get; }
        /// <summary>Beats since the fight began, with the fraction of the one under way; the music's place in its loop.</summary>
        double BeatsInto { get; }
    }
}
