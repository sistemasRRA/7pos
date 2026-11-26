# 0006 · Client specs are a guide; deviations are recorded

- **Status:** Accepted
- **Date:** 2026-11-12

## Context
The client documents (see [specs](../specs/README.md)) describe a large system across several phases. They contain gaps and inconsistencies: tables that are referenced but never defined, enum values that differ between documents, totals stored in two places, no tax (IVA) fields, and no table for cash withdrawals. It is unclear how strictly they must be followed.

## Decision
- Treat the specs as the source of business intent, not as a literal schema.
- Keep their concepts, but fix problems with sensible defaults.
- Record every deviation in [roadmap.md](../roadmap.md) (open questions) or an ADR.

## Alternatives considered
- **Follow literally:** builds known bugs into the foundation (e.g. phone as primary key, shift totals that can drift).
- **Ignore and redesign:** loses alignment with the client and their analysts.

## Consequences
- The schema differs in places from the client's diagram. The glossary and ADRs explain each difference.
- The open questions must be sent to the client and answered before building the affected features.
