# Round 1: the facilitator's sheet

For the person running a session. Copy this page for each tester; the protocol, the bar and the gate are in
`docs/design/playtest-round1.md`.

## Before

- [ ] A fresh build (`pwsh tools/build.ps1 -Development`), and no old save on this machine. Saves live in the game's
      data folder, under `saves/`; move them aside rather than delete them.
- [ ] Start it as `LastCartographer.exe -playtest -tester <name> -playtestOut logs/playtest/<name>.json`.
- [ ] A pad and a keyboard on the desk; the tester chooses.
- [ ] Sound on, at a level they choose. A quiet room.
- [ ] Give them `tester-brief.md` to read. Answer questions about the brief, not about the game.

## During

- Say nothing about how to play. If they ask, say "what would you try?" If they are stuck for five minutes and
  want to stop, let them stop. That is data, not a failure.
- If the game freezes or closes, start it again with the same line plus `-continue`. It picks up from the last
  desk they rested at, and the session file carries on. Note the time it happened.
- Write down what you see and they don't say. Keep it short, with the time on the game clock or yours.

| Time | Where (room or what's on screen) | What happened | What they said |
|---|---|---|---|
| | | | |
| | | | |
| | | | |
| | | | |
| | | | |
| | | | |

Things worth a line:
- the first time they open the atlas;
- the first death, and how they took it;
- every time they stop moving to think;
- what they try to talk to;
- the Lamp-Keeper's first attempt;
- anything they laugh at.

## After

1. Before you talk about it, give them the twelve questions (one to five). Read the ends of each scale aloud if
   asked. A question they couldn't answer stays blank.
2. Then the open lines. Write their words, not your summary.
3. Put the twelve numbers in `logs/playtest/answers.csv` as a row (template:
   `docs/playtest/round1/answers-template.csv`), and their open answers and your notes in
   `logs/playtest/<name>.md` (template: `notes-template.md`).
4. Check that `logs/playtest/<name>.json` is there. If they pressed F12, its folders are in the game's data folder
   under `BugReports/`; copy them to `logs/playtest/<name>-bugs/`.
5. Thank them.

## Once the round is done

`pwsh tools/playtest-gate.ps1` reads every answer and session and writes `logs/playtest/gate.md`: the verdict, why
not, every question's median, and how far each tester got and where they were lost and died.
