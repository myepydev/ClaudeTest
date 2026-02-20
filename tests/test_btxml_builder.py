"""Tests for BTXML builder."""

import xml.etree.ElementTree as ET

from bartender_xml_test.btxml_builder import BTXMLBuilder
from bartender_xml_test.models import (
    BarTenderConfig,
    GHSPictogram,
    HazardLabel,
    SignalWord,
)
from bartender_xml_test.sample_labels import SAMPLE_LABELS


def _make_label() -> HazardLabel:
    return HazardLabel(
        product_name="Test Product",
        signal_word=SignalWord.PERICOLO,
        pictograms=[GHSPictogram.GHS02, GHSPictogram.GHS07],
        hazard_statements=["H225 - Liquido infiammabile"],
        precautionary_statements=["P210 - Tenere lontano dal calore"],
        supplier_name="Supplier Srl",
        supplier_address="Via Test 1",
        supplier_phone="+39 000 000",
        cas_number="123-45-6",
        copies=2,
    )


def _make_config() -> BarTenderConfig:
    return BarTenderConfig(
        btw_format_path="C:\\Labels\\Test.btw",
        printer_name="TestPrinter",
    )


class TestBTXMLBuilder:
    def test_build_produces_valid_xml(self):
        builder = BTXMLBuilder()
        xml_str = builder.build(_make_label(), _make_config())
        root = ET.fromstring(xml_str)
        assert root.tag == "XMLScript"
        assert root.attrib["Version"] == "2.0"

    def test_build_contains_format_path(self):
        builder = BTXMLBuilder()
        xml_str = builder.build(_make_label(), _make_config())
        root = ET.fromstring(xml_str)
        fmt = root.find(".//Format")
        assert fmt is not None
        assert fmt.text == "C:\\Labels\\Test.btw"

    def test_build_contains_printer(self):
        builder = BTXMLBuilder()
        xml_str = builder.build(_make_label(), _make_config())
        root = ET.fromstring(xml_str)
        printer = root.find(".//Printer")
        assert printer is not None
        assert printer.text == "TestPrinter"

    def test_build_contains_named_substrings(self):
        builder = BTXMLBuilder()
        xml_str = builder.build(_make_label(), _make_config())
        root = ET.fromstring(xml_str)
        nss_elements = root.findall(".//NamedSubString")
        names = {el.attrib["Name"] for el in nss_elements}
        assert "ProductName" in names
        assert "SignalWord" in names
        assert "HazardStatements" in names

    def test_signal_word_value(self):
        builder = BTXMLBuilder()
        xml_str = builder.build(_make_label(), _make_config())
        root = ET.fromstring(xml_str)
        for nss in root.findall(".//NamedSubString"):
            if nss.attrib["Name"] == "SignalWord":
                assert nss.find("Value").text == "Pericolo"
                break
        else:
            raise AssertionError("SignalWord NamedSubString not found")

    def test_copies_from_label(self):
        builder = BTXMLBuilder()
        xml_str = builder.build(_make_label(), _make_config())
        root = ET.fromstring(xml_str)
        copies = root.find(".//IdenticalCopiesOfLabel")
        assert copies is not None
        assert copies.text == "2"

    def test_build_multi(self):
        builder = BTXMLBuilder()
        labels = [_make_label(), _make_label()]
        xml_str = builder.build_multi(labels, _make_config())
        root = ET.fromstring(xml_str)
        commands = root.findall("Command")
        assert len(commands) == 2

    def test_build_format_setup(self):
        builder = BTXMLBuilder()
        xml_str = builder.build_format_setup(_make_label(), _make_config())
        root = ET.fromstring(xml_str)
        fs = root.find(".//FormatSetup")
        assert fs is not None
        assert root.find(".//Print") is None

    def test_all_samples_produce_valid_xml(self):
        builder = BTXMLBuilder()
        config = _make_config()
        for name, factory in SAMPLE_LABELS.items():
            label = factory()
            xml_str = builder.build(label, config)
            root = ET.fromstring(xml_str)
            assert root.tag == "XMLScript", f"Failed for sample: {name}"

    def test_pictograms_joined(self):
        builder = BTXMLBuilder()
        xml_str = builder.build(_make_label(), _make_config())
        root = ET.fromstring(xml_str)
        for nss in root.findall(".//NamedSubString"):
            if nss.attrib["Name"] == "Pictograms":
                assert "GHS02" in nss.find("Value").text
                assert "GHS07" in nss.find("Value").text
                break

    def test_custom_field_map(self):
        custom_map = {"product_name": "CustomProduct", "signal_word": "CustomSignal"}
        builder = BTXMLBuilder(field_map=custom_map)
        xml_str = builder.build(_make_label(), _make_config())
        root = ET.fromstring(xml_str)
        names = {el.attrib["Name"] for el in root.findall(".//NamedSubString")}
        assert "CustomProduct" in names
        assert "CustomSignal" in names
        assert "ProductName" not in names

    def test_empty_fields_omitted(self):
        label = HazardLabel(
            product_name="Minimal",
            signal_word=SignalWord.ATTENZIONE,
        )
        builder = BTXMLBuilder()
        xml_str = builder.build(label, _make_config())
        root = ET.fromstring(xml_str)
        names = {el.attrib["Name"] for el in root.findall(".//NamedSubString")}
        assert "ProductName" in names
        assert "SignalWord" in names
        # Empty fields should not appear
        assert "CASNumber" not in names
        assert "UNNumber" not in names

    def test_no_printer_when_empty(self):
        config = BarTenderConfig(btw_format_path="C:\\test.btw", printer_name="")
        builder = BTXMLBuilder()
        xml_str = builder.build(_make_label(), config)
        root = ET.fromstring(xml_str)
        assert root.find(".//Printer") is None
