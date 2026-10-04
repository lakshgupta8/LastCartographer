# Recurring-Character Bibles (NAR-05, v1)

The five birds the player meets again and again, across regions and acts: **Pell, Sable, Runa, Teodor,
Marrow.** For each: who they are, what they want, the image they own, where they stand on the map act by act,
and what they say each time. Section 8 covers the rest of the returning cast in short form. The style guide's
voice sheet (§4) is the summary; this is the long form it points to.

The map is also data: `Cast` (Core) lists every member and every appearance by act and `WorldGraph` zone, and
the tests check that each appearance stands on a zone that exists, that the five recur, and that every scene
marked staged has its Yarn node in the project (and none marked unstaged does). When a scene is written, flip
its `Staged` flag in `Cast.cs`; the test will tell you if the node name drifted.

## 0. Rules that apply to all five

- **Acts.** 0 prologue, 1, 2, 3, 4 epilogue. Emberdown and the Verdance are Act 1 for one of them and Act 2
  for the other, by the player's climb; their rows say "1/2".
- **Each meeting is state-aware, never a repeat.** A returning character reads what Wren has done since (flags,
  the atlas, the ledger) and speaks to it first, as Sable does on the quay. The only lines that repeat are
  `#still` lines in anchored places.
- **One image per speaker** (style guide §6). It is named below and must appear in every scene the character has.
- **Flags.** What happened in a scene lives under the region: `saltmarrow.sable.reed`. What a character carries
  across regions lives under their name: `pell.report_sent`, `sable.tether_sold`, `runa.named_wren`,
  `teodor.keystone_given`, `marrow.words`.
- **Files.** One Yarn file per character per region: `Halden_Hall_Pell.yarn`, `Emberdown_Bell_Runa.yarn`.
  Companion interjections in `Companions/<name>.yarn`, tagged `#interject:<name>`.
- **Pronouns.** Wren she/her by default (player may change). Sable, Runa she/her. Teodor he/him. Pell they/them.
  Marrow: "it" from everyone at first, "they" from Wren once she has chosen to; the change is a choice, not a
  line.

---

## 1. Pell

**Who.** A jackdaw, journeyman of the Meridian Guild, Wren's roommate in the Journeyman's Hall for six years.
Small, quick, always carrying three more things than they have hands for. Grew up in Halden. Has never left the
Plateau before Act 1's end. Sits the master's exam "next spring" and, unlike Tam down the corridor, actually
will.

**What they want.** For everything to be all right, and to be accurate. Those two pull apart in Act 2 and Pell
has to choose; the report is the choice.

**Image.** *Lists.* Pell notices in threes and fives and says them all. As Pell grows surer, the lists shorten:
five things at the Edge, three in Halden, one at the Return. Their last list has one item.

**Voice** (style guide: long, breathless, lists; "which, technically, means…"; never unkind). Pell and Isolde
are the two who speak plainly. When Pell is frightened the sentences get longer, not shorter.

**Secret they carry.** Voss sent Pell to watch Wren because Pell is the only journeyman who would tell the truth.
Pell knows that is why. They also know something they do not know they know: they have seen the vault's sixth
slot empty (5.2) and the chick's drawing in Voss's office (5.5), and say both without weighing them.

**Never says.** Anything unkind. (When the report is sent, Pell does not apologise. They read it aloud.)

### Where and when

