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
| `text` | Spanish UI copy. Free to edit. |
| `options` | Only for `CustomPoll` (2 or more, unique). |
| `minSelections` / `maxSelections` | Polls only; default 1 / 1. A pairing is always exactly 2. |
| `allowOther` | Polls: adds the free-text "Otro" answer. |
| `allowNobody` | Superlatives. |
| `rangeMin` / `rangeMax` | Scales; default 1 / 10. |
| `retired` | `true` stops the template being offered anywhere. The row stays in the database. |
| `legacyText` / `legacyPack` | One-time hints, see below. |

Metadata equal to the per-type default is omitted (see `TemplateEntry.ToMetadata`).

## Common edits

- **Add a question:** append an entry with a new `key`.
- **Fix a wording:** change `text`. The same row is updated; the key stays.
- **Move to another pack:** delete the entry from the old file and add it (same `key`) to the new one.
- **Remove a question:** set `"retired": true`. Never delete the entry or the row — clones in real groups reference it.
- **Bring one back:** remove `retired`.

## `legacyText` / `legacyPack`

Rows seeded before the catalog existed have no key. On the first sync each one is matched by
`(pack, text)`. When an entry was reworded or moved in the same change, `legacyText` /
`legacyPack` hold the old wording / pack so the sync still finds the row instead of creating a
duplicate. Once every database has been synced they are dead weight and can be deleted.

## Checks

`PackCatalogTests` validates the files (unique keys and texts, poll shapes, unknown properties).
An invalid catalog makes the startup sync log a warning and skip, instead of crashing the API.
