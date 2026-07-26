# WinUI3 Naming Lock
- Questionnaire completed and locked on 2026-07-26.
- Instruction file: .github/instructions/csharp-xaml-naming-rules.instructions.md
- Ruleset docs: Documents/Conventions/CSharp-Xaml-WinUI3-Naming-Ruleset.md and CSharp-Xaml-Naming-Compliance-Spec.md
- CI checker script: .github/scripts/validate-csharp-xaml-naming.ps1
- CI workflow: .github/workflows/csharp-xaml-naming-compliance.yml
- Smoke test confirms validator catches legacy non-conforming RESW keys (AppDisplayName, AppDescription, AppNotificationSamplePayload).