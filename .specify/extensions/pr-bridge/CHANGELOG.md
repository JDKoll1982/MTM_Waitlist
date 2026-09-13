# Changelog

## 1.0.0 (2026-04-10)

- Initial release
- Add `/speckit.pr.generate` command for auto-generating PR descriptions from spec artifacts
- Add `/speckit.pr.checklist` command for reviewer checklists mapped to spec requirements
- Add `/speckit.pr.summary` command for spec-vs-implementation change summaries
- Optional `after_implement` hook for auto-generating PR description after implementation
- Closes the SDD workflow loop: specify → plan → tasks → implement → PR
