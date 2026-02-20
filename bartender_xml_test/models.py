"""Data models for GHS hazard labels."""

from dataclasses import dataclass, field
from enum import Enum
from typing import Optional


class SignalWord(Enum):
    """GHS signal words."""
    DANGER = "Danger"
    WARNING = "Warning"
    PERICOLO = "Pericolo"
    ATTENZIONE = "Attenzione"


class GHSPictogram(Enum):
    """GHS hazard pictograms (UN codes)."""
    GHS01 = "GHS01"  # Exploding bomb
    GHS02 = "GHS02"  # Flame
    GHS03 = "GHS03"  # Flame over circle
    GHS04 = "GHS04"  # Gas cylinder
    GHS05 = "GHS05"  # Corrosion
    GHS06 = "GHS06"  # Skull and crossbones
    GHS07 = "GHS07"  # Exclamation mark
    GHS08 = "GHS08"  # Health hazard
    GHS09 = "GHS09"  # Environment


@dataclass
class HazardLabel:
    """Represents a GHS hazard label with all required fields.

    Fields follow GHS/CLP regulation requirements:
    - product_name: Product identifier
    - signal_word: "Danger" or "Warning"
    - pictograms: List of GHS pictogram codes
    - hazard_statements: H-codes with descriptions
    - precautionary_statements: P-codes with descriptions
    - supplier_name: Manufacturer/distributor name
    - supplier_address: Manufacturer/distributor address
    - supplier_phone: Emergency phone number
    """
    product_name: str
    signal_word: SignalWord
    pictograms: list[GHSPictogram] = field(default_factory=list)
    hazard_statements: list[str] = field(default_factory=list)
    precautionary_statements: list[str] = field(default_factory=list)
    supplier_name: str = ""
    supplier_address: str = ""
    supplier_phone: str = ""
    cas_number: str = ""
    un_number: str = ""
    quantity: str = ""
    batch_number: str = ""
    date: str = ""
    copies: int = 1


@dataclass
class BarTenderConfig:
    """Configuration for BarTender connection and print job."""
    host: str = "localhost"
    port: int = 5170  # Default Commander TCP port
    btw_format_path: str = ""
    printer_name: str = ""
    timeout: float = 30.0
    identical_copies: int = 1
    serialized_labels: int = 1
