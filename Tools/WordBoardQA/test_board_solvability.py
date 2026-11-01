"""Deterministic checks for read-only solvability analyzer."""
import unittest
from collections import Counter
import tempfile
from pathlib import Path
from board_solvability import Analyzer, is_playable_generation_word, read_dictionaries, summary_rows


class BoardAnalyzerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.primary, cls.fallback = read_dictionaries()

    def create_analyzer(self, seed=53):
        return Analyzer(self.primary, self.fallback, seed)

    def test_live_dictionary_totals(self):
        self.assertEqual(len(self.primary), 21004)
        self.assertEqual(len(self.fallback), 32887)

    def test_dictionary_validation_rejects_repetition_and_rare(self):
        self.assertTrue(is_playable_generation_word('MAGIC'))
        self.assertFalse(is_playable_generation_word('ZZQX'))
        self.assertFalse(is_playable_generation_word('BZZZZ'))
        self.assertFalse(is_playable_generation_word('THRTH'))

    def test_solver_accepts_independent_non_adjacent_tiles(self):
        a = self.create_analyzer()
        sample = list('IGIDDEEDEODGSLMI')
        self.assertEqual(len(sample), 16)
        mask = a.playable_mask(sample)
        playable = {w for w, ok in zip(a.words, mask) if ok}
        self.assertIn('SMILED', playable)
        self.assertNotIn('AARDVARK', playable)
        self.assertEqual(len(playable), 301)
        measured = a.measure(sample)
        self.assertLessEqual(measured["primary_total"], measured["total"])
        self.assertEqual(
            measured["primary_basic_3_5"] + measured["primary_medium_6_9"]
            + measured["primary_10plus"], measured["primary_total"])

    def test_refill_preserves_budget_and_letters(self):
        a = self.create_analyzer()
        self.assertEqual(len(a.refill(0)), 0)
        for n in range(1, 17):
            result = a.refill(n)
            self.assertEqual(len(result), n)
            self.assertTrue(all('A' <= x <= 'Z' for x in result))

    def test_startup_and_locked_scenarios(self):
        a = self.create_analyzer()
        initial = a.initial_board()
        self.assertEqual(len(initial), 16)
        self.assertGreater(a.measure(initial)['total'], 0)
        self.assertEqual(len(a.locked(initial, 2)), 14)
        self.assertEqual(len(a.locked(initial, 4)), 12)
        self.assertEqual(len(a.small_replace(initial, 1)), 16)
        self.assertEqual(len(a.small_replace(initial, 2)), 16)
        self.assertEqual(len(a.scramble()), 16)

    def test_deterministic_seed(self):
        a = self.create_analyzer(1001)
        b = self.create_analyzer(1001)
        self.assertEqual(a.initial_board(), b.initial_board())
        self.assertEqual(a.refill(2), b.refill(2))
        self.assertEqual(a.scramble(), b.scramble())

    def test_sample_rows_complete(self):
        a = self.create_analyzer(11)
        cases = a.one_sample(1, 11)
        self.assertEqual(len(cases), 9)
        self.assertTrue(all(r['stage'] == 1 for r in cases))
        for r in cases:
            if not r['unavailable']:
                self.assertEqual(r['total'], sum(r[k] for k in (
                    'basic_3_5','medium_6_9','strong_10_13','ultimate_14_16')))


if __name__ == '__main__':
    unittest.main(verbosity=2)
