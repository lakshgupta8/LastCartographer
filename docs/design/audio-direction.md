# Audio direction (AUD-01, v1)

This is the brief for everyone who will write or record sound for the game: what each region sounds like, the
one tune the whole game is built on, and the rules that keep a fight readable by ear. It is for the composer and
the sound designer first, and it is binding on the code: the numbers live in `OWSBG.Core.AudioDirection`, and the
tests hold the game's rhythms, the mix (`docs/design/audio-mix.md`) and the telegraph floors (`docs/design/tuning.md`)
to it.

## 1. What the game sounds like

The world is ink on paper and it is forgetting itself. The sound follows three rules:

- **Places are sung, not scored.** The world holds itself together by naming things aloud (bible 3.4). Music comes
  from people as much as from the score: a work-song, a roll-call, a fisher's concertina, a Cantor's bell.
- **Forgetting is subtraction.** A fading place loses sound before it loses colour: the ambience thins a layer a
  stage, and what is left is dulled (§5). The Blank is not silent: it is everything remembered wrong.
- **A fight is read by eye; the ear confirms it.** Every attack has a visual telegraph first. The sound of a tell
  arrives with it, never before and never alone, so a player who can't hear loses nothing but comfort (§4).

## 2. The regions

Each region has a **beat**, and it is not only the composer's. Everything rhythmic in the region sits on whole
beats of it: its smudges flicker a beat drawn and a beat undrawn (combat doc 5, "their rhythm is the region's music
tempo"), its bounds-walks call a name every few beats, and the rhythm bosses fight to it. The Complete Survey
(boss 6.15) fought in three phases named "Saltmarrow's tide", "Emberdown's ash" and "Halden's late afternoon"
before this table existed, and its three beats set the table's spine.

| Region | Mood (bible 4) | Beat | BPM | Mode | Silence | Lead instruments |
|---|---|---|---|---|---|---|
| Saltmarrow | wet, patient, superstitious | 0.9 s | 67 | D Dorian | 20% | hardanger fiddle, low whistle, tongue drum, concertina, bowed psaltery |
| Emberdown | stubborn, loud, communal | 0.8 s | 75 | G Mixolydian | 10% | work-song chorus, hurdy-gurdy, frame drum, anvil, tuba |
| The Verdance | reverent, near-silent, unsettling | 1.2 s | 50 | E Phrygian | 70% | viola da gamba harmonics, bowed glass, a root-chapel organ pedal, Cantor handbells |
| Halden Reach | orderly, handsome, wrong | 0.6 s | 100 | C major, never cadences | 15% | harpsichord, string quartet, music box, Guild brass |
| Windreach | free, lonely, hospitable | 1.0 s | 60 | A pentatonic | 35% | long flute, cittern, overtone voice, wind harp, a hand drum at the fire |
| The Greyfold | dread, then awe | 1.5 s | 40 | no mode: one held tone | 60% | a held sine and bowed cymbal, the Half-Cathedral's bells, a low choir at the Threshold only |
| The Blank | the faded are not dead | 1.8 s | 33 | the roll-call's own, reversed | 40% | reversed piano, celesta, Remnant voices, the roll-call reversed |

**Silence** is the share of a region's time the music bus is empty by design, with the ambience carrying the
room. It is a target for the composer's loops and for the music system's rests, not a gap to fill.

What each region's music is for, in one line:

- **Saltmarrow.** The tide keeps the time: a drone that swells and draws back, and a fiddle that waits for it.
- **Emberdown.** A work-song for many voices: everyone sings, nobody solos, the anvil keeps the count.
- **The Verdance.** Mostly nothing. When music comes it is one bowed voice in a very large room, and it stops
  before it resolves.
- **Halden Reach.** A handsome clockwork piece that loops a bar it never finishes: the Stillness (bible 5.1), heard.
- **Windreach.** Open air: a long flute and a plucked string over the wind, and the camp's drum when the fire is lit.
- **The Greyfold.** A single held tone that frays at the edges. At the Threshold, a choir arrives all at once.
- **The Blank.** Everything the player has heard, remembered wrong: motifs reversed, the Remnant's voices under them.

**Faded things sing at half speed.** The Blank's beat is Saltmarrow's doubled. The faded whale on the Bone Bridge
sings Runa's roll-call at it (bible 8.1, [F 3.4]), so the first time a player hears the tune it is already
slowed and far away. They hear it whole in Emberdown and recognise it.

## 3. The roll-call

The game has one tune. It is Runa's roll-call (bible 3.4): the Holdfast's way of holding a place by naming it.

- **The call:** the caller lifts into each name half a beat ahead, from the sixth down to the fifth. The name
  lands on the beat, on the fifth, the reciting tone. This is exactly the bounds-walk's call, a name half a beat
  ahead of the beat it must be walked on (`BoundsWalk.CallAhead`, `docs/design/bounds-walk.md`).
- **The answer:** when a verse ends, the chorus answers down the scale, fifth, third, second, tonic, and holds.
- It sits within an octave, so anyone can sing it. Every bird in the game can.

In semitones above the region's tonic, with lengths in beats: `9:½ | 7:1 | 7:½ 4:½ 2:1 0:2`. Its forms are made by
the same four operations (`AudioDirection.RollCall`: whole, reversed, augmented, inverted):

| Where | Form | Who sings |
|---|---|---|
| Merrow's End, Dotha's Last Season | whole, one voice, nine songs short | Dotha |
| The Bone Bridge [F 3.4] | augmented ×2, at the Blank's beat | the faded whale |
| The Roll-Call Bell, Emberdown | whole, at Emberdown's beat | Runa and the Holdfast |
| Every bounds-walk | the call on every beat, the answer at each verse's end | the walk's chorus |
| Hollowvein, the long roll-call (boss 6.4) | whole, one voice fewer each verse | the families |
| The Blank's islands | reversed | the Remnant |
| The Archivist (boss 6.14) | inverted about the reciting tone | Corvin |
| The Complete Survey (boss 6.15) | the call alone, at each phase's region beat | the chorus keeping the beat |
| The Open World (bible 9.2) | whole, every voice she has met | everyone |

**Only the true ending has everyone.** Every other use is short of voices, backwards, slowed or pulled the other
way. The Open World's chorus is the first time the tune is heard as it should be, and it should be built from the
voices the player met: the roster of who sings is the roster of who is alive and allied.

Regional themes may quote the roll-call; the Verdance's never does. The Unwriters don't sing names.

## 4. Combat

**Every attack has a tell,** named for its kind (`AttackKind` in the boss code, `AudioDirection.Tell`):

| Tell | For | Sound | At most |
|---|---|---|---|
| Strike | a hit that takes one mask | a short, high scratch of the pen | 6 frames |
| Slam | a hit that takes two (CMB-19) | a low drawn breath, the only low tell, so it is never mistaken | 8 frames |
| Window | an opening to strike into | a chime as it opens; the opening itself is silent | 4 frames |
| Shape | a hazard drawn across the floor | a swell that follows the shape | 8 frames |

A tell starts on the telegraph's first frame and is over before the fastest read it could announce (tier IV's
floor, 8 frames): the ear hears "now" while the eye still has the whole read. No tell may say anything the
telegraph doesn't show.

