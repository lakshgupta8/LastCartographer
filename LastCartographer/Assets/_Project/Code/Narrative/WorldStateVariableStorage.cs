#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using OWSBG.Core;
using UnityEngine;
using Yarn;
using Yarn.Unity;

namespace OWSBG.Narrative
{
    /// <summary>
    /// Yarn variable storage backed by the game's WorldState, so "$" variables set in dialogue are
    /// saved with everything else. Booleans live in Flags (0/1), numbers in Numbers, strings in Strings.
    /// Mirrors InMemoryVariableStorage's rules: initial values come from the Program, smart
    /// variables from the evaluator.
    /// </summary>
    public sealed class WorldStateVariableStorage : VariableStorageBehaviour
    {
        static WorldState W => GameState.World;

        public override bool TryGetValue<T>(string variableName, [NotNullWhen(true)] out T result)
        {
            if (!variableName.StartsWith("$"))
            {
                Debug.LogError("[OWSBG] Yarn variable names must start with '$': " + variableName);
                result = default!;
                return false;
            }

            var kind = GetVariableKind(variableName);
            if (kind == VariableKind.Smart)
            {
                if (SmartVariableEvaluator != null && SmartVariableEvaluator.TryGetSmartVariable(variableName, out result))
                    return true;
                result = default!;
                return false;
            }

            object? value = null;
            if (W.Numbers.TryGetValue(variableName, out var f)) value = f;
            else if (W.Strings.TryGetValue(variableName, out var s)) value = s;
            else if (W.Flags.TryGetValue(variableName, out var b)) value = b != 0;

            if (value == null)
            {
                if (Program != null) return Program.TryGetInitialValue<T>(variableName, out result);
                result = default!;
                return false;
            }

            if (value is T typed) { result = typed; return true; }
            try
            {
                result = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception)
            {
                result = default!;
                return false;
            }
        }

        public override void SetValue(string variableName, string stringValue)
        {
            W.Numbers.Remove(variableName);
            W.Flags.Remove(variableName);
            W.Strings[variableName] = stringValue;
            NotifyVariableChanged(variableName, stringValue);
        }

        public override void SetValue(string variableName, float floatValue)
        {
            W.Strings.Remove(variableName);
            W.Flags.Remove(variableName);
            W.Numbers[variableName] = floatValue;
            NotifyVariableChanged(variableName, floatValue);
        }

        public override void SetValue(string variableName, bool boolValue)
        {
            W.Strings.Remove(variableName);
            W.Numbers.Remove(variableName);
            W.Set(variableName, boolValue);
            NotifyVariableChanged(variableName, boolValue);
        }

        public override bool Contains(string variableName) =>
            W.Numbers.ContainsKey(variableName) || W.Strings.ContainsKey(variableName) || W.Flags.ContainsKey(variableName);

        public override void Clear()
        {
            W.Numbers.Clear();
            W.Strings.Clear();
            var yarnFlags = new List<string>();
            foreach (var k in W.Flags.Keys) if (k.StartsWith("$")) yarnFlags.Add(k);
            foreach (var k in yarnFlags) W.Flags.Remove(k);
        }

        public override void SetAllVariables(Dictionary<string, float> floats, Dictionary<string, string> strings, Dictionary<string, bool> bools, bool clear = true)
        {
            if (clear) Clear();
            foreach (var kv in floats) SetValue(kv.Key, kv.Value);
            foreach (var kv in strings) SetValue(kv.Key, kv.Value);
            foreach (var kv in bools) SetValue(kv.Key, kv.Value);
        }

        public override (Dictionary<string, float> FloatVariables, Dictionary<string, string> StringVariables, Dictionary<string, bool> BoolVariables) GetAllVariables()
        {
            var bools = new Dictionary<string, bool>();
            foreach (var kv in W.Flags) if (kv.Key.StartsWith("$")) bools[kv.Key] = kv.Value != 0;
            return (new Dictionary<string, float>(W.Numbers), new Dictionary<string, string>(W.Strings), bools);
        }
    }
}
