# Boss Character Sheets (NAR-06, v1)

Bosses are characters (bible §6). Each sheet: why the fight happens, what the arena does across three phases,
the three lines (on entry, at the turn, and the last, spoken as the final phase begins; each under twelve words,
style guide §6), the answers the kit asks for (combat doc §7–8), and the aftermath. Fifteen bosses, seventeen
fights: Halvard is fought three times.

The sheets are data too: `Bosses` (Core) carries id, number, zone, tier, optional, the three lines, what the
fight grants, and the flag it sets (`boss.<id>.defeated`). `BossSheetTests` prove the counts, the twelve-word
rule, that every fight stands on a `WorldGraph` zone that names it, and the tiers and options against the bible.
The staged Lamp-Keeper reads her name, tier and lines from the sheet when the room is built, and
`BossSheetPlayTests` checks the built room against the data.

Tiers: **I** early, **II** mid, **III** late, **IV** endgame. Telegraphs 12+ frames at I, 8+ at IV. No more than
four attacks per phase. A desk or wax seal within fifteen seconds of every door.

---

## 6.1 The Lamp-Keeper — Saltmarrow, the Lantern Chain (I)
**A Remnant gannet fused to the fourth lighthouse's lamp mechanism.** Grey, half-drawn, her wings are the
lamp's shutters. Staged (`Greybox_Saltmarrow_Lighthouse`).

**Why.** She will not let the light go out. The light is the last thing she remembers, and it is lit for a
flock that is not coming (plant 5.6).

**Arena.** The lamp room under the perch, doors sealed. Phase 1: the beam sweeps low and slow; she dives
through it. Phase 2: the lamp turns faster and splits; the floor outside the beam dims to paper, so the beam is
the only place to stand. Phase 3: the lamp gutters; she perches on it and dives with no wind-up, but the beam
lies along her line a beat before.

**Lines.**
1. "The light stays."
2. "I remember the light. I remember nothing else."
3. "If it goes out, I go with it."

**Answers.** Pogo the dive; stand in the sweep's shadow; strike the perch between beams.

**Aftermath.** Wingbeat ("she remembered one beat"). The lamp lights and becomes a travel beacon
(`Saltmarrow_Lighthouse/Lamp`); the housing's inscription can be read; Halvard walks in. One vellum scrap.

## 6.2 Reedmother's Brood [optional] — Saltmarrow, the Pale Iris Fields (I)
**A swarm of half-drawn marsh chicks around a giant reed-nest.** The nest is the boss; the chicks are the
weather.

**Why.** The Guild's agents have set the iris field alight (the Iris Harvest). The Brood defends the beds and is
mad with the smoke; it fights anything that comes near the fire, and the fire is what Wren is trying to reach.

**Arena.** The nest in the burning beds. Phase 1: chicks in waves, the nest shut. Phase 2: the nest opens to call
them home; its shell can be struck; smoke narrows the room from the east. Phase 3: the fire reaches the nest; the
chicks stop attacking and huddle on it; the player chooses what to strike.

**Lines** (the chicks, in chorus; the Reedmother has no voice).
1. "Ours. Ours. Ours."
2. "Burning. Burning. Ours."
3. "...ours?"

**Answers.** Blot the swarms; pogo the shell; stamp the fire out with the plumb weight.

**Aftermath.** Killing the nest burns the field: Ferrymen prices rise by half (`saltmarrow.iris_burned`).
Stamping the fire with the nest alive keeps the beds; the commission's grower pays. Either way: the Tether-hook.

## 6.3 Warden-Sergeant Halvard — recurring, three fights (I → II → III)
**A Guild heron: brass gorget, sighting-lance; long reach, lunges, and a "survey" attack that marks the floor.**
His image is paces and counts; his lines count.

**Why.** He hunts the unlicensed cartographer. He is not cruel; he is exact. He has measured her reach at every
meeting and says so.