**The mix keeps the tells.** The rules below are held by test against `Mix`:
- Tells ride the Sfx bus. No snapshot but Paused leaves it under 80%, and no duck lowers it at all.
- A fight never talks over a voice: only a spoken line's own duck touches the Dialogue bus.
- When too much sounds at once, voices are kept in this order and dropped from the bottom: telegraph tells, Wren
  hurt, dialogue, Wren's strikes and Bind, enemy hits and deaths, world one-shots, ambience, music. At most 24
  one-shots at once.

**Boss music** starts on the fight, not the room: the room's ambience carries the approach, and the theme arrives
with the first telegraph. A phase change adds or changes a layer on the next bar line rather than cutting to a
new track. A beaten boss's theme resolves on the roll-call's answer, in the boss's region's key. The aftermath is
the Boss snapshot's slow release (2.5 s). The rhythm bosses (the Collapse, the Complete Survey) are scored to the
beat the fight keeps, so the music is the timing aid a hearing player gets and the lamps are the one everyone gets.

**Wren's own sounds:** her quill is ink and paper, never metal. A strike is a pen stroke, a pogo a nib's tap, a
Bind a word being written, a dash a page turned fast. Hurt is a smudge, not a cry. Her voice is never heard in
combat.

## 5. Fading

A region's ambience is written as layers, most present first. Each fade stage takes the last layer away, and
the Mix's low-pass dulls what's left (`Mix.StageCutoff`: open, 9 kHz, 3.5 kHz, 1.2 kHz). At least one layer stays
until the place is erased (stage 4), and an erased place is silent. The music is never the place's: a region's
theme plays the same in a fading room, which is the point.

