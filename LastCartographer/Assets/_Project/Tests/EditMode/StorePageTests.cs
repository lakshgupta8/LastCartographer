using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// The store page and the press kit (PRO-08, docs/design/store-page.md): the copy fits Steam's fields, the tags are
    /// within the limit and unique, the factsheet says TBA rather than inventing, the requirements name the project's
    /// target card, the press kit links every asset ENV-13 made and the trailer, and the written files are the data's.
    /// </summary>
    public class StorePageTests
    {
        static string Doc(string relative) => Path.GetFullPath(Path.Combine("..", "docs", "marketing", relative));
        static string Lf(string s) => s.Replace("\r\n", "\n");

        [Test]
        public void TheCopyFitsSteamsFields()
        {
            Assert.LessOrEqual(StorePage.Short.Length, StorePage.ShortLimit, "the short description");
            Assert.Greater(StorePage.Short.Length, 150, "and says enough");
            Assert.IsFalse(StorePage.Short.Contains("\n"));
            Assert.GreaterOrEqual(StorePage.About.Count, 3);
            Assert.GreaterOrEqual(StorePage.Features.Count, 6);
            Assert.LessOrEqual(StorePage.Tags.Count, StorePage.TagLimit);
            CollectionAssert.AllItemsAreUnique(StorePage.Tags);
            Assert.AreEqual("Metroidvania", StorePage.Tags[0], "the first tag weighs most");
            StringAssert.Contains("GTX 1060", StorePage.Requirements.First(r => r.Field == "Graphics").Minimum, "the minimum card is the target (PRO-06)");
            Assert.IsTrue(StorePage.Requirements.Any(r => r.Field == "Storage"));
        }

        [Test]
        public void TheFactsheetSaysTbaRatherThanInventing()
        {
            foreach (var field in new[] { "Developer", "Release date", "Price", "Website", "Press contact" })
                StringAssert.Contains("TBA", StorePage.Factsheet.First(f => f.Field == field).Value, field + " is the team's to decide");
            Assert.IsFalse(StorePage.PressKitHtml().Contains("@"), "no invented address");
            Assert.IsFalse(Regex.IsMatch(StorePage.PressKitHtml(), @"https?://"), "no invented link");
            Assert.IsFalse(StorePage.PressKitHtml().Contains("<script"), "no scripts in the kit");
        }

        [Test]
        public void ThePressKitLinksEveryAsset()
        {
            var html = StorePage.PressKitHtml();
            foreach (var (file, _, _, _) in Marketing.Capsules) StringAssert.Contains("../capsules/" + file, html, file);
            foreach (var s in Marketing.Shots) StringAssert.Contains("../screenshots/" + s.Id + ".png", html, s.Id);
            foreach (var c in Marketing.Clips) StringAssert.Contains("../trailer/" + c.Id + ".mp4", html, c.Id);
            StringAssert.Contains("../trailer/the_last_cartographer_trailer.mp4", html, "the cut");
            foreach (Match m in Regex.Matches(html, @"(?:src|href)=""(\.\./[^""]+)"""))
            {
                var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Doc("press-kit/index.html")), m.Groups[1].Value));
                Assert.IsTrue(File.Exists(target), m.Groups[1].Value + " exists beside the kit");
            }
        }

        [Test]
        public void TheWrittenPagesAreTheDatas()
        {
            Assert.AreEqual(Lf(StorePage.Markdown()), Lf(File.ReadAllText(Doc("store-page.md"))), "store-page.md is out of date: OWSBG → Marketing → Write the Store Page");
            Assert.AreEqual(Lf(StorePage.PressKitHtml()), Lf(File.ReadAllText(Doc("press-kit/index.html"))), "press-kit/index.html is out of date");
        }
    }
}
