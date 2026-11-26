"""Generate a Mermaid ER diagram (docs/database.md) from the real SQLite database.

Usage:  python3 tools/db_diagram.py [path/to/7pos.db]
Run it again after every migration to keep the diagram up to date.
"""
import sqlite3
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DB_PATH = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "src/SevenPos.Web/7pos.db"
OUT_PATH = ROOT / "docs/database.md"


def main() -> None:
    if not DB_PATH.exists():
        sys.exit(f"Database not found: {DB_PATH}\nRun the app once first: dotnet run --project src/SevenPos.Web")

    con = sqlite3.connect(DB_PATH)
    tables = [r[0] for r in con.execute(
        "SELECT name FROM sqlite_master WHERE type='table' "
        "AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EF%' ORDER BY name")]

    entities, relations = [], []
    for table in tables:
        columns = con.execute(f'PRAGMA table_info("{table}")').fetchall()      # cid, name, type, notnull, default, pk
        fks = con.execute(f'PRAGMA foreign_key_list("{table}")').fetchall()    # id, seq, table, from, to, ...
        fk_cols = {fk[3]: fk[2] for fk in fks}
        unique_cols = set()
        for idx in con.execute(f'PRAGMA index_list("{table}")').fetchall():  # seq, name, unique, origin, partial
            if idx[2]:
                cols = con.execute(f'PRAGMA index_info("{idx[1]}")').fetchall()
                if len(cols) == 1:
                    unique_cols.add(cols[0][2])

        lines = []
        for _, name, col_type, notnull, _, pk in columns:
            keys = [k for k, on in (("PK", pk), ("FK", name in fk_cols), ("UK", name in unique_cols and not pk)) if on]
            note = "" if notnull or pk else ' "nullable"'
            lines.append(f"        {col_type or 'ANY'} {name}{' ' + ', '.join(keys) if keys else ''}{note}")
        entities.append(f"    {table} {{\n" + "\n".join(lines) + "\n    }")

        notnull_by_col = {c[1]: c[3] for c in columns}
        for _, _, parent, from_col, _, *_ in fks:
            one = "||" if notnull_by_col.get(from_col) else "|o"
            relations.append(f'    {parent} {one}--o{{ {table} : "{from_col}"')

    diagram = "erDiagram\n" + "\n".join(entities) + "\n\n" + "\n".join(relations)
    OUT_PATH.write_text(
        "# Database diagram\n\n"
        "Generated from the real database by `python3 tools/db_diagram.py`. Do not edit by hand.\n\n"
        "Reading the lines: `||--o{` = one to many; `|o--o{` = optional (nullable FK).\n\n"
        f"```mermaid\n{diagram}\n```\n",
        encoding="utf-8")
    print(f"{len(tables)} tables, {len(relations)} relationships -> {OUT_PATH.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