Saltmarrow's layers, for example, go: tide on the pilings, reeds, rain on the boardwalk, gulls far off, a bell
buoy. A stage-3 Merrow's End has the tide and the reeds, under a low-pass.

## 6. Delivery

For AUD-02 onward:
- **Music** as stems (lead, bed, pulse, voices at least), looping on whole bars at the region's beat, with the
  loop point marked. Integrated loudness about −18 LUFS per stem mix.
- **Ambience** as separate layers in the region's order, each looping independently, about −24 LUFS.
- **Dialogue** barks and the roll-call voices at about −20 LUFS; there is no voiced dialogue beyond them.
- **Every one-shot** peaks under −1 dBTP.
- Files are named `<region>_<kind>_<name>_<bpm>.wav` (`saltmarrow_music_tide-bed_67.wav`), 48 kHz, 24-bit.
- What each row delivers: AUD-02 the roll-call in all its forms (§3); AUD-03 Wren's sounds (§4); AUD-04 to AUD-07
  the region and boss themes (§2, §4); AUD-05 the ambience layers (§5); AUD-08 the Blank and the endings.

## 7. Tests

`AudioDirectionTests` (EditMode, 5):
- every region scored to the bible's mood line, the Verdance the most silent, Halden's clock the fastest;
- every rhythm in the game on its region's beat: the Complete Survey's three phases are the three regions its inks
  name, the Collapse keeps Emberdown's, the Merrow's End walk (as its scene sets it) and every smudge's flicker;
- the roll-call: the pickup is the walk's call-ahead, whole beats after it, home to the tonic, within an octave,
  its forms undo themselves, the whale's slowing is the Blank's beat over Saltmarrow's, only the true ending has
  everyone;
- the combat rules against the mix and the floors: a tell for every attack kind by name, none longer than the
  fastest read, the Sfx bus kept by every snapshot and never ducked, a fight never ducking a voice;
- a fade takes the ambience a layer a stage and an erased place is silent.

`SmudgeTempoTests` (PlayMode, 1): a smudge in an Emberdown room flickers a beat drawn and a beat undrawn at
Emberdown's beat; one on no region keeps its own.

## 8. What changed in play

Four numbers moved to sit on their region's beat:

| What | Was | Now | Why |
|---|---|---|---|
| Smudge flicker in a region's room | 0.9 s drawn, 0.7 s undrawn | a beat each (Saltmarrow 0.9 / 0.9) | combat doc 5 |
| Complete Survey, Emberdown's ash | 0.75 s | 0.8 s | Emberdown's beat |
| Merrow's End bounds-walk | 3.5 s a beat | 3.6 s, four Saltmarrow beats | on the grid |
| Hollowvein walk (planned, bounds-walk doc) | 3.6 s | 3.2 s, four Emberdown beats | on the grid |

Smudges elsewhere now differ by region: quicker in Halden (0.6 s each), slower in the Greyfold (1.5 s). They
want the feel-test's eye.

## 9. Open

- **The roll-call is the first sound** (AUD-02, `docs/design/roll-call.md`): sung by an in-engine synth in every
  walk, under the Bone Bridge, at the Bell and in the ending's chorus. **Wren's sounds and the four tells** are the
  second (AUD-03, `docs/design/wren-sounds.md`), made from ink and paper in code. **The coast's theme and the
  Lamp-Keeper's** are the third (AUD-04, `docs/design/music.md`), with the music system under them. AUD-05 to
  AUD-08 are the rest; the mixer (AUD-09) is running.
- **The music system** is built (AUD-04, `MusicDriver`): stems in step, layer changes on the bar line, the rests
  baked into each region theme's loop at its silence share. It clocks from the beat table.
- **Smudges in the Blank's islands** follow the Blank's slow beat (1.8 s each). Whether that's too easy wants a
  playtest.
- **Accessibility:** captions for tells ("[a low breath]") would give deaf players the ear's "now" as text. The
  captions system (DES-14) can take them when the tells exist; the roll-call's uses already caption themselves.
