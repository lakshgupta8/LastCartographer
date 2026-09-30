# Ambience (AUD-05, v1)

Every region's ambience is written as layers, most present first (audio-direction 5); each fade stage takes the
last layer away and the mix dulls what is left; at least one layer stays until the place is erased, and an erased
place is silent. Version one makes every layer the direction names, for every region, from a recipe in code, and
plays them under the mix.

## 1. The layers

`Ambience` (Core) turns each name in `AudioDirection.AmbienceLayers` into a **recipe** with three numbers, and
renders it to its own loop. The loops are 11, 13, 17, 19 and 23 seconds by layer, lengths that share no factor, so
the layers drift against each other and the bed never repeats as one. Every render is brought to −24 dBFS RMS (the
direction's −24 LUFS, near enough for a bed; a sparse layer sits under it once its peaks are held) and never over
−1 dBTP. Levels by layer: 1, 0.8, 0.65, 0.5, 0.4.

| Recipe | Sound | Layers |
|---|---|---|
| Wash | noise through a low-pass and a high-pass, breathing on a slow cycle that divides the loop | the tide, reeds, the furnaces, falling ash, canopy wind, the city's murmur, fountains, paper mills, wind through grass, white noise, drift |
| Drops | sparse pings around a pitch, each a little different | rain on boardwalk, hot springs, dripping moss |
| Ticks | sparse bursts of filtered noise | picks in the rock, copper roofs ticking, the wagons, paper settling |
| Calls | a sine sliding down with a wobble, a soft rise and fall, rare | gulls far off, one bird far off, a voice from another island |
| Toll | an inharmonic ping ringing long | a bell buoy |
| Hum | two sines beating, a soft wash under | the Roll-Call Bell's hum (Emberdown's G), standing stones humming |
| Rumble | a long swell of very low noise, rare | far thunder |
| Crackle | dense tiny bursts over a hot wash | a fire |
| Steps | pairs of soft thumps, half a second apart, every few seconds | footsteps, too close |
| Clockwork | a tick every beat exactly, a tock between | the Observatory's clockwork, on Halden's 0.6 s |

## 2. In the game

`AmbienceDriver` (Narrative) wakes with the game. It reads the room's region (`Mix.RegionOf`), renders that region's
layers once on a worker thread, and plays each on its own looping source, started together, at its level times the
**Ambience bus's gain** and under an `AudioLowPassFilter` at the bus's cutoff. The mix already sets that cutoff from
the place's fade stage (`Mix.StageCutoff`: open, 9 kHz, 3.5 kHz, 1.2 kHz) and the snapshot; the driver reads the
same stage (`FadeStages.Get` of the room's place) and takes the layers away, last first, each over a 1.5 s fade.
A stage-3 Merrow's End has the tide and the reeds under a 1.2 kHz low-pass, as the direction says; an erased place
has nothing. A room in another region stops the bed and starts that region's. Nothing is loaded from disk.

**Points.** A wash, the rain and the thunder are everywhere (2D). Everything sparse (a bell buoy, gulls, a fire,
picks, footsteps, the clockwork, a hum, a voice) is a 3D source at a point in the room, chosen once per room and
layer from a hash so it is always the same spot (`Ambience.PointIn`): calls from high up, a toll and steps and a
fire from low down, the rest at a hashed height, two units in from the room's camera bounds (or a 16-unit span with
no room). Linear rolloff, full within 3 units, gone past the room's width; the source sits at the listener's depth
so distance is in the room's plane, and the camera following Wren pans it.

## 3. The pipeline

```
Unity.exe -batchmode -nographics -projectPath LastCartographer -executeMethod OWSBG.Setup.AmbienceExport.Render -quit
```

(or **OWSBG → Render the Ambience**) writes `docs/audio/ambience/<region>_ambience_<layer>.wav` for all 28 layers,
48 kHz 24-bit mono, each its loop, for the sound designer to hear and replace. A recorded layer replaces the render
by region and name in the driver's cache (`_clips`); a loader is the whole change.

## 4. Verification

- `AmbienceTests` (EditMode, 5): every layer the direction names has a recipe in its order, most present first,
  loop lengths coprime; the fade takes the last layer first, keeps one until erased, then none; every layer renders
  exactly its loop at the bed's level under the peak, the same every time; the recipes sound like their names (the
  tide low and breathing, the reeds a hiss, the clock on Halden's beat with quiet between, thunder low, one bird
  rare, the Bell humming G); the deliverables are rendered.
- `AmbienceDriverTests` (PlayMode, 4): the coast's five loops of different lengths play at their levels under the
  bus, open; two stages take the bell buoy and the gulls and dull the rest to 3.5 kHz, erasure takes everything;
  the sparse layers are 3D at points in the span (the gulls high, the buoy low, the same spot per room), the washes
  and the rain 2D; the Edge brings the Greyfold's two layers and the coast's are gone.

## 5. Open

- **Recipes are sketches.** Filtered noise is a tide by suggestion; the sound designer's recordings are the point.
- **Per-room layers** (the camp at night, the Choir's room, the Lighthouse's lamp) are the same driver with a room
  key; nothing asks for one yet.
- **Loudness** is RMS, not LUFS.
- **Crossfades between regions** are a stop and a start, like the music's (AUD-06).
