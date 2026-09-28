using System;
using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// The last lines the game logged (PRO-07), kept in a ring so a bug report can carry them. Errors and exceptions
    /// are counted, the first exception's stack is kept, and each exception is announced so the reporter can write a
    /// report by itself in a built player.
    /// </summary>
    public static class LogTail
    {
        public const int Capacity = 200;

        static readonly Queue<string> _lines = new Queue<string>(Capacity + 1);
        static bool _installed;

        public static int Errors { get; private set; }
        public static int Exceptions { get; private set; }
        /// <summary>The first exception's message and stack, or null.</summary>
        public static string FirstException { get; private set; }
        /// <summary>An exception was logged: its message, then its stack.</summary>
        public static event Action<string, string> ExceptionLogged;

        public static IReadOnlyCollection<string> Lines => _lines;

        /// <summary>Listen to the game's log. Once is enough; a second call does nothing.</summary>
        public static void Install()
        {
            if (_installed) return;
            Application.logMessageReceived += OnLog;
            _installed = true;
        }

        static void OnLog(string condition, string stack, LogType type) => Record(type, condition, stack, Time.realtimeSinceStartup);

        /// <summary>One line into the ring: "12.345s E message". Exceptions keep their stack's first lines.</summary>
        public static void Record(LogType type, string condition, string stack, float seconds)
        {
            char tag = type switch { LogType.Error => 'E', LogType.Assert => 'A', LogType.Warning => 'W', LogType.Exception => 'X', _ => ' ' };
            string line = seconds.ToString("0.000") + "s " + tag + " " + (condition ?? "").Replace("\r", "").Replace("\n", " | ");
            if (type == LogType.Exception && !string.IsNullOrEmpty(stack))
            {
                var first = stack.Replace("\r", "").Split('\n');
                line += " @ " + first[0].Trim();
            }
            _lines.Enqueue(line);
            while (_lines.Count > Capacity) _lines.Dequeue();

            if (type == LogType.Error || type == LogType.Assert) Errors++;
            if (type == LogType.Exception)
            {
                Exceptions++;
                FirstException ??= condition + "\n" + stack;
                ExceptionLogged?.Invoke(condition, stack);
            }
        }

        /// <summary>The ring as text, oldest first.</summary>
        public static string Text() => string.Join("\n", _lines);

        /// <summary>Tests: forget everything, keep listening.</summary>
        public static void Clear()
        {
            _lines.Clear();
            Errors = 0;
            Exceptions = 0;
            FirstException = null;
        }
    }
}
