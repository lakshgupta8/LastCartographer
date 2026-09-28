using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The physics layers' masks, looked up once (PRG-24): <c>LayerMask.GetMask</c> builds an array every call, and the
    /// hit checks that ran it every fixed frame made garbage for nothing.
    /// </summary>
    public static class Layers
    {
        static int _player = -1, _ground = -1;

        public static int Player => _player >= 0 ? _player : (_player = LayerMask.GetMask("Player"));
        public static int Ground => _ground >= 0 ? _ground : (_ground = LayerMask.GetMask("Ground"));
    }
}
