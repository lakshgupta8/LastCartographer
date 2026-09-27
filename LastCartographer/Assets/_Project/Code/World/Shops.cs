using System;

namespace OWSBG.World
{
    /// <summary>A conversation asking for a hub's shop to open (Yarn's &lt;&lt;shop hub&gt;&gt;); the UI's shop page answers.</summary>
    public static class Shops
    {
        public static event Action<string> Requested;

        public static void Request(string hub) => Requested?.Invoke(hub);
    }
}