### First hunt — the Salt Chapel (I)
The measuring is staged in the fourth lighthouse (`Lighthouse_Halvard_Hunt`); the fight is the chapel's, one room
east past the tide gap, and it is built (`Halvard`, CMB-12). **Arena.** The chapel's salt floor: his marks stay for the whole fight and erupt when he calls the
count. Phase 1: reach and lunge. Phase 2: he surveys three squares; stand elsewhere. Phase 3: the whole floor
is marked but one pace.
1. "Three paces. I measured them."
2. "You were taught by the best. So was I."
3. "Noted. Halden will hear the count."
**Answers.** Parry the lance; pogo over the lunge; read the marks. **Aftermath.** He withdraws; she stays
unlicensed; the Wardens in anchored towns know her (`act1.unlicensed`).

### Second hunt — Halden, the Seven Bridges (II)
New kit: a second lance, thrown and recalled on a cord; he cuts bridge sections. **Arena.** A bridge over the
drop; sections fall as he marks them; Inkthread or Talonhold to recover. Phase 3 is fought on the last span.
1. "Four paces, now. I said I'd measured you."
2. "The bridge is Guild property. So is the fall."
3. "Entered. Twice. There is no third book."
**Aftermath.** He leaves the span standing. If Pell's report has been sent, Oriel takes over the hunt (6.8).

### Third — the Threshold, beside Voss (III)
He fights before Voss's phase, at the white edge, with everything: both lances, the marks, the count. Halfway
through he stops counting. **Arena.** The Threshold's edge, the Blank eating the floor from the west.
1. "The count ends here, journeyman."
2. "He's going in. I am not. Understand me."
3. "Go. I'll enter it as a survey."
**Aftermath.** He holds the Wardens back while she crosses. "Survey", not "count", is the change.

## 6.4 The Collapse — Emberdown, Hollowvein (II)
**A smudge-beast: the mine collapse itself, ink, rubble and lantern-light.**

**Why.** Bounds-walking into Hollowvein wakes what buried it. Runa leads the chorus down; the fight is the last
verse.

**Arena.** The bottom of the mine, dark. The walk's beat keeps going through the fight: on each beat a chorus lamp
lights one section, and only lit sections are drawn enough to strike (the Smudge rule). Phase 1: rubble falls
where the dust rises. Phase 2: ink surges along the floor between lamps. Phase 3: it reaches for the lamps; if
one goes out, that beat is lost.

**Lines** (it speaks the roll-call wrong).
1. "Thirty. Thirty. Thirty."
2. "Who counts the ones under?"
3. "Thirty-one. Say it. Say it."

**Answers.** Pogo the rubble; Longstroke the surge; strike on the beat.

**Aftermath.** The families recover the dead; Runa counts thirty-one and, for the first time, says the number.
Keystone (the second). `emberdown.hollowvein.walked`.

## 6.5 Cinder Warden Brann — Emberdown, the Furnace Stair (II)
**A Guild crane in furnace-blackened brass, twin lances.** The arena is a cooling furnace.

**Why.** Sent to anchor Kettil's Rest by force, with the survey already written. He believes it is a kindness.

**Arena.** The furnace floor, red-hot in sections that cool as the fight goes. Phase 1: stand on the cool; one
lance. Phase 2: both lances, cross-cuts; the floor half cool. Phase 3: the furnace is dark and only his brass
glows; his telegraphs are the glow.

**Lines.**
1. "Kettil's Rest is scheduled. I am the schedule."
2. "You'd let them fade to spite the Guild?"
3. "Cold. It's cold. Tell her it's cold."

**Answers.** Parry the lance; pogo the cross-cut; Longstroke the line he holds when both lances are out.

**Aftermath.** Kettil's Rest stays free. Kettil counts him out at the Bell that night ("one, gone"). Three scraps.

## 6.6 The Choir [optional] — the Verdance, Aldermere (II)
**Three Cantor doves in unison; bells that erase parts of the arena, and of Wren's map, as they ring.**

**Why.** Only if Wren tries to stop Aldermere's last day. The village asked to be let go. The Choir sings it out.

**Arena.** The village square on its last evening. Each bell erases a platform and a vantage of Aldermere. Phase
1: three doves ring in turn. Phase 2: two, in canon. Phase 3: one dove, singing alone, the square mostly white.

**Lines.**
1. "They asked. Let them go."
2. "Which of us are you saving them from?"
3. "Then it's held. Ask them if they're glad."

