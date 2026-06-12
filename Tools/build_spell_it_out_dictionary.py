#!/usr/bin/env python3
"""Build a curated Spell It Out dictionary from the larger gameplay dictionary."""

from __future__ import annotations

import csv
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets" / "Prefabs" / "Resources" / "clean_gameplay_dictionary.json"
OUTPUT = ROOT / "Assets" / "Prefabs" / "Resources" / "spell_it_out_dictionary.json"
REJECTS = ROOT / "Assets" / "Prefabs" / "Resources" / "spell_it_out_rejects.csv"

MIN_LENGTH = 4
MAX_LENGTH = 10
MAX_DEFINITION_LENGTH = 140
MIN_DEFINITION_WORDS = 4

CURATED_DEFINITIONS: dict[str, str] = {
    "ABLE": "Having the skill or power to do something.",
    "BOOK": "A set of written pages that people read.",
    "BRAVE": "Ready to face danger or difficulty.",
    "CALM": "Peaceful and not upset.",
    "CARE": "Attention given to keep someone or something safe.",
    "DARK": "Having little or no light.",
    "FAIR": "Treating people equally and honestly.",
    "FIRE": "Heat and light made by burning.",
    "GAME": "An activity played for fun or competition.",
    "GIFT": "Something given to another person.",
    "HAND": "The body part at the end of your arm.",
    "HELP": "To make something easier for someone.",
    "HOME": "The place where someone lives.",
    "HOPE": "A feeling that something good may happen.",
    "KIND": "Gentle, helpful, and caring toward others.",
    "LIGHT": "Brightness that lets people see.",
    "LOVE": "A strong feeling of care for someone or something.",
    "MOON": "The natural object that orbits Earth.",
    "RAIN": "Water that falls from clouds.",
    "ROAD": "A path or street used for travel.",
    "SAFE": "Protected from danger or harm.",
    "STAR": "A bright object seen in the night sky.",
    "TIME": "The passing of moments, hours, and days.",
    "TREE": "A tall living thing with a trunk and branches.",
    "TRUE": "Based on fact and not false.",
    "WIND": "Air that moves across a place.",
    "WORK": "Effort used to do or make something.",
    "ANSWER": "A reply to a question.",
    "BUILD": "To make something by putting parts together.",
    "CLEAN": "Free from dirt or mess.",
    "DREAM": "Thoughts or images in the mind while sleeping.",
    "EARTH": "The planet we live on.",
    "GRACE": "Calm, polite, and thoughtful behavior.",
    "GREEN": "The color of grass and many leaves.",
    "HAPPY": "Feeling pleased or joyful.",
    "HEART": "The organ that pumps blood through the body.",
    "HOUSE": "A building where people live.",
    "LEARN": "To gain knowledge or skill.",
    "MONEY": "Something used to buy goods or services.",
    "MUSIC": "Organized sounds made to be heard and enjoyed.",
    "PEACE": "A calm state without fighting.",
    "POWER": "The ability to do something or control something.",
    "RIVER": "A natural stream of water.",
    "SHARE": "To let others use or have part of something.",
    "SMILE": "A happy expression made with the mouth.",
    "SOUND": "Something that can be heard.",
    "STONE": "A hard piece of rock.",
    "STORY": "A description of events, real or imagined.",
    "TRUST": "Belief that someone is reliable or truthful.",
    "TRUTH": "The real facts about something.",
    "WATER": "A clear liquid that people and animals need.",
    "WORLD": "The earth and all the people and things on it.",
    "BEACON": "A light or signal used to guide people.",
    "CHANGE": "To become different or make something different.",
    "CHOICE": "An act of picking between options.",
    "CREATE": "To make something new.",
    "FAMILY": "A group of people related to one another.",
    "FRIEND": "A person you like and trust.",
    "FUTURE": "The time that has not happened yet.",
    "GARDEN": "A cared-for outdoor space beside a home.",
    "HEALTH": "The condition of the body or mind.",
    "LISTEN": "To pay attention to a sound or someone speaking.",
    "MEMORY": "Something remembered from the past.",
    "NATURE": "The living world and its natural features.",
    "PEOPLE": "Human beings in general or as a group.",
    "REASON": "A cause or explanation for something.",
    "RESCUE": "To save someone from danger.",
    "SAFETY": "The state of being protected from harm.",
    "SCHOOL": "A place where students learn.",
    "SIMPLE": "Easy to understand or do.",
    "STRONG": "Having great physical or mental power.",
    "WONDER": "A feeling of surprise and curiosity.",
    "BALANCE": "A steady state where things are even or stable.",
    "BELIEVE": "To accept that something is true.",
    "COMFORT": "A feeling of ease or relief.",
    "COURAGE": "The ability to do something even when it is difficult or scary.",
    "FREEDOM": "The power to choose or act without unfair control.",
    "HONESTY": "The quality of telling the truth and being fair.",
    "JOURNEY": "A trip from one place to another.",
    "JUSTICE": "Fair treatment under rules or laws.",
    "PROTECT": "To keep someone or something safe from harm.",
    "RESPECT": "To treat someone or something as important.",
    "SCIENCE": "The study of the natural world through evidence.",
    "SERVICE": "Work done to help other people.",
    "SUPPORT": "To help someone or hold something up.",
    "THOUGHT": "An idea formed in the mind.",
    "TREASURE": "Something very valuable or special.",
    "VICTORY": "Success in a contest or struggle.",
    "ADVENTURE": "An exciting or unusual experience.",
    "CREATIVE": "Able to make or imagine new things.",
    "DISCOVER": "To find or learn something for the first time.",
    "GENERATE": "To produce or create something.",
    "IMAGINE": "To form an idea or picture in your mind.",
    "KINDNESS": "The quality of being gentle and helpful.",
    "LANGUAGE": "A system of words used to communicate.",
    "PATIENCE": "The ability to wait calmly without getting upset.",
    "PRACTICE": "To do something repeatedly to improve a skill.",
    "QUESTION": "A sentence used to ask for information.",
    "REMEMBER": "To keep something in the mind.",
    "SOLUTION": "An answer to a problem.",
    "TOGETHER": "With each other or in one group.",
    "KNOWLEDGE": "Information and understanding gained through learning.",
    "CONFIDENCE": "Belief in your own ability.",
    "FRIENDSHIP": "A close and trusting relationship between people.",
    "HAPPINESS": "A feeling of joy or satisfaction.",
    "IMPORTANT": "Having great value or meaning.",
    "PROTECTION": "The act of keeping someone or something safe.",
    "RESPONSIBLE": "Trusted to do what is right or needed.",
}

