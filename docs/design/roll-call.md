# The Roll-Call Sung (AUD-02, v1)

The game has one tune (audio-direction 3): Runa's roll-call, the Holdfast's way of holding a place by naming it.
Version one makes it sound. The notes are the direction's (`AudioDirection.RollCall`); a **voice** is a bird's
timbre; a **form** is the tune arranged for a use; a small synth in the game renders any form for any voices in any
key at any beat, and the same renders go out as WAV and MIDI for the composer to hear and replace.

## 1. What plays where

| Where | Who | Form | Key, beat | How it starts |
|---|---|---|---|---|
| Every bounds-walk | the walk's chorus (Merrow's End: Dotha alone) | the call on every beat; a miss falters; the answer at each verse's end | the walk's region; the call at the walk's beat, the answer at the region's | the walk itself (`BoundsWalk` events) |
| The Bone Bridge | the faded whale | whole, each note twice as long | Saltmarrow's D, at the Blank's beat (33 bpm) | drawing the Reedmother: the whale sings under the bridge 1.5 s later, far off |
| The Roll-Call Bell | Runa and the Holdfast | a verse: three names and the answer | Emberdown's G, 75 bpm | `<<sing runa>>` as she counts Wren in |
| Merrow's End, Dotha's Last Season | Dotha | whole, one voice | Saltmarrow's D, 67 bpm | `<<sing dotha>>` when she keeps the songs and sings them to the water |
| The Open World's chorus | everyone met, in order, Runa first, Isolde last | a verse with a name for every voice | the coast's D where it was first heard, at Runa's 75 bpm | `<<sing chorus>>` in `Observatory_Runa_Chorus` |
| The Blank's islands | the Remnant, three | reversed | D, at the Blank's beat | `<<sing blank>>` (AUD-08 places it) |
| The Archivist | Corvin | inverted about the reciting tone | Halden's C, 100 bpm | `<<sing archivist>>` (AUD-07 places it) |

Only the true ending has everyone, and its roster is not a list in the code but the world: `RollCallSong.Chorus`
adds a voice for each flag Runa's chorus scene checks, in her order (Sable, Dotha, Kettil, Teodor, Pell, Idrenne,
Ilse, Corra, Voss), and the test holds the table to the script. Wren has no voice and is never asked for one.

