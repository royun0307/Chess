# Match setup and in-game display

The MainScene opens with a setup overlay. Choose 1–180 minutes per side,
0–60 seconds of increment, and White or Black. Defaults are 5 minutes + 3 seconds,
playing White. The Start Game button begins the clock; choosing Black also starts
the engine's opening move as White. New game returns to setup. Restart in the
existing pause/result UI repeats the selected settings.

The player's pieces and clock are below the board, with the opponent above.
BoardManager changes the grid-to-world mapping for both pieces and move targets;
the logical board coordinates and piece sprite orientation stay consistent.

Each card shows remaining time, images of pieces captured by that side, and a
positive material advantage when present. There is no displayed material total.
Advantage uses pawn 1, knight/bishop 3, rook 5, queen 9, king 0, and compares the
remaining armies. Capture history records actual moves including en passant;
promotion is not counted as a captured pawn. This is separate from positional evaluation.

The evaluation bar and its numeric badge use White's score (100 centipawns = 1 pawn) regardless of
player color. Its White end follows White's screen position. Numeric scores are
never interpreted as win probabilities. Mate scores use M / -M. PositionAnalysis
analyzes a detached snapshot with the built-in SimpleChessEngine, at up to depth 4
and a cooperative 350 ms budget. Only completed depths are published. A changed
revision cancels/discards old analysis, and the UI shows Analyzing until a current
score is available. No completed depth means Evaluation unavailable. These are
the built-in engine's estimates, not Stockfish evaluations.

The active side's clock includes promotion selection and receives increment only
after a legal move. Pause and setup freeze clocks. A monotonic real-time clock is
checked before move/selection callbacks as well as every frame, preventing a late
move from beating timeout. Timeout cancels AI work, rejects moves and opens the
result UI. An opponent with only a king, or a position covered by the existing
insufficient-material detector, draws on timeout. This reuses the project's
limited material detector; it does not solve every possible dead position.

New runtime components are created automatically by GameManager, so no scene or
prefab assignment is required. UI labels follow the existing English UI/font.
BoardBackdrop renders a dark green procedural felt background behind the board,
with a soft central light and subtle texture; it has no collider or input handling.

Validation covers clocks, increment, timeout during promotion, color ownership,
completed versus incomplete evaluations, setup button actions, automatic White
AI opening, flipped move targets and end-of-game UI. Rendered layout checks cover
desktop resolutions and 540×960 portrait, with screenshots in
`Validation/MatchUI/screens/`.

Initial match-setup Unity 6000.0.30f1 run: **135 passed, 0 failed, 0 skipped**.
Results: `Validation/MatchUI-final.xml`; log: `Validation/MatchUI-final.log`.
The run used a graphics device, including the rendered layout tests. Screenshots
were also visually inspected for setup, both perspectives, and portrait layout.

Captured-piece/background revision: targeted clock, capture, en passant, game-flow
and rendered-layout checks are recorded in `Validation/CapturedUI.xml` and
`Validation/CapturedUI.log`. Capture screenshots include the `-capture` suffix.

Stage 7 integration validation is recorded in [stage-7.md](validation/stage-7.md).
Published evaluation values now become unavailable immediately when the session
revision changes or analysis is disabled, including before the next frame update.
The game HUD renders behind pause/promotion/result dialogs; setup remains above
the other interfaces. Current rendered-test captures go to
`Validation/Stage7/screens/`; the paths above describe the earlier runs.
