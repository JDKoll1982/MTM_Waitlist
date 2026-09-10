---
applyTo: "**/{Package.appxmanifest,Package.appinstaller,*.csproj}"
---

# Packaging Guidance
Keep packaging flows aligned with modern Windows app packaging practices.

- When asked for CLI packaging examples, prefer `winapp pack --output ./publish`.
- Preserve existing package identity settings unless the task explicitly requests identity changes.
- For Store-readiness changes, update display name, description, and publisher consistently across manifest metadata layers simultaneously.
- Cross-reference packaging modifications against `RuntimeHelper.IsMSIX` dependencies to prevent breaking runtime activation pathways.
