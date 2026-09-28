# Localisation (PRG-19 and NAR-18, v1)

This is the wiring that gets every string the player reads from a table in their language, and the checks that keep
it that way. No language but English is written yet.
- **PRG-19** wired the locale, dialogue, the UI and captions.
- **NAR-18**, the localisation-ready pass, wired the catalogs, added plurals and lists, and turned the pseudo-locale
  into an audit of every page.

## The locale

`Loc` (Core) holds the player's locale.
- **Where it comes from:** the saved choice (`PlayerPrefs` `owsbg.locale`, so it outlives saves and slots). Failing
  that, the system's language if we ship a table for it; failing that, English.
- **Changing it:** `Loc.SetLocale(code)` switches, saves, and raises `Loc.Changed`. An unknown locale is refused.
- **Which locales exist:** English (`en`), the pseudo-locale (`en-XA`), and any locale with a UI table.
- **The picker** is the Language row of the options page (DES-14, `accessibility.md`): left and right step through
  `Loc.Choosable`, each language named in its own words (`Loc.NativeName`). The pseudo-locale is offered only in
  development builds.

## Two tables, one locale

| What | Where the English is | Where a translation goes | How it's keyed |
|---|---|---|---|
| **Dialogue** (Yarn lines and options) | the `.yarn` files | a Yarn strings CSV per locale, listed under `localisation` in `LastCartographer.yarnproject` | each line's `#line:` id |
| **Everything else** (HUD, desk, atlas, journal, ledger, shop, tutorials, captions) | in code: `Loc.T("hud.death", "the ink runs out")`, `Loc.F("caption.travelled", "{0} on the road. {1}, day {2}.", ...)`, labels via `InkTheme.Say(element, key, english, ...)` | `Settings/Localization/Resources/Localization/ui.<locale>.csv` (`key,text,comment`) | a dotted key |

- **Formatting.** `Loc.F` formats with `{0}`, `{1}`, so a translation can reorder the pieces. Nothing a player reads
  is joined out of fragments any more.
- **Fallbacks.** A key a table lacks shows its English. A locale with no dialogue table speaks its lines in English:
  `DialogueService.DialogueLocale` picks the project's localisation for the locale, else the base language.
- **Following a change.** The dialogue runner's line provider follows `Loc.Locale` at every conversation start and
  whenever it changes. Labels made with `InkTheme.Say` re-text themselves the moment it changes; everything else
  re-texts on its next refresh.
- **Why not `com.unity.localization`** (the engine doc's choice)? Yarn Spinner's localisation is already
  CSV-per-locale, so one plain format covers dialogue and UI for translators. Lookups are synchronous, with no
  Addressables loading at startup, and all of it can be tested headless. Unity Localization stays installed if its
  smart strings or asset tables are wanted later.

## The pseudo-locale, `en-XA`

Every string comes out accented, a third longer and «bracketed»: «Hàvé ýöû éàţéñ? ···········».
- **What's kept:** placeholders (`{0}`), Yarn markup (`[b]`), rich-text tags, and a dialogue line's `Speaker: `.
- **What it's for:** switch to it and anything still in plain English skipped the tables, and anything too tight
  for a longer language clips. That makes it NAR-18's audit tool.
- **Where it comes from:** UI text is dressed on the fly. Dialogue has a generated table,
  `Dialogue/Localisation/en-XA.csv`, which the Yarn project imports like any translation.

## Keeping it in step

Run this after writing dialogue or UI text:
```
Unity.exe -batchmode -projectPath LastCartographer -executeMethod OWSBG.Setup.LocalizationSetup.Refresh -quit
```
It does three things (`Assets/Editor/Setup/LocalizationSetup.cs`):
1. Every Yarn line and option without a `#line:` id gets one, from Yarn Spinner's own tagger. This pass tagged
   1,087 lines and options in 43 files. Ids are what translations are filed under, so a line keeps its id when it's
   rewritten; the CSV's lock column tells a translator the English changed.
2. It rewrites the pseudo-locale's dialogue table from the lines as they are now.
3. It harvests every `Loc.T` / `Loc.F` / `Loc.P` / `Say(...)` in `Code/`, plus the catalogs' keys (`DataText`,
   `WorldText`), into `Settings/Localization/ui.en.csv`, the English UI table translators start from. That's 486
   keys, each with the files or catalogs that use it. One key with two different English texts stops the harvest.

## Tests

`LocalizationTests` (EditMode, 8). The last four fail with a pointer to the refresh:
- English is English; the pseudo-locale is plainly not, keeps placeholders, markup, tags and the speaker, and still
  formats.
- A table answers what it has and English the rest; a translation can reorder `{0}` and `{1}`; a system language
  with a table is picked.
- Unknown locales are refused, the change is raised once, and the choice outlives the session.
- Tables are CSV that round-trips commas, quotes, newlines and edge spaces.
- Every line of dialogue has its own id, none shared (over 800 lines read).
- The pseudo-locale's table has exactly the lines as written, all pseudo.
- The English UI table is exactly what the code says.
- A lint: no `InkTheme.Text` with a lettered literal, no `.text = "..."`, and no `Captions.Show("...")`. Player text
  goes through the tables.

`LocalizationPlayTests` (PlayMode, 2), through the shipped Yarn project:
- **Dialogue:**
  - In English, then in the pseudo-locale, the speaker's name still comes through.
  - Options are localised too.
  - A locale with a UI table but no dialogue speaks English lines rather than none.
- **Captions:** "Clarity grows." comes out in the pseudo-locale.

