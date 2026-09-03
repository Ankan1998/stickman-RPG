"""
Stage the markdown in this repo into a MkDocs-friendly `site-docs/` tree.

WHY THIS EXISTS
---------------
The docs are written to be read on GitHub, where relative paths like
`../../src/Rpg.Core/Combat/Battle.cs` resolve to real files. A published site has
no source tree, so those links would 404.

This script therefore does three things:

  1. Copies the markdown into the layout the site wants:

         README.md              ->  index.md
         docs/gamedev/*.md      ->  course/*.md        (the course)
         docs/*.md              ->  reference/*.md     (the project manual)

  2. Rewrites every internal markdown link for that new layout.

  3. Turns every link that points at a SOURCE FILE (.cs, .csproj, project.godot)
     into a GitHub blob URL, so clicking it on the site opens the real code.

`site-docs/` is generated. Never edit it, and never point this at `docs/` -
the source lives there and this script deletes its output directory first.

Run:  python scripts/build_docs.py
"""

from __future__ import annotations

import os
import re
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
STAGE = ROOT / "site-docs"

GITHUB_REPO = "Ankan1998/stickman-RPG"
GITHUB_BRANCH = "main"
BLOB = f"https://github.com/{GITHUB_REPO}/blob/{GITHUB_BRANCH}/"
TREE = f"https://github.com/{GITHUB_REPO}/tree/{GITHUB_BRANCH}/"

# Reference-manual order, matching the table in README.md.
REFERENCE_ORDER = [
    "how-to-play.md",
    "00-how-to-run.md",
    "01-getting-started.md",
    "02-csharp-crash-course.md",
    "03-godot-crash-course.md",
    "04-architecture.md",
    "05-code-tour.md",
    "06-anatomy-of-a-turn.md",
    "07-recipes.md",
    "08-glossary.md",
    "09-art-pipeline.md",
    "10-campaign-implementation-plan.md",
    "11-positioning.md",
    "roadmap.md",
]

LINK_RE = re.compile(r"(!?\[[^\]]*\])\(\s*([^)\s]+)\s*\)")
FENCE_RE = re.compile(r"^\s*(`{3,}|~{3,})")
INLINE_CODE_RE = re.compile(r"`+[^`\n]*`+")
PLACEHOLDER_RE = re.compile("\x00(\\d+)\x00")

EXTERNAL_PREFIXES = ("http://", "https://", "mailto:", "tel:", "#", "data:")

# Extensions that are real files in the repo but never pages on the site.
SOURCE_SUFFIXES = (".cs", ".csproj", ".sln", ".godot", ".py", ".json", ".tscn", ".yml", ".png")


def build_mapping() -> dict[str, str]:
    """Map each source markdown path -> its staged path. Both repo-relative, posix."""
    mapping: dict[str, str] = {"README.md": "index.md"}

    for page in sorted((ROOT / "docs" / "gamedev").glob("*.md")):
        dest = "course/index.md" if page.name == "README.md" else f"course/{page.name}"
        mapping[f"docs/gamedev/{page.name}"] = dest

    for page in sorted((ROOT / "docs").glob("*.md")):
        mapping[f"docs/{page.name}"] = f"reference/{page.name}"

    return mapping


def apply_outside_code(text: str, transform) -> str:
    """
    Run `transform` over the prose only, never over code.

    Code blocks are full of things that LOOK like markdown links - this repo has
    a C# string containing "[b](critical!)" - and rewriting inside one would
    silently corrupt the sample. Inline code spans get the same protection.
    """
    out: list[str] = []
    in_fence = False
    fence_marker = ""

    for line in text.splitlines(keepends=True):
        if in_fence:
            out.append(line)
            if line.lstrip().startswith(fence_marker):
                in_fence = False
            continue

        opening = FENCE_RE.match(line)
        if opening:
            in_fence = True
            fence_marker = opening.group(1)[:3]
            out.append(line)
            continue

        # Prose. Mask inline code spans, transform, then restore them.
        spans: list[str] = []

        def stash(match: re.Match) -> str:
            spans.append(match.group(0))
            return "\x00" + str(len(spans) - 1) + "\x00"

        masked = transform(INLINE_CODE_RE.sub(stash, line))
        out.append(PLACEHOLDER_RE.sub(lambda m: spans[int(m.group(1))], masked))

    return "".join(out)


