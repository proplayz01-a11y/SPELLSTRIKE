#!/usr/bin/env python3
"""Read-only SPELLSTRIKE combat board-solvability benchmark.

Uses the exact local gameplay + fallback dictionary, and faithfully mirrors
the generic TileManager 16-tile word-planning, small replacement and balanced
letter refill pathways. Does not execute Unity, change any scene, import the
Bookworm reference dictionary or rewrite gameplay code.

EXCLUSIONS: Stage2 maritime objective and Stage3 authored riddle override
paths depend on runtime quest state and cannot be reproduced offline; the
report identifies the modeled path as generic combat baseline.
"""
from __future__ import annotations

import argparse
from collections import Counter, defaultdict
from datetime import datetime, timezone
import json
import random
import re
from pathlib import Path
from statistics import mean, median

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
RESOURCES = ROOT / "Assets" / "Prefabs" / "Resources"
VOWELS = "AEIOU"
RARE = "QXZJVWKY"
WEIGHTED_VOWELS = "EEEEEEEEAAAAAAIIIIIOOOOUU"
WEIGHTED_CONSONANTS = "RRRRRRTTTTTTNNNNNNSSSSSLLLLLDDDDGGGCCCMMMBBBPPPHHFFVVWWYYKJXQZ"
STAGES = (1, 2, 3, 4)
SCENARIOS = (
    "startup", "replace_one", "replace_two", "locked_one",
    "locked_two", "locked_four", "attack_short_refill",
    "attack_medium_refill", "scramble"
)


def is_playable_generation_word(word: str) -> bool:
    # TileManager.IsPlayableWord (normal, not stage-specific riddle logic).
    if len(word) < 3 or not word.isascii() or not word.isalpha():
        return False
    n_vowel = sum(ch in VOWELS for ch in word)
    n_rare = sum(ch in RARE for ch in word)
    c = Counter(word)
    return n_vowel >= (2 if len(word) >= 5 else 1) and (
        n_vowel / len(word) >= .30 and n_rare < 2 and max(c.values()) < 3
    )


def read_dictionaries() -> tuple[list[str], list[str]]:
    raw = json.loads((RESOURCES / "clean_gameplay_dictionary.json").read_text(encoding="utf-8-sig"))
    primary = sorted(set(str(e["word"]).strip().upper() for e in raw["entries"]
                         if e.get("word") and re.fullmatch("[A-Za-z]+", str(e["word"]).strip())
                         and 3 <= len(str(e["word"]).strip()) <= 16))
    primary_set = set(primary)
    # DictionaryManager.LoadFallbackWordList accepts only *already lowercase*
    # 3..16 ASCII words. It excludes anything already in primary.
    fallback = sorted(set(raw_line.strip().upper() for raw_line in
                          (RESOURCES / "dictionary.txt.txt").read_text(encoding="utf-8-sig").splitlines()
                          if 3 <= len(raw_line.strip()) <= 16
                          and re.fullmatch("[a-z]+", raw_line.strip())
                          and raw_line.strip().upper() not in primary_set))
    return primary, fallback


def encode_word_counts(words: list[str]) -> tuple[np.ndarray, np.ndarray]:
    mat = np.zeros((len(words), 26), dtype=np.uint8)
    lens = np.empty(len(words), dtype=np.uint8)
    for i, w in enumerate(words):
        lens[i] = len(w)
        for letter in w:
            mat[i, ord(letter) - 65] += 1
    return mat, lens


