# Verification

## Fresh publication check — 24 September 2026

The saved project was copied into a clean clone of this repository and opened with **Unity 6000.3.24f1**. All **18 PlayMode tests passed**, with no failures: **11 SHIFT tests and 7 Polar Relay regression tests**. See the [sanitized result list](verification/2026-09-24-playmode.json) and [test source](../Assets/Tests/PlayMode).

The run compiled/imported the project and ran in batch mode without graphics. It covers:

- Stations require a nearby, grounded robot to change mode.
- Pistons require a nearby, grounded heavy robot and remain latched.
- Panels support a light robot, break under a heavy robot and reject heavy side-contact activation.
- Falling preserves opened mechanisms; a full restart restores them.
- Completion requires the necessary pistons and physical proximity to the exit.
- Pause/resume and level transitions work.
- Three SHIFT routes complete using keyboard input and physics after spawning: level 1, level 2 bridge-first and level 2 early descent/recovery. These tests do not teleport through the route or directly activate its mechanisms.
- Earlier magnet capture, gates, reset, movement and scene-flow behavior remains covered.

Some component tests position fixtures directly to test a precondition. Only the three named physical-route tests establish full traversal under their scripted inputs.

## Run the tests

Open the project with the recorded Unity version. Use **Window → General → Test Runner**, select **PlayMode**, then run all tests.

For automation, invoke your installed Unity executable with the following arguments, replacing the paths:

```text
-batchmode -nographics -projectPath "<repository-root>" -runTests -testPlatform PlayMode -testResults "<output>/results.xml" -logFile "<output>/unity-tests.log"
```

Do not add `-quit` to that test command; the test runner exits after writing its result. Keep output files outside tracked source. The 03/04/05/06 scenes must remain enabled in Build Profiles; they are included in this snapshot.

## Windows download

The release archive's **176 runtime/data files** were SHA-256 compared with the existing 24 September SHIFT Windows build: all matched, with no missing or extra runtime files. Only the portable English/Turkish play guide was revised for distribution. The ZIP is 39,297,507 bytes.

```text
SHA-256: c3c633b08694d267108feb82dc8d1fa68c52e107a95a5bd254ed4c8ecb9d7528
```

The earlier development task opened the Windows build and inspected the English/Turkish menus and first-level image at 1280 × 720. This publication check verified the archive and reran the technical suite; it did **not** repeat a rendered visual review or a physical-keyboard playthrough of the packaged executable.

## What these checks do not show

Automated route timings are not first-time-player completion times. Passing tests does not establish enjoyment, readability, accessibility, or that every corner snag is fixed. Real target-player sessions, other computers, wider display coverage and the final portfolio presentation remain unfinished. The separate onboarding study has not been executed.
