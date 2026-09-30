# Saltmarrow Rooms (DES-08, v1)

Room-by-room design for the coast (bible 4.1, world-map.md §3), starting with the vertical slice. Rooms are
40 units wide and 17 to 19 high; the floor is y = 0; exits are the room's edges (west, east) or a gap in the
floor and a trigger above the highest platform (down, up). Each room is one additive scene, `Greybox_<Id>`,
and the greybox builds every one of them from a **recipe** in `ProjectSetup` (`SaltmarrowRecipes()`), so the
table below and the scenes cannot drift apart. The four hand-built rooms (the Quay, Merrow's End, the fourth
lighthouse, the Edge) keep their bespoke content.

## 1. The slice (M1): sixteen rooms

```
                       Roots_3 ── Roots_4 (Crown)
                          │
                       Roots_2 (Bole)
                          │
                       Roots_1
                          │
Shore ── Quay (A) ── Stilts ── Boardwalk ── Merrow's End (B) ── Tetherline ── Ferry ── Chain_1 ── Chain_2 ── Chain_3 ── Lighthouse (C)
  ~sea                                                                                       (1st lamp)  (2nd lamp)  (faded 3rd)   (Lamp-Keeper)
```

| Room `Saltmarrow_…` | Zone | What it is for | Vantage | Enemies | Exits |
|---|---|---|---|---|---|
| **Shore** | Shore | Where the Blank put her down. The sea-fade is a wall of white to the west; the first blank the player sees. A rock to survey. | Tideline | crab, smudge | E → A |
| **A** (hand-built) | Quay, hub | Sable, the desk, the ledger, the dummy, the Reedmother vantage, the weak floor and hidden platform. Wren wakes at its west end. | Reedmother | crab, skimmer, smudge; 2 Wardens (anchored) | W → Shore, E → Stilts |
| **Stilts** | Quay | The stilt-roosts: a climb of five platforms to a top exit; the first vertical room. Teaches the up-exit. | — | skimmer ×2, crab | W → A, E → Boardwalk, up → Roots_1 |
| **Boardwalk** | Quay | A long walk with two gaps over the shallows (drop in, climb out). Crabs on the planks. | — | crab ×2, smudge | W → Stilts, E → B |
| **B** (hand-built) | Merrow's End | Dotha, the tether-post vantage, the Talonhold shaft, a Cantor over the east end. | Tetherpost | Cantor; 2 Wardens (anchored) | W → Boardwalk, E → Tetherline |
| **Tetherline** | Merrow's End | The tether-posts of the village: ropes into the white. Skimmers in the rigging. | — | skimmer ×2, crab | W → B, E → Ferry |
| **Ferry** | Merrow's End | The Ferrymen's landing; the last of the village. Two smudges where the boats were. | — | smudge ×2 | W → Tetherline, E → Chain_1 |
| **Chain_1** | Lantern Chain | The first lighthouse: a spiral of platforms to the lamp room. | FirstLamp | skimmer, crab | W → Ferry, E → Chain_2 |
| **Chain_2** | Lantern Chain | The second: the same spiral mirrored; a Cantor watches the chain. | SecondLamp | Cantor, crab | W → Chain_1, E → Chain_3 |
| **Chain_3** | Lantern Chain | The faded third: paler paper, smudges, the tether landing for Aury's island (`saltmarrow.tether`, later). | — | smudge ×2 | W → Chain_2, E → C |
| **C** (hand-built) | Lantern Chain | The fourth: desk, the Lamp-Keeper's arena, her lamp (a travel point once lit). | Lamp (the beacon) | the Lamp-Keeper | W → Chain_3, E → Chapel |
| **Chapel** (hand-built) | Salt Chapel | The tide gap (nine units: a jump and a Wingbeat, or a pogo off the skimmer), a desk on the near bank, Halvard's first fight behind two doors, the altar past it. | Altar | skimmer; Halvard | W → C |
| **Roots_1** | Reedmother's Roots | The bole of the great reed: a gap in the floor drops back to the stilts; platforms lead east. | — | crab, skimmer | down → Stilts, E → Roots_2 |
| **Roots_2** | Reedmother's Roots | The Bole vantage on the floor, then a climb to a top exit. | Bole | skimmer, crab | W → Roots_1, up → Roots_3 |
| **Roots_3** | Reedmother's Roots | Among the roots: a smudge in the dark, a skimmer in the light; the gap drops back down. | — | smudge, skimmer | down → Roots_2, E → Roots_4 |
| **Roots_4** | Reedmother's Roots | The crown: three platforms up to the highest vantage on the coast; the view is the point. | Crown | crab, skimmer | W → Roots_3 |
| **Greyfold_Edge** (hand-built) | Half-Cathedral | The prologue. | HalfCathedral | 3 smudges | the white → A "Shore" |

Vantages in the slice: 8 (Tideline, Reedmother, Tetherpost, Bole, Crown, FirstLamp, SecondLamp, Lamp).

## 2. Rules the recipes follow
- **Gates.** Nothing in the slice needs Wingbeat; the coast ends at the Lamp-Keeper (world-map §1). Gaps are
  4 units, jumpable; climbs are 2.5 units a step, pogo-able off skimmers.
- **Vertical rooms** are 19 high; the up-trigger sits 2.2 above the top platform; the lower room's `Top` spawn is
  on that platform and the upper room's `Bottom` spawn stands beside the gap, not over it.
- **Enemies** are placed by answer: crabs on floors (pogo), skimmers over climbs (any hit as they dive), smudges
  where something was (Longstroke), one Cantor per lighthouse pair, Wardens only in hub rooms and only inactive.
- **Fade.** Every room has a `FadeGroup`; the place is the room. The faded third lighthouse uses the paler
  palette from the start.
- **The look.** Three paper layers per room (mid reeds, far roosts, farther cliffs) and a foreground reed line
  in hub rooms; the sea-fade is the Edge's white sheets mirrored to the west. Since ENV-02 the layers and the
  ground come off the Saltmarrow paper kit (`docs/design/paper-kit.md`): planks on the coast, paler planks in
  the faded third, the tide in the Boardwalk's gaps, salt stone under the lighthouse and the chapel.

## 3. The rest of the coast (after the slice)
| Zone | Rooms | Notes |
|---|---|---|
| The Pale Iris Fields | 3 | East of Reedmother's Roots; iris seeds (DES-05); the iris gap to the Verdance (Wingbeat, soft). |
| Lighthouses 5–7 | 3 | Beyond the fourth: the Salt Chapel road. |
| The Salt Chapel | 2 more | The chapel's first room is built (the gap, the fight); a chapel of salt-eaten paper behind it. |
| The Bone Bridge | 3 | The whale; the second step of the Bone Bridge commission; the climb to Emberdown. |

## 4. Open
- Where the Ferrymen sell tethers (Sable, or the Ferry room's landing) once DES-05 prices them.
- Roots_4's reward beyond the view (a vellum scrap, a memory, the first Remnant?).
- Real layouts on paper (ENV-02) will replace the recipes' platform arithmetic; the recipe keeps the exits,
  spawns, vantages and enemy answers, which is what the tests hold on to.
