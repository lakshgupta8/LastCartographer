using UnityEngine;

namespace OWSBG.World
{
    /// <summary>Shared runtime materials for code-spawned greybox objects (darts, weights, anchors).</summary>
    public static class InkMaterials
    {
        static Material _dark;

        public static Material Dark
        {
            get
            {
                if (_dark != null) return _dark;
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                _dark = new Material(shader) { name = "M_Ink_Black (runtime)" };
                var ink = new Color(0.06f, 0.06f, 0.08f);
                if (_dark.HasProperty("_BaseColor")) _dark.SetColor("_BaseColor", ink);
                if (_dark.HasProperty("_Color")) _dark.SetColor("_Color", ink);
                return _dark;
            }
        }
    }
}
