# Module_Setup Infor Visual Queue Scripts

This folder holds checked-in SQL script files used by the Module_Setup workflow.

These scripts are treated as queue-style query definitions or stored-procedure-style templates.
The workflow does not write runtime JSON artifacts here.

Target Visual environment: server VISUAL, database MTMFG.
Credentials are intentionally not stored in source control.

Default Infor Visual lookup connection settings are now read from appsettings.json under InforVisualDatabaseOptions.
Environment variables (INFOR_VISUAL_SQL_CONNECTION_STRING and the INFOR_VISUAL_SQL_* values) override appsettings defaults when present.
