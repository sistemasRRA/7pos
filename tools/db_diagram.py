"""Generate a Mermaid ER diagram (docs/database.md) from the real PostgreSQL database.

Usage:  python3 tools/db_diagram.py
Reads the schema with the `psql` client, using the same local connection string as
the app (dotnet user-secrets of SevenPos.Web). Override it with SEVENPOS_DB
(a libpq URI, e.g. postgresql://user@host:5432/sevenpos).
Run it again after every migration to keep the diagram up to date.
"""
import json
import os
import re
import subprocess
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT_PATH = ROOT / "docs/database.md"
WEB_PROJECT = ROOT / "src/SevenPos.Web/SevenPos.Web.csproj"

# ADO.NET connection string keys -> libpq environment variables
LIBPQ_ENV = {"host": "PGHOST", "server": "PGHOST", "port": "PGPORT", "database": "PGDATABASE",
             "username": "PGUSER", "user id": "PGUSER", "password": "PGPASSWORD"}


def psql_connection() -> tuple[list[str], dict]:
    """psql arguments and environment. The password goes in PGPASSWORD, never on the command line."""
    if os.environ.get("SEVENPOS_DB"):
        return [os.environ["SEVENPOS_DB"]], dict(os.environ)

    match = re.search(r"<UserSecretsId>([^<]+)</UserSecretsId>", WEB_PROJECT.read_text())
    secrets_file = Path.home() / ".microsoft/usersecrets" / (match.group(1) if match else "") / "secrets.json"
    if not match or not secrets_file.exists():
        sys.exit("No connection configured. Set it once with dotnet user-secrets (see docs/getting-started.md) "
                 "or set SEVENPOS_DB.")
    connection_string = json.loads(secrets_file.read_text(encoding="utf-8-sig")).get("ConnectionStrings:Pos", "")

    env = dict(os.environ)
    for part in connection_string.split(";"):
        key, _, value = part.partition("=")
        if key.strip().lower() in LIBPQ_ENV:
            env[LIBPQ_ENV[key.strip().lower()]] = value.strip()
    return [], env

COLUMNS_SQL = """
SELECT c.table_name, c.column_name, c.udt_name, c.is_nullable
FROM information_schema.columns c
JOIN information_schema.tables t USING (table_schema, table_name)
WHERE c.table_schema = 'public' AND t.table_type = 'BASE TABLE'
  AND c.table_name NOT LIKE '\\_\\_EF%'
ORDER BY c.table_name, c.ordinal_position
"""

# (table, column, kind, referenced table) for primary keys, foreign keys and single-column unique indexes
KEYS_SQL = """
SELECT t.relname, a.attname, 'PK', '' FROM pg_index i
  JOIN pg_class t ON t.oid = i.indrelid JOIN pg_namespace n ON n.oid = t.relnamespace
  JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY (i.indkey)
  WHERE i.indisprimary AND n.nspname = 'public'
UNION ALL
SELECT t.relname, a.attname, 'UK', '' FROM pg_index i
  JOIN pg_class t ON t.oid = i.indrelid JOIN pg_namespace n ON n.oid = t.relnamespace
  JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = i.indkey[0]
  WHERE i.indisunique AND NOT i.indisprimary AND i.indnatts = 1 AND n.nspname = 'public'
UNION ALL
SELECT t.relname, a.attname, 'FK', r.relname FROM pg_constraint c
  JOIN pg_class t ON t.oid = c.conrelid JOIN pg_class r ON r.oid = c.confrelid
  JOIN pg_namespace n ON n.oid = t.relnamespace
  JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = c.conkey[1]
  WHERE c.contype = 'f' AND n.nspname = 'public'
"""


def query(sql: str) -> list[list[str]]:
    args, env = psql_connection()
    result = subprocess.run(
        ["psql", *args, "-At", "-F", "\t", "-v", "ON_ERROR_STOP=1", "-c", sql],
        capture_output=True, text=True, env=env)
    if result.returncode != 0:
        sys.exit(f"Could not read the database:\n{result.stderr.strip()}\n"
                 "Is PostgreSQL running and has the app been started once (to apply migrations)?")
    return [line.split("\t") for line in result.stdout.splitlines() if line]


def main() -> None:
    columns = defaultdict(list)
    for table, column, col_type, nullable in query(COLUMNS_SQL):
        columns[table].append((column, col_type, nullable == "YES"))

    keys = defaultdict(set)
    relations = []
    for table, column, kind, referenced in query(KEYS_SQL):
        keys[(table, column)].add(kind)
        if kind == "FK":
            relations.append((referenced, table, column))

    entities = []
    for table, cols in columns.items():
        lines = []
        for column, col_type, nullable in cols:
            k = keys[(table, column)]
            if "PK" in k:
                k = k - {"UK"}
            key_text = ", ".join(x for x in ("PK", "FK", "UK") if x in k)
            note = ' "nullable"' if nullable else ""
            lines.append(f"        {col_type} {column}{' ' + key_text if key_text else ''}{note}")
        entities.append(f"    {table} {{\n" + "\n".join(lines) + "\n    }")

    nullable_cols = {(t, c) for t, cols in columns.items() for c, _, n in cols if n}
    rel_lines = [
        f'    {parent} {"|o" if (child, col) in nullable_cols else "||"}--o{{ {child} : "{col}"'
        for parent, child, col in sorted(relations)]

    diagram = "erDiagram\n" + "\n".join(entities) + "\n\n" + "\n".join(rel_lines)
    OUT_PATH.write_text(
        "# Database diagram\n\n"
        "Generated from the real database by `python3 tools/db_diagram.py`. Do not edit by hand.\n\n"
        "Reading the lines: `||--o{` = one to many; `|o--o{` = optional (nullable FK).\n"
        "Types are PostgreSQL types: `numeric` = money (exact decimals), `timestamptz` = UTC date and time.\n\n"
        f"```mermaid\n{diagram}\n```\n",
        encoding="utf-8")
    print(f"{len(columns)} tables, {len(rel_lines)} relationships -> {OUT_PATH.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