**The walk.** `BoundsWalk.NameCalled` (half a beat ahead) starts the call: the pickup for half a walk beat, the name
landing on the beat and held half a beat to the next call, one walk beat in all, so a verse is a continuous chant.
`BeatLanded` with a miss stops the name and plays it faltering: it breaks off after a third and slips a semitone
as it goes (the bounds-walk doc's open item: misses are the chorus faltering, not a mark). `VerseDone` (new) plays
the answer, fifth-third-second-tonic, at the **region's** beat: at Merrow's End that is four Saltmarrow beats,
exactly one walk beat, so the answer fills the gap before the next verse's first call, and it starts on the note
the last name held. A restart stops the caller. The walk's chorus is by walk id (`RollCallSong.WalkChorus`).

**Captions.** Every use shows one (`caption.sing.<use>`: "[far off, under the bridge: a song, slow, like names]"),
the direction's ask for deaf players.

## 2. The voices

`RollCallSong.Voice`: a transpose from the written octave (the tune is written for Runa's range, an octave above
the region's low tonic), two formants for the vowel, vibrato, breath, attack and release, a glide between notes.
The synth builds one period of the voice's spectrum per note (32 harmonics through the two formants over a 1/h
source, the faded losing their highs) and reads it with vibrato, an envelope and coloured breath; a chorus is each
voice rendered a few cents and up to 36 ms apart and summed under −1 dBTP.

| Voice | Bird | From Runa | Character |
|---|---|---|---|
| runa | capercaillie | 0 | warm, loud, the lead |
| kettil | old capercaillie | −7 | lower, wavering |
| dotha | oystercatcher | +5 | thin, high, dry |
| sable | cormorant | −5 | low, flat, no ornament |
| teodor | mourning dove | −3 | soft, cooing |
| pell | jackdaw | +4 | quick, bright |
| idrenne | crane | −9 | low and long |
| maren | swan | −2 | clean, exact |
| corvin | great owl | −12 | the lowest, hollow |
| voss | grey heron | −10 | low, precise |
| halvard | heron | −8 | |
| isolde | curlew | +2 | |
| ilse, aury, remnant | the faded | +7, −4, +1 | grey: breathier, dull, a little flat |
| corra, marrow | chicks | +12 | high, eager |
| family | the Holdfast, one of many | −1 | a stranger sings as one of these |
| whale | bones | −12 | a sine with a little second harmonic, a 0.9 s attack, a slow wide vibrato, four echoes and a low-pass; played at 0.45. Not lower: under about 60 Hz a laptop speaker carries nothing |

## 3. The pipeline

```
Unity.exe -batchmode -nographics -projectPath LastCartographer -executeMethod OWSBG.Setup.RollCallExport.Render -quit
```

(or **OWSBG → Render the Roll-Call**) writes `docs/audio/rollcall/`: one WAV and one MIDI per use, plus the
Merrow's End walk's call, falter and answer, named to the delivery spec (`saltmarrow_voice_rollcall-whale_33.wav`;
48 kHz, 24-bit mono, peaks at −1 dBTP). The MIDI is format 1 at 480 ticks a beat with the tempo and one track per
voice (choir aahs, the whale on voice oohs), transposed as the voice sings it: the hand-off for a real score.

In the game nothing is loaded: `RollCallSinger` (Narrative; wakes with the game like the mix) renders a clip the
first time a form is asked for in a key, at a beat, for a set of voices, and keeps it. Two sources, the caller (the
walk's held name, so a miss can cut it) and the voices (one-shots), both at the **Dialogue bus's gain** each frame,
so the snapshots, the ducks and the player's Voices volume all apply (the mix doc's rule: the roll-call's names are
voices).

## 4. Reworking by hand

- **A recorded roll-call** replaces the synth per use: give `RollCallSinger` a clip for a use's key and it will play
  that instead of rendering (v1 has the hook in `Clip`: a cached clip by key). The keys are `use:form:key:beat:names:voices`.
- **A voice** is a row in `RollCallSong`'s table; the composer's transposes are the MIDI's.
- **The tune itself** is `AudioDirection.RollCall`: change the notes there and every form, file and test follows.

## 5. Verification

- `RollCallSongTests` (EditMode, 7): the synth sings the phrase in tune (each note's pitch heard by autocorrelation
  within 0.6 semitones); a chick is an octave up, the owl down and darker, the whale two octaves down with nothing
  above 400 Hz and twice as long at the Blank's beat; the forms are the direction's forms and the walk's call is one
  walk beat; the true ending's roster is exactly the flags Runa's scene checks in her order, Wren never, Dotha alone
  at Merrow's End, the scenes ask for the song; a chorus sits at −1 dBTP, a faltered name has broken off by its
  middle and slips flat; the WAV and MIDI headers are the spec; the deliverables are rendered and named to it.
- `RollCallSingerTests` (PlayMode, 4): a walk's first name is sung as it is called and the call is a walk beat long;
  the beat landing without Wren falters; on the post the name holds and the verse's end is answered at the region's
  beat; the uses play with captions, are rendered once and kept, the chorus grows with the flags; drawing the
  Reedmother brings the whale after its delay and no other vantage does; the voices' volume is the Dialogue bus's,
  to nothing when paused and back.

## 6. Open

- **It is a synth.** Formant voices sing the notes; they do not sing words. The names in the walk ("Dotha's stoop")
  are the caption strip's; the composer's recordings will carry them.
- **The Bone Bridge** is built (ENV-03): drawing the whale's bones there sings it too (`RollCallSinger.WhaleVantages`).
  A looped, distant version under the boards is still to do.
- **Kettil's Rest and Hollowvein** are unbuilt; their walks will sing Runa, Kettil and the families through the
  same events, Hollowvein one voice fewer each verse (a per-verse chorus, not yet in `WalkChorus`).
- **The Complete Survey and the Blank** are AUD-07's and AUD-08's: `Form.Call` at each phase's beat and the reversed
  use exist; nothing calls them.
- **Loudness** is by peak, not LUFS; the delivery spec's −20 LUFS for voices wants a meter when a DAW is in the loop.
