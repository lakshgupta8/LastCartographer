using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The planning papers, the model sheets and the rendered audio under docs/ are working files kept off the public
    /// repository (.gitignore, CONTRIBUTING.md). A test that reads them runs wherever they exist and is ignored,
    /// or skips that one check, wherever they do not (a fresh clone, CI).
    /// </summary>
    static class DevDocs
    {
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        /// <summary>Ignores the test unless every named path (relative to the repository root) is present.</summary>
        public static void Require(params string[] relative)
        {
            foreach (var r in relative)
                if (!File.Exists(Path.Combine(Root, r)) && !Directory.Exists(Path.Combine(Root, r)))
                    Assert.Ignore(r + " is a development file, not in this checkout");
        }

        /// <summary>A model sheet is asserted where docs/art exists and skipped where it does not.</summary>
        public static void AssertSheet(string path, string message)
        {
            if (!Directory.Exists(Path.Combine(Root, "docs", "art"))) return;
            Assert.IsTrue(File.Exists(path), message);
        }
    }
}
