# Localisation (PRG-19, v1)

This is the wiring that gets every string the player reads from a table in their language, and the checks that keep
it that way. No language but English is written yet. That, and the pass over everything that isn't wired, are
NAR-18 (M4).

## The locale

`Loc` (Core) holds the player's locale.
- **Where it comes from:** the saved choice (`PlayerPrefs` `owsbg.locale`, so it outlives saves and slots). Failing
  that, the system's language if we ship a table for it; failing that, English.
- **Changing it:** `Loc.SetLocale(code)` switches, saves, and raises `Loc.Changed`. An unknown locale is refused.
- **Which locales exist:** English (`en`), the pseudo-locale (`en-XA`), and any locale with a UI table.
- **No picker yet.** Choosing a language belongs on the options screen (DES-14); until then it's the saved
  preference, set by code.

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
3. It harvests every `Loc.T` / `Loc.F` / `Say(...)` in `Code/` into `Settings/Localization/ui.en.csv`, the English
   UI table translators start from: 115 keys, each with the files that use it. One key with two different English
   texts stops the harvest.

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

## Not wired yet (NAR-18)

- **Data text** comes from catalogs, not from `Loc`:
  - place, vantage and waypoint names (`Atlas`), and fade-stage and fate words;
  - Instrument names and blurbs, Charter names and blurbs;
  - commission titles, briefs, steps and aftermaths; boss names and phase lines; memory names;
  - island and ending titles; the camp's Yarn-only speaker names; interactable prompts ("Talk", "Rest").

  These need keys by id and a harvest over the catalogs. In the pseudo-locale they show in plain English, which is
  the list.
- **Fonts.** The greybox font covers Latin-1, which is enough for the pseudo-locale. CJK and Cyrillic need font
  assets and fallbacks (ENV-11).
- **Plurals and gender.** Yarn's `[plural]`/`[select]` markup works in dialogue; `Loc.F` has no plural rules. "1
  scraps" is English's problem now, and every language's later.
- **Right-to-left and text expansion** are layout questions for the UI art pass. The pseudo-locale already runs a
  third long.
- **Voice-over assets per locale** (Yarn's asset folders) are unused.
