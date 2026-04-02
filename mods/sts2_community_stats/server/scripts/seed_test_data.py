#!/usr/bin/env python3
"""
Seed script — generates realistic fake run data for testing.
Usage: python seed_test_data.py [--runs N] [--api-url URL]
"""

import argparse
import random
import httpx
import sys

# ── Game constants ──────────────────────────────────────────

CHARACTERS = ["IRONCLAD", "SILENT", "DEFECT", "WATCHER", "VAGABOND", "NECROMANCER"]

CARDS = {
    "IRONCLAD": [
        "BASH", "STRIKE", "DEFEND", "ANGER", "BODY_SLAM", "CLASH",
        "CLEAVE", "CLOTHESLINE", "FLEX", "HEADBUTT", "HEAVY_BLADE",
        "IRON_WAVE", "PERFECTED_STRIKE", "POMMEL_STRIKE", "SHRUG_IT_OFF",
        "SWORD_BOOMERANG", "THUNDERCLAP", "WAR_CRY", "WILD_STRIKE",
        "BATTLE_TRANCE", "BLOODLETTING", "BURNING_PACT", "CARNAGE",
        "COMBUST", "DARK_EMBRACE", "DISARM", "DROPKICK", "DUAL_WIELD",
        "ENTRENCH", "FLAME_BARRIER", "GHOSTLY_ARMOR", "HEMOKINESIS",
        "INFERNAL_BLADE", "INFLAME", "INTIMIDATE", "METALLICIZE",
        "POWER_THROUGH", "PUMMEL", "RAGE", "RAMPAGE", "RECKLESS_CHARGE",
        "RUPTURE", "SEARING_BLOW", "SECOND_WIND", "SEEING_RED",
        "SENTINEL", "SHOCKWAVE", "SPOT_WEAKNESS", "UPPERCUT", "WHIRLWIND",
    ],
    "SILENT": [
        "STRIKE", "DEFEND", "NEUTRALIZE", "SURVIVOR", "ACROBATICS",
        "BACKFLIP", "BANE", "BLADE_DANCE", "CLOAK_AND_DAGGER", "DAGGER_SPRAY",
        "DAGGER_THROW", "DEADLY_POISON", "DEFLECT", "DODGE_AND_ROLL",
        "FLYING_KNEE", "OUTMANEUVER", "PIERCING_WAIL", "POISONED_STAB",
        "PREPARED", "QUICK_SLASH", "SLICE", "SNEAKY_STRIKE", "SUCKER_PUNCH",
    ],
}

# Shared cards for characters without specific lists
DEFAULT_CARDS = ["STRIKE", "DEFEND", "BASH", "NEUTRALIZE"]

RELICS = [
    "BURNING_BLOOD", "RING_OF_THE_SNAKE", "CRACKED_CORE", "PURE_WATER",
    "BAG_OF_MARBLES", "BLOOD_VIAL", "BRONZE_SCALES", "CENTENNIAL_PUZZLE",
    "CERAMIC_FISH", "DREAM_CATCHER", "HAPPY_FLOWER", "JUZU_BRACELET",
    "LANTERN", "MAW_BANK", "MEAL_TICKET", "NUNCHAKU", "ODDLY_SMOOTH_STONE",
    "OMAMORI", "ORICHALCUM", "PEN_NIB", "POTION_BELT", "PRESERVED_INSECT",
    "REGAL_PILLOW", "SMILING_MASK", "SNAKE_SKULL", "STRAWBERRY",
    "THE_BOOT", "TINY_CHEST", "TOY_ORNITHOPTER", "VAJRA", "WAR_PAINT",
]

EVENTS = [
    "NEOW", "BIG_FISH", "DEAD_ADVENTURER", "GOLDEN_IDOL", "GOLDEN_SHRINE",
    "LIVING_WALL", "MUSHROOMS", "SCRAP_OOZE", "SHINING_LIGHT",
    "THE_CLERIC", "THE_SSSSSERPENT", "WING_STATUE", "WORLD_OF_GOOP",
]

