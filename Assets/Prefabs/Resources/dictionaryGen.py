import json
import re

DEFINITIONS_PATH = "Assets/Prefabs/Resources/word_definitions.json"
WORDS_ONLY_PATH = "Assets/Prefabs/Resources/dictionary.txt.txt"
OUTPUT_PATH = "Assets/Prefabs/Resources/clean_gameplay_dictionary.json"

MIN_LENGTH = 3
MAX_LENGTH = 16

MANUAL_DEFINITIONS = {
    "DETAILED": "Developed or described with careful attention to many small parts or facts."
}

def is_clean_word(word):
    return (
        MIN_LENGTH <= len(word) <= MAX_LENGTH
        and re.fullmatch(r"[A-Z]+", word) is not None
    )

def calculate_rarity(word):
    rare_letters = set("QXZ")
    uncommon_letters = set("JKVWY")
    word_set = set(word)

    if len(word) >= 14:
        return "mythic"

    if len(word_set.intersection(rare_letters)) >= 2:
        return "mythic"

    if any(letter in word_set for letter in rare_letters):
        return "rare"

    if len(word) >= 8:
        return "uncommon"

    if any(letter in word_set for letter in uncommon_letters):
        return "uncommon"

    return "common"

with open(DEFINITIONS_PATH, "r", encoding="utf-8") as f:
    definitions = json.load(f)

with open(WORDS_ONLY_PATH, "r", encoding="utf-8") as f:
    allowed_words = set(
        line.strip().upper()
        for line in f
        if line.strip()
    )

clean_entries = []

for word, definition in definitions.items():
    upper_word = word.upper().strip()

    if upper_word not in allowed_words:
        continue

    if not is_clean_word(upper_word):
        continue

    if not isinstance(definition, str):
        continue

    definition = definition.strip()

    if not definition:
        continue

    clean_entries.append({
        "word": upper_word,
        "definition": definition,
        "length": len(upper_word),
        "rarity": calculate_rarity(upper_word)
    })

for word, definition in MANUAL_DEFINITIONS.items():
    upper_word = word.upper().strip()

    if upper_word not in allowed_words:
        continue

    if not is_clean_word(upper_word):
        continue

    if any(entry["word"] == upper_word for entry in clean_entries):
        continue

    clean_entries.append({
        "word": upper_word,
        "definition": definition.strip(),
        "length": len(upper_word),
        "rarity": calculate_rarity(upper_word)
    })

output_data = {
    "entries": clean_entries
}

with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
    json.dump(output_data, f, indent=2, ensure_ascii=False)

print("DONE!")
print(f"Total clean words: {len(clean_entries)}")
print(f"Saved to: {OUTPUT_PATH}")
