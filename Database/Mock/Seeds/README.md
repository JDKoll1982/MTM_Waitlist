# Database/Mock/Seeds

File-per-artifact seed content for the `mtm_mock` cache database.

Rows written by a seed carry `is_seed_content = 1` so the read-status surface can tell
seeded content (never refreshed) from refreshed content.

Register every seed in `../AllSeeds.sql`.
