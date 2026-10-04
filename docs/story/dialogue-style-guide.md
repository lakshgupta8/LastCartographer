# Dialogue Style Guide

For everyone writing Yarn files under `LastCartographer/Assets/_Project/Dialogue/`. The story bible (section 11) has the narrative rules; this is the line-level craft.

## 1. Voice of the game
- **Plain words, specific nouns.** "The bread is the same" beats "nothing has changed here."
- **Warmth first.** People in Aurenne are tired and kind. Sarcasm belongs to Sable and almost nobody else.
- **No modern idiom.** No "okay", "yeah", "guys", "kind of". Contractions are fine.
- **No exposition dumps.** If a fact needs saying, someone needs a reason to say it *now*, to *this* person.

## 2. Line rules
1. **20 words or fewer** per line. Split long thoughts across lines; the player clicks through.
2. **One beat per line.** A line does one thing: informs, asks, jokes, or turns.
3. **Player choices are 8 words or fewer** and phrased as what Wren *says*, not what they *do*. Never "[Lie]". The player should hear the lie in the words.
4. **Three choices maximum.** If you need four, one of them is a follow-up.
5. **Silence is a choice.** Where it fits, offer "..." as an option. NPCs respond to it.

## 3. Wren's three voices
Every choice set should, where natural, include one line from each. Not labelled in-game.

| Voice | Sounds like | Example |
|---|---|---|
| **Surveyor** | Precise, curious, notices | "The leaves. You're the only one who rakes them." |
| **Warden** | Protective, blunt, responsible | "Then I'll carry her out myself." |
| **Drift** | Dreamy, empathetic, comfortable with not knowing | "Maybe it doesn't need a name yet." |

## 4. Character voice sheet (short form; the full bibles are `character-bibles.md`)

| Character | Rhythm | Tell | Never says |
|---|---|---|---|
| **Isolde** | Short sentences. Questions instead of answers. | Calls Wren "journeyman" when proud, "Wren" when scared. | "I'm sorry." |
| **Pell** | Long, breathless, lists. | "Which, technically, means..." | Anything unkind. |
| **Sable** | Clipped. Ends conversations first. | Names the price of everything. | "I hope." |
| **Runa** | Warm, loud, sings mid-sentence. | Uses "we" for her town even when alone. | "I don't know how." |
| **Teodor** | Slow, complete sentences, gentle. | Speaks of the faded in present tense. | A raised voice. |
| **Voss** | Formal, exact, generous with titles. | Flinches at "Halloway". Talks about the Guild as "we". | "Corra". |
| **Old Kettil** | Proverbs, orders, laughter. | Counts people out loud. | "Guild" without spitting. |
| **Idrenne** | Amused, unhurried. | Answers questions with where she was standing when she learned the answer. | "Always" or "never". |
| **Corvin** | Grand, weary, self-correcting. | Starts sentences with "When I—" and stops. | "It wasn't my fault." |
| **Marrow** | Echoes: repeats fragments others have said. | Gains one original word per act. | (Its first original word is the ending's verdict.) |
| **Remnant (generic)** | Half-sentences. Present tense. Polite. | Ask visitors whether they've eaten. | "I'm dead." |

## 5. Yarn conventions
- One file per scene or per NPC, named `Region_Place_Character.yarn` (for example `Halden_Orchard_Keeper.yarn`).
- Node titles: `PascalCase`, prefixed by scene: `Orchard_FirstMeeting`, `Orchard_AfterCache`.
- Flags are set only through the `<<flag key value>>` command, never inline in prose. Flag keys are `snake_case` and namespaced: `halden.orchard.found_cache`.
- A line the player can hear again, unchanged, on a later visit is tagged `#still` so QA does not file the repeat as a bug. In an anchored town that is the Stillness itself; the tag means the same everywhere. A line on a visit that always changes something (a flag a script or the game reads, a fade, a grant) is not `#still`. `ForeshadowingTests` checks both ways (`docs/story/foreshadowing.md` §3). (This is the `[still]` rule from the bible.)
- Lines that plant a secret are tagged `#plant:5.1` (etc.) so the foreshadowing audit can count them; the one line that says it is `#reveal:5.1`, and a line that leans on it once it is known is `#callback:5.1`. Three plants in three scenes before every reveal (bible 11.1; `docs/story/foreshadowing.md`).
- A line said with a face its punctuation would not give it is tagged `#face:grave` (or `bright`, `wary`, `asking`); the portrait wears that mood for the line. Untagged, a question asks, an exclamation is bright, a line that trails off is grave, and anything else is plain. One tag a line, never on Wren's options or a faceless speaker, and the mood does not carry to the next line (`docs/design/portraits.md` §3).
- Companion interjections use `#interject:pell` and are written in the companion's file, not the NPC's, so they can be omitted when the companion is absent.
- Every line and option ends with a `#line:` id: the key its translations are filed under (PRG-19, `docs/design/localisation.md`). Don't write the ids by hand. After writing dialogue, run `-executeMethod OWSBG.Setup.LocalizationSetup.Refresh`; it tags new lines and rebuilds the pseudo-locale's table. Keep a line's id when you rewrite it; the table's lock notices the change.
- Keep the speaker at the head of the line (`Sable: ...`), and never build a line out of pieces in a command. A translation moves words; it can't move a sentence that only exists once the code has joined it.

```yarn
title: Orchard_FirstMeeting
---
Keeper: You're Isolde's. #plant:5.4
Keeper: She stood where you're standing. Asked about the leaves.
-> You're the only one who rakes them.
    Keeper: Someone has to. Nobody else remembers they fall.
-> Where did she go after here?
    Keeper: Down. Same as everyone who asks the right question.
-> ...
    Keeper: That's what she said too.
<<flag halden.orchard.met_keeper 1>>
===
```

## 6. The cryptic register (Silksong-style delivery)
Most birds in Aurenne speak in the **cryptic register**: short, formal, oblique, and true. Rules:
- **Withhold, never lie.** An NPC may say a third of what they know. Everything they say must be literally accurate and reconstructible later.
- **Archaic, not fake-medieval.** "Wings high, stranger. You've come a long way to be lost." Not "thee" and "thou".
- **Name the place, not the mechanic.** "The lamp remembers the flock" (Wingbeat), never "you can dash now."
- **One image per speaker.** Each NPC owns one recurring image (Sable: prices; Kettil: counting; Teodor: present tense for the dead). Repeat it.
- **Bosses speak three times.** A line on entry, one at the phase change, one on defeat or victory. Under twelve words each.
- **Environment first.** If a corpse, inscription, or tapestry can carry the fact, don't give it to an NPC.
- **Isolde and Pell are the exception.** They speak plainly. That is why the player trusts them, and why Pell's report hurts.

## 7. Things to avoid
- Characters explaining the Stillness. They *exhibit* it.
- Any NPC using the word "theme", "memory-mechanic", or "faction".
- Wren narrating their feelings. Let the choice carry it.
- Villain speeches. Voss gets one long speech in the whole game, at the Threshold, and it must be wrong in a way the player can see.
