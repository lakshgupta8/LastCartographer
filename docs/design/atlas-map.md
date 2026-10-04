# The Atlas's Map (ENV-11, v1)

The map the pen draws as she surveys (bible: "unsurveyed areas are blank pages"). The atlas opens at the page she
stands on, with the region's rooms drawn as far as she has drawn them, a nib in the room she is in. ← → turn the pages
round the book. The place list, the vantage line and the travel rows stay under the map as they were (`ui-art.md`).

Code: `AtlasMap` (Core: the pages, the layout, the ink), `AtlasWalk` (World: the road she walks),
`AtlasPageMap` (UI: the pen), `AtlasView` (the page turning).

## 1. The pages

One page per region, in the book's order: **The Saltmarrow, Emberdown, The Verdance, Halden, Windreach, The
Greyfold, The Blank.** A room's page is its region (`AtlasMap.PageOfRoom`); an island of the Blank's generator is no
page's, since the white moves them.

The planned regions' rooms and ways come from `RoomPlans`. The coast was built by hand before the plans, so its
rooms and ways are listed in `AtlasMap.Coast`:

- the shore east along the quay, the stilts, the boardwalk, the market, the tether line, the ferry, the three chain
  rooms, the lighthouse, the chapel and the Bone Bridge;
- the roots climbing up from the stilts to the iris fields;
- the ways off the page: the Bone Bridge to Emberdown, the iris fields to the Verdance.

## 2. The layout

Each page is laid on a grid by walking its rooms' exits from its first room, breadth first: a way west, east, up or
down is a step of one cell that way. Where that cell is taken, the room goes on along the same way to the next free
cell. A room that nothing on its page leads to starts a new piece to the right of what is laid. So the page is the
shape she walks, and every room has a cell of its own.

Most doors join neighbours on the grid. The coast's all do. The plateau is the tightest: 6 of Halden's 40 doors join
rooms a cell or more apart, and the pen draws those with a corner. A way to another region is a **leave**, drawn as
an arrow out of the room's side with "to Emberdown" (the region it opens on) beside it.

## 3. The ink

How a room stands on its page (`AtlasMap.InkOf`), from what she has done:

| Ink | When | Drawn |
|---|---|---|
| Unknown | she has never been, and no vantage has seen it | nothing: the page is blank there |
| Seen | unwalked, next to a room with a view that she has surveyed | a dashed pencil box: the vantage saw it |
| Walked | walked, has a view, not yet surveyed | a pencil box, its vantages as thin rings |
| Drawn | surveyed (a room with a view), or walked (a room with nothing to survey: the pen has the road) | an ink box over a wash, its surveyed vantages filled |
| Erased | the place has been erased | a faint box with the eraser's three strokes across it |

A room is walked the first time she stands in it: every transition writes `room.<id>.walked`
(`AtlasMap.Walk` from `RoomManager.Transitioned`), and so does opening the book, for the room she opened it in.
The flags save with the world, so the map does too. A door is in ink between two drawn rooms and in pencil
otherwise; a door to a room still blank is a short stub, so she can see there is more.

## 4. The pen

`AtlasPageMap` is a `VisualElement` that draws with `Painter2D`, 290 px tall between the day line and the place
list. It frames what she has drawn, with a cell round it for the stubs and the arrows, so an early page is a
few boxes in the middle and the page fills as she draws; the cells are at most 64 px and shrink to fit. Every stroke is in the UI's ink
colours (`InkTheme.Ink`, `Dim`, `InkFaint`, `Wash`), so high contrast flattens it with the rest. A box's corners
wander by up to a pixel, by a hash of the room's name, so the hand is the same every time the page is drawn. The
nib is a filled pen point with its slit, standing under the room she is in.

The page's name sits above the map with its place in the book: "The Saltmarrow ◂ 1 / 7 ▸".

## 5. Controls

← → (or A D, the d-pad's left and right, the shoulder buttons) turn the page; ↑ ↓ still choose a destination. The
book opens at her page every time; turning is only for looking.

## 6. Tests

- `AtlasMapTests` (edit mode):
  - every room and every place is on exactly one page, in a cell of its own;
  - every door goes both ways, and at least 80% of each page's doors join neighbours (the coast's all of them, the
    roots above the stilts);
  - the coast's two leaves are marked, and every page leads somewhere off itself;
  - the ink follows a walk, a survey and an erasure, and an island walks onto no page.
- `AtlasMapPlayTests` (play mode): she comes into the stilts and it is walked; she surveys the quay and opens the
  book. It opens at the Saltmarrow with the nib in the stilts, the quay and the stilts in ink. Turning the page shows
  a blank Emberdown, and turning back goes round the back of the book. The open page is pictured in
  `logs/atlas/Saltmarrow.png`.

## 7. Open

- The region headings are in the UI's serif. The hand-cut lettering is the hand pass's (`ui-art.md`).
- A room's box is the same size whatever the room's size; the long rooms (the chain, the stair) could take two cells.
- No marks yet for lamps, desks, benches or shops on the page; the travel rows under the map still carry them.
- The Blank's page holds its built rooms (the Hollow, the Capital, Aury's); the generated islands come and go off it.
