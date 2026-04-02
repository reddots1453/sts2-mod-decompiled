"""Pydantic models — mirrors the C# ApiModels.cs on the client side."""

from pydantic import BaseModel, Field, field_validator


# ============================================================
#  Upload Models (Client → Server)
# ============================================================

class CardChoiceUpload(BaseModel):
    card_id: str = Field(..., max_length=64)
    upgrade_level: int = Field(0, ge=0, le=10)
    was_picked: bool
    floor: int = Field(0, ge=0, le=200)


class EventChoiceUpload(BaseModel):
    event_id: str = Field(..., max_length=64)
    option_index: int = Field(..., ge=0, le=20)
    total_options: int = Field(..., ge=1, le=20)


class DeckCardUpload(BaseModel):
    card_id: str = Field(..., max_length=64)
    upgrade_level: int = Field(0, ge=0, le=10)


class ShopPurchaseUpload(BaseModel):
    item_id: str = Field(..., max_length=64)
    item_type: str = Field(..., pattern=r"^(card|relic|potion)$")
    cost: int = Field(..., ge=0, le=9999)
    floor: int = Field(0, ge=0, le=200)


class CardRemovalUpload(BaseModel):
    card_id: str = Field(..., max_length=64)
    source: str = Field(..., pattern=r"^(shop|event)$")
    floor: int = Field(0, ge=0, le=200)


class CardUpgradeUpload(BaseModel):
    card_id: str = Field(..., max_length=64)
    source: str = Field(..., pattern=r"^(campfire|event|other)$")


class EncounterUpload(BaseModel):
    encounter_id: str = Field(..., max_length=64)
    encounter_type: str = Field(..., pattern=r"^(normal|elite|boss)$")
    damage_taken: int = Field(..., ge=0, le=999999)
    turns_taken: int = Field(..., ge=0, le=999)
    player_died: bool
    floor: int = Field(0, ge=0, le=200)


class ContributionUpload(BaseModel):
    source_id: str = Field(..., max_length=64)
    source_type: str = Field(..., pattern=r"^(card|relic)$")
    encounter_id: str | None = None
    times_played: int = Field(0, ge=0)
    direct_damage: int = Field(0, ge=0)
    attributed_damage: int = Field(0, ge=0)
    block_gained: int = Field(0, ge=0)
    cards_drawn: int = Field(0, ge=0)
    energy_gained: int = Field(0, ge=0)
    hp_healed: int = Field(0, ge=0)


class RunUploadPayload(BaseModel):
    mod_version: str = Field(..., max_length=16)
    game_version: str = Field(..., max_length=16)
    character: str = Field(..., max_length=32)
    ascension: int = Field(..., ge=0, le=20)
    win: bool
    player_win_rate: float = Field(0.0, ge=0.0, le=1.0)
    num_players: int = Field(1, ge=1, le=4)
    floor_reached: int = Field(0, ge=0, le=200)

    card_choices: list[CardChoiceUpload] = Field(default_factory=list, max_length=500)
    event_choices: list[EventChoiceUpload] = Field(default_factory=list, max_length=100)
    final_deck: list[DeckCardUpload] = Field(default_factory=list, max_length=200)
    final_relics: list[str] = Field(default_factory=list, max_length=50)
    shop_purchases: list[ShopPurchaseUpload] = Field(default_factory=list, max_length=100)
    card_removals: list[CardRemovalUpload] = Field(default_factory=list, max_length=100)
    card_upgrades: list[CardUpgradeUpload] = Field(default_factory=list, max_length=100)
    encounters: list[EncounterUpload] = Field(default_factory=list, max_length=100)
    contributions: list[ContributionUpload] = Field(default_factory=list, max_length=5000)

    @field_validator("character")
    @classmethod
    def character_upper(cls, v: str) -> str:
        return v.upper()


# ============================================================
#  Response Models (Server → Client)
# ============================================================

class CardStats(BaseModel):
    pick: float = 0.0
    win: float = 0.0
    removal: float = 0.0
    upgrade: float = 0.0
    shop_buy: float = 0.0
    n: int = 0


class RelicStats(BaseModel):
    win: float = 0.0
    pick: float = 0.0
    shop_buy: float = 0.0
    n: int = 0


class EventOptionStats(BaseModel):
    idx: int
    sel: float = 0.0
    win: float = 0.0
    n: int = 0


class EventStats(BaseModel):
    options: list[EventOptionStats] = []


class EncounterStats(BaseModel):
    type: str = ""
    avg_dmg: float = 0.0
    death: float = 0.0
    avg_turns: float = 0.0
    n: int = 0


class BulkStatsBundle(BaseModel):
    generated_at: str = ""
    total_runs: int = 0
    cards: dict[str, CardStats] = {}
    relics: dict[str, RelicStats] = {}
    events: dict[str, EventStats] = {}
    encounters: dict[str, EncounterStats] = {}
