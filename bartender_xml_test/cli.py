"""CLI application for testing BarTender XML label printing."""

import argparse
import json
import logging
import sys
from pathlib import Path

from .btxml_builder import BTXMLBuilder
from .commander_client import CommanderClient
from .models import BarTenderConfig, HazardLabel, SignalWord, GHSPictogram
from .sample_labels import SAMPLE_LABELS


def setup_logging(verbose: bool) -> None:
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
        datefmt="%H:%M:%S",
    )


def cmd_generate(args: argparse.Namespace) -> None:
    """Generate BTXML XML and save to file or print to stdout."""
    config = _build_config(args)
    builder = BTXMLBuilder()

    if args.sample:
        if args.sample == "all":
            labels = [fn() for fn in SAMPLE_LABELS.values()]
            xml = builder.build_multi(labels, config)
        else:
            label = SAMPLE_LABELS[args.sample]()
            xml = builder.build(label, config)
    elif args.json_file:
        label = _load_label_from_json(args.json_file)
        xml = builder.build(label, config)
    else:
        print("Error: specify --sample or --json-file", file=sys.stderr)
        sys.exit(1)

    if args.output:
        Path(args.output).write_text(xml, encoding="utf-8")
        print(f"BTXML saved to: {args.output}")
    else:
        print(xml)


def cmd_send(args: argparse.Namespace) -> None:
    """Send a BTXML script to BarTender Commander via TCP."""
    config = _build_config(args)
    client = CommanderClient(config)

    if args.xml_file:
        btxml = Path(args.xml_file).read_text(encoding="utf-8")
    else:
        builder = BTXMLBuilder()
        if args.sample:
            label = SAMPLE_LABELS[args.sample]()
        elif args.json_file:
            label = _load_label_from_json(args.json_file)
        else:
            print("Error: specify --xml-file, --sample, or --json-file", file=sys.stderr)
            sys.exit(1)
        btxml = builder.build(label, config)

    print(f"Connecting to Commander at {config.host}:{config.port}...")
    try:
        response = client.send(btxml)
        print("\n--- BarTender Response ---")
        print(response)
        print("--- End Response ---\n")
    except (ConnectionError, TimeoutError) as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)


def cmd_test_connection(args: argparse.Namespace) -> None:
    """Test TCP connectivity to BarTender Commander."""
    config = _build_config(args)
    client = CommanderClient(config)

    print(f"Testing connection to Commander at {config.host}:{config.port}...")
    if client.test_connection():
        print("OK - Commander is reachable")
    else:
        print("FAILED - Commander is not reachable", file=sys.stderr)
        sys.exit(1)


def cmd_list_samples(args: argparse.Namespace) -> None:
    """List available sample hazard labels."""
    print("Available sample labels:\n")
    for name, factory in SAMPLE_LABELS.items():
        label = factory()
        pictograms = ", ".join(p.value for p in label.pictograms)
        print(f"  {name:12s}  {label.product_name}")
        print(f"  {'':12s}  Signal: {label.signal_word.value} | Pictograms: {pictograms}")
        print(f"  {'':12s}  CAS: {label.cas_number}")
        print()


def _build_config(args: argparse.Namespace) -> BarTenderConfig:
    return BarTenderConfig(
        host=getattr(args, "host", "localhost"),
        port=getattr(args, "port", 5170),
        btw_format_path=getattr(args, "btw_path", "") or "",
        printer_name=getattr(args, "printer", "") or "",
        timeout=getattr(args, "timeout", 30.0),
        identical_copies=getattr(args, "copies", 1),
    )


def _load_label_from_json(path: str) -> HazardLabel:
    """Load a HazardLabel from a JSON file."""
    data = json.loads(Path(path).read_text(encoding="utf-8"))

    pictograms = [GHSPictogram(p) for p in data.get("pictograms", [])]

    signal_raw = data.get("signal_word", "Danger")
    try:
        signal_word = SignalWord(signal_raw)
    except ValueError:
        signal_word = SignalWord.PERICOLO

    return HazardLabel(
        product_name=data["product_name"],
        signal_word=signal_word,
        pictograms=pictograms,
        hazard_statements=data.get("hazard_statements", []),
        precautionary_statements=data.get("precautionary_statements", []),
        supplier_name=data.get("supplier_name", ""),
        supplier_address=data.get("supplier_address", ""),
        supplier_phone=data.get("supplier_phone", ""),
        cas_number=data.get("cas_number", ""),
        un_number=data.get("un_number", ""),
        quantity=data.get("quantity", ""),
        batch_number=data.get("batch_number", ""),
        date=data.get("date", ""),
        copies=data.get("copies", 1),
    )


def main() -> None:
    parser = argparse.ArgumentParser(
        prog="btxml-test",
        description="Test tool for sending BTXML scripts to BarTender for hazard label printing",
    )
    parser.add_argument("-v", "--verbose", action="store_true", help="Enable debug logging")

    subparsers = parser.add_subparsers(dest="command", required=True)

    # --- generate ---
    gen_parser = subparsers.add_parser("generate", help="Generate BTXML XML (no sending)")
    gen_parser.add_argument("--sample", choices=list(SAMPLE_LABELS.keys()) + ["all"],
                            help="Use a built-in sample label")
    gen_parser.add_argument("--json-file", help="Load label data from a JSON file")
    gen_parser.add_argument("--btw-path", default="C:\\Labels\\HazardLabel.btw",
                            help="Path to .btw template on the BarTender server")
    gen_parser.add_argument("--printer", default="", help="Target printer name")
    gen_parser.add_argument("--copies", type=int, default=1, help="Number of copies")
    gen_parser.add_argument("-o", "--output", help="Save XML to file instead of stdout")
    gen_parser.set_defaults(func=cmd_generate)

    # --- send ---
    send_parser = subparsers.add_parser("send", help="Send BTXML to BarTender Commander")
    send_parser.add_argument("--host", default="localhost", help="Commander host (default: localhost)")
    send_parser.add_argument("--port", type=int, default=5170, help="Commander TCP port (default: 5170)")
    send_parser.add_argument("--timeout", type=float, default=30.0, help="TCP timeout in seconds")
    send_parser.add_argument("--xml-file", help="Send a pre-built BTXML file")
    send_parser.add_argument("--sample", choices=list(SAMPLE_LABELS.keys()),
                            help="Use a built-in sample label")
    send_parser.add_argument("--json-file", help="Load label data from a JSON file")
    send_parser.add_argument("--btw-path", default="C:\\Labels\\HazardLabel.btw",
                            help="Path to .btw template on the BarTender server")
    send_parser.add_argument("--printer", default="", help="Target printer name")
    send_parser.add_argument("--copies", type=int, default=1, help="Number of copies")
    send_parser.set_defaults(func=cmd_send)

    # --- test-connection ---
    conn_parser = subparsers.add_parser("test-connection", help="Test Commander TCP connectivity")
    conn_parser.add_argument("--host", default="localhost", help="Commander host")
    conn_parser.add_argument("--port", type=int, default=5170, help="Commander TCP port")
    conn_parser.add_argument("--timeout", type=float, default=5.0, help="TCP timeout")
    conn_parser.set_defaults(func=cmd_test_connection)

    # --- list-samples ---
    list_parser = subparsers.add_parser("list-samples", help="List available sample labels")
    list_parser.set_defaults(func=cmd_list_samples)

    args = parser.parse_args()
    setup_logging(args.verbose)
    args.func(args)


if __name__ == "__main__":
    main()
