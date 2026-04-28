#!/usr/bin/env python3
"""
Sync diagrams/ from this repo into the GitHub Wiki repo (`<repo>.wiki.git`).

The Wiki has a flat namespace, so this script:
  1. Clones the wiki repo.
  2. Wipes existing .md files (so deletions in diagrams/ propagate).
  3. Flattens diagrams/**/*.md to <Folder>-<Filename>.md naming.
  4. Rewrites cross-doc links (../backend/01-architecture.md) to flat names.
  5. Rewrites source-file links (LessonsHub/Controllers/...) to absolute GitHub URLs.
  6. Generates _Sidebar.md grouping pages by folder.
  7. Commits. Pushes only with --push.

Usage:
  python scripts/sync-wiki.py            # clone + commit, leave for review
  python scripts/sync-wiki.py --push     # clone + commit + push

Pre-requisite: the wiki must be initialized. Visit
  https://github.com/<owner>/<repo>/wiki
once and create a first page (any content). Re-run the script after.

Stdlib only — no external deps.
"""
from __future__ import annotations

import argparse
import os
import re
import shutil
import stat
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
DIAGRAMS = REPO_ROOT / "diagrams"
WIKI_DIR = REPO_ROOT / ".wiki-sync"

# Files inside diagrams/ to skip when syncing to the wiki.
# Paths are relative to diagrams/ (POSIX form). PROMPT.md is the regeneration
# spec — meant for an agent re-running it, not for end-user browsing.
WIKI_IGNORE = {"PROMPT.md"}


def _force_remove(func, path, exc):
    """rmtree onexc callback: chmod read-only files writable, then retry.

    Windows refuses os.unlink on the read-only files git keeps under
    .git/objects/. shutil.rmtree raises PermissionError without this.
    No-op on Linux/macOS.
    """
    os.chmod(path, stat.S_IWRITE)
    func(path)


def run(cmd: list[str], cwd: Path | None = None, check: bool = True) -> tuple[int, str, str]:
    """Run a command, return (returncode, stdout, stderr)."""
    p = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True)
    if check and p.returncode != 0:
        print(f"$ {' '.join(cmd)}", file=sys.stderr)
        print(p.stdout, file=sys.stderr)
        print(p.stderr, file=sys.stderr)
        sys.exit(p.returncode)
    return p.returncode, p.stdout.strip(), p.stderr.strip()


def parse_origin(origin: str) -> tuple[str, str, str]:
    """Return (owner, repo, wiki_url) — wiki_url mirrors the auth method of origin."""
    m = re.search(r"github\.com[:/]([^/]+)/([^/]+?)(\.git)?$", origin)
    if not m:
        sys.exit(f"Cannot parse GitHub repo from origin: {origin}")
    owner, repo = m.group(1), m.group(2)
    base = origin[: -4] if origin.endswith(".git") else origin
    return owner, repo, f"{base}.wiki.git"


def title_case(name: str) -> str:
    """Convert 'lesson-plan-default' → 'Lesson-Plan-Default'."""
    return "-".join(p.capitalize() for p in name.split("-"))


def to_wiki_name(rel_path: str) -> str:
    """Map diagrams/-relative path → flat wiki page name (no .md)."""
    parts = list(Path(rel_path).parts)
    fname = parts[-1]
    if fname.lower() == "readme.md":
        if len(parts) == 1:
            return "Home"
        return title_case(parts[-2])
    stem = Path(fname).stem
    if len(parts) == 1:
        return title_case(stem)
    folder = parts[-2]
    return f"{title_case(folder)}-{title_case(stem)}"


def build_mapping() -> dict[str, str]:
    """{ 'backend/01-architecture.md': 'Backend-01-Architecture', ... }"""
    mapping: dict[str, str] = {}
    for src in sorted(DIAGRAMS.rglob("*.md")):
        rel = src.relative_to(DIAGRAMS).as_posix()
        if rel in WIKI_IGNORE:
            continue
        mapping[rel] = to_wiki_name(rel)
    return mapping


_LINK_RE = re.compile(r"\[([^\]]+)\]\(([^)]+)\)")


def rewrite_links(content: str, src_rel: str, mapping: dict[str, str], owner: str, repo: str) -> str:
    """Rewrite all markdown links inside `content`.

    - `https://...` / `http://` / `mailto:` — left alone.
    - Anchor-only `(#foo)` — left alone.
    - Relative `.md` link inside diagrams/ — rewritten to flat wiki name.
    - Relative non-md link — assumed to be a source file in the main repo,
      rewritten to absolute `https://github.com/{owner}/{repo}/blob/main/...` URL.
    """
    src_dir = (DIAGRAMS / src_rel).parent

    def repl(m: re.Match[str]) -> str:
        text, target = m.group(1), m.group(2)
        target_path, _, anchor = target.partition("#")
        anchor_part = f"#{anchor}" if anchor else ""

        # Skip URLs and anchor-only.
        if target_path.startswith(("http://", "https://", "mailto:")):
            return m.group(0)
        if not target_path:
            return m.group(0)

        # Resolve relative to the source file's directory.
        try:
            resolved = (src_dir / target_path).resolve()
        except Exception:
            return m.group(0)

        # Inside diagrams/ → flat wiki name.
        try:
            rel_to_diagrams = resolved.relative_to(DIAGRAMS).as_posix()
        except ValueError:
            rel_to_diagrams = None

        if rel_to_diagrams and rel_to_diagrams.endswith(".md"):
            wiki = mapping.get(rel_to_diagrams)
            if wiki:
                return f"[{text}]({wiki}{anchor_part})"

        # Otherwise treat as a source-file link in the main repo.
        try:
            rel_repo = resolved.relative_to(REPO_ROOT).as_posix()
        except ValueError:
            return m.group(0)

        return f"[{text}](https://github.com/{owner}/{repo}/blob/main/{rel_repo}{anchor_part})"

    return _LINK_RE.sub(repl, content)


def build_sidebar(mapping: dict[str, str]) -> str:
    """Group pages by folder, render as a wiki sidebar."""
    groups: dict[str, list[tuple[str, str]]] = {}
    for src_rel, wiki in mapping.items():
        parts = Path(src_rel).parts
        group = "Top" if len(parts) == 1 else title_case(parts[0])
        groups.setdefault(group, []).append((src_rel, wiki))

    # Order groups: Top first, then alphabetical.
    order = ["Top"] + sorted(g for g in groups if g != "Top")
    lines = ["# LessonsHub Documentation", ""]
    for g in order:
        if g not in groups:
            continue
        lines.append(f"## {g}")
        for src_rel, wiki in groups[g]:
            display = wiki.replace("-", " ")
            lines.append(f"- [[{display}|{wiki}]]")
        lines.append("")
    return "\n".join(lines)


def main() -> None:
    ap = argparse.ArgumentParser(description="Sync diagrams/ to the GitHub Wiki.")
    ap.add_argument("--push", action="store_true", help="git push after committing")
    args = ap.parse_args()

    if not DIAGRAMS.is_dir():
        sys.exit(f"diagrams/ not found at {DIAGRAMS}")

    _, origin, _ = run(["git", "remote", "get-url", "origin"], cwd=REPO_ROOT)
    owner, repo, wiki_url = parse_origin(origin)
    print(f"Repo: {owner}/{repo}")
    print(f"Wiki: {wiki_url}")

    if WIKI_DIR.exists():
        shutil.rmtree(WIKI_DIR, onexc=_force_remove)

    print(f"Cloning wiki into {WIKI_DIR} ...")
    rc, _, err = run(["git", "clone", wiki_url, str(WIKI_DIR)], check=False)
    if rc != 0:
        print(err, file=sys.stderr)
        sys.exit(
            "\nWiki clone failed. Most likely cause: the wiki has not been initialized.\n"
            f"  1. Visit https://github.com/{owner}/{repo}/wiki\n"
            "  2. Create at least one page (any content)\n"
            "  3. Re-run this script.\n"
        )

    # Wipe existing .md files so deletions in diagrams/ propagate.
    for p in WIKI_DIR.rglob("*.md"):
        p.unlink()

    mapping = build_mapping()
    print(f"Syncing {len(mapping)} files ...")
    for src_rel, wiki in mapping.items():
        content = (DIAGRAMS / src_rel).read_text(encoding="utf-8")
        content = rewrite_links(content, src_rel, mapping, owner, repo)
        (WIKI_DIR / f"{wiki}.md").write_text(content, encoding="utf-8")

    (WIKI_DIR / "_Sidebar.md").write_text(build_sidebar(mapping), encoding="utf-8")

    run(["git", "add", "-A"], cwd=WIKI_DIR)
    rc, status, _ = run(["git", "status", "--porcelain"], cwd=WIKI_DIR)
    if not status:
        print("No changes — wiki already in sync.")
        return

    run(["git", "commit", "-m", "Sync diagrams from main repo"], cwd=WIKI_DIR)

    if args.push:
        print("Pushing ...")
        run(["git", "push"], cwd=WIKI_DIR)
        print("Done.")
    else:
        print()
        print(f"Committed locally in {WIKI_DIR}")
        print(f"  cd {WIKI_DIR} && git log -1 --stat   # review")
        print(f"  python scripts/sync-wiki.py --push   # publish")


if __name__ == "__main__":
    main()
