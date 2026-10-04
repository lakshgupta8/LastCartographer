using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OWSBG.Core
{
    /// <summary>
    /// The store page and the press kit as data (PRO-08, docs/design/store-page.md): the copy Steam asks for, the tags,
    /// the requirements, the press kit's factsheet, and the renderers that write them as Markdown and HTML.
    /// <c>StorePageSetup.Write</c> puts them in docs/marketing/; <c>StorePageTests</c> holds the files to this. Nothing
    /// here is published: it is the draft the team edits and pastes into Steamworks.
    /// </summary>
    public static class StorePage
    {
        public const string Title = Marketing.Title;
        public const string Tagline = "She fell off the edge of the map and drew her way back.";

        /// <summary>Steam's short description: at most 300 characters, shown on the capsule's hover and the page's top.</summary>
        public const string Short =
            "A 2.5D action metroidvania drawn in ink on paper. In a kingdom of birds that forgot how to fly, forgotten places " +
            "vanish into white. Wren, a small cartographer with a needle-quill, is cast to the edge of the map and must survey " +
            "her way back to its centre, deciding what is kept and what is let go.";
        public const int ShortLimit = 300;

        /// <summary>"About this game", as paragraphs.</summary>
        public static readonly IReadOnlyList<string> About = new[]
        {
            "The Kingdom of Aurenne is forgetting itself. Places nobody remembers lose their ink and go white, and the Guild " +
            "that once saved the kingdom now anchors what it chooses to keep. Wren, a cartographer with no memory of her own " +
            "childhood, is cast to the far edge of the map. The only way back is to draw it.",
            "Survey from vantage points and the atlas inks itself in. Fight with a needle-quill: strike, pogo, dash and thread " +
            "through a kingdom of birds, and spend the Inkwell on healing or on flourishes. Every ability you win back is " +
            "something the kingdom forgot how to do.",
            "In each region, decide what happens to a place: anchor it as it is, hold it a while, or let it go. Nothing you do " +
            "is forgotten. The final act is built from what you left behind, and the ending is yours.",
            "Told quietly, through sparse and courteous strangers, the things they leave lying about, and bosses who are people " +
            "with reasons. The world is hand-drawn ink and wash on layered paper, with real depth.",
        };

        public static readonly IReadOnlyList<string> Features = new[]
        {
            "Survey the world to draw the map: blank paper is the invitation",
            "Fast aerial combat with a needle-quill: strike, pogo, dash, thread",
            "The Inkwell: one resource for healing and for flourishes, a tempo decision in every fight",
            "Six Charters that rewrite your combo, and Instruments with limited uses",
            "Anchor, hold or release each region; the last act is generated from your choices",
            "Fifteen bosses who are characters, with reasons and aftermaths",
            "Hand-drawn ink and wash on layered paper, in 2.5D",
            "Remappable controls on keyboard and pad, a high-contrast mode, holds as toggles, and no timed text",
        };

        /// <summary>Steam allows up to twenty tags; the first few weigh most.</summary>
        public static readonly IReadOnlyList<string> Tags = new[]
        {
            "Metroidvania", "Action", "Platformer", "2.5D", "Hand-drawn", "Exploration", "Story Rich", "Atmospheric",
            "Choices Matter", "Multiple Endings", "Precision Platformer", "Fantasy", "Singleplayer", "Indie",
        };
        public const int TagLimit = 20;

        /// <summary>The factsheet. TBA where the team has not decided; the test allows TBA and refuses a made-up value.</summary>
        public static readonly IReadOnlyList<(string Field, string Value)> Factsheet = new[]
        {
            ("Developer", "TBA (a small university team)"),
            ("Release date", "TBA"),
            ("Platforms", "Windows 10 or later, 64-bit"),
            ("Price", "TBA"),
            ("Website", "TBA"),
            ("Press contact", "TBA"),
            ("Engine", "Unity 6"),
            ("Input", "Keyboard, Xbox and DualShock controllers (any pad the system sees)"),
            ("Languages", "English (interface and text); no voice acting"),
            ("Players", "Single player"),
        };

        /// <summary>The target card is the combat doc's and performance.md's; the rest is Unity 6's floor with headroom.</summary>
        public static readonly IReadOnlyList<(string Field, string Minimum, string Recommended)> Requirements = new[]
        {
            ("OS", "Windows 10 64-bit", "Windows 10 or 11 64-bit"),
            ("Processor", "Quad-core, 2.5 GHz", "Quad-core, 3 GHz"),
            ("Memory", "8 GB RAM", "8 GB RAM"),
            ("Graphics", "GeForce GTX 1060 or Radeon RX 580 (1080p, 60 fps)", "GeForce GTX 1660 or better"),
            ("DirectX", "Version 11", "Version 11"),
            ("Storage", "1 GB available", "1 GB available"),
        };

        public static readonly IReadOnlyList<string> History = new[]
        {
            "The Last Cartographer is made by a small university team in Unity 6. Its first complete version was built with " +
            "tooling the team can take apart: the birds are modelled and posed in Blender and rendered to ink; the score is " +
            "written as data and rendered with a soft synth; the dialogue is Yarn. Every part of it is being reworked by hand " +
            "from that version: the drawing, the animation, the music, the words.",
        };

        // ---------------------------------------------------------------- the renderers

        /// <summary>docs/marketing/store-page.md: the fields in Steamworks' order, ready to paste.</summary>
        public static string Markdown()
        {
            var sb = new StringBuilder();
            sb.Append("# ").Append(Title).Append(": the store page (PRO-08, draft)\n\n");
            sb.Append("Written from `StorePage` (Core) by **OWSBG → Marketing → Write the Store Page**; edit the data, not this file. ")
              .Append("Each heading is a Steamworks field. Nothing here is published.\n\n");
            sb.Append("## Short description\n\n").Append(Short).Append("\n\n*").Append(Short.Length).Append(" of ").Append(ShortLimit).Append(" characters.*\n\n");
            sb.Append("## About this game\n\n");
            foreach (var p in About) sb.Append(p).Append("\n\n");
            sb.Append("### Features\n\n");
            foreach (var f in Features) sb.Append("- ").Append(f).Append('\n');
            sb.Append("\n## Tags\n\n").Append(string.Join(", ", Tags)).Append("\n\n*").Append(Tags.Count).Append(" of ").Append(TagLimit).Append("; the first ones weigh most.*\n\n");
            sb.Append("## System requirements\n\n| | Minimum | Recommended |\n|---|---|---|\n");
            foreach (var (field, min, rec) in Requirements) sb.Append("| ").Append(field).Append(" | ").Append(min).Append(" | ").Append(rec).Append(" |\n");
            sb.Append("\n*The minimum card is the project's target (`docs/design/performance.md`); the 60 fps claim on it is not yet measured on that hardware.*\n\n");
            sb.Append("## Assets\n\n");
            sb.Append("The capsules, screenshots and trailer clips are in `docs/marketing/` (ENV-13, `docs/design/marketing-assets.md`):\n\n");
            foreach (var (file, w, h, _) in Marketing.Capsules) sb.Append("- `capsules/").Append(file).Append("` (").Append(w).Append('x').Append(h).Append(")\n");
            foreach (var s in Marketing.Shots) sb.Append("- `screenshots/").Append(s.Id).Append(".png`: ").Append(s.Caption).Append('\n');
            sb.Append("- `trailer/the_last_cartographer_trailer.mp4`: the rough cut, from the clips `");
            sb.Append(string.Join("`, `", Marketing.Clips.Select(c => c.Id))).Append("`\n");
            sb.Append("\n## Factsheet (press kit)\n\n| | |\n|---|---|\n");
            foreach (var (field, value) in Factsheet) sb.Append("| ").Append(field).Append(" | ").Append(value).Append(" |\n");
            sb.Append("\nThe press kit is `docs/marketing/press-kit/index.html`.\n");
            return sb.ToString();
        }

        /// <summary>docs/marketing/press-kit/index.html: one page, paper and ink, every asset linked, no scripts.</summary>
        public static string PressKitHtml()
        {
            var sb = new StringBuilder();
            sb.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
            sb.Append("<title>").Append(Title).Append(": press kit</title>\n");
            sb.Append("<style>\n");
            sb.Append("body{margin:0;background:#ede3cc;color:#141a28;font:17px/1.55 Georgia,'Times New Roman',serif}\n");
            sb.Append("main{max-width:980px;margin:0 auto;padding:32px 20px 80px}\n");
            sb.Append("h1,h2{font-weight:normal;letter-spacing:.01em}h1{font-size:2.4em;margin:.2em 0 0}h2{font-size:1.5em;margin:2em 0 .4em;border-bottom:1px solid #141a28;padding-bottom:.2em}\n");
            sb.Append(".tag{color:#334573;font-style:italic;margin:0 0 1.5em}\n");
            sb.Append("img{max-width:100%;height:auto;display:block;border:1px solid rgba(20,26,40,.25)}\n");
            sb.Append(".grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(300px,1fr));gap:14px}figure{margin:0}figcaption{font-size:.9em;color:#334573;margin-top:4px}\n");
            sb.Append("table{border-collapse:collapse}td{padding:4px 14px 4px 0;vertical-align:top}td:first-child{color:#334573;white-space:nowrap}\n");
            sb.Append("video{max-width:100%;border:1px solid rgba(20,26,40,.25)}a{color:#334573}\n");
            sb.Append("</style>\n</head>\n<body>\n<main>\n");
            sb.Append("<img src=\"../capsules/header_capsule.png\" alt=\"").Append(Title).Append(" header capsule\" width=\"920\" height=\"430\">\n");
            sb.Append("<h1>").Append(Title).Append("</h1>\n<p class=\"tag\">").Append(Tagline).Append("</p>\n");
            sb.Append("<h2>Factsheet</h2>\n<table>\n");
            foreach (var (field, value) in Factsheet) sb.Append("<tr><td>").Append(Esc(field)).Append("</td><td>").Append(Esc(value)).Append("</td></tr>\n");
            sb.Append("</table>\n");
            sb.Append("<h2>Description</h2>\n");
            foreach (var p in About) sb.Append("<p>").Append(Esc(p)).Append("</p>\n");
            sb.Append("<h2>History</h2>\n");
            foreach (var p in History) sb.Append("<p>").Append(Esc(p)).Append("</p>\n");
            sb.Append("<h2>Features</h2>\n<ul>\n");
            foreach (var f in Features) sb.Append("<li>").Append(Esc(f)).Append("</li>\n");
            sb.Append("</ul>\n");
            sb.Append("<h2>Trailer</h2>\n<video controls preload=\"metadata\" src=\"../trailer/the_last_cartographer_trailer.mp4\"></video>\n<p>The clips it is cut from:</p>\n<ul>\n");
            foreach (var c in Marketing.Clips) sb.Append("<li><a href=\"../trailer/").Append(c.Id).Append(".mp4\">").Append(Esc(c.Caption)).Append("</a></li>\n");
            sb.Append("</ul>\n");
            sb.Append("<h2>Screenshots</h2>\n<div class=\"grid\">\n");
            foreach (var s in Marketing.Shots)
                sb.Append("<figure><a href=\"../screenshots/").Append(s.Id).Append(".png\"><img src=\"../screenshots/").Append(s.Id).Append(".png\" alt=\"").Append(Esc(s.Caption)).Append("\" loading=\"lazy\"></a><figcaption>").Append(Esc(s.Caption)).Append("</figcaption></figure>\n");
            sb.Append("</div>\n");
            sb.Append("<h2>Key art and logo</h2>\n<div class=\"grid\">\n");
            foreach (var (file, w, h, titled) in Marketing.Capsules)
                sb.Append("<figure><a href=\"../capsules/").Append(file).Append("\"><img src=\"../capsules/").Append(file).Append("\" alt=\"").Append(file).Append("\" loading=\"lazy\"></a><figcaption>")
                  .Append(file).Append(", ").Append(w).Append('x').Append(h).Append(titled ? "" : ", no text").Append("</figcaption></figure>\n");
            sb.Append("</div>\n");
            sb.Append("<h2>Team</h2>\n<p>A small university team. Names and a contact follow when the team decides them; the roles are the production plan's: ")
              .Append("creative director and lead writer, lead programmer, combat designer, art director, environment artist, technical artist, audio designer and composer, producer.</p>\n");
            sb.Append("<h2>Contact</h2>\n<p>TBA</p>\n");
            sb.Append("</main>\n</body>\n</html>\n");
            return sb.ToString();
        }

        static string Esc(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