OVERRIDES: dict[str, str] = {
    **CURATED_DEFINITIONS,
    "HONESTY": "The quality of telling the truth and being fair.",
    "COURAGE": "The ability to do something even when it is difficult or scary.",
    "JUSTICE": "Fair treatment under rules or laws.",
    "PATIENCE": "The ability to wait calmly without getting upset.",
    "MEMORY": "Something remembered from the past.",
    "BALANCE": "A steady state where things are even or stable.",
    "CREATIVE": "Able to make or imagine new things.",
    "KNOWLEDGE": "Information and understanding gained through learning.",
    "FRIEND": "A person you like and trust.",
    "FAMILY": "A group of people related to one another.",
    "SCHOOL": "A place where students learn.",
    "LISTEN": "To pay attention to a sound or to someone speaking.",
    "LEARN": "To gain knowledge or skill through study or practice.",
    "ANSWER": "A reply to a question.",
    "BELIEVE": "To accept that something is true.",
    "RESPECT": "To treat someone or something as important.",
    "PROTECT": "To keep someone or something safe from harm.",
    "DISCOVER": "To find or learn something for the first time.",
    "IMAGINE": "To form an idea or picture in your mind.",
    "PRACTICE": "To do something repeatedly to improve a skill.",
}

FORBIDDEN_TERMS = {
    "archaic",
    "botanic",
    "botanical",
    "botany",
    "called also",
    "common name",
    "cruciferous",
    "family of",
    "flower",
    "genus",
    "herb",
    "latin",
    "obsolete",
    "partitions",
    "perennial",
    "plant",
    "pods",
    "scientific",
    "species",
    "taxonomic",
    "taxonomy",
}

REDIRECT_PREFIXES = (
    "see ",
    "see also ",
    "same as ",
    "variant of ",
    "alternative form of ",
    "alternate form of ",
    "plural of ",
    "past tense of ",
    "present participle of ",
    "a form of ",
    "form of ",
    "archaic form of ",
    "obsolete form of ",
    "imp. of ",
    "p. p. of ",
    "p. pr. of ",
)


def normalize_definition(text: str) -> str:
    text = re.sub(r"\s+", " ", (text or "").strip())
    text = text.replace(" ;", ";").replace(" .", ".")
    return text


def tokenize(text: str) -> list[str]:
    return re.findall(r"[a-z]+", text.lower())


