# Pack catalog

One JSON file per pack. They are embedded in the assembly and synced into the database at
startup by `PackSeeder` (spec §6.8). Edit the files, open a PR, deploy — no migration needed.

## Pack file

```json
{
  "name": "Humor negro",
  "description": "Shown in the group settings.",
  "order": 2,
  "disabledByDefault": true,
  "templates": [ ... ]
}
```

- `name` identifies the pack in the database. Renaming it creates a new pack, so don't.
- `order` fixes the creation order; it must be unique across files.
- `disabledByDefault`: new groups start with the pack switched off (admins can enable it).

## Template entry

One line per template keeps diffs readable.

```json
{ "key": "base-la-eterna-tortilla", "type": "CustomPoll", "text": "La eterna tortilla:", "options": ["Con cebolla", "Sin cebolla"] }
```

| Field | Notes |
|---|---|
| `key` | **Stable identity. Never change it.** Lowercase letters, digits and dashes; unique across all files. |
| `type` | `CustomPoll`, `Superlative`, `Deathmatch`, `Scale`, `SecretPairing`, `OpenText`. Cannot change once synced. |
| `text` | Spanish UI copy. Free to edit (see *Translations* below). |
| `options` | Only for `CustomPoll` (2 or more, unique). |
| `minSelections` / `maxSelections` | Polls only; default 1 / 1. A pairing is always exactly 2. |
| `allowOther` | Polls: adds the free-text "Otro" answer. |
| `allowNobody` | Superlatives. |
| `retired` | `true` stops the template being offered anywhere. The row stays in the database. |
| `legacyText` / `legacyPack` | One-time hints, see below. |

Metadata equal to the per-type default is omitted (see `TemplateEntry.ToMetadata`).

## Common edits

- **Add a question:** append an entry with a new `key`.
- **Fix a wording:** change `text`. The same row is updated; the key stays.
- **Move to another pack:** delete the entry from the old file and add it (same `key`) to the new one.
- **Remove a question:** set `"retired": true`. Never delete the entry or the row — clones in real groups reference it.
- **Bring one back:** remove `retired`.

## Translations (`en/`, `de/`)

Spanish is the original. Each other language has its own folder with one file per pack, named
like the Spanish file (`en/base.json` translates `base.json`). Each player reads pack questions
in their own language; a missing translation falls back to the Spanish text.

```json
{
  "pack": "Base",
  "name": "Basics",
  "description": "Basic questions for every group.",
  "templates": {
    "base-la-eterna-tortilla": { "text": "The eternal omelette debate:", "options": ["With onion", "Without onion"] },
    "base-quien-llegaria-tarde-a-su-propio-evento": { "text": "Who would be late to their own party?" }
  }
}
```

| Field | Notes |
|---|---|
| `pack` | The Spanish pack `name` this file translates. |
| `name` / `description` | What players see in the pack list. |
| `templates` | Keyed by the template's `key`. A key must be translated in its own pack's file. |
| `text` | The question in this language. |
| `options` | Polls only: **the same number of options, in the same order** as the Spanish ones. |

- **Adapt, don't translate word for word** (spec §13, rama 24): the joke has to work for the
  player. Swap Spanish TV, customs and idioms for local equivalents with the same intent and tone.
- **It is still the same question.** Everyone in a group votes on one question whatever their
  language, so a translation keeps the type and the options' meaning and order — a poll's
  second option is the same answer in every language.
- **Retired templates need no translation.**
- **When you reword the Spanish text**, check the translations still match it.

## `legacyText` / `legacyPack`

Rows seeded before the catalog existed have no key. On the first sync each one is matched by
`(pack, text)`. When an entry was reworded or moved in the same change, `legacyText` /
`legacyPack` hold the old wording / pack so the sync still finds the row instead of creating a
duplicate. Once every database has been synced they are dead weight and can be deleted.

## Checks

`PackCatalogTests` validates the files (unique keys and texts, poll shapes, unknown properties,
translations that match their Spanish entry).
An invalid catalog makes the startup sync log a warning and skip, instead of crashing the API.