| Act | Zone | Scene | Says | Writes |
|---|---|---|---|---|
| 1 | Greyfold.RoadThatStops | **The act break.** Sent to watch. Sees her step in without a tether and come back herself | Five things they noticed, in order; the fifth is "you came back". First plain question in the game: "Wren. How?" | `pell.saw_her_cross` |
| 2 | Halden.JourneymansHall | **The minder.** Walks Halden with her (interjections through Halden's files). Interlude A: the report is written; Wren can read it; Pell decides | Three things: what the Hall says about her, what Tam said again, what they wrote. If she reads it: "It's accurate. That's the whole problem." | `pell.report_read`, `pell.report_sent` |
| 2 | Halden.Vault | The empty slot | "Six. Which, technically, means seven minus one. Which means someone counted." #plant:5.2 | `halden.vault.pell_counted` |
| 2 | Halden.Observatory | Voss's office by the flyer-tower; the framed drawing | "He's never surveyed anything. Not one place. I checked the ledgers, all of them, twice." #plant:5.5 | `halden.office.pell_ledgers` |
| 2 | Greyfold.Threshold | Only if the report was not sent: Pell comes to see her cross | One list of three: her name, Isolde's, and "whoever's in there". Then: "Go on. I'll write it down." | `pell.at_threshold` |
| 3 | Halden.Observatory | **The Return.** Holds the door of the frame while she chooses | The last list, one item long. It is whatever Wren's most-chosen voice has been (Surveyor: "the leaves"; Warden: "her"; Drift: "the sky") | `pell.last_list` |
| 4 | Halden.JourneymansHall | The epilogue walk | By ending. Fixed: still sits the exam next spring, and knows it. Open: has left the Plateau. Unwritten: keeps the Hall's ledger by hand | |

### The report (Interlude A, the hinge)
Pell writes it plainly; that is why it hurts. It records that Wren entered the Greyfold without a tether and
returned unchanged, that she carries Isolde Marr's torn pages, that she is unlicensed and has not surrendered
them. Every sentence is true. If Wren has read it before Pell decides, Pell's choice weighs her voice: two or
more Warden lines in Halden and Pell does not send it ("You'd have carried me out. I know that now."); otherwise
Pell sends it and says so to her face. Sent: boss 6.8 (Oriel), and Wardens in every anchored town hunt her.
Not sent: the Guild's stance stays Halvard's alone, and Pell is at the Threshold.

### Sample
```yarn
title: Hall_Pell_Minder
---
Pell: Five things. No, three. Three things, I've been practising.
Pell: The Hall thinks you drowned. Tam thinks you'll sit the exam with him. I think neither.
-> You wrote it down.
    Pell: All of it. It's accurate. That's the whole problem, isn't it.
-> Then don't send it.
    Pell: If I don't, someone less accurate will. Which, technically, is worse for you.
-> ...
    Pell: You do that. Isolde did that. I always talked into it.
<<flag halden.hall.pell_minder 1>>
===
```

---

## 2. Sable

**Who.** A cormorant, Captain of the Ferrymen, who run the Drowned Quay and sell tethers to anyone who can pay.
Forty-odd. Black feathers gone brown at the edges from salt and lamp-oil. Sits low, like the boats. Her
brother **Aury** kept the third lighthouse and faded with it; she rows out to him.

**What she wants.** To keep the quay counting: boats out, boats back, prices paid. She has stopped counting who
comes back from the Blank, which is the Ferrymen's rot, and she knows it is hers.

**Image.** *Prices.* She names the cost of nearly everything, and ends the conversation, always. When she
cannot name a price, she names the thing that has none ("Merrow's End. No price on it. That's how you know.").

**Voice** (clipped; ends talks first; never says "I hope"). Sarcasm belongs to her and almost nobody else. She
uses "you'll" for the future and never "I will".

**Secret she carries.** She knows Aury faded and has never told him. She sits with him in the Blank and talks
prices. Her line for it is the one she gives Wren at the quay, before Wren can know what it means: "Keeps it.
He doesn't know the difference." (`saltmarrow.sable.aury`, staged.)

**Never says.** "I hope." In the true ending's epilogue she gets as far as "I h…" and prices something instead.

### Where and when

| Act | Zone | Scene | Says | Writes |
|---|---|---|---|---|
| 1 | Saltmarrow.Quay | **The quay** (staged: `Quay_Sable`, `_First`, `_Again`, `_Widow`, `_Night`; `docs/story/saltmarrow-arc.md`) | The Guild's haste; the board; the reed; the whale; the widow; the shop; Halvard; Aury | `saltmarrow.sable.*`, `saltmarrow.bone_bridge.heard`, `saltmarrow.widow.decided` |
| 1 | Saltmarrow.BoneBridge | The whale's step: she rows Wren under the Bone Bridge and does not sing along | "Its names cost nothing. That's the only free thing on this coast. Listen." #plant:3.4 | `saltmarrow.bone_bridge.rowed` |
| 2 | Saltmarrow.LanternChain | **The tether.** She sells the cord to the third lighthouse, then goes with it herself | "Tether's fifteen. My rowing's free. Don't read anything into that." | `sable.tether_sold`, `saltmarrow.tether` |
| 3 | Blank.AurysLighthouse | **Aury.** She sits with him; talks prices; does not tell him. Wren can, or not | If Wren tells him: Sable says nothing for the first time in the game, then "Boat's leaving." | `sable.aury_told`, `blank.aury.knows` |
| 4 | Saltmarrow.Quay | Epilogue, by ending | Fixed: "Prices are the same." `#still`. Open: "Prices went up. Good. Things do." Unwritten: she is counting boats back again, out loud | |

### Rules for her state-reads (the quay pattern, to copy for other regions)
Order of precedence when she has several things to say: what Wren just did (the ledger, the fate of a place) >
what the coast has done to Wren (Halvard) > what Wren has drawn > standing lines. She says one, then offers the
three choices. The whale exchange is the exception: she asks, does not tell.

### Sample (Act 2, the tether)
```yarn
title: Chain_Sable_Tether
---
Sable: Third light. Faded. Tether's fifteen.
-> Who's out there?
    Sable: A keeper. Keeps it. #plant:5.3
-> You're coming.
    Sable: My rowing's free. Don't read anything into that.
-> ...
    Sable: Fifteen. Silence isn't a discount.
<<flag saltmarrow.chain.tether_offered 1>>
===
```

---

## 3. Runa

**Who.** A capercaillie, Old Kettil's daughter, thirty, the strongest climber in Emberdown. Climbs the chimneys
"the old way": talons in the basalt, no rope. Leads the nightly roll-call at the Bell because her mother's
voice went in the mine-smoke. The roll-call song is hers; the faded whale under the Bone Bridge sings it, which
means it is older than her and she does not know that yet.

**What she wants.** For nobody in Kettil's Rest to be a stranger, ever. That includes the dead in Hollowvein,
and it will include Wren if Wren lets it.

**Image.** *Counting people out loud*, sung. She counts Wren in on the first meeting ("stranger, one") and
never again calls her stranger. Every scene with Runa has a number in it, and the number is people.

**Voice** (warm, loud, sings mid-sentence; "we" for her town even alone; never says "I don't know how"). Where
she does not know how, she says "we'll learn it walking."

**Secret she carries.** She has walked the bounds of Hollowvein alone, at night, every year since the collapse.
It has not held; you cannot hold a place by yourself. The bounds-walk down (6.4) is the first time she has asked
anyone to come.

**Never says.** "I don't know how."

### Where and when

| Act | Zone | Scene | Says | Writes |
|---|---|---|---|---|
| 1/2 | Emberdown.RollCallBell | **Counted in.** The evening roll-call; she adds "stranger, one" and looks at Wren while she sings it | "Forty-one. Forty-two, stranger, one. Stand where I can count you." | `emberdown.runa.counted` |
| 1/2 | Emberdown.NineChimneys | **Talonhold.** The old way up. She goes first, sings the holds | "Talon, talon, breathe. Talon, talon, name. The chimney remembers who climbed it." #plant:5.6 | `emberdown.runa.climbed` (with the ability) |
| 1/2 | Emberdown.Overlook | The Greyfold from the north | "We counted it. Wider by a wingspan a year. Then we stopped counting. Then Mother started." #plant:4.6 | `emberdown.overlook.seen` |
| 1/2 | Emberdown.CinderBaths | **The debate.** Kettil and the surveyor argue in numbers; Runa sings the surveyor's numbers back as a roll-call and they stop being his | "Six hundred and twelve. He's right. Now sing them." | `emberdown.debate.heard` |
| 2 | Saltmarrow.MerrowsEnd | **A fish village counted.** Asked at the bell (Act 2, `Bell_Runa_Named`: "Merrow's End. Nine songs, nobody counting"), she comes to the coast and stands by Dotha's stoop. Unwalked, she teaches the walk to a Wren who never learned it and leads it; held, she puts Merrow's End in the roll-call | "Forty-two, Wren. Forty-three, Dotha. Forty-four, the water, since it listens." | `emberdown.runa.asked_for_merrow`, `saltmarrow.runa.came`, `saltmarrow.runa.counted_merrow` (`Saltmarrow_MerrowsEnd_Runa`) |
| 1/2 | Emberdown.Hollowvein | **The long roll-call down.** Decision: walk, or leave buried. If walked, she leads the chorus and the Collapse wakes (6.4) | Before: "Thirty-one names. We'll learn the rest walking." After: she counts thirty-one, and stops the song before the number | `emberdown.hollowvein.walked` / `.buried` |
| 2 | Emberdown.RollCallBell | **Named.** Once Wren has held any place by a bounds-walk, Runa's roll-call has Wren's name in it, wherever Wren is | "Forty-two, Wren. She's not here. Doesn't matter. That's the point." | `runa.named_wren` |
| 3 | Halden.Observatory | **The chorus** (true ending, 6.15). She leads the roll-call round the bounds of the Blank | The song with every name the player has met, in the order met; her voice is the one that is not a leitmotif but the leitmotif's source (AUD-02: sung, `docs/design/roll-call.md`) | `ending.chorus_led` |
| 4 | Emberdown.KettilsRest | Epilogue | Fixed: the roll-call is the same forty-two every night, `#still`. Open: forty-three, and she is teaching it to someone from Merrow's End. Unwritten: she counts, and the number is lower, and she keeps counting | |

### The roll-call in other regions
Runa's song is the one the whale sings (Saltmarrow, staged: `saltmarrow.bone_bridge.heard` teaches the walk),
the one Dotha half-remembers (nine of eleven), and the one the clans have never needed (Idrenne: "we walk it
instead"). Merrow's End's Holdfast way (bible 4.1) is Runa coming to the coast in Act 2 if asked
(`emberdown.runa.asked_for_merrow`); the walk in Room B is hers by proxy until then.

### Sample
```yarn
title: Bell_Runa_Count
---
Runa: Forty. Forty-one. Hold on.
Runa: Forty-two, stranger, one. Stand where I can count you.
-> I'm not staying.
    Runa: Nobody's staying. We're counted anyway. That's the whole trick of it.
-> Who taught you the song?
    Runa: Mother. Before the smoke. She says the mine taught her. We don't ask the mine.
-> ...
    Runa: Quiet ones count too. Louder, next time. We'll learn you.
<<flag emberdown.runa.counted 1>>
===
```

---

## 4. Teodor

**Who.** Brother Teodor Ashe, a mourning dove, sixty, leader of the Unwriters at the Quiet House. Once a Guild
surveyor, and a good one: he anchored eleven villages in the Guild's first decade and watched all eleven go
still. He left the Guild after the ninth and un-anchored the tenth himself. He does not say which; the Sunken
Library's Brother Ansel was the tenth's schoolmaster, and Teodor keeps him.

**What he wants.** For the faded to be allowed to go, and to be right about that. The second want is the one
Aldermere tests.

**Image.** *The faded in the present tense.* He speaks of everyone who has faded as though they are in the next
room, because to him they are. He names them. He never says "was" of a bird.

**Voice** (slow, complete sentences, gentle; never a raised voice). He asks permission before every hard
sentence: "May I say a thing you will not like?" He calls Wren "cartographer", never "journeyman", never "Guild".

**Secret he carries.** The tenth village was his own. He un-anchored it because his mother, held still at
seventy for four years, asked him to, and he has never been sure she meant it. Aldermere asked. He has made
certain, this time, that they asked. That certainty is what Wren can honestly speak to.

**Never says.** Anything at a raised voice. When the Choir is fought, his voice drops instead.

### Where and when

| Act | Zone | Scene | Says | Writes |
|---|---|---|---|---|
| 1/2 | Verdance.QuietHouse | **First meeting.** He is expecting her; the reeds carry things. Invites her to the vigil | "Isolde Marr is a careful woman. She is careless with one thing. You are standing in it." #plant:5.2 | `verdance.teodor.met` |
| 1/2 | Verdance.RootChapel | **Inkthread.** The solvent-line reversed: a line that holds instead of dissolves | "The Cantors draw a line to let go. Draw it the other way. It is the same line." #plant:5.6 | `verdance.teodor.thread` (with the ability) |
| 1/2 | Verdance.LanternGrove | **The vigil.** Sit with him. No choices. The grove's lanterns are for the eleven | Eleven names, in present tense, one per lantern. The player does nothing. It lasts as long as it lasts | `verdance.grove.vigil` |
| 1/2 | Verdance.SunkenLibrary | Ansel and page 214 | "He is reading. He has been reading for thirty-eight years. He is happy on that page. I will not turn it for you." #plant:5.1 | `verdance.library.teodor_asked` |
| 1/2 | Verdance.Aldermere | **The last day.** Attend, or stop it (6.6). Decision | Attend: "They asked. I made sure. Stay for the end, cartographer; it is the kindest thing you will see." Stop it: his voice drops; the Choir sings over him | `verdance.aldermere.attended` / `.stopped` |
| 2 | Verdance.QuietHouse | **The keystone.** He gives it if Wren can say why he does what he does; the check reads Aldermere's choices and her voices, not a stat | Right answer: "Yes. That is why. Take it; it is heavier than it looks." Wrong: "No. But you were close, and you were kind." | `teodor.keystone_given` / `teodor.refused` |
| 3 | Halden.Observatory | **The Unwritten.** If the keystones go to him: he dissolves them, one by one, naming their places in the present tense | "Hollowvein is. Aldermere is. Merrow's End is. Thessaly Hollow is. There. Now nothing is held." | `ending.unwritten` |
| 4 | Verdance.QuietHouse | Epilogue | Fixed: he has stopped speaking in the present tense. Open: the Quiet House is teaching the thread to Guild journeymen. Unwritten: he is walking Aldermere's bounds, alone, and it is holding | |

### The keystone check (NAR-08 will script it)
Not a stat. The check passes if the player attended Aldermere's last day *and*, in the keystone conversation,
picks the line that names what he made certain of (that they asked) rather than the one that praises him or
the one that pities him. Stopping Aldermere fails it outright; the Choir's defeat is not an argument.

### Sample
```yarn
title: QuietHouse_Teodor
---
Teodor: You are Isolde's. May I say a thing you will not like?
-> Say it.
    Teodor: She is a careful woman. She is careless with one thing. You are standing in it. #plant:5.2
-> Everyone says that first.
    Teodor: Then everyone is careful. Sit. The reeds have been telling me about you for a week.
-> ...
    Teodor: Good. The grove is quieter than that. You will do well there.
<<flag verdance.teodor.met 1>>
===
```

---

## 5. Marrow

**Who.** A grey chick, half-drawn, species unreadable; the outline suggests a wren and the game never confirms
it. Marrow is a child of the Blank: born there, after the fade, the way Wren was. Nobody in the game says this.
Marrow starts following Wren in Act 3 at the Lantern (7.3 step 2) and does not stop.

**What it wants.** To be counted. It has no other want, which is why it is the ending's witness.

**Image.** *Echoes.* Marrow speaks only in fragments other birds have already said, in the same scene, in
their words. What it chooses to echo is the scene's judgement: the one line that mattered.

**Voice** (echoes; gains one original word per act). Marrow's ladder, in this v1's reading of the bible: silent
through the prologue and Act 1; one echo in Act 2 (the Mirror Pool); echoes through Act 3, with one word of its
own on each island; and the ending's verdict, its last original word, which is the last thing NAR-13 writes.

**Secret it carries.** Which bird's chick it is. The game does not answer. Ilse does not recognise it; Corvin
does not draw it; it is not in the Great Atlas. That is the point: it is the first thing in Aurenne that was
never recorded, and it is fine.

**Never says.** A word that is not the scene's. Its original words are the exception, and there are four.

### Where and when

| Act | Zone | Scene | Says | Writes |
|---|---|---|---|---|
| 0 | Greyfold.HalfCathedral | The prologue's crossing: glimpsed in the white at the thirtieth step, silent, out of focus | Nothing | (no flag; the plant is visual, `#plant:5.3` on Isolde's line) |
| 2 | Greyfold.MirrorPool | A grey chick in the pool's reflection and not on the bank; one echo | Wren's own last chosen line, said back | `marrow.seen_in_pool` |
| 3 | Blank.ThessalyHollow | **Following.** From the Lantern on, it walks behind her; at the Hollow, Ilse does not recognise it | Echoes of Ilse's and Isolde's lines. First original word: `marrow.words` = 1 | `marrow.following`, `marrow.words` |
| 3 | Blank.OldCapital | Corra's room; Corvin's mirror-Observatory | Echoes of Corvin's speech, which makes his speech wrong in a way the player can hear. Second word | `marrow.words` = 2 |
| 3 | Greyfold.Threshold | The Return | Echoes Voss (changed or not). Third word | `marrow.words` = 3 |
| 4 | Blank.ThessalyHollow | **The verdict** (epilogue's last scene). Its last original word, or none | Fixed: bright, beautiful, no new word (it echoes "same"). Open: laughs, and says a word nobody has said. Unwritten: fades holding Wren's wing, unafraid; its last word is its third, repeated. Rest: not present | `marrow.words` = 4 |

### Staging rules
Marrow has no portrait, no name plate until Wren names it (the name plate reads "…" until then; the player can
type a name at Thessaly Hollow or not). It never blocks a path, never fights, cannot be hurt. It stands one pace
behind Wren and one pace to the left, and when Wren surveys, it stands on the vantage after she leaves it.
Its `_Ink` sits at fade stage 3 and rises one stage per original word.

### Sample (echo form)
```yarn
title: Capital_Marrow_Word
---
// Marrow's lines are copies. The writer marks the echo's source so localisation keeps them identical.
Corvin: When I— I held it. I held all of it. Nothing was lost.
Marrow: Nothing was lost. #echo:corvin
Corvin: You see? Even the child.
Marrow: Lost. #echo:corvin
<<flag marrow.words 2>>
===
```

---

## 6. Where they cross
- **Sable and Runa** never meet, but sing the same song: the whale's names are Runa's roll-call (3.4). Sable does
  not sing along; Runa, told of the whale in Act 2, says "then we're older than we thought."
- **Pell and Teodor** meet once if the report was not sent and Teodor allied: Pell reads the report to Teodor at
  the Quiet House, and Teodor says "she is careful with one thing" of Pell.
- **Marrow and everyone**: each of the other four gets one echo in Act 3 if they are present in the Blank.
  Sable's is "Boat's leaving." Runa's is a number. Teodor's is a name. Pell's is "accurate".
- **Isolde** is in every one of their stories (Pell's minder, Sable's first customer, Runa's mother's argument,
  Teodor's careless careful woman) and meets none of them on screen until Act 3.

## 7. What each of the five is for
| | Theme (bible §0) | Faction truth they carry | Faction rot they carry |
|---|---|---|---|
| Pell | Who decides what is remembered | The Guild records truly | The record is used |
| Sable | Home is a practice | The Blank can be crossed | She stopped counting who came back |
| Runa | Home is a practice | A place can be held by walking it | Only if everyone is known |
| Teodor | Preservation versus life | Letting go is love | Mercy not always asked for |
| Marrow | Flight is memory | (none) | (none): it is what was never recorded |

## 8. The rest of the returning cast (short form)

| Character | Species | Image | Appears | Sheet |
|---|---|---|---|---|
| **Isolde Marr** | (Wren's mentor) | Questions instead of answers; "journeyman" when proud, "Wren" when scared | Prologue (staged), the cache (Act 1), her Last Camp and the Hollow (Act 3), the epilogue | NAR-03, NAR-12 |
| **Halvard** | Guild heron, Warden-Sergeant | Paces and counts | The lighthouse (staged, Act 1), the Seven Bridges (Act 2, new kit), the Threshold beside Voss (Act 2) | NAR-06 §6.3 |
| **Dotha** | last elder of Merrow's End | Songs, counted down | Merrow's End (staged, Act 1); the island in the Blank if released (Act 3) | saltmarrow-arc.md |
| **Old Kettil** | capercaillie | Counts people; spits at "Guild" | Kettil's Rest, the Cinder Baths, Hollowvein | NAR-07 |
| **Idrenne** | crane, Speaker | Where she was standing when she learned it | The moving camp, the Wind Gate, her Fire; the ending's witness | NAR-10 |
| **Aurelian Voss** | grey heron, Guildmaster | Titles; "we" for the Guild; flinches at "Halloway" | His office by proxy (Act 2), the Threshold (one speech), the Return, the Observatory | NAR-11, NAR-06 §6.11 |
| **Corvin Halloway** | great owl, the Archivist | "When I—" | The Old Capital (Act 3) | NAR-12, NAR-06 §6.14 |
| **Aury** | cormorant, Sable's brother | Asks whether you've eaten | The third lighthouse (Act 2, by tether), his island (Act 3) | NAR-14 |
| **Ilse** | (Wren's mother, grey) | Lets go | Thessaly Hollow (Act 3) | NAR-12 |

## 9. Open
- Pell's species (a jackdaw) and pronouns (they) are this bible's decisions; CHR-11's v1 model sheet draws the
  jackdaw (`docs/art/pell-turnaround.png`). It also chose species the bible left open: Isolde a curlew, Dotha an
  oystercatcher (`docs/design/npc-animation.md` §2); each is one line to change.
- ~~Marrow's four original words.~~ Written: the first three at the Hollow (act3.md §5), the last at the frame
  ("Skywalk", "Look.", or none: endings.md).
- The bible says Marrow gains "one original word per act" but Marrow only speaks in Act 3; §5 reads this as
  one per island plus the verdict. If the bible meant Marrow should appear earlier and speak, the prologue and
  Mirror Pool glimpses become lines.
- ~~Teodor's tenth village: named here as his mother's; the bible does not say.~~ The bible says it now (story-bible.md §4,
  the Verdance, 2026-10-04).
- ~~Runa coming to Merrow's End in Act 2.~~ Written (2026-10-04, `Merrow_Runa`, §3). Decided with it: the whale keeps
  teaching the walk on the coast (hearing it under the bridge, `BoundsWalks.IsLearned`), because the slice holds
  Merrow's End that way and it is proven in play; Runa's visit is bible 4.1's "needs Emberdown" way for a Wren who
  never heard the whale, and for everyone else it is the village counted in. The whale's song is her roll-call either way.
- Sable's Act 2 voyage to Windreach by sea (world-map.md §6) would give her a fourth region; not written.