`UiTests.TheHudFollowsThePlayersLanguage`: the HUD's label follows English → pseudo → a French table → English,
the moment the locale changes.

## The catalogs (NAR-18)

Most of what the pages print comes from catalogs, not from `Loc.T` calls. Each catalog keeps its English as it was,
and gains an accessor that looks up a key built from the id. The English is the fallback.

| What | Key | Accessor |
|---|---|---|
| Place, vantage, desk and lamp names; region headings | `place.<id>`, `vantage.<id>`, `waypoint.<id>`, `region.<slug>` | `Atlas.PlaceName`, `VantageName`, `WaypointName`, `RegionName` |
| Fade stages and place fates | `fade.<stage>`, `fate.<fate>` | `FadeStages.Display`, `Places.Display` |
| Commissions: title, poster, brief, journal, aftermath, each step | `commission.<id>.<field>`, `commission.<id>.step.<n>` | `Commissions.TitleOf`, `StepOf`, … |
| Hubs, abilities | `hub.<hub>`, `abilities.<Ability>` | `Commissions.HubName`, `AbilityNames.Of` |
| Bosses: name and the three phase lines | `boss.<id>.name`, `.entry`, `.turn`, `.last` | `Bosses.NameOf`, `LineOf`, through `Boss.BossName` and `PhaseLine` |
| Memories | `memory.<id>` | `Memories.Name`, `Describe` |
| Gauntlets | `gauntlet.<id>` | `Gauntlets.NameOf` |
| The seller's pitches | `stock.<hub>.<kind>` | `Economy.PitchOf` |
| The walks' verses and bounds | `walk.<id>.verse.<n>`, `walk.<id>.bound.<slug>` | `BoundsWalks.VerseTitle`, `BoundName`, through `BoundsWalk` |
| Instruments: name and blurb | `instrument.<kind>.name`, `.blurb` | `InstrumentInfo.Name`, `Blurb` (the English is `EnglishName`) |
| Charters: name and blurb | `charter.<kind>.name`, `.blurb` | `CharterProfile.LocalName`, `LocalBlurb` |

- **Where the table gets these keys.** They are built from ids, so the harvest can't find them in source.
  `DataText.All()` (Core) and `WorldText.All()` (World) list them from the shipped catalogs instead, and the refresh
  adds them to `ui.en.csv`: 486 keys, up from 162.
- **Only English for scripts and logs.** Yarn functions such as `place_fate()`, `phase()` and `commission_state()`
  still answer in English, because scripts compare the words. The `Display` accessors are for the pages.
- **Scenes carry the catalogs' words:**
  - A boss in a scene is found by its sheet's name.
  - A desk or lamp takes its name from the atlas catalog when the catalog has one (the chapel's desk was added).
  - The Merrow's End walk that ProjectSetup builds says what `BoundsWalks.Words` lists.
  - A test checks all three.

## Counts and lists

- **`Loc.P("desk.scraps", n, "{0} scrap", "{0} scraps")`** picks the table's `key.one`, `key.few`, `key.many` or
  `key.other` by the language's plural rules (`Loc.PluralOf`), else `key.other`, else the English.
  - **Languages covered:** the CLDR cardinal rules for whole numbers in English, German, Spanish, Italian and
    Dutch; French and Portuguese (0 and 1 are singular); Russian and Polish (one/few/many); Japanese, Korean and
    Chinese (no plural).
  - **What uses it:** the desk's scraps, masks and belt; the ledger's reward; travel hours.
  - **The harvest** writes `key.one` and `key.other` for each.
- **`Loc.List(items)`** joins "a, b and c" with the table's `list.comma` and `list.and`. The memories a fall
  drops are listed through it.
- **Loose English caught on the way:** the atlas's region headings and "(off the page)", the ledger's "vellum scrap(s)" and
  "[needs …]", raw hub names, and ability names printed from code.

## The pseudo-locale as an audit

`PseudoLocaleAuditTests` boots the game in `en-XA` and puts something on every page: a commission taken, a place
drawn, scraps and seeds. It then opens each page and fails on any text shown with a letter outside «…»:
- the HUD;
- the desk, every row;
- the atlas with its journal;
- the ledger;
- the shop;
- the options and their controls page.

Text that stays as it is in every language is marked `InkTheme.Verbatim`: a language's own name on the picker, and a
key's or button's name.

`DataTextTests` (EditMode, 5):
- plurals by each language's rules, and a table with only `key.other`;
- lists joined in English and in a French table;
- every catalog string has its own key and one English;
- the catalogs answer in English, the pseudo-locale and a French table, and fall back to English for keys the table
  lacks;
- the scenes carry the catalogs' words: the walk, the bosses and every desk and lamp.

## Still open

- **Fonts.** The greybox font covers Latin-1, which is enough for the pseudo-locale. CJK and Cyrillic need font
  assets and fallbacks, and wait for the ink font (ENV-11).
- **Right-to-left and text expansion** are layout questions for the UI art pass. The pseudo-locale already runs a
  third long.
- **Voice-over assets per locale** (Yarn's asset folders) are unused.
- **Not wired:** interactable prompts ("Talk", "Rest") are never shown, so none of them go through the tables yet.
  Rooms that exist only in `RoomPlans` show their id with its underscores opened until the atlas catalogs them.
- **No language is written.** A translation is `ui.<locale>.csv` next to `ui.en.csv`, plus a Yarn strings CSV per
  locale listed in the project. Both start from the English files the refresh writes.
