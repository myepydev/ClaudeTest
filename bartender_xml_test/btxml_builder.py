"""BTXML script builder for BarTender hazard label printing."""

import xml.etree.ElementTree as ET
from xml.dom import minidom
from typing import Optional

from .models import HazardLabel, BarTenderConfig


class BTXMLBuilder:
    """Builds BTXML (BarTender XML Script) documents for label printing.

    Generates XML conforming to BTXML Version 2.0 specification,
    using NamedSubString elements to pass data to BarTender label templates.
    """

    BTXML_VERSION = "2.0"

    # Default mapping from HazardLabel fields to BarTender NamedSubString names.
    # These names must match the Named Data Sources configured in the .btw template.
    DEFAULT_FIELD_MAP = {
        "product_name": "ProductName",
        "signal_word": "SignalWord",
        "hazard_statements": "HazardStatements",
        "precautionary_statements": "PrecautionaryStatements",
        "supplier_name": "SupplierName",
        "supplier_address": "SupplierAddress",
        "supplier_phone": "SupplierPhone",
        "cas_number": "CASNumber",
        "un_number": "UNNumber",
        "quantity": "Quantity",
        "batch_number": "BatchNumber",
        "date": "Date",
        "pictograms": "Pictograms",
    }

    def __init__(self, field_map: Optional[dict[str, str]] = None):
        """Initialize the builder.

        Args:
            field_map: Optional custom mapping from HazardLabel field names
                       to BarTender NamedSubString names. If None, uses defaults.
        """
        self.field_map = field_map or self.DEFAULT_FIELD_MAP.copy()

    def build(
        self,
        label: HazardLabel,
        config: BarTenderConfig,
        job_name: str = "HazardLabel",
    ) -> str:
        """Build a complete BTXML script for printing a hazard label.

        Args:
            label: The hazard label data to print.
            config: BarTender connection/print configuration.
            job_name: Name for the print job.

        Returns:
            Complete BTXML XML string ready to send to BarTender.
        """
        root = ET.Element("XMLScript")
        root.set("Version", self.BTXML_VERSION)
        root.set("Name", job_name)
        root.set("ID", f"{job_name}_001")

        command = ET.SubElement(root, "Command")
        command.set("Name", job_name)

        print_elem = ET.SubElement(command, "Print")

        # Format path (path to .btw label template)
        format_elem = ET.SubElement(print_elem, "Format")
        format_elem.text = config.btw_format_path

        # Named data source values
        self._add_named_substrings(print_elem, label)

        # Print setup
        print_setup = ET.SubElement(print_elem, "PrintSetup")

        if config.printer_name:
            printer = ET.SubElement(print_setup, "Printer")
            printer.text = config.printer_name

        identical = ET.SubElement(print_setup, "IdenticalCopiesOfLabel")
        identical.text = str(label.copies if label.copies > 1 else config.identical_copies)

        serialized = ET.SubElement(print_setup, "NumberSerializedLabels")
        serialized.text = str(config.serialized_labels)

        return self._to_pretty_xml(root)

    def build_multi(
        self,
        labels: list[HazardLabel],
        config: BarTenderConfig,
        job_name_prefix: str = "HazardLabel",
    ) -> str:
        """Build a BTXML script with multiple print commands (one per label).

        Args:
            labels: List of hazard labels to print.
            config: BarTender connection/print configuration.
            job_name_prefix: Prefix for each print job name.

        Returns:
            Complete BTXML XML string with multiple commands.
        """
        root = ET.Element("XMLScript")
        root.set("Version", self.BTXML_VERSION)
        root.set("Name", f"{job_name_prefix}_Batch")

        for i, label in enumerate(labels, start=1):
            name = f"{job_name_prefix}_{i:03d}"
            command = ET.SubElement(root, "Command")
            command.set("Name", name)

            print_elem = ET.SubElement(command, "Print")

            format_elem = ET.SubElement(print_elem, "Format")
            format_elem.text = config.btw_format_path

            self._add_named_substrings(print_elem, label)

            print_setup = ET.SubElement(print_elem, "PrintSetup")
            if config.printer_name:
                printer = ET.SubElement(print_setup, "Printer")
                printer.text = config.printer_name

            identical = ET.SubElement(print_setup, "IdenticalCopiesOfLabel")
            identical.text = str(label.copies if label.copies > 1 else config.identical_copies)

            serialized = ET.SubElement(print_setup, "NumberSerializedLabels")
            serialized.text = str(config.serialized_labels)

        return self._to_pretty_xml(root)

    def build_format_setup(
        self,
        label: HazardLabel,
        config: BarTenderConfig,
        job_name: str = "HazardLabel",
    ) -> str:
        """Build a BTXML FormatSetup command (configure without printing).

        Useful for previewing or validating label data in BarTender Designer.

        Args:
            label: The hazard label data.
            config: BarTender configuration.
            job_name: Name for the job.

        Returns:
            BTXML XML string with FormatSetup command.
        """
        root = ET.Element("XMLScript")
        root.set("Version", self.BTXML_VERSION)
        root.set("Name", job_name)

        command = ET.SubElement(root, "Command")
        command.set("Name", job_name)

        format_setup = ET.SubElement(command, "FormatSetup")

        format_elem = ET.SubElement(format_setup, "Format")
        format_elem.text = config.btw_format_path

        self._add_named_substrings(format_setup, label)

        if config.printer_name:
            print_setup = ET.SubElement(format_setup, "PrintSetup")
            printer = ET.SubElement(print_setup, "Printer")
            printer.text = config.printer_name

        return self._to_pretty_xml(root)

    def _add_named_substrings(self, parent: ET.Element, label: HazardLabel) -> None:
        """Add NamedSubString elements for all label fields."""
        data = self._label_to_dict(label)
        for field_name, bt_name in self.field_map.items():
            value = data.get(field_name, "")
            if value:
                nss = ET.SubElement(parent, "NamedSubString")
                nss.set("Name", bt_name)
                val_elem = ET.SubElement(nss, "Value")
                val_elem.text = str(value)

    def _label_to_dict(self, label: HazardLabel) -> dict[str, str]:
        """Convert a HazardLabel to a flat dict of string values."""
        return {
            "product_name": label.product_name,
            "signal_word": label.signal_word.value,
            "pictograms": ", ".join(p.value for p in label.pictograms),
            "hazard_statements": "\n".join(label.hazard_statements),
            "precautionary_statements": "\n".join(label.precautionary_statements),
            "supplier_name": label.supplier_name,
            "supplier_address": label.supplier_address,
            "supplier_phone": label.supplier_phone,
            "cas_number": label.cas_number,
            "un_number": label.un_number,
            "quantity": label.quantity,
            "batch_number": label.batch_number,
            "date": label.date,
        }

    @staticmethod
    def _to_pretty_xml(root: ET.Element) -> str:
        """Convert an ElementTree to a pretty-printed XML string."""
        rough = ET.tostring(root, encoding="unicode", xml_declaration=False)
        dom = minidom.parseString(rough)
        pretty = dom.toprettyxml(indent="  ", encoding=None)
        # Remove the minidom XML declaration (we add our own)
        lines = pretty.split("\n")
        if lines[0].startswith("<?xml"):
            lines = lines[1:]
        xml_body = "\n".join(line for line in lines if line.strip())
        return f'<?xml version="1.0" encoding="utf-8"?>\n{xml_body}\n'
