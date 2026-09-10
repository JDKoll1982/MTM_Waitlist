# Database/Mock/Tables

File-per-artifact table definitions for the `mtm_mock` cache database.

Each shape contributes a mirror table and its `_stage` twin:

```text
Tables/<table_name>/create.sql
Tables/<table_name>/rollback.sql
```

Register every table in `../AllTables.sql` and `../Bootstrap/update_table_descriptions.sql`.
