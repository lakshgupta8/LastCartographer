using UnityEngine;

namespace OWSBG.World
{
    /// <summary>Shared runtime materials for code-spawned greybox objects (darts, weights, anchors).</summary>
    public static class InkMaterials
    {
        static Material _dark;
        static readonly System.Collections.Generic.Dictionary<string, Material> _lit = new System.Collections.Generic.Dictionary<string, Material>();

        /// <summary>A plain lit greybox colour, cached by name (boss props made at runtime).</summary>
        public static Material Lit(string name, Color color)
        {
            if (_lit.TryGetValue(name, out var m) && m != null) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader) { name = "M_" + name + " (runtime)" };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
            _lit[name] = m;
            return m;
        }

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