ENCOUNTERS = {
    "normal": [
        "JAW_WORM", "CULTIST", "SMALL_SLIMES", "BLUE_SLAVER", "RED_SLAVER",
        "FUNGI_BEAST", "LOOTER", "ACID_SLIME_M", "SPIKE_SLIME_M",
        "SHELLED_PARASITE", "CHOSEN", "BYRD", "SPHERIC_GUARDIAN",
    ],
    "elite": [
        "GREMLIN_NOB", "LAGAVULIN", "SENTRY_AND_SENTRY",
        "BOOK_OF_STABBING", "GREMLIN_LEADER", "TASKMASTER",
    ],
    "boss": [
        "THE_GUARDIAN", "HEXAGHOST", "SLIME_BOSS",
        "THE_CHAMP", "THE_COLLECTOR", "AUTOMATON",
        "AWAKENED_ONE", "TIME_EATER", "DONU_AND_DECA",
    ],
}


def generate_run(game_version: str = "v0.99.1") -> dict:
    char = random.choice(CHARACTERS)
    asc = random.randint(0, 20)
    win = random.random() < 0.35  # ~35% win rate
    floor_reached = random.randint(40, 57) if win else random.randint(1, 56)

    cards_pool = CARDS.get(char, DEFAULT_CARDS)
    num_card_rewards = random.randint(5, 15)

    card_choices = []
    for _ in range(num_card_rewards):
        offered = random.sample(cards_pool, min(3, len(cards_pool)))
        picked = random.choice(offered) if random.random() < 0.7 else None
        for c in offered:
            card_choices.append({
                "card_id": c,
                "upgrade_level": 0,
                "was_picked": c == picked,
                "floor": random.randint(1, floor_reached),
            })

    num_events = random.randint(2, 6)
    event_choices = []
    for _ in range(num_events):
        event = random.choice(EVENTS)
        total = random.randint(2, 4)
        event_choices.append({
            "event_id": event,
            "option_index": random.randint(0, total - 1),
            "total_options": total,
        })

    num_relics = random.randint(3, 12)
    final_relics = random.sample(RELICS, min(num_relics, len(RELICS)))

    encounters_list = []
    for etype, pool in ENCOUNTERS.items():
        n = {"normal": random.randint(5, 12), "elite": random.randint(1, 3), "boss": random.randint(1, 3)}[etype]
        for _ in range(n):
            enc = random.choice(pool)
            died = not win and random.random() < 0.1
            encounters_list.append({
                "encounter_id": enc,
                "encounter_type": etype,
                "damage_taken": random.randint(0, 40),
                "turns_taken": random.randint(2, 12),
                "player_died": died,
                "floor": random.randint(1, floor_reached),
            })

    return {
        "mod_version": "1.0.0",
        "game_version": game_version,
        "character": char,
        "ascension": asc,
        "win": win,
        "player_win_rate": round(random.uniform(0.1, 0.8), 2),
        "num_players": 1,
        "floor_reached": floor_reached,
        "card_choices": card_choices,
        "event_choices": event_choices,
        "final_deck": [{"card_id": c, "upgrade_level": random.randint(0, 1)}
                       for c in random.sample(cards_pool, min(random.randint(15, 35), len(cards_pool)))],
        "final_relics": final_relics,
        "shop_purchases": [],
        "card_removals": [],
        "card_upgrades": [],
        "encounters": encounters_list,
        "contributions": [],
    }


def main():
    parser = argparse.ArgumentParser(description="Seed STS2 Stats database with test runs")
    parser.add_argument("--runs", type=int, default=100, help="Number of runs to generate")
    parser.add_argument("--api-url", default="http://localhost:8080", help="API base URL")
    parser.add_argument("--version", default="v0.99.1", help="Game version")
    args = parser.parse_args()

    print(f"Seeding {args.runs} runs to {args.api_url}/v1/runs ...")

    success = 0
    errors = 0
    with httpx.Client(timeout=30) as client:
        for i in range(args.runs):
            run = generate_run(args.version)
            try:
                resp = client.post(f"{args.api_url}/v1/runs", json=run)
                if resp.status_code == 200:
                    success += 1
                else:
                    errors += 1
                    if errors <= 3:
                        print(f"  Error {resp.status_code}: {resp.text[:200]}")
            except Exception as e:
                errors += 1
                if errors <= 3:
                    print(f"  Error: {e}")

            if (i + 1) % 10 == 0:
                print(f"  {i + 1}/{args.runs} (ok={success}, err={errors})")

    print(f"\nDone: {success} uploaded, {errors} failed")
    return 0 if errors == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
