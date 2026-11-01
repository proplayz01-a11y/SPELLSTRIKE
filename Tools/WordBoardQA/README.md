# SPELLSTRIKE Board Solvability Analyzer

Reproducible read-only Monte Carlo harness for the actual main-combat dictionary and generic tile-generation logic. It does not alter the Unity project scene, runtime code, or saved word lists.

## Run

From the project root, using Python 3.10+ and NumPy:

```
python Tools/WordBoardQA/test_board_solvability.py
python Tools/WordBoardQA/board_solvability.py --smoke --output "$HOME/Downloads/board_smoke.json"
python Tools/WordBoardQA/board_solvability.py --samples-per-stage 200 --seed 20261101 --output "$HOME/Downloads/board_solvability.json"
```

On Windows PowerShell, pass a full output path like `$env:USERPROFILE\Downloads\board_solvability.json`. The script refuses to overwrite existing result files or place them inside the Unity project. All random choices use an explicit seed.

## What is actually measured

The real `clean_gameplay_dictionary.json` plus the lowercase, ASCII-filtered `dictionary.txt.txt` fallback, matching `DictionaryManager` validation (without the separate Word Master supplement). The generic normal-combat planner forms 16 tiles from a viable 9-13 letter word plus a viable complement, or uses the balanced-vowel emergency fallback. The script enumerates ALL accepted 3-16-letter words formable from each board, accounting for repeated letter multiplicity, with no adjacency constraint.

For each stage number, the harness simulates the **generic combat path** for:

- untouched initial board
- random replacement of 1 and 2 tiles, stress tests for tiny refills
- 1, 2 and 4 locked tiles (4 means two sequential debuffs; one invocation caps at 2)
- forming and consuming an accepted 3-5 or 6-9 word, then refilling the number of tiles used
- full emergency-balanced shuffle (not original Bookworm's RNG)

Reports include totals by attack-length tier, zero-playable board counts, zero-short-word boards, worst boards, and reproducible seed/board strings. They cannot prove physical Android performance or literal parity with UnityEngine.Random.

## Important exclusions

The stage 2 maritime-objective planner and stage 3 authored riddle override require runtime quest state. These are *not simulated*. Stage numbers in this report use independent random streams but **identical generic generator rules**. Do not interpret stage-by-stage differences as a confirmed difficulty curve. Neither enemy status timing nor damage balancing are modeled. Code only reads Unity data and never imports the extracted BA2 wordlist.

The tool's exact-word refill simulation assumes the ordinary successful path of `GetPlayableWordInRange`. A pathological exhausted/rejection fallback may differ from the generated sample. A future Unity editor integration test could exercise the exact private generator functions with isolated temporary objects after Play Mode is safely stopped.