**Answers.** Hit a dove during its ring to cancel it (the Cantor rule); Longstroke the line they stand in;
re-survey between verses.

**Aftermath.** Aldermere is held against its wish. Teodor will not give the keystone. The Unwriter's Charter, from
the last dove's wool.

## 6.7 The Gatekeeper — the Verdance, the Overgrown Gate (II)
**A flying-age statue of a great eagle, roots for wings.**

**Why.** It guards the canopy road to the Plateau. It was built to admit the winged, and nobody has been winged
for forty-one years.

**Arena.** The gate. The roots are Inkthread anchors. Phase 1: wing sweeps; stone feathers fall. Phase 2: it rises
and the fight goes vertical on the threads. Phase 3: the roots tear free and it flies, badly, for the first time
since the Grounding; heavy, wrong, and open underneath.

**Lines** (it speaks in the flying age's register).
1. "Passage is for the winged."
2. "You climb. We flew. Which is stranger?"
3. "Ah. That is how it went. I had forgotten." #plant:5.6

**Answers.** Thread to the roots; pogo the feathers; strike the belly on the pass.

**Aftermath.** The road to Halden opens. The statue lies with its wings open; the fledglings in the background
glide one span farther. Two scraps.

## 6.8 Warden-Captain Oriel — Halden, the Bastion (III)
**A Guild egret, the Watch's best: fast, clean, mirrors Wren's own kit.**

**Why.** Interlude A, if Pell's report is sent. She has read it. She fights to see if it is accurate.

**Arena.** The Bastion's drill-yard, chalk-lined. Phase 1: she uses Wren's Charter's combo, reversed. Phase 2:
she uses Wren's default Flourish. Phase 3: she Binds once, at a third; the fight is about denying it.

**Lines.**
1. "Pell writes well. Show me the rest."
2. "That's my stance. Where did you learn it?"
3. "Accurate. All of it. Go before I read it twice."

**Answers.** Whatever Wren's Charter asks; parry the mirror; interrupt the Bind.

**Aftermath.** Sets the Guild's stance for Act 3. Beaten without a mask lost, she stands the Wardens down in
Halden (`halden.oriel.stood_down`); otherwise they hunt until the Threshold.

## 6.9 Surveyor Hale [optional] — Windreach, the Nine Stones (III)
**A Guild surveyor with a quill of his own. The only duel in the game.**

**Why.** He is finishing a secret survey of the Steppe. Nine stones sighted and the Guild can anchor Windreach.

**Arena.** The Nine Stones at dusk. He surveys during the fight: each stone he sights becomes his (a hazard that
strikes when he calls it). Wren can survey a stone first to deny it. Phase 2 begins at five stones, phase 3 at
eight, whoever's they are.

**Lines.**
1. "Journeyman. Professional courtesy: I'll finish first."
2. "Eight stones. One more and it's theirs."
3. "Nine. No. Tear it. Tear it, I can't."

**Answers.** Survey faster than him; parry the quill; pogo the stones' strikes.

**Aftermath.** Windreach stays unanchored. His pages: burn them (Idrenne asks) or keep them (scraps, and a
`#plant` for 5.5: the Guildmaster's signature on a survey he never made). Hale's lens: the sighting lens's
cooldown halves.

## 6.10 The Fallen Star [optional] — Windreach, the Fallen Star (III)
**An iron meteorite-golem, woken when the keystone is lifted from it.** The clans have used it as an anvil.

**Why.** Idrenne's laughing "no cost" has one cost after all.

**Arena.** The anvil-crater. Magnetic: the quill is pulled toward it, so strikes drift. Phase 1: it walks and
slams. Phase 2: it draws iron up from the ground into walls. Phase 3: it burns, and the heat makes updrafts
(Windmemory) that are the only way over the walls.

**Lines** (it has no voice; Idrenne, from the rim).
1. "There it is. The cost. Mind the iron."
2. "Forty years our anvil. It's owed a swing."
3. "Ha. Told you. No cost. Well. One."

**Answers.** Windmemory over the walls; pogo the slam; strike the seam where the keystone sat.

**Aftermath.** Idrenne still laughs. The keystone. The anvil is cold and the smith complains for the rest of the
game. Two scraps.

## 6.11 Guildmaster Aurelian Voss — the Greyfold, the Threshold (III)
**A great grey heron in full Guild brass: sighting-lance, compass-rose shield.** In his second phase he anchors
the arena.

**Why.** He will not let Wren cross. He has never crossed himself (plant 5.5) and cannot say why.

**Arena.** The Threshold, the white edge. Phase 1: lance and shield, formal, textbook. Phase 2: he anchors:
sections of the arena freeze mid-air, platforms lock, the colour grade locks; a section sealed with Wren inside
holds her for a beat. Phase 3: the Blank eats the frozen sections from the west and he fights from a shrinking
island, still holding.

**Lines** (he gets one speech in the game and it is before this; these are not it).
1. "Journeyman Halloway. This is as far as the map goes."
2. "Hold. Everything holds. Do you see? Nothing is lost."
3. "Take it in. Tell her— no. Nothing. Go."

**Answers.** Parry the lance; break the seal by striking its edge; stay off the west.

**Aftermath.** Wren crosses; Halvard holds the Wardens. Voss is changed in Act 3 only if Wren carries Corra's
memory out (`blank.corra.carried`); otherwise a statue stands at the Observatory (9.5).

## 6.12 The Half-Cathedral Bells — the Greyfold, the Half-Cathedral (III)
**An environmental boss: a faded cathedral whose bells erase Wren's lantern-radius.** The prologue's site.

**Why.** The Road That Stops runs through the nave. It is in the way, and it is the first thing Wren surveyed.

**Arena.** The nave, white. Bells ring in sequence; each ring shrinks the radius Wren can see. Between rings, find
the bell-rope by its outline in peripheral vision and cut it (Inkthread). Phase 1: one bell. Phase 2: two, in
canon. Phase 3: the great bell, whose rope is in the white.

**Lines** (the inscriptions on the bells, read as each is silenced).
1. "For the flock, going north."
2. "For the ones who stayed."
3. "For whoever reads this. We did not forget you."

**Answers.** Thread the ropes; survey the vantage Isolde taught her to steady the radius; Field lantern.

**Aftermath.** Clarity grows; the Road That Stops continues past the nave. No scraps; a bound memory instead
(the prologue's, if the player kept it).

## 6.13 Corra's Drawing — the Blank, the Old Capital District (IV)
**A child's drawing of her father, huge, crayon-lined, wrong.**

**Why.** It guards the Old Capital District. Corra made it to keep him; it has kept everyone out for forty-one
years, including him.

**Arena.** A white room with a crayon floor. The drawing redraws its limbs as they are struck (only drawn frames
can be hit). Phase 2: it draws a second Voss, smaller, holding its hand. Phase 3: the crayon runs out and it
fights in outline, faster, and the room's colour goes with it.

**Lines** (Corra, unseen, drawing).
1. "That's my father. He's coming. Don't."
2. "I drew him bigger. He was bigger."
3. "Is that him? Behind you? Is he here?"

**Answers.** Strike the drawn frames; Remnant Charter drains its colour; do not strike the small one.

**Aftermath.** Corra's memory can be carried out (`blank.corra.carried`); the district opens; the small drawing
stays.

## 6.14 The Archivist (Corvin Halloway) — the Blank, the Old Capital District (IV)
**An enormous half-drawn owl holding the seventh keystone.** The arena is the mirror-Observatory; he draws
obstacles into existence.

**Why.** He wants Wren to finish the Complete Survey. He has waited forty-one years for someone of both worlds.

**Arena.** The mirror-Observatory. Phase 1: he draws walls and floors; they are real while his quill is on them.
Phase 2: he draws Wren, and her drawing fights beside him with her kit. Phase 3: he draws the Atlas frame around
the arena and stops drawing, and holds, and the frame closes by a wingspan a beat.

**Lines.**
1. "When I— granddaughter. Sit. I have drawn you a chair." #plant:5.4
2. "I held all of it. Nothing was lost. Look."
3. "Then draw it better than I did. Go on."

**Answers.** Strike his quill hand to unmake the drawing; Longstroke the frame's edge; ignore her drawing.

**Aftermath.** The endings branch (§9). He offers the seventh keystone, and the Rest (9.4). Reveals 5.4 and 5.6.

## 6.15 The Complete Survey — Halden, the Observatory (IV, true ending only)
**The Great Atlas itself, trying to draw Wren into the page.**

**Why.** Reassembling it on Wren's terms: the keystones go back to their places, and the page does not agree.

**Arena.** The Observatory floor as the page. Each phase is a region's ink trying to fix her where she stands:
Saltmarrow's tide, Emberdown's ash, Halden's late afternoon. The chorus (Runa, and everyone met) sings the
roll-call and the beat is the only safe rhythm: be on the named ground on the beat (the bounds-walk, at speed).

**Lines** (the Atlas speaks in every voice she has heard; the echoes are Marrow's device).
1. "Stand still. Everything that stood still is kept."
2. "Wren Halloway, journeyman. Drawn. Stay drawn."
3. "Unheld. Un— oh. Oh. That's how it goes."

**Answers.** Walk the bounds on the beat; strike the ink where it pools; Bind between verses.

**Aftermath.** The Open World. Wren flies, once, briefly (the Sky).

---

## Summary

| # | Boss | Zone | Tier | Opt | Grants | Flag |
|---|---|---|---|---|---|---|
| 6.1 | The Lamp-Keeper | Saltmarrow.LanternChain | I | | Wingbeat, beacon | `boss.lamp_keeper.defeated` |
| 6.2 | Reedmother's Brood | Saltmarrow.IrisFields | I | opt | Tether-hook; prices | `boss.reedmother_brood.defeated` |
| 6.3 | Halvard, first | Saltmarrow.SaltChapel | I | | | `boss.halvard.defeated` |
| 6.3 | Halvard, second | Halden.SevenBridges | II | | | `boss.halvard_2.defeated` |
| 6.3 | Halvard, third | Greyfold.Threshold | III | | | `boss.halvard_3.defeated` |
| 6.4 | The Collapse | Emberdown.Hollowvein | II | | keystone | `boss.collapse.defeated` |
| 6.5 | Cinder Warden Brann | Emberdown.FurnaceStair | II | | | `boss.brann.defeated` |
| 6.6 | The Choir | Verdance.Aldermere | II | opt | Unwriter's Charter | `boss.choir.defeated` |
| 6.7 | The Gatekeeper | Verdance.OvergrownGate | II | | the road | `boss.gatekeeper.defeated` |
| 6.8 | Warden-Captain Oriel | Halden.Bastion | III | | the Guild's stance | `boss.oriel.defeated` |
| 6.9 | Surveyor Hale | Windreach.NineStones | III | opt | Hale's lens | `boss.hale.defeated` |
| 6.10 | The Fallen Star | Windreach.FallenStar | III | opt | keystone's cost | `boss.fallen_star.defeated` |
| 6.11 | Aurelian Voss | Greyfold.Threshold | III | | the crossing | `boss.voss.defeated` |
| 6.12 | The Half-Cathedral Bells | Greyfold.HalfCathedral | III | | Clarity grows | `boss.bells.defeated` |
| 6.13 | Corra's Drawing | Blank.OldCapital | IV | | Corra's memory | `boss.corras_drawing.defeated` |
| 6.14 | The Archivist | Blank.OldCapital | IV | | keystone; endings | `boss.archivist.defeated` |
| 6.15 | The Complete Survey | Halden.Observatory | IV | | the Sky | `boss.complete_survey.defeated` |

## Open
- The Salt Chapel fight (6.3 first) follows the lighthouse measuring by one room; the bible has the Guild find
  her "first" at step 3, and v1 reads the measuring as that finding and the chapel as the fight.
- 6.10's lines are Idrenne's and 6.12's are inscriptions; the twelve-word rule is applied to them all the same.
- Oriel's stand-down condition (no mask lost) needs a test in play; it may be too hard at Tier III.
- Kits are sketches. CMB-12 to CMB-16 own the frame data; these sheets own the reason and the words.
