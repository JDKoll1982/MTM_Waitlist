# Database/Mock/Tables

File-per-artifact table definitions for the `mtm_mock` cache database.

Each shape contributes a mirror table and its `_stage` twin:

```text
Tables/<table_name>/create.sql
Tables/<table_name>/rollback.sql
```

Register every table in `../AllTables.sql` and `../Bootstrap/update_table_descriptions.sql`.

**A shape contributes exactly two tables: its live result table and its `_stage` twin. Never add a `_prev`
table here.** The `_prev` name is transient: `sp_visual_<shape>_refresh` renames result -> prev, stage ->
result, prev -> stage in one statement, so a table that exists under that name before a refresh makes the swap
fail with "table already exists" and the shape can never refresh again. Feature 011 shipped one for the sixth
shape; a host bootstrapped from `AllTables.sql` refreshed the other five shapes and failed the sixth forever.