class Analyzer:
    def __init__(self, primary: list[str], fallback: list[str], seed: int):
        self.rng = random.Random(seed)
        self.words = sorted(set(primary) | set(fallback))
        self.mat, self.lens = encode_word_counts(self.words)
        primary_set = set(primary)
        self.primary_mask = np.array([word in primary_set for word in self.words], dtype=np.bool_)
        self.primary = primary
        self.accepted = set(self.words)
        self.by_length = defaultdict(list)
        for w in primary:
            if is_playable_generation_word(w):
                self.by_length[len(w)].append(w)
        # Sampling from pre-filtered pools is equivalent to original 100-attempt
        # rejection sampling conditional on success. The generator normally
        # succeeds immediately for these length budgets.
        self.by_9_13 = [w for n in range(9, 14) for w in self.by_length[n]]
        if not self.by_9_13:
            raise ValueError("No 9-13 letter generation candidates")
        self.fallback_count = len(fallback)

    def sample_by_length(self, length: int) -> str:
        pool = self.by_length[length]
        return self.rng.choice(pool) if pool else ""

    def fill_balanced(self, letters: list[str], amount: int) -> list[str]:
        while len(letters) < amount:
            n_vowels = sum(ch in VOWELS for ch in letters)
            needs = not letters or n_vowels / len(letters) < .35
            letters.append(self.rng.choice(WEIGHTED_VOWELS if needs else WEIGHTED_CONSONANTS))
        return letters

    def initial_board(self) -> list[str]:
        for _ in range(100):
            main = self.rng.choice(self.by_9_13)
            remainder = 16 - len(main)
            second = self.sample_by_length(remainder)
            if second:
                letters = list(main + second)
                self.rng.shuffle(letters)
                return letters
        # This is the original TileManager's emergency fallback.
        letters = self.fill_balanced([], 16)
        self.rng.shuffle(letters)
        return letters

    def refill(self, budget: int) -> list[str]:
        if budget <= 0:
            return []
        first = self.sample_by_length(budget)
        second = ""
        if not first:
            for left in range(budget - 3, 2, -1):
                right = budget - left
                if right < 3:
                    continue
                main = self.sample_by_length(left)
                other = self.sample_by_length(right)
                if main and other:
                    first, second = main, other
                    break
        if not first:
            for n in range(budget, 2, -1):
                first = self.sample_by_length(n)
                if first:
                    break
        letters = list((first + second)[:budget])
        return self.fill_balanced(letters, budget)

    def small_replace(self, board: list[str], n: int) -> list[str]:
        positions = set(self.rng.sample(range(len(board)), n))
        result = [letter for i, letter in enumerate(board) if i not in positions]
        result.extend(self.refill(n))
        self.rng.shuffle(result)
        return result

    def locked(self, board: list[str], n: int) -> list[str]:
        # DebuffManager.ApplyTileLocking caps a *single* invocation to 2;
        # n=4 represents two consecutive invocations before recovery.
        positions = set(self.rng.sample(range(len(board)), n))
        return [ch for i, ch in enumerate(board) if i not in positions]

    def scramble(self) -> list[str]:
        # Explicit emergency / uniform board re-roll scenario. This uses
        # TileManager's emergency balanced generator, not Bookworm fan RNG.
        output = self.fill_balanced([], 16)
        self.rng.shuffle(output)
        return output

    def playable_mask(self, board: list[str]) -> np.ndarray:
        counts = np.zeros(26, dtype=np.uint8)
        for letter in board:
            counts[ord(letter) - 65] += 1
        return np.all(self.mat <= counts[np.newaxis, :], axis=1)

    def measure(self, board: list[str]) -> dict:
        mask = self.playable_mask(board)
        available = self.lens[mask]
        primary_available = self.lens[mask & self.primary_mask]
        return {
            "total": int(available.size),
            "primary_total": int(primary_available.size),
            "primary_basic_3_5": int(np.count_nonzero((primary_available >= 3) & (primary_available <= 5))),
            "primary_medium_6_9": int(np.count_nonzero((primary_available >= 6) & (primary_available <= 9))),
            "primary_10plus": int(np.count_nonzero(primary_available >= 10)),
            "basic_3_5": int(np.count_nonzero((available >= 3) & (available <= 5))),
            "medium_6_9": int(np.count_nonzero((available >= 6) & (available <= 9))),
            "strong_10_13": int(np.count_nonzero((available >= 10) & (available <= 13))),
            "ultimate_14_16": int(np.count_nonzero(available >= 14)),
            "max_word_len": int(available.max()) if available.size else 0,
        }

    def attack_then_refill(self, board: list[str], low: int, high: int) -> list[str] | None:
        candidates = np.flatnonzero(self.playable_mask(board) &
                                    (self.lens >= low) & (self.lens <= high))
        if candidates.size == 0:
            return None
        selected_index = int(candidates[self.rng.randrange(len(candidates))])
        word = self.words[selected_index]
        new_board = board.copy()
        for ch in word:
            new_board.remove(ch)
        new_board.extend(self.refill(len(word)))
        self.rng.shuffle(new_board)
        return new_board

    def one_sample(self, stage: int, seed: int) -> dict:
        # stage intentionally does not modify generic combat candidate pool.
        # Stage 2 and 3 authored-quest overrides are EXCLUDED and noted.
        board = self.initial_board()
        scenarios = {
            "startup": board,
            "replace_one": self.small_replace(board, 1),
            "replace_two": self.small_replace(board, 2),
            "locked_one": self.locked(board, 1),
            "locked_two": self.locked(board, 2),
            "locked_four": self.locked(board, 4),
            "attack_short_refill": self.attack_then_refill(board, 3, 5),
            "attack_medium_refill": self.attack_then_refill(board, 6, 9),
            "scramble": self.scramble(),
        }
        records = []
        for case, letters in scenarios.items():
            if letters is None:
                records.append({"stage": stage, "seed": seed, "scenario": case,
                                "unavailable": True, "board": "", "usable_tiles": 0})
                continue
            if case in ("startup", "replace_one", "replace_two",
                        "attack_short_refill", "attack_medium_refill", "scramble"):
                assert len(letters) == 16, (case, letters)
            stats = self.measure(letters)
            records.append({
                "stage": stage, "seed": seed, "scenario": case,
                "unavailable": False, "board": "".join(letters),
                "usable_tiles": len(letters), **stats,
            })
        return records