def rewrite_links(
    text: str, src_rel: str, dest_rel: str, mapping: dict[str, str]
) -> tuple[str, list[str]]:
    """Re-point every internal link so it still resolves after staging."""
    src_dir = os.path.dirname(src_rel)
    dest_dir = os.path.dirname(dest_rel)
    unresolved: list[str] = []

    def replace(match: re.Match) -> str:
        label, target = match.group(1), match.group(2)

        if target.startswith(EXTERNAL_PREFIXES) or target.startswith("<"):
            return match.group(0)

        raw, sep, anchor = target.partition("#")
        if not raw:
            return match.group(0)

        resolved = os.path.normpath(os.path.join(src_dir, raw)).replace(os.sep, "/")

        # A staged markdown page: point at wherever it now lives.
        if resolved in mapping:
            new_path = os.path.relpath(mapping[resolved], dest_dir or ".").replace(os.sep, "/")
            return f"{label}({new_path}{sep}{anchor})"

        # A source file: send the reader to it on GitHub.
        if resolved.endswith(SOURCE_SUFFIXES) or (ROOT / resolved).is_file():
            return f"{label}({BLOB}{resolved}{sep}{anchor})"

        # A directory in the repo: link to the GitHub tree view.
        if (ROOT / resolved).is_dir():
            return f"{label}({TREE}{resolved})"

        unresolved.append(target)
        return match.group(0)

    staged = apply_outside_code(text, lambda chunk: LINK_RE.sub(replace, chunk))
    return staged, unresolved


def write_pages_file(path: Path, title: str | None, nav: list[str]) -> None:
    lines: list[str] = []
    if title:
        lines.append(f'title: "{title}"')
    if nav:
        lines.append("nav:")
        lines.extend(f"  - {entry}" for entry in nav)
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    if STAGE.exists():
        shutil.rmtree(STAGE)
    STAGE.mkdir(parents=True)

    mapping = build_mapping()
    print(f"Staging {len(mapping)} markdown pages -> {STAGE.name}/")

    all_unresolved: list[tuple[str, str]] = []

    for src_rel, dest_rel in mapping.items():
        source = ROOT / src_rel
        if not source.is_file():
            print(f"  ! missing, skipping: {src_rel}")
            continue

        text = source.read_text(encoding="utf-8")
        text, unresolved = rewrite_links(text, src_rel, dest_rel, mapping)
        all_unresolved.extend((src_rel, link) for link in unresolved)

        dest = STAGE / dest_rel
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_text(text, encoding="utf-8")

    # ---- sidebar ordering -------------------------------------------------

    write_pages_file(STAGE / ".pages", title=None, nav=["index.md", "course", "reference"])

    course = STAGE / "course"
    chapters = sorted(p.name for p in course.glob("*.md") if p.name != "index.md")
    write_pages_file(course / ".pages", title="The Course", nav=["index.md", *chapters])

    reference = STAGE / "reference"
    present = {p.name for p in reference.glob("*.md")}
    ordered = [name for name in REFERENCE_ORDER if name in present]
    ordered += sorted(present - set(ordered))          # anything new, alphabetically
    write_pages_file(reference / ".pages", title="Project Reference", nav=ordered)

    # ---- static extras ----------------------------------------------------

    assets = ROOT / "site-assets"
    if assets.is_dir():
        for item in assets.iterdir():
            target = STAGE / item.name
            if item.is_dir():
                shutil.copytree(item, target)
            else:
                shutil.copy2(item, target)
        print(f"Copied static assets from {assets.name}/")

    if all_unresolved:
        print(f"\n!! {len(all_unresolved)} link(s) could not be resolved:")
        for src, link in all_unresolved[:40]:
            print(f"   {src} -> {link}")
        return 1

    print(f"OK: {len(mapping)} pages staged, all internal links rewritten.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
