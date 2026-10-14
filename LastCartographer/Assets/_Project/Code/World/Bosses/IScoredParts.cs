namespace OWSBG.World
{
    /// <summary>
    /// A fight whose pieces are in its music (AUD-20, docs/design/music.md §3e): a boss theme's stem that names a part
    /// (<see cref="OWSBG.Core.Score.Stem.Part"/>) is heard while the fight says that part is there, and leaves when it is
    /// gone, at once rather than on the bar line. The Half-Cathedral Bells' hums are their ropes: a rope cut, a bell gone.
    /// </summary>
    public interface IScoredParts
    {
        /// <summary>Whether the part with this number (the boss's own count) is gone.</summary>
        bool PartGone(int part);
    }
}