def answer_variants(word: str) -> set[str]:
    lower = word.lower()
    variants = {lower, lower + "s", lower + "es", lower + "ed", lower + "ing", lower + "ly"}

    if len(lower) > 4 and lower.endswith("s"):
        variants.add(lower[:-1])
    if len(lower) > 5 and lower.endswith("es"):
        variants.add(lower[:-2])
    if len(lower) > 4 and lower.endswith("e"):
        variants.add(lower[:-1])
        variants.add(lower[:-1] + "ing")
    if len(lower) > 4 and lower.endswith("y"):
        stem = lower[:-1]
        variants.update({stem, stem + "ies", stem + "ily", stem + "ly"})
    if len(lower) > 6 and lower.endswith("ing"):
        variants.add(lower[:-3])
    if len(lower) > 5 and lower.endswith("ed"):
        variants.add(lower[:-2])

    return variants


def reject_reason(word: str, definition: str) -> str | None:
    if not word or not re.fullmatch(r"[A-Z]+", word):
        return "non_alpha_word"

    if len(word) < MIN_LENGTH or len(word) > MAX_LENGTH:
        return "length_out_of_range"

    definition = normalize_definition(definition)
    if not definition:
        return "empty_definition"

    lower = definition.lower()
    if lower == "definition unavailable in the gameplay dictionary.":
        return "unavailable_definition"

    if len(definition) > MAX_DEFINITION_LENGTH:
        return "too_long"

    if len(tokenize(definition)) < MIN_DEFINITION_WORDS:
        return "too_short_or_not_a_definition"

    if ";" in definition:
        return "semicolon_dictionary_dump"

    if "--" in definition or "—" in definition:
        return "dash_dictionary_dump"

    if lower.startswith(REDIRECT_PREFIXES):
        return "redirect_definition"

    if lower.startswith("of or pertaining to"):
        return "technical_relation_definition"

    if lower.startswith("one who ") or lower.startswith("one that "):
        return "role_phrase_definition"

    if lower.startswith("l.") or " l." in lower:
        return "latin_abbreviation"

    for term in FORBIDDEN_TERMS:
        if term in lower:
            return "forbidden_term"

    variants = answer_variants(word)
    if any(token in variants for token in tokenize(definition)):
        return "contains_answer_or_variant"

    if definition.count(".") + definition.count("?") + definition.count("!") > 1:
        return "multi_sentence"

    return None


def entry_score(definition: str) -> int:
    score = len(definition)
    if "," in definition:
        score += 12
    return score


def main() -> None:
    with SOURCE.open("r", encoding="utf-8") as handle:
        source_data = json.load(handle)

    accepted: dict[str, dict[str, object]] = {}
    rejected: list[dict[str, str]] = []

    for raw_entry in source_data.get("entries", []):
        word = str(raw_entry.get("word", "")).strip().upper()
        raw_definition = str(raw_entry.get("definition", ""))
        definition = normalize_definition(OVERRIDES.get(word, raw_definition))
        reason = reject_reason(word, definition)

        if reason:
            rejected.append({"word": word, "reason": reason, "definition": normalize_definition(raw_definition)})
            continue

        candidate = {
            "word": word,
            "definition": definition,
            "length": len(word),
            "rarity": raw_entry.get("rarity", "common") or "common",
            "_score": entry_score(definition),
        }

        existing = accepted.get(word)
        if existing is None or int(candidate["_score"]) < int(existing["_score"]):
            accepted[word] = candidate

    accepted.clear()
    for word, definition in CURATED_DEFINITIONS.items():
        if MIN_LENGTH <= len(word) <= MAX_LENGTH:
            reason = reject_reason(word, definition)
            if reason:
                raise ValueError(f"Curated word {word} failed validation: {reason}")

            accepted[word] = {
                "word": word,
                "definition": definition,
                "length": len(word),
                "rarity": "common",
                "_score": 0,
            }

    output_entries = sorted(
        ({key: value for key, value in entry.items() if key != "_score"} for entry in accepted.values()),
        key=lambda entry: (entry["length"], entry["word"]),
    )

    OUTPUT.write_text(json.dumps({"entries": output_entries}, indent=2) + "\n", encoding="utf-8")

    with REJECTS.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=["word", "reason", "definition"])
        writer.writeheader()
        writer.writerows(rejected)

    print(f"Accepted {len(output_entries)} Spell It Out entries.")
    print(f"Rejected {len(rejected)} entries.")
    print(f"Wrote {OUTPUT.relative_to(ROOT)}")
    print(f"Wrote {REJECTS.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
