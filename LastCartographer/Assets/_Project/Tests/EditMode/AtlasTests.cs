using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>The atlas rules (DES-02): erasure, recovery by re-survey, waypoints, and the save.</summary>
    public class AtlasTests
    {
        [SetUp]
        public void SetUp() { Atlas.Reset(); Atlas.EnsureDefaults(); }

        [Test]
        public void ErasureWipesDrawnVantagesAndBlanksThePlace()
        {
            var w = new WorldState();
            Assert.IsTrue(Atlas.Survey(w, "Saltmarrow_A/Reedmother"));
            Assert.IsFalse(Atlas.Survey(w, "Saltmarrow_A/Reedmother"), "drawn once");
            Assert.IsTrue(FadeStages.Advance(w, "Saltmarrow_A", 1));
            Assert.IsTrue(Atlas.IsDrawn(w, "Saltmarrow_A"));
            Assert.AreEqual(1, Atlas.DrawnCount(w, "Saltmarrow_A"));

            int erasedEvents = 0;
            Atlas.PlaceErased += _ => erasedEvents++;
            Assert.IsTrue(Atlas.Erase(w, "Saltmarrow_A"));
            Assert.AreEqual(1, erasedEvents);
            Assert.IsFalse(w.IsSurveyed("Saltmarrow_A/Reedmother"), "off the page");
            Assert.IsTrue(w.IsEverSurveyed("Saltmarrow_A/Reedmother"), "but it was drawn once: story gates hold");
            Assert.IsTrue(w.IsErased("Saltmarrow_A/Reedmother"));
            Assert.IsFalse(Atlas.IsDrawn(w, "Saltmarrow_A"));
            Assert.IsTrue(Atlas.IsErased(w, "Saltmarrow_A"));
            Assert.AreEqual(FadeStages.Max, FadeStages.Get(w, "Saltmarrow_A"), "the ink goes blank");
            Assert.AreEqual(1, FadeStages.StageBeforeErasure(w, "Saltmarrow_A"));
            Assert.IsFalse(Atlas.Erase(w, "Saltmarrow_A"), "already erased");
            Assert.IsFalse(FadeStages.Restore(w, "Saltmarrow_A", 1), "story restore is refused while erased; recovery is the survey's");

            Assert.IsTrue(Atlas.Survey(w, "Saltmarrow_B/Tetherpost"));
            Assert.IsTrue(Places.Anchor(w, "Saltmarrow_B"));
            Assert.IsFalse(Atlas.Erase(w, "Saltmarrow_B"), "the Guild's seal holds against a field bell");
            Assert.IsTrue(w.IsSurveyed("Saltmarrow_B/Tetherpost"));
        }

        [Test]
        public void ReSurveyRecoversTheInkToTheStageItHad()
        {
            var w = new WorldState();
            Atlas.Survey(w, "Saltmarrow_A/Reedmother");
            FadeStages.Advance(w, "Saltmarrow_A", 2);
            Atlas.Erase(w, "Saltmarrow_A");
            int recovered = 0;
            Atlas.PlaceRecovered += _ => recovered++;

            Assert.IsTrue(Atlas.Survey(w, "Saltmarrow_A/Reedmother"), "re-drawing an erased vantage counts");
            Assert.AreEqual(1, recovered);
            Assert.IsTrue(w.IsSurveyed("Saltmarrow_A/Reedmother"));
            Assert.IsFalse(w.IsErased("Saltmarrow_A/Reedmother"));
            Assert.IsFalse(Atlas.IsErased(w, "Saltmarrow_A"));
            Assert.AreEqual(2, FadeStages.Get(w, "Saltmarrow_A"), "back to the stage it had, not to drawn");
            Assert.IsFalse(FadeStages.Recover(w, "Saltmarrow_A"), "nothing left to recover");
            Assert.IsTrue(FadeStages.Advance(w, "Saltmarrow_A", 3), "and the story can move it on again");
        }

        [Test]
        public void WaypointsNeedTheirPlaceOnThePage()
        {
            var w = new WorldState();
            Assert.IsFalse(Atlas.Discover(w, "nowhere"));
            Assert.IsTrue(Atlas.Discover(w, "desk.Saltmarrow_A"));
            Assert.IsFalse(Atlas.Discover(w, "desk.Saltmarrow_A"), "once");
            var desk = Atlas.FindWaypoint("desk.Saltmarrow_A");
            Assert.IsNotNull(desk);
            Assert.IsTrue(Atlas.IsKnown(w, desk.Id));
            Assert.IsFalse(Atlas.CanTravelTo(w, desk), "known, but the quay is not drawn");
            Atlas.Survey(w, "Saltmarrow_A/Reedmother");
            Assert.IsTrue(Atlas.CanTravelTo(w, desk));

            Atlas.Discover(w, "lamp.Saltmarrow_Lighthouse");
            CollectionAssert.IsEmpty(Atlas.Destinations(w, "desk.Saltmarrow_A"), "the lighthouse is not on the page yet");
            Atlas.Survey(w, "Saltmarrow_Lighthouse/Lamp");
            CollectionAssert.AreEqual(new[] { "lamp.Saltmarrow_Lighthouse" }, Atlas.Destinations(w, "desk.Saltmarrow_A").ConvertAll(p => p.Id), "the lighthouse desk is not known yet");
            CollectionAssert.AreEqual(new[] { "desk.Saltmarrow_A" }, Atlas.Destinations(w, "lamp.Saltmarrow_Lighthouse").ConvertAll(p => p.Id), "never the point you stand at");

            Atlas.Erase(w, "Saltmarrow_A");
            CollectionAssert.IsEmpty(Atlas.Destinations(w, "lamp.Saltmarrow_Lighthouse"), "erasure takes a destination off the page");
            Assert.IsTrue(Atlas.IsKnown(w, desk.Id), "but it is not forgotten");
        }

        [Test]
        public void SavesKeepErasureAndWaypoints()
        {
            var w = new WorldState();
            Atlas.Survey(w, "Saltmarrow_A/Reedmother");
            Atlas.Survey(w, "Saltmarrow_B/Tetherpost");
            FadeStages.Advance(w, "Saltmarrow_A", 1);
            Atlas.Erase(w, "Saltmarrow_A");
            Atlas.Discover(w, "desk.Saltmarrow_A");

            var back = GameState.FromJson(GameState.ToJson(w));
            Assert.IsFalse(back.IsSurveyed("Saltmarrow_A/Reedmother"));
            Assert.IsTrue(back.IsErased("Saltmarrow_A/Reedmother"));
            Assert.IsTrue(back.IsSurveyed("Saltmarrow_B/Tetherpost"));
            Assert.IsTrue(Atlas.IsErased(back, "Saltmarrow_A"));
            Assert.AreEqual(1, FadeStages.StageBeforeErasure(back, "Saltmarrow_A"));
            Assert.IsTrue(Atlas.IsKnown(back, "desk.Saltmarrow_A"));
            Assert.IsTrue(Atlas.Survey(back, "Saltmarrow_A/Reedmother"));
            Assert.AreEqual(1, FadeStages.Get(back, "Saltmarrow_A"));
        }
    }
}