def summary_rows(records: list[dict]) -> list[dict]:
    result = []
    for stage in STAGES:
        for case in SCENARIOS:
            lines = [r for r in records if r["stage"] == stage
                     and r["scenario"] == case and not r["unavailable"]]
            skipped = sum(1 for r in records if r["stage"] == stage
                          and r["scenario"] == case and r["unavailable"])
            if not lines:
                result.append(dict(stage=stage, scenario=case, evaluated=0,
                                   unavailable=skipped))
                continue
            totals = sorted(r["total"] for r in lines)
            basic = [r["basic_3_5"] for r in lines]
            long = [r["strong_10_13"] + r["ultimate_14_16"] for r in lines]
            result.append({
                "stage": stage, "scenario": case, "evaluated": len(lines),
                "unavailable": skipped, "min_valid_words": min(totals),
                "median_valid_words": round(median(totals), 1),
                "mean_valid_words": round(mean(totals), 1),
                "p05_valid_words": totals[max(0, int(len(totals) * .05))],
                "zero_word_boards": sum(v == 0 for v in totals),
                "below_5_words": sum(v < 5 for v in totals),
                "min_basic_words": min(basic),
                "zero_basic_boards": sum(v == 0 for v in basic),
                "median_primary_words": round(median(r["primary_total"] for r in lines), 1),
                "min_primary_words": min(r["primary_total"] for r in lines),
                "zero_primary_basic_boards": sum(r["primary_basic_3_5"] == 0 for r in lines),
                "zero_primary_medium_boards": sum(r["primary_medium_6_9"] == 0 for r in lines),
                "zero_primary_10plus_boards": sum(r["primary_10plus"] == 0 for r in lines),
                "min_10plus_words": min(long),
                "zero_10plus_boards": sum(v == 0 for v in long),
                "worst_board": min(lines, key=lambda r: r["total"])["board"],
            })
    return result


def main() -> None:
    cli = argparse.ArgumentParser(description=__doc__)
    cli.add_argument("--samples-per-stage", type=int, default=200)
    cli.add_argument("--seed", type=int, default=20261101)
    cli.add_argument("--output", type=Path, required=True)
    cli.add_argument("--smoke", action="store_true",
                     help="run 8 samples per stage and fail on broken invariants")
    args = cli.parse_args()
    if args.smoke:
        args.samples_per_stage = 8
    if args.samples_per_stage <= 0 or args.samples_per_stage > 2000:
        cli.error("samples-per-stage must be 1..2000")
    if args.output.resolve() == ROOT or ROOT in args.output.resolve().parents:
        cli.error("write benchmark outputs OUTSIDE the Unity project")
    if args.output.exists():
        cli.error("output already exists; refusing to overwrite evidence")
    primary, fallback = read_dictionaries()
    analyzer = Analyzer(primary, fallback, args.seed)
    assert len(analyzer.words) == len(primary) + len(fallback)
    assert len(analyzer.words) == 53891, "Dictionary data changed; investigate before trusting benchmark"
    for case in ("MAGIC", "RAYED", "SMILED"):
        assert case in analyzer.accepted
    records = []
    for stage in STAGES:
        for i in range(args.samples_per_stage):
            records.extend(analyzer.one_sample(stage, args.seed + i))
        print("stage", stage, "done", args.samples_per_stage, "boards", flush=True)
    summary = summary_rows(records)
    data = {
        "timestamp_utc": datetime.now(timezone.utc).isoformat(),
        "seed": args.seed, "samples_per_stage": args.samples_per_stage,
        "stage_count": len(STAGES), "source": "offline-mirror-of-generic-TileManager",
        "scenario_count": len(SCENARIOS),
        "dictionary": {"primary": len(primary), "fallback": len(fallback),
                       "union": len(analyzer.words)},
        "limitations": [
            "No Play Mode or actual Unity TileManager methods were invoked.",
            "Stage 2 maritime and Stage 3 riddle override branches were not simulated.",
            "Random draws use Python seeded RNG, not UnityEngine.Random's exact sequence.",
            "The 1-2 tile replacement scenarios are stress tests, not normal 3+ attack submissions.",
            "Locked four is two sequential lock applications, not one single debuff event.",
            "Dictionary/formability counts do not establish actual reachable combat damage.",
            "No physical Android device performance was measured.",
        ],
        "summary": summary,
        "records": records,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(data, indent=2), encoding="utf-8")
    print(f"SAVED {args.output} ({len(records)} scenario evaluations)")
    for case in SCENARIOS:
        rows = [r for r in summary if r["scenario"] == case]
        print(f"{case:22s} min_valid={min(r.get('min_valid_words',0) for r in rows):5d}"
              f" zero_boards={sum(r.get('zero_word_boards',0) for r in rows):3d}"
              f" zero_basic={sum(r.get('zero_basic_boards',0) for r in rows):3d}"
              f" zero_10plus={sum(r.get('zero_10plus_boards',0) for r in rows):3d}"
              f" unavailable={sum(r['unavailable'] for r in rows):3d}")


if __name__ == "__main__":
    main()
