using System.IO;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Setup
{
    /// <summary>
    /// Writes the store page draft and the press kit from <see cref="StorePage"/> (PRO-08): docs/marketing/store-page.md
    /// and docs/marketing/press-kit/index.html. Headless: <c>-executeMethod OWSBG.Setup.StorePageSetup.Write</c>.
    /// <c>StorePageTests</c> fails when the files are out of date with the data.
    /// </summary>
    public static class StorePageSetup
    {
        public static string MarkdownPath => Path.GetFullPath("../docs/marketing/store-page.md");
        public static string PressKitPath => Path.GetFullPath("../docs/marketing/press-kit/index.html");

        [MenuItem("OWSBG/Marketing/Write the Store Page and Press Kit")]
        public static void Write()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PressKitPath));
            File.WriteAllText(MarkdownPath, StorePage.Markdown());
            File.WriteAllText(PressKitPath, StorePage.PressKitHtml());
            Debug.Log("[OWSBG] store page: " + MarkdownPath + " and " + PressKitPath);
        }
    }
}
