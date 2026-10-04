using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The late bosses drawn (CHR-08 to CHR-10, docs/design/boss-animation.md): their moves name their clips and the
    /// animator plays them; a kit carrying a part skin dresses the pieces it makes and plays their states; the memory
    /// smudge spawns drawn.
    /// </summary>
    public class BossClipPlayTests
    {
        readonly List<GameObject> _made = new List<GameObject>();
        readonly List<Texture2D> _textures = new List<Texture2D>();

        SheetClip Clip(string name, int frames, float fps = 12f, bool loop = false)
        {
            var tex = new Texture2D(frames * 4, 4);
            _textures.Add(tex);
            return new SheetClip { Name = name, Sheet = tex, Frames = frames, Fps = fps, Loop = loop };
        }

        List<SheetClip> Clips(params string[] names)
        {
            var list = new List<SheetClip>();
            foreach (var n in names) list.Add(Clip(n, 3, 12f, n == "idle" || n == "move" || n == "guard" || n == "draw" || n == "lit" || n == "dark" || n == "bare" || n == "hale" || n == "wren" || n == "run"));
            return list;
        }

        T Make<T>(string name, Vector2 pos, Vector2 size, IEnumerable<SheetClip> sheets, float cell = 4f) where T : Boss
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("Enemy") };
            _made.Add(go);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.AddComponent<BoxCollider2D>().size = size;
            go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(go.transform, false);
            quad.transform.localScale = new Vector3(-cell, cell, 1f);
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = new Material(Shader.Find("OWSBG/InkSprite"));
            if (sheets != null) go.AddComponent<InkSheetPlayer>().Configure(r, sheets);
            var boss = go.AddComponent<T>();
            if (sheets != null) go.AddComponent<EnemyAnimator>();
            return boss;
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            var floor = new GameObject("Floor") { layer = LayerMask.NameToLayer("Ground") };
            floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);
            floor.transform.position = new Vector3(9f, -0.5f, 0f);
            _made.Add(floor);
        }

        [TearDown]
        public void TearDown()
        {
            if (MemoryDrops.Current != null) Object.Destroy(MemoryDrops.Current.gameObject);
            foreach (var go in _made) if (go != null) Object.Destroy(go);
            _made.Clear();
            foreach (var p in Object.FindObjectsByType<BossPart>(FindObjectsSortMode.None)) Object.Destroy(p.gameObject);
            foreach (var t in _textures) Object.Destroy(t);
            _textures.Clear();
            GameState.NewGame();
        }

        [UnityTest]
        public IEnumerator VossAndTheStarNameTheirMoves()
        {
            var voss = Make<Voss>("Voss", new Vector2(14f, 1.1f), new Vector2(0.9f, 2.2f),
                Clips("idle", "move", "telegraph", "thrust", "lunge", "guard", "anchor", "recover", "hurt", "death"), 3.2f);
            var anim = voss.GetComponent<EnemyAnimator>();
            yield return null;
            Assert.IsTrue(voss.HasSheets);
            Assert.AreEqual("idle", voss.Clip);
            voss.BeginFight();
            voss.ForceAttack(Voss.Attack.Thrust);
            Assert.AreEqual("telegraph", voss.Clip, "the lance drawn back");
            yield return null;
            Assert.AreEqual("telegraph", anim.Clip);
            voss.ForceAttack(Voss.Attack.Anchor);
            Assert.AreEqual("anchor", voss.Clip, "the lance planted: the rose is drawn");
            yield return null;
            Assert.AreEqual("anchor", anim.Clip);
            Assert.AreEqual(3f / 12f, voss.DeathSeconds, 0.001f, "his withdrawal runs the death clip's length");

            var star = Make<FallenStar>("Star", new Vector2(6f, 1.6f), new Vector2(2.4f, 3.2f),
                Clips("idle", "walk", "telegraph", "slam", "raise", "recover", "burn", "hurt", "death"), 4.5f);
            yield return null;
            Assert.AreEqual("idle", star.Clip);
            star.BeginFight();
            star.ForceAttack(FallenStar.Attack.Slam);
            Assert.AreEqual("telegraph", star.Clip, "the fist raised");
            star.ForceAttack(FallenStar.Attack.Walls);
            Assert.AreEqual("raise", star.Clip, "the iron drawn up");
            star.ForceAttack(FallenStar.Attack.Walk);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual("walk", star.Clip);
        }

        [UnityTest]
        public IEnumerator TheGatekeeperDressesItsFeathersAndFliesOnceTorn()
        {
            var gate = Make<Gatekeeper>("Gatekeeper", new Vector2(9f, 1.6f), new Vector2(2.4f, 3.2f),
                Clips("idle", "perch", "fly", "rise", "telegraph", "sweep", "shake", "pass", "land", "recover", "hurt", "death"), 5f);
            gate.arenaMinX = 0.5f; gate.arenaMaxX = 17.5f; gate.floorY = 0f;
            gate.AddPartSkin("Feather", 1.2f, Clips("idle"));
            yield return null;
            Assert.AreEqual("idle", gate.Clip, "on its plinth");
            gate.BeginFight();
            gate.ForceAttack(Gatekeeper.Attack.Feathers);
            Assert.AreEqual("shake", gate.Clip, "it shakes the feathers loose");
            yield return new WaitForSeconds(0.5f);
            Assert.GreaterOrEqual(gate.Feathers.Count, 1, "the feathers fall");
            Assert.IsTrue(gate.Feathers[0].IsDressed, "each wears the Feather sheets");
            Assert.AreEqual("idle", gate.Feathers[0].Sheets.Current);
            Assert.AreEqual("Sprite", gate.Feathers[0].Visual.name, "the block is gone");
        }

        [UnityTest]
        public IEnumerator HalesStonesChangeHandsByClip()
        {
            var hale = Make<Hale>("Hale", new Vector2(13f, 0.9f), new Vector2(0.8f, 1.8f),
                Clips("idle", "move", "sight", "call", "count", "telegraph", "quill", "recover", "hurt", "death"), 2.8f);
            hale.floorY = 0f; hale.arenaMinX = 0.5f; hale.arenaMaxX = 17.5f;
            for (int i = 0; i < Hale.StoneCount; i++) hale.stoneXs.Add(1f + i * 2f);
            hale.AddPartSkin("Stone", 1.6f, Clips("bare", "hale", "wren"));
            hale.AddPartSkin("StoneStrike", 3.5f, new[] { Clip("erupt", 3, 24f) });
            yield return null;
            var stone = GameObject.Find("Stone_7");
            Assert.IsNotNull(stone, "the stones stand");
            var player = stone.GetComponent<InkSheetPlayer>();
            Assert.IsNotNull(player, "each wears the Stone sheets");
            Assert.AreEqual("bare", player.Current);
            hale.BeginFight();
            hale.ForceAttack(Hale.Attack.Sight);
            Assert.AreEqual("sight", hale.Clip, "the lens up");
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.GreaterOrEqual(hale.ClipProgress, 0f, "sought by the sighting");
            yield return new WaitForSeconds(0.9f);
            Assert.GreaterOrEqual(hale.HaleStones, 1, "the nearest stone is his");
            int his = 0;
            for (int i = 1; i <= Hale.StoneCount; i++)
            {
                var p = GameObject.Find("Stone_" + i)?.GetComponent<InkSheetPlayer>();
                if (p != null && p.Current == "hale") his++;
            }
            Assert.AreEqual(hale.HaleStones, his, "his stones show the sighting mark");
        }

        [UnityTest]
        public IEnumerator TheArchivistsHandDrawsAndHerDrawingStandsOnTheFloor()
        {
            var owl = Make<Archivist>("Archivist", new Vector2(9f, 3f), new Vector2(2.2f, 2.4f),
                Clips("idle", "telegraph", "draw", "swoop", "recover", "hold", "hurt", "death"), 4f);
            owl.floorY = 0f; owl.arenaMinX = 0.5f; owl.arenaMaxX = 17.5f; owl.perchY = 3f;
            owl.AddPartSkin("QuillHand", 1.2f, Clips("idle", "draw"));
            owl.AddPartSkin("WrenDrawing", 2f, Clips("idle", "run", "strike1"));
            yield return null;
            Assert.IsNotNull(owl.Hand);
            Assert.IsTrue(owl.Hand.IsDressed, "the hand wears its sheets");
            Assert.AreEqual("idle", owl.Hand.Sheets.Current);
            owl.BeginFight();
            owl.ForceAttack(Archivist.Attack.DrawWren);
            Assert.AreEqual("telegraph", owl.Clip, "the quill lifted");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual("draw", owl.Clip, "the quill on the page");
            Assert.AreEqual("draw", owl.Hand.Sheets.Current, "and the hand draws");
            Assert.IsNotNull(owl.WrenDrawing, "her drawing is made");
            Assert.IsTrue(owl.WrenDrawing.IsDressed, "on Wren's own sheets");
            Assert.AreEqual(0f, owl.WrenDrawing.Visual.position.y - 1f, 0.05f, "standing with the cell's bottom edge on the floor");
        }

        [UnityTest]
        public IEnumerator TheChoirIsItsDovesAndTheDrawingGoesToOutline()
        {
            var choir = Make<Choir>("Choir", new Vector2(9f, 0.5f), new Vector2(1f, 1f), Clips("idle"), 1f);
            choir.centreX = 9f; choir.floorY = 0f; choir.hoverY = 2.6f;
            choir.AddPartSkin("ChoirDove", 2.4f, new[] { Clip("idle", 4, 12f, true), Clip("ring", 6), Clip("hurt", 2), Clip("death", 4) });
            yield return null;
            Assert.IsFalse(choir.GetComponentInChildren<MeshRenderer>().enabled, "the Choir is the song: no body");
            Assert.AreEqual(3, choir.Doves.Count);
            Assert.IsTrue(choir.Doves[1].IsDressed, "the doves are the drawing");
            choir.BeginFight();
            choir.ForceRing(1);
            Assert.AreEqual("ring", choir.Doves[1].Sheets.Current, "a dove's bell rising");
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(choir.RingProgress(1), 0.2f);
            Assert.Greater(choir.Doves[1].Sheets.Frame, 0, "sought through its frames");
            Assert.AreEqual("idle", choir.Doves[0].Sheets.Current, "the others wait");

            var drawing = Make<CorrasDrawing>("Drawing", new Vector2(13f, 1.8f), new Vector2(2.2f, 3.6f),
                Clips("idle", "move", "telegraph", "swipe", "lift", "stomp", "recover", "hurt", "death", "idle_outline", "lift_outline"), 5f);
            drawing.floorY = 0f; drawing.arenaMinX = 0.5f; drawing.arenaMaxX = 17.5f;
            drawing.SetMaxHealth(3);
            yield return null;
            Assert.AreEqual("idle", drawing.Clip);
            drawing.BeginFight();
            drawing.ForceAttack(CorrasDrawing.Attack.Stomp);
            Assert.AreEqual("lift", drawing.Clip, "the foot raised over the mark");
            drawing.TakeHit(new HitInfo { Damage = 2, Direction = Vector2.right });
            Assert.AreEqual(3, drawing.Phase, "the crayon runs out");
            Assert.IsTrue(drawing.IsOutline);
            StringAssert.EndsWith("_outline", drawing.Clip, "every clip in outline now");
        }

        [UnityTest]
        public IEnumerator TheMemorySmudgeSpawnsDrawn()
        {
            var systems = new GameObject("Systems");
            _made.Add(systems);
            var drops = systems.AddComponent<MemoryDrops>();
            drops.ConfigureSheets(2f, Clips("idle", "move", "hurt", "death"));
            Assert.IsTrue(drops.HasSheets);
            var w = GameState.World;
            w.DropX = 4f; w.DropY = 0f;
            var smudge = drops.Spawn();
            // Its sheets are on it from the first frame (the drops' own Start tidies away a smudge with no drop behind it).
            Assert.IsNotNull(smudge);
            Assert.IsTrue(smudge.HasSheets, "the smudge of her death wears its sheets");
            Assert.IsNotNull(smudge.GetComponent<EnemyAnimator>(), "and is animated like any smudge");
            Assert.AreEqual("Sprite", smudge.GetComponentInChildren<MeshRenderer>().name, "a drawing, not a sphere");
            Assert.AreEqual("MemorySmudge", smudge.Family);
            yield return null;
        }
    }
}
