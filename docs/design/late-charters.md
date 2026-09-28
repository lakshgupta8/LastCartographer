# The Late Charters: Ferryman, Unwriter, Remnant (CMB-17, v1)

The combat doc (§5) names three Charters found in the world after the base three. Each rewrites Wren's combo and
default Flourish like the base ones, adds a passive, and is handed over where the story puts it. They are profiles
in `CharterProfile` (World, `CharterSet.cs`) beside the Surveyor, Warden and Drifter. `CharterProfile.For(kind)`
returns any of the six, and Oriel mirrors the late ones too.

| Charter | Combo | Flourish | Passive | Found |
|---|---|---|---|---|
| **Ferryman's Charter** | Hook (3.2), Swing (3.4, heavier shove), Reel (3.0, 2 damage, pulls) | Longstroke | Inkthread costs 1 pip, not 2 (`InkthreadCost`, read by the thread when it exists) | Sable, at the Lantern Chain's tether-post, with the cord (`Chain_Sable_Tether`) |
| **Unwriter's Charter** | Hush, hush, Toll (2 damage, tall) | Blot | Strikes erase enemy projectiles; Bind costs 4 (`BindCost`) | The Choir's last dove (6.6): the arena hands it over (`BossSheet.Charter`, `BossArena.RewardCharter`) |
| **Remnant Charter** | Fade, fade, Pale | Crosshatch | Each landed quill strike drains a third of the target's colour (`Drain`) | Ilse, in Thessaly Hollow (`Hollow_Ilse`), Act 3 |

## What each passive does

- **The reel.** A combo step can pull (`ComboStep.Pulling()`, `HitInfo.Pulls`). The target's knockback comes toward
  Wren instead of away. The Ferryman's third step reels enemies into the next swing.
- **Unwriting.** `EnemyProjectile` (World) is the game's first thrown thing. It flies straight, costs a mask on
  contact, and breaks on the ground or when its time runs out. A strike goes through it unless the striker wears a
  Charter that `ErasesProjectiles`, and then any strike or Flourish that meets it unwrites it. The first enemy to
  throw one is Surveyor Hale: his **flick** (ink off the nib, aimed at her, 14-frame wind-up) joins his phase 2 and
  3 patterns, still four attacks a phase.
- **The dearer Bind.** `WrenVitals.BindCost` is set by the Charter: 3 for every Charter except the Unwriter's 4.
- **Draining colour.** Every enemy has `Colour`, from 1 down to 0. A landed strike with `HitInfo.Drain` takes some
  away. At 0 the enemy is grey: each strike on it slows it for 1.5 s, and it draws in grey. Colour starts coming
  back 3 s after the last drain and is full 2 s later. `OnGrey` lets an enemy answer. **Corra's Drawing** does:
  grey, it has no colour to redraw its limbs with, so it can be struck without waiting (the sheet's answer, 6.13).

## Handing them over

`<<charter Ferryman>>` (Yarn, `DialogueService`) adds a Charter to what Wren owns and captions it; wearing it is
still the drafting desk's job (`DeskMenu` lists the owned ones). Sable's line comes with the cord: "And this.
Ferryman's charter. Comes with the cord. Price is you come back." Ilse's comes after she tells Wren to ask her
father: "Take this. The Remnant's charter. Grey is a colour you can carry." The Choir's comes from its sheet, so any
arena built from the sheet hands it over.

## Tests

`LateChartersTests` (PlayMode, 7 tests):
- There are six profiles, and a Charter can't be worn until it is owned.
- Wearing each late Charter sets its combo, Flourish, Bind cost, thread cost, drain and projectile rule; the
  Surveyor sets them back.
- The reel pulls where a plain strike shoves.
- Three Remnant strikes turn an enemy grey and slow, and its colour comes back.
- Grey, Corra's Drawing takes strike after strike.
- The Surveyor's quill goes through a projectile, the Unwriter's unwrites it, and an unstruck one lands.
- Hale's flick flies at her and lands.
- Beating the Choir hands over the Unwriter's Charter.

`LateChartersDialogueTests` (PlayMode, 1 test) runs `Chain_Sable_Tether` and `Hollow_Ilse` through the shipped Yarn
project and checks each hands its Charter over and the Remnant can be worn. The ending routes replay both scenes
with the new lines.

## Open

- **Silhouettes** (cowl and grip per Charter) are CHR-05's; the greybox tint stands in.
- **The Ferryman's tether-swing** is a reach-and-reel combo until Inkthread exists (CMB-04). Then its cheap thread,
  and swinging attacks off it, can be built for real.
- **Only Hale throws.** The roster's spitters (CMB-09) will use `EnemyProjectile`, and a Sighting-lens parry could
  send a projectile back.
- **The drain's picture.** Grey is a tint for now; the fade shader's `_Ink` could carry it.
