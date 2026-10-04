using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>One screenshot of the game as it plays: a room, where Wren stands, and what is on screen.</summary>
    public sealed class MarketingShot
    {
        public string Id;
        public string Scene;
        /// <summary>The spawn she enters by; the shot moves her <see cref="Along"/> units from it.</summary>
        public string Spawn;
        public float Along;
        /// <summary>A Yarn node to open on its first line (the dialogue page in the shot), or null.</summary>
        public string Node;
        /// <summary>Whether the HUD shows (masks, the Inkwell, the Charter); the clean shots hide it.</summary>
        public bool Hud;
        public string Caption;
    }

    /// <summary>One short gameplay clip for the trailer: a room, and what the player does, a step at a time.</summary>
    public sealed class TrailerClip
    {
        public string Id;
        public string Scene;
        public string Spawn;
        public float Along;
        public Ability[] Abilities = new Ability[0];
        /// <summary>The script, at 30 frames a second: each step held for its frames.</summary>
        public TrailerStep[] Steps;
        public string Node;
        public string Caption;
    }

    public struct TrailerStep
    {
        public int Frames;
        public float Move;
        public bool Jump, JumpHeld, Dash, Attack;
        public TrailerStep(int frames, float move = 0f, bool jump = false, bool jumpHeld = false, bool dash = false, bool attack = false)
        {
            Frames = frames; Move = move; Jump = jump; JumpHeld = jumpHeld; Dash = dash; Attack = attack;
        }
    }

    /// <summary>
    /// The store's assets as data (ENV-13, docs/design/marketing-assets.md): the screenshots, the trailer's clips, and
    /// the capsule sizes Steam asks for. <c>MarketingCaptureTests</c> (play mode, run on request) captures the shots and
    /// clips from the game itself; <c>tools/marketing/capsules.py</c> composes the key art and capsules from them.
    /// </summary>
    public static class Marketing
    {
        public const int ShotWidth = 1920, ShotHeight = 1080;
        public const int KeyArtWidth = 3840, KeyArtHeight = 2160;
        public const int ClipFps = 30;
        public const string Title = "The Last Cartographer";

        /// <summary>The capsules and art Steam asks for, by file name: width, height, and whether the title is on it.</summary>
        public static readonly IReadOnlyList<(string File, int Width, int Height, bool Titled)> Capsules = new List<(string, int, int, bool)>
        {
            ("key_art.png", 3840, 2160, true),
            ("header_capsule.png", 920, 430, true),
            ("small_capsule.png", 462, 174, true),
            ("main_capsule.png", 1232, 706, true),
            ("vertical_capsule.png", 748, 896, true),
            ("library_capsule.png", 600, 900, true),
            ("library_hero.png", 3840, 1240, false),   // Steam lays the logo over it
            ("library_logo.png", 1280, 720, true),     // the title alone, on transparency
            ("page_background.png", 1438, 810, false),
        };

        public static readonly IReadOnlyList<MarketingShot> Shots = new List<MarketingShot>
        {
            S("dialogue_sable", "Greybox_Saltmarrow_A", "Start", 3f, "Sable, at the quay", node: "Quay_Sable_First", hud: true),
            S("saltmarrow_stilts", "Greybox_Saltmarrow_Stilts", "West", 6f, "The stilt-roosts of Saltmarrow, where the fledglings leap"),
            S("saltmarrow_boardwalk", "Greybox_Saltmarrow_Boardwalk", "West", 5f, "The boardwalk charges by the plank", hud: true),
            S("saltmarrow_lighthouse", "Greybox_Saltmarrow_Lighthouse", "West", 4f, "Halvard's lighthouse"),
            S("emberdown_chimneys", "Greybox_Emberdown_Chimneys_3", "East", -5f, "The ninth chimney, and the Guild's agent over its door"),
            S("emberdown_baths", "Greybox_Emberdown_Baths_2", "West", 6f, "The cinder-baths, where six hundred and twelve are counted"),
            S("verdance_aldermere", "Greybox_Verdance_Aldermere_1", "West", 6f, "Aldermere, where the festival never ended"),
            S("verdance_library", "Greybox_Verdance_Library_1", "West", 5f, "The Sunken Library"),
            S("halden_lowmarket", "Greybox_Halden_Lowmarket_1", "East", -6f, "The strike at Lowmarket", hud: true),
            S("halden_observatory", "Greybox_Halden_Observatory_1", "West", 5f, "The Observatory above the Plateau"),
            S("windreach_gate", "Greybox_Windreach_Gate_1", "East", -6f, "The Wind Gate"),
            S("windreach_camp", "Greybox_Windreach_Camp_1", "West", 6f, "The moving camp on the steppe"),
            S("blank_capital", "Greybox_Blank_Capital_1", "West", 6f, "The old capital, in the Blank"),
        };

        public static readonly IReadOnlyList<TrailerClip> Clips = new List<TrailerClip>
        {
            new TrailerClip
            {
                Id = "run_the_boardwalk", Scene = "Greybox_Saltmarrow_Boardwalk", Spawn = "West", Along = 1f,
                Caption = "Wren runs the boardwalk and jumps the gap",
                Steps = new[] { new TrailerStep(20), new TrailerStep(40, 1f), new TrailerStep(1, 1f, jump: true, jumpHeld: true), new TrailerStep(14, 1f, jumpHeld: true), new TrailerStep(45, 1f), new TrailerStep(20) },
            },
            new TrailerClip
            {
                Id = "quill_strikes", Scene = "Greybox_Saltmarrow_Boardwalk", Spawn = "West", Along = 4f,
                Caption = "The quill's three swings",
                Steps = new[] { new TrailerStep(15), new TrailerStep(1, attack: true), new TrailerStep(9), new TrailerStep(1, attack: true), new TrailerStep(9),
                                new TrailerStep(1, attack: true), new TrailerStep(20), new TrailerStep(12, 1f), new TrailerStep(1, 1f, dash: true), new TrailerStep(30) },
            },
            new TrailerClip
            {
                Id = "the_leap", Scene = "Greybox_Windreach_Gate_1", Spawn = "East", Along = -2f,
                Abilities = new[] { Ability.Wingbeat, Ability.Talonhold, Ability.Windmemory },
                Caption = "The leap at the Wind Gate, and the air remembering her",
                Steps = new[] { new TrailerStep(20), new TrailerStep(30, -1f), new TrailerStep(1, -1f, jump: true, jumpHeld: true), new TrailerStep(80, -1f, jumpHeld: true), new TrailerStep(20) },
            },
            new TrailerClip
            {
                Id = "sable_speaks", Scene = "Greybox_Saltmarrow_A", Spawn = "Start", Along = 3f, Node = "Quay_Sable_First",
                Caption = "Sable speaks: the portrait beside the line",
                Steps = new[] { new TrailerStep(120) },
            },
        };

        static MarketingShot S(string id, string scene, string spawn, float along, string caption, string node = null, bool hud = false)
            => new MarketingShot { Id = id, Scene = scene, Spawn = spawn, Along = along, Caption = caption, Node = node, Hud = hud };
    }
}
