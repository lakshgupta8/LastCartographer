using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>Windreach's decision (bible 4.5) as the endings will read it, and Idrenne's one rule of voice.</summary>
    public class SteppeTests
    {
        [Test]
        public void TheSteppeIsOnTheGuildsMapOnlyIfSomebodyFinishedIt()
        {
            var w = new WorldState();
            Assert.IsFalse(Steppe.OnTheGuildsMap(w) || Steppe.HeldByWalking(w), "undecided: neither");
            w.Set(Steppe.SurveyFlag, Steppe.Undrawn);
            Assert.IsFalse(Steppe.OnTheGuildsMap(w));
            w.Set(Steppe.SurveyFlag, Steppe.Walked);
            Assert.IsTrue(Steppe.HeldByWalking(w));
            Assert.IsFalse(Steppe.OnTheGuildsMap(w));
            w.Set(Steppe.HaleFinishedFlag, true);
            Assert.IsTrue(Steppe.OnTheGuildsMap(w), "Hale finished: it is on paper whatever Wren does after");
            var v = new WorldState();
            v.Set(Steppe.SurveyFlag, Steppe.Drawn);
            Assert.IsTrue(Steppe.OnTheGuildsMap(v), "and paper goes where paper goes");
        }

        [Test]
        public void IdrenneSaysNeitherAlwaysNorNever()
        {
            var dir = Path.Combine(Application.dataPath, "_Project/Dialogue");
            var rx = new Regex(@"^\s*Idrenne:(.*)$", RegexOptions.Multiline);
            int lines = 0;
            foreach (var f in Directory.GetFiles(dir, "*.yarn", SearchOption.AllDirectories))
                foreach (Match m in rx.Matches(File.ReadAllText(f)))
                {
                    lines++;
                    var said = m.Groups[1].Value.ToLowerInvariant();
                    Assert.IsFalse(Regex.IsMatch(said, @"\b(always|never)\b"), Path.GetFileName(f) + ": Idrenne said it: " + m.Value.Trim());
                }
            Assert.Greater(lines, 30, "she has a lot to say");
            Assert.AreEqual("\"always\" or \"never\"", Cast.Find("idrenne").NeverSays);
        }
    }
}
