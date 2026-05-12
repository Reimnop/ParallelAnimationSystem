#!/usr/bin/env python3
"""
Generate .tmpx font files for a font family using Tmpx.Generator.exe.

Usage:
    generate_fonts.py <generator_exe> <font_dir> <output_dir> --character-range <range> [--character-range <range> ...]

The script searches <font_dir> for:
    <FontName>-Regular.ttf      (required)
    <FontName>-Bold.ttf         (optional)
    <FontName>-Italic.ttf       (optional)
    <FontName>-BoldItalic.ttf   (optional)

If Bold/Italic variants are missing, fake-bold (-fb) and/or fake-italic (-fi) flags are passed to
the generator to synthesize them from the Regular or the available real variant.

Note from a human: This script is vibe-coded!!! I'm lazy ok
"""

import argparse
import os
import subprocess
import sys
from pathlib import Path


def find_font(font_dir: Path, name: str, variant: str) -> Path | None:
    p = font_dir / f"{name}-{variant}.ttf"
    return p if p.exists() else None


def run(generator: Path, args: list[str]):
    cmd = [str(generator)] + args
    print("  $", " ".join(cmd))
    result = subprocess.run(cmd)
    if result.returncode != 0:
        print(f"ERROR: generator exited with code {result.returncode}", file=sys.stderr)
        sys.exit(result.returncode)


def generate(
    generator: Path,
    input_ttf: Path,
    output_path: Path,
    style_name: str,
    character_ranges: list[str],
    fake_bold: bool,
    fake_italic: bool,
):
    args = [
        "--input", str(input_ttf),
        "--output", str(output_path),
        "--style-name", style_name,
    ]
    for cr in character_ranges:
        args += ["--character-range", cr]
    if fake_bold:
        args.append("-fb")
    if fake_italic:
        args.append("-fi")
    run(generator, args)


def main():
    parser = argparse.ArgumentParser(description="Generate .tmpx files for a font family.")
    parser.add_argument("generator", type=Path, help="Path to Tmpx.Generator.exe")
    parser.add_argument("font_dir", type=Path, help="Directory containing the .ttf source files")
    parser.add_argument("output_dir", type=Path, help="Directory to write the .tmpx output files")
    parser.add_argument(
        "--character-range", "-c",
        dest="character_ranges",
        action="append",
        required=True,
        metavar="RANGE",
        help="Character range(s) to include, e.g. U+0020-U+007E or U+0021,U+00A4. Repeatable.",
    )
    args = parser.parse_args()

    generator: Path = args.generator
    font_dir: Path = args.font_dir
    output_dir: Path = args.output_dir
    character_ranges: list[str] = args.character_ranges

    if not generator.exists():
        print(f"ERROR: generator not found: {generator}", file=sys.stderr)
        sys.exit(1)

    if not font_dir.is_dir():
        print(f"ERROR: font directory not found: {font_dir}", file=sys.stderr)
        sys.exit(1)

    output_dir.mkdir(parents=True, exist_ok=True)

    # Discover font family name from the -Regular.ttf file
    regulars = list(font_dir.glob("*-Regular.ttf"))
    if not regulars:
        print(f"ERROR: no *-Regular.ttf found in {font_dir}", file=sys.stderr)
        sys.exit(1)
    if len(regulars) > 1:
        names = [r.name for r in regulars]
        print(f"ERROR: multiple *-Regular.ttf files found: {names}", file=sys.stderr)
        sys.exit(1)

    font_name = regulars[0].stem.removesuffix("-Regular")
    print(f"Font family: {font_name}")

    regular     = find_font(font_dir, font_name, "Regular")      # guaranteed to exist
    bold        = find_font(font_dir, font_name, "Bold")
    italic      = find_font(font_dir, font_name, "Italic")
    bold_italic = find_font(font_dir, font_name, "BoldItalic")

    has_bold   = bold is not None
    has_italic = italic is not None

    print(f"  Regular:    {regular}")
    print(f"  Bold:       {bold or '(missing — will synthesize)'}")
    print(f"  Italic:     {italic or '(missing — will synthesize)'}")
    print(f"  BoldItalic: {bold_italic or '(missing — will synthesize)'}")
    print()

    # ---- Regular ----
    print("Generating Regular...")
    generate(
        generator,
        input_ttf=regular,
        output_path=output_dir / f"{font_name}-Regular.tmpx",
        style_name="Regular",
        character_ranges=character_ranges,
        fake_bold=False,
        fake_italic=False,
    )

    # ---- Bold ----
    print("Generating Bold...")
    generate(
        generator,
        input_ttf=bold if has_bold else regular,
        output_path=output_dir / f"{font_name}-Bold.tmpx",
        style_name="Bold",
        character_ranges=character_ranges,
        fake_bold=not has_bold,
        fake_italic=False,
    )

    # ---- Italic ----
    print("Generating Italic...")
    generate(
        generator,
        input_ttf=italic if has_italic else regular,
        output_path=output_dir / f"{font_name}-Italic.tmpx",
        style_name="Italic",
        character_ranges=character_ranges,
        fake_bold=False,
        fake_italic=not has_italic,
    )

    # ---- BoldItalic ----
    # Source priority: real BoldItalic > real Bold (add fake italic) > real Italic (add fake bold) > Regular (add both)
    print("Generating BoldItalic...")
    if bold_italic is not None:
        bi_source, fb, fi = bold_italic, False, False
    elif has_bold:
        bi_source, fb, fi = bold, False, True
    elif has_italic:
        bi_source, fb, fi = italic, True, False
    else:
        bi_source, fb, fi = regular, True, True

    generate(
        generator,
        input_ttf=bi_source,
        output_path=output_dir / f"{font_name}-BoldItalic.tmpx",
        style_name="BoldItalic",
        character_ranges=character_ranges,
        fake_bold=fb,
        fake_italic=fi,
    )

    print()
    print("Done. Output files:")
    for f in sorted(output_dir.glob(f"{font_name}-*.tmpx")):
        print(f"  {f}")


if __name__ == "__main__":
    main()