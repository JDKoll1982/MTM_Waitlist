---
name: workspace-dependency-modernization-audit
description: Perform a complete workspace-wide dependency audit, technology modernization assessment, security review, and AI development optimization, with individual user approval through VS Code Ask Questions before any proposed change enters the implementation specification.
agent: agent
tools:
  - vscode/askQuestions
---

# WORKSPACE DEPENDENCY, TECHNOLOGY MODERNIZATION & AI DEVELOPMENT OPTIMIZATION

## 1. ROLE AND OBJECTIVE

Act as a Senior Software Architect, Principal Developer, Dependency Management Specialist, Cybersecurity Analyst, Performance Engineer, and AI Development Workflow Specialist.

Perform a comprehensive assessment of the entire accessible VS Code workspace, including all relevant repositories, solutions, projects, applications, shared libraries, development tools, and AI-assisted development infrastructure.

This is NOT simply a package version checker.

Investigate three major areas:

### A. Dependency Health and Updates

Identify outdated dependencies, research current releases, compare available versions against the actual existing implementations, and identify all changes necessary throughout the workspace to safely adopt improvements.

### B. Technology and Code Modernization

Identify opportunities to replace, improve, consolidate, or eliminate existing custom-written functionality, hardcoded implementations, outdated methods, and inefficient processes through better libraries, packages, framework-native capabilities, APIs, SDKs, or development tools.

### C. AI-Assisted Development Optimization

Investigate opportunities to improve GitHub Copilot, coding agents, MCP servers, VS Code extensions, repository context, documentation retrieval, and development workflows.

Focus on improving:

- Application performance.
- Application security.
- Application stability.
- Code quality.
- Code organization.
- Maintainability.
- Scalability.
- End-user experience.
- Developer productivity.
- AI coding accuracy.
- AI context management.
- AI response efficiency.
- Build and testing performance.
- Development workflows.

### Required Deliverables

Generate two documents:

1. **PDF Report:** A polished, user-friendly assessment that explains proposed changes, their benefits, risks, affected files, and approval decisions.
2. **Markdown Specification:** A technically detailed specification suitable for attaching to `/speckit-specify` to initiate a new implementation specification.

Both documents must reflect the actual findings and the user's individual approval decisions.

---

# 2. CRITICAL: MANDATORY USER APPROVAL WORKFLOW

This is one of the most important requirements of the entire assessment.

You MUST use the native VS Code `askQuestions` tool to request user approval for EVERY proposed change.

Tool reference:

`#tool:vscode/askQuestions`

Do not substitute ordinary chat messages, assumptions, or general approval requests for this interactive tool.

## 2.1 What Requires Individual Approval?

Every distinct proposed change must receive its own interactive approval question.

This includes:

### Dependency Changes
- Every proposed package update.
- Every proposed package replacement.
- Every proposed package removal.
- Every proposed dependency consolidation.
- Every proposed framework or SDK upgrade.
- Every proposed transitive dependency override.

### Application Modernization
- Every proposed replacement of a custom implementation.
- Every proposed new library.
- Every proposed framework-native replacement.
- Every proposed security implementation change.
- Every proposed performance optimization involving application code.
- Every proposed architecture or organization change.
- Every proposed UI or user experience improvement.
- Every proposed configuration change.

### AI and Development Environment
- Every proposed VS Code extension installation or removal.
- Every proposed MCP server installation or configuration change.
- Every proposed AI integration.
- Every proposed Copilot instruction modification.
- Every proposed custom agent, skill, or prompt.
- Every proposed Serena or Context7 configuration change.
- Every proposed development tool addition or removal.
- Every proposed AI workflow modification.

If several individual file changes are inseparable parts of one larger improvement, they may be presented as one coherent proposal. Clearly identify every affected file within that proposal.

Do not combine unrelated changes into a single approval question.

## 2.2 Approval Process

After completing research for a proposed change, assign it a unique identifier.

Use these identifier formats:

- `DEP-001` — Dependency update.
- `MOD-001` — Application modernization.
- `SEC-001` — Security improvement.
- `PERF-001` — Performance improvement.
- `UX-001` — User experience improvement.
- `AI-001` — AI tooling improvement.
- `DEV-001` — Development workflow improvement.

Maintain a master proposal register.

For every proposal, gather:

1. Proposal ID.
2. Proposal name.
3. Existing implementation or current state.
4. Recommended change.
5. Why the change is being proposed.
6. Expected benefits.
7. Affected projects.
8. Affected files.
9. Compatibility requirements.
10. Security implications.
11. Potential drawbacks.
12. Implementation effort.
13. Official research references.

Only after the research is sufficient should you ask the user for a decision.

## 2.3 Mandatory Interactive Question Format

For EACH individual proposal, invoke:

`#tool:vscode/askQuestions`

Present one distinct question for each proposal.

Use appropriate selectable choices, such as:

**Question: [Proposal ID] — [Proposal Name]**

Explain the recommendation in accessible language.

Include:
- What currently exists.
- What you recommend changing.
- Why the change is beneficial.
- What files or projects are affected.
- Any meaningful risks or compatibility concerns.
- The recommended implementation approach.

Provide these response choices:

1. **APPROVE** — Include this change in the implementation specification.
2. **REJECT** — Do not include this change.
3. **DEFER** — Save this recommendation for possible future consideration.
4. **MORE RESEARCH** — Investigate additional alternatives or provide more evidence.
5. **KEEP CURRENT** — Preserve the existing implementation.

Where there are multiple viable alternatives, include those alternatives as additional selectable options or allow the user to specify an alternative in a free-text response.

Do not automatically select the recommended option on behalf of the user.

## 2.4 Question Presentation Strategy

For efficiency, multiple separate proposal questions may be presented together in an interactive question carousel.

However:

- Every proposal must have its own question.
- Every proposal must have its own recorded answer.
- Unrelated proposals must never be combined into one decision.
- Do not use one global approval question for the entire assessment.
- Do not infer approval from approval of another related proposal.
- Do not interpret silence as approval.

For large assessments, present manageable batches of related proposals.

Continue presenting questions until every identified proposal has a recorded decision or is explicitly marked pending.

## 2.5 Handling User Responses

Record every response against its proposal ID.

Use these final statuses:

- `APPROVED`
- `REJECTED`
- `DEFERRED`
- `MORE_RESEARCH_REQUIRED`
- `KEEP_CURRENT`
- `PENDING_USER_DECISION`

If the user requests more research:

1. Investigate the requested information.
2. Update the proposal.
3. Present the revised proposal using `askQuestions`.
4. Record the new response.

If the user changes an earlier decision, update the proposal register accordingly.

Preserve a record of the previous decision and its replacement.

If the user skips questions, cancels the interaction, or does not provide an answer, mark the corresponding proposal `PENDING_USER_DECISION`.

Never assume that an unanswered proposal is approved.

## 2.6 Important Distinction: Approval Is Not Implementation Authorization

Approval means the user authorizes inclusion of the proposal in the generated Markdown implementation specification.

It does NOT authorize you to:

- Install packages.
- Modify application source code.
- Update dependency manifests.
- Change project configurations.
- Install VS Code extensions.
- Install MCP servers.
- Modify AI instructions.
- Perform the proposed migration.

The entire assessment remains read-only except for generating the requested output documents.

## 2.7 If Ask Questions Is Unavailable

If `vscode/askQuestions` cannot be found or invoked:

1. Do not silently replace it with ordinary text questions.
2. Do not assume user approval.
3. Do not mark any proposal as approved.
4. Continue factual research where practical.
5. Mark all undecided proposals `PENDING_USER_DECISION`.
6. Report that the interactive approval capability was unavailable.

The requirement to obtain individual user decisions must never be bypassed.

---

# 3. WORKSPACE DISCOVERY

Before analyzing packages or proposing changes, thoroughly inspect the workspace.

## 3.1 Existing AI Instructions and Development Context

Review applicable files and resources:

- `AGENTS.md`
- `.github/copilot-instructions.md`
- `.github/instructions/`
- `.github/prompts/`
- `.github/agents/`
- `CLAUDE.md`
- `GEMINI.md`
- `README.md`
- `CONTRIBUTING.md`
- Existing architecture documentation.
- Existing coding standards.
- Serena instructions and configuration.
- MCP server configurations.
- Spec Kit configuration.
- Existing reusable AI skills.
- Relevant memory and context files.

Identify existing instructions that affect how AI agents interact with the workspace.

Do not modify existing AI instructions during this assessment.

## 3.2 Workspace Inventory

Determine:

- Workspace root.
- All accessible workspace folders.
- All relevant repositories.
- Solutions.
- Projects.
- Application types.
- Programming languages.
- Frameworks.
- Target frameworks.
- SDK and runtime versions.
- Shared libraries.
- External services.
- Database technologies.
- Build systems.
- Testing frameworks.
- Deployment infrastructure.
- Development tools.

Inspect relevant files, including:

- `.sln`, `.slnx`
- `.csproj`, `.vbproj`, `.fsproj`
- `Directory.Build.props`
- `Directory.Build.targets`
- `Directory.Packages.props`
- `packages.config`
- `global.json`
- `NuGet.config`
- `packages.lock.json`
- `package.json`
- `package-lock.json`
- `pnpm-lock.yaml`
- `yarn.lock`
- `requirements.txt`
- `pyproject.toml`
- `Cargo.toml`
- `go.mod`
- `pom.xml`
- `build.gradle`
- Dockerfiles.
- CI/CD configuration.
- Other relevant dependency manifests.

Exclude irrelevant generated directories, caches, build artifacts, and unrelated personal files.

Assess every accessible workspace root, not just the currently open solution.

Clearly document anything inaccessible.

## 3.3 Project Relationships

Map:

- Project references.
- Shared libraries.
- Shared dependencies.
- Centralized dependency management.
- Duplicate dependencies.
- Version inconsistencies.
- Shared configuration.
- Cross-project interfaces.
- Dependencies affecting multiple applications.

Create an internal dependency and project relationship map.

---

# 4. COMPLETE PACKAGE AND DEPENDENCY AUDIT

## 4.1 Dependency Inventory

Identify:

- Direct dependencies.
- Transitive dependencies.
- Development dependencies.
- Build dependencies.
- Test dependencies.
- Compiler and analyzer packages.
- SDKs.
- Framework integrations.
- Internal packages.
- Deployment dependencies.
- AI development dependencies.

Identify packages that are unused, obsolete, redundant, unsupported, or potentially unnecessary.

Distinguish declared versions from actually resolved versions.

## 4.2 Current Release Research

For each relevant package, research:

- Current declared version.
- Current resolved version.
- Latest stable version.
- Latest compatible version.
- Recommended target version.
- Major, minor, and patch updates.
- Deprecated versions.
- End-of-support status.
- Known vulnerabilities.
- Breaking changes.
- Migration requirements.
- New features.
- Performance improvements.
- Compatibility requirements.

Research authoritative sources:

1. Official package registries.
2. Official vendor documentation.
3. Official source repositories.
4. Release notes.
5. Changelogs.
6. Migration guides.
7. Security advisories.
8. Relevant issue trackers.

Use current external research rather than relying entirely on cached package metadata or previous model knowledge.

Record research dates and source URLs.

Never recommend preview, alpha, beta, nightly, or release-candidate versions as production defaults.

## 4.3 Package Update Recommendations

For each package with a meaningful update, prepare a detailed proposal.

Include:

- Package name.
- Current version.
- Latest stable version.
- Recommended version.
- Update type.
- Projects affected.
- Files affected.
- Benefits.
- Breaking changes.
- Compatibility.
- Security implications.
- Performance implications.
- Implementation effort.
- Risks.
- Official sources.

Every recommended package update requires its own `askQuestions` approval.

---

# 5. HARDCODED FUNCTIONALITY AND TECHNOLOGY MODERNIZATION

Do not limit the assessment to dependencies already installed.

Investigate opportunities to replace, improve, consolidate, or eliminate custom-built functionality.

## 5.1 Inspect Existing Implementations

Investigate applicable areas.

### Application Infrastructure

- Logging.
- Exception handling.
- Error reporting.
- Diagnostics.
- Configuration management.
- Dependency injection.
- Background processing.
- Scheduled tasks.
- Thread management.
- Asynchronous operations.
- Retry handling.
- Caching.
- Resource management.

### Data and Database Operations

- Database connection management.
- SQL execution.
- Database access.
- Object-relational mapping.
- Data validation.
- Serialization.
- Deserialization.
- Data mapping.
- Data transformation.
- Bulk operations.
- Transaction management.
- Connection pooling.

### User Interface

- Custom controls.
- UI components.
- Data grids.
- Search.
- Filtering.
- Sorting.
- Pagination.
- Form validation.
- Notifications.
- Progress indicators.
- Keyboard shortcuts.
- Accessibility.
- Error dialogs.
- Printing.
- Reporting.
- Export and import functionality.

### Security

- Authentication.
- Authorization.
- Password handling.
- Encryption.
- Secure communication.
- Input validation.
- Output encoding.
- Token management.
- Certificate management.
- Secret management.
- Audit logging.
- Permission handling.

### Application Performance

- Repeated calculations.
- Inefficient loops.
- Unnecessary database requests.
- Excessive object creation.
- Memory-intensive operations.
- Blocking operations.
- Unnecessary UI updates.
- Redundant data processing.
- Inefficient serialization.
- Repeated file operations.
- Inefficient network communication.

### Development Infrastructure

- Custom build scripts.
- Manual code formatting.
- Manual testing.
- Custom static analysis.
- Duplicate validation tools.
- Documentation generation.
- Code generation.
- Git automation.
- Release automation.
- Dependency management scripts.

These are investigation categories, not instructions to replace every existing implementation.

Only recommend changes supported by actual source-code evidence.

## 5.2 Compare Alternatives

For every meaningful custom implementation, investigate:

1. The current custom implementation.
2. Framework-native alternatives.
3. Available third-party libraries.
4. Official SDKs or APIs.
5. Existing installed packages providing similar functionality.
6. Partial replacement or integration options.

Compare:

- Performance.
- Memory utilization.
- Security.
- Reliability.
- Compatibility.
- Development complexity.
- Maintenance burden.
- Readability.
- Testability.
- Extensibility.
- User experience.
- Long-term support.
- Licensing.
- Package size.
- Additional dependencies.

Prefer native framework capabilities when they provide equivalent or better results without unnecessary dependency overhead.

Do not introduce a package simply to replace a few lines of efficient, maintainable code.

## 5.3 Hardcoded Logic Assessment

Identify:

- Hardcoded configuration.
- Duplicated business logic.
- Repeated utility methods.
- Manual implementations of standard algorithms.
- Repeated validation logic.
- Repeated common programming patterns.
- Duplicated infrastructure.
- Inconsistent implementations between projects.
- Configuration embedded in source code.
- Opportunities for reusable abstractions.

Distinguish legitimate business-specific rules from unnecessarily duplicated infrastructure.

Do not externalize business rules simply because they are hardcoded.

Never expose secrets or move sensitive configuration into an unsecured location.

## 5.4 Modernization Classification

Assign each finding one classification:

- `REPLACE`
- `MODERNIZE`
- `ADD`
- `CONSOLIDATE`
- `REMOVE`
- `NATIVE_ALTERNATIVE`
- `KEEP`
- `INVESTIGATE`

For every proposed change, identify exact source files, classes, methods, components, and other relevant implementation locations.

Explain why the existing solution should change.

Each proposed change requires its own `askQuestions` approval.

---

# 6. SECURITY, PERFORMANCE, AND USER EXPERIENCE

Analyze opportunities that may not involve package updates.

## 6.1 Performance

Investigate:

- Startup time.
- Execution speed.
- CPU utilization.
- Memory consumption.
- Database performance.
- Network efficiency.
- UI responsiveness.
- Resource consumption.
- Build times.
- Test execution times.

Identify improvements achievable through code optimization, framework capabilities, or better dependencies.

Use documented benchmarks or propose a measurable validation method.

Do not invent performance improvements.

## 6.2 Security

Investigate:

- Known CVEs.
- Vulnerable dependencies.
- Unsupported packages.
- Unsafe APIs.
- Outdated cryptography.
- Insecure configurations.
- Supply-chain risks.
- Package ownership and maintenance.
- License compatibility.
- Security weaknesses in custom implementations.

Explain both the security benefits and new risks of proposed alternatives.

## 6.3 User Experience

Identify opportunities involving:

- Application usability.
- Accessibility.
- Workflow efficiency.
- UI responsiveness.
- Search and filtering.
- Validation.
- Error reporting.
- Notifications.
- Keyboard navigation.
- Printing and reporting.
- User feedback.
- Data entry efficiency.
- Consistency between applications.

Only recommend features that make practical sense for the existing application.

Every actual proposed change requires individual approval.

## 6.4 Maintainability

Evaluate:

- Code duplication.
- Shared utility opportunities.
- Project separation.
- Dependency consistency.
- Configuration organization.
- Architecture.
- Naming conventions.
- Abstraction quality.
- Testability.
- Documentation.
- Long-term maintenance.

Prioritize reductions in unnecessary complexity.

---

# 7. AI DEVELOPMENT ENVIRONMENT AND COPILOT OPTIMIZATION

Investigate opportunities to improve the AI-assisted development environment.

## 7.1 AI Context and Instructions

Evaluate:

- Copilot instructions.
- `AGENTS.md` organization.
- Path-specific instructions.
- Custom prompt files.
- Custom agents.
- Reusable skills.
- Serena integration.
- Code indexing.
- Symbol navigation.
- Semantic code search.
- Documentation indexing.
- Existing memory systems.
- Context management.
- Duplicate or conflicting instructions.

Identify opportunities to improve AI accuracy while reducing unnecessary context consumption.

## 7.2 MCP Servers and Integrations

Investigate applicable integrations:

- Microsoft Learn.
- Context7.
- Serena.
- GitHub integration.
- Git history analysis.
- Database tools.
- Testing tools.
- Browser automation.
- API documentation.
- Architecture visualization.
- Dependency research.
- Security analysis.

Identify:

- Currently installed servers.
- Available but unused integrations.
- Missing integrations.
- Redundant integrations.
- Potential performance benefits.
- Required permissions.
- Configuration requirements.

Prefer free and open-source options when appropriate.

For every proposed MCP server, research:

- Official project.
- Maintenance.
- Compatibility.
- Security history.
- Permissions.
- Data access.
- External code execution.
- Configuration complexity.
- Resource consumption.
- Actual usefulness.

Be cautious with integrations requiring unrestricted filesystem, terminal, network, or credential access.

## 7.3 VS Code Extensions and Development Tools

Investigate opportunities involving:

- C# and .NET development.
- Language servers.
- Code navigation.
- Static analysis.
- Formatting.
- Documentation.
- Testing.
- Debugging.
- Security scanning.
- Dependency management.
- Git and GitHub.
- AI-assisted development.
- Build optimization.

Identify underused features in tools already installed.

Avoid redundant extensions that introduce conflicts or unnecessary resource consumption.

## 7.4 AI Coding Performance

Evaluate ways to improve:

- Code understanding.
- Context selection.
- Repository-wide reasoning.
- Multi-project awareness.
- Accuracy of modifications.
- Tool selection.
- Documentation retrieval.
- API version accuracy.
- Test generation.
- Debugging.
- Token efficiency.
- Response latency.
- Consistency between agents.
- Autonomous task execution.

Investigate reusable instructions, specialized agents, skills, MCP servers, and other improvements.

Identify ways to prevent AI agents from:

- Repeatedly reading irrelevant files.
- Reimplementing existing functionality.
- Using deprecated APIs.
- Guessing package versions.
- Making inconsistent architectural decisions.
- Duplicating utilities.
- Generating unnecessary code.
- Missing project conventions.

Recommend measurable improvements where possible.

Every proposed tooling or instruction change must receive individual approval.

---

# 8. RESEARCH AND CANDIDATE EVALUATION

Every proposed package, library, tool, framework feature, or replacement must undergo research.

Compare at least two reasonable alternatives when meaningful alternatives exist.

Include the native framework solution where applicable.

For every candidate, record:

| Field | Required Information |
|---|---|
| Proposal ID | Unique identifier |
| Name | Package, tool, framework, or capability |
| Type | Runtime, development, AI, security, etc. |
| Current Solution | Existing implementation |
| Proposed Alternative | Recommended replacement |
| Latest Version | Current stable version, where applicable |
| License | License and compatibility |
| Maintenance | Project activity and support |
| Security | Vulnerabilities and security practices |
| Compatibility | Workspace compatibility |
| Performance | Documented or estimated benefits |
| Complexity | Integration and maintenance impact |
| Dependencies | Additional packages or tools |
| Risks | Migration and operational concerns |
| Recommendation | Adopt, replace, pilot, keep, etc. |
| Approval | User's individual decision |

Determine whether the alternative:

- Solves an actual problem.
- Provides meaningful improvements.
- Reduces maintenance.
- Improves security.
- Improves performance.
- Improves productivity.
- Improves user experience.
- Fits existing architecture.
- Justifies additional complexity.

Do not recommend adoption unless the benefits justify the costs and risks.

---

# 9. USER APPROVAL INTERVIEW AND DECISION REGISTER

After completing the factual research and preparing the proposals, conduct the complete approval interview using `#tool:vscode/askQuestions`.

Do not generate a final implementation specification with assumed approvals.

## 9.1 Present Every Proposal

For each proposal:

1. Assign its ID.
2. Prepare a plain-language explanation.
3. Explain the existing problem or opportunity.
4. Explain the proposed change.
5. Identify affected files and projects.
6. Explain expected benefits.
7. Explain risks and compatibility.
8. Present the individual interactive question.
9. Record the user's response.

Do not ask for approval before enough research has been completed to explain the proposal meaningfully.

## 9.2 Decision Register

Maintain a master register with these columns:

| Proposal ID | Proposed Change | Category | Priority | Decision | Notes |
|---|---|---|---|---|---|

Track all approved, rejected, deferred, pending, and further-research decisions.

## 9.3 Decision Rules

- APPROVED: Include in implementation scope.
- REJECTED: Exclude from implementation scope.
- DEFERRED: Document as future consideration.
- MORE RESEARCH: Investigate and ask again.
- KEEP CURRENT: Explicitly preserve the existing implementation.
- PENDING: Do not assume approval.

A user approving a package update does not automatically approve related code refactoring unless that refactoring was included in the same clearly defined proposal.

## 9.4 Final Decision Review

Once all proposals have been presented, summarize the decisions.

Identify:

- Total proposals.
- Approved changes.
- Rejected changes.
- Deferred changes.
- Pending decisions.
- Further research required.

Any newly discovered implementation change must also receive individual approval before being included as approved scope.

---

# 10. WORKSPACE-WIDE MODERNIZATION ROADMAP

Analyze all proposals together.

Identify:

- Shared files needing modification.
- Packages that must be updated together.
- Projects requiring framework upgrades.
- Common functionality that should be consolidated.
- Multiple implementations that could share one solution.
- AI tooling changes affecting multiple repositories.
- Changes that could conflict.
- Required implementation order.

Group related approved changes into logical phases.

Examples:

1. Critical security and compatibility changes.
2. Shared dependency updates.
3. Framework and SDK changes.
4. Custom functionality replacements.
5. Performance improvements.
6. User experience improvements.
7. AI development environment improvements.
8. Testing and final validation.

Adapt the actual roadmap to the findings.

Do not include rejected recommendations as required implementation work.

Clearly mark deferred and pending proposals.

---

# 11. TESTING AND VALIDATION STRATEGY

Determine how each approved change should be validated.

Include:

- Dependency restoration.
- Clean builds.
- Unit tests.
- Integration tests.
- Database testing.
- UI testing.
- API testing.
- Regression testing.
- Security scanning.
- Performance benchmarking.
- Memory profiling.
- Deployment validation.
- AI workflow validation.

Identify:

- Existing tests requiring modification.
- Missing tests.
- Expected results.
- Performance baselines.
- Security verification procedures.
- Rollback requirements.

Define measurable acceptance criteria wherever possible.

Testing recommendations do not authorize implementation.

---

# 12. PDF DELIVERABLE

Generate:

`Dependency_Update_and_Modernization_Report.pdf`

This report is intended for the end user.

## Audience

Assume the reader understands their applications and general computer terminology but does not necessarily understand programming languages, package management, or implementation details.

Avoid unnecessary technical jargon.

Explain technical concepts in terms of their effects on applications, security, performance, user experience, and maintenance.

## Required Structure

### 1. Cover Page

Include:

- Workspace name.
- Dependency Update & Technology Modernization Assessment.
- Research date.
- Report version.
- Repositories reviewed.
- Projects reviewed.
- Dependencies reviewed.
- Available update opportunities.
- Modernization opportunities.

### 2. Executive Summary

Explain:

- Overall workspace health.
- Dependency update status.
- Security concerns.
- Performance opportunities.
- Code modernization opportunities.
- User experience improvements.
- AI development improvements.
- Highest-value recommendations.

### 3. Dependency Update Overview

Include:

- Package.
- Current version.
- Latest stable version.
- Recommended version.
- Projects affected.
- Priority.
- Approval decision.

### 4. Individual Dependency Assessments

For each package:

- Package name.
- Current version.
- Available versions.
- Recommended version.
- Projects affected.
- Files requiring updates.
- Why the update matters.
- Performance benefits.
- Security improvements.
- Compatibility.
- Required changes.
- Risks.
- Estimated effort.
- User decision.

### 5. Custom Functionality and Modernization

Explain:

- Existing functionality.
- Recommended alternative.
- Affected files.
- Why the existing implementation could improve.
- Performance benefits.
- Security benefits.
- Maintainability improvements.
- User experience improvements.
- Risks.
- Implementation effort.
- User decision.

### 6. Development Tools and AI Optimization

Document opportunities involving:

- GitHub Copilot.
- Coding agents.
- MCP servers.
- VS Code extensions.
- Repository instructions.
- Code navigation.
- Documentation retrieval.
- Testing and debugging.
- Developer productivity.

Explain the practical benefits of each recommendation and its approval status.

### 7. Workspace-Wide Changes

Summarize changes affecting shared files, dependencies, libraries, frameworks, and multiple projects.

### 8. Implementation Roadmap

Present approved changes in logical phases.

Show priorities, benefits, effort, and dependencies.

Show deferred and pending recommendations separately.

### 9. Testing and Risk Assessment

Explain the validation process and important risks.

### 10. User Approval Summary

Show:

- Approved proposals.
- Rejected proposals.
- Deferred proposals.
- Pending decisions.
- Recommendations requiring more research.

### 11. Final Recommendations

Present the highest-value approved changes.

### 12. Sources

Include clickable official references.

## PDF Formatting

- US Letter, 8.5 × 11 inches.
- Professional typography.
- Consistent headings.
- Clear tables.
- Page numbers.
- Headers and footers.
- Table of contents.
- Clickable hyperlinks.
- Readable margins.
- Proper table pagination.
- No clipped or overflowing content.
- Subtle professional colors.
- Clear visual priority indicators.

Make the report suitable for printing and management review.

Do not make it resemble raw technical documentation.

---

# 13. SPECKIT-READY MARKDOWN SPECIFICATION

Generate:

`DEPENDENCY_AND_MODERNIZATION_SPECIFICATION.md`

This document is intended for:

`/speckit-specify`

It must provide a technically detailed, actionable starting point for a subsequent AI coding agent.

## Required Structure

# Workspace Dependency and Technology Modernization Specification

## 1. Overview

Describe the modernization objectives and scope.

## 2. Workspace Context

Document:

- Repositories.
- Projects.
- Frameworks.
- Architecture.
- Runtimes.
- SDKs.
- Package management systems.

## 3. Existing Dependency Inventory

Document all dependencies, current declared versions, resolved versions, support status, and available updates.

## 4. Approved Dependency Updates

For every approved update:

- Package name.
- Current version.
- Target version.
- Projects affected.
- Required changes.
- Compatibility requirements.
- Benefits.
- Breaking changes.
- Security impact.
- Migration requirements.
- Official sources.
- Approval ID.

## 5. Existing Custom Functionality Assessment

Document relevant existing implementations:

- File path.
- Class or method.
- Current behavior.
- Limitations.
- Reason for modernization.
- Current user decision.

## 6. Approved Replacements and Modernizations

For each approved replacement:

- Proposal ID.
- Existing solution.
- Proposed replacement.
- Package, framework, SDK, or tool.
- Recommended version where applicable.
- Alternatives considered.
- Comparison.
- Benefits.
- Risks.
- Compatibility.
- Dependencies.
- Implementation effort.
- User approval.

## 7. File-Level Change Requirements

For each affected file:

- Exact repository-relative path.
- Existing implementation.
- Required modification.
- Associated package or modernization.
- Reason.
- Mandatory or optional classification.
- Expected outcome.
- Associated approval ID.

## 8. Cross-Project Changes

Document:

- Shared libraries.
- Common components.
- Dependency consolidation.
- Centralized configuration.
- Multi-project implications.

## 9. AI Development Environment Improvements

Document:

- Copilot instructions.
- Custom agents.
- Prompt files.
- Reusable skills.
- Serena.
- MCP servers.
- VS Code extensions.
- Documentation retrieval.
- Code navigation.
- AI context improvements.
- Security and permission requirements.

Separate installed tools from approved proposed additions.

## 10. Security Requirements

Document vulnerabilities, CVEs, package trust, licensing, and approved security improvements.

## 11. Performance Requirements

Document evidence-backed performance opportunities and validation requirements.

## 12. User Experience and Maintainability Requirements

Document improvements to usability, accessibility, application consistency, maintainability, and development workflows.

## 13. Architecture and Compatibility Requirements

Document framework, runtime, SDK, API, and dependency compatibility.

## 14. Implementation Plan

Create ordered implementation phases.

For each phase include:

- Objective.
- Projects involved.
- Packages involved.
- Files affected.
- Required changes.
- Prerequisites.
- Dependencies on other phases.
- Approval IDs.
- Completion criteria.

## 15. Testing and Verification Strategy

Document required tests and validation procedures.

## 16. Acceptance Criteria

Use specific, verifiable checklist items.

Examples:

- [ ] Approved packages are updated to their specified target versions.
- [ ] Updated dependencies restore successfully.
- [ ] All affected projects compile successfully.
- [ ] Existing application functionality is preserved.
- [ ] Approved custom functionality replacements are implemented.
- [ ] No unnecessary dependencies are introduced.
- [ ] Identified security issues are addressed.
- [ ] Performance improvements are validated where applicable.
- [ ] Relevant automated tests pass.
- [ ] AI development workflow changes are validated.
- [ ] No rejected or unapproved changes are implemented.

## 17. Risks and Constraints

Document migration risks, limitations, deferred improvements, and compatibility uncertainties.

## 18. Out of Scope

Identify unrelated work and rejected proposals.

## 19. User Decisions and Approval Register

Record every proposal:

- Proposal ID.
- Description.
- Category.
- User decision.
- Rationale or comments.
- Whether it belongs in implementation scope.

## 20. Decisions Required

Identify pending decisions and unresolved research questions.

## 21. Research References

Include authoritative URLs for package documentation, release notes, migration guides, security advisories, and tool documentation.

---

# 14. SPEC KIT READINESS AND SCOPE PROTECTION

The Markdown document must be suitable for supplying to `/speckit-specify`.

Follow these requirements:

1. Describe desired outcomes, not unnecessarily prescriptive implementation details.
2. Document existing application behavior that must be preserved.
3. Include all affected projects and shared components.
4. Include accurate file-level technical evidence.
5. Include specific, testable acceptance criteria.
6. Distinguish mandatory changes from optional improvements.
7. Identify dependencies between implementation phases.
8. Include risks and constraints.
9. Protect existing business logic.
10. Preserve individual approval decisions.

### Approval-Based Scope Rules

Only `APPROVED` proposals may appear as approved implementation requirements.

`REJECTED` proposals must be excluded from implementation scope.

`DEFERRED` proposals must appear only as future considerations.

`PENDING_USER_DECISION` proposals must remain unresolved and must not be treated as approved.

`MORE_RESEARCH_REQUIRED` proposals must be clearly identified as incomplete.

`KEEP_CURRENT` decisions must explicitly protect the existing implementation.

The Markdown document may include all findings for traceability, but implementation scope must strictly reflect the user's recorded decisions.

---

# 15. FINAL QUALITY VALIDATION

Before completing the task, validate both documents.

## PDF Validation

- PDF opens correctly.
- All pages render.
- No clipped text.
- No broken tables.
- Links work.
- Findings are complete.
- Language is understandable.
- Approval statuses are accurate.

## Markdown Validation

- Valid Markdown.
- All required sections exist.
- File paths are accurate.
- Package versions are verified.
- Technical recommendations are actionable.
- Acceptance criteria are testable.
- Spec Kit requirements are addressed.
- Approval decisions are accurate.
- Unapproved changes are excluded from implementation scope.

## Cross-Document Validation

Ensure:

- Package counts match.
- Version recommendations match.
- Modernization opportunities match.
- Affected files match.
- Priorities match.
- Security findings match.
- Proposal identifiers match.
- User decisions match.
- Approved implementation scope matches.

---

# 16. OUTPUT AND COMPLETION

Create this directory at the workspace root:

`Dependency_Assessment/`

Generate:

1. `Dependency_Update_and_Modernization_Report.pdf`
2. `DEPENDENCY_AND_MODERNIZATION_SPECIFICATION.md`

If either file already exists, preserve it and create a versioned alternative rather than silently overwriting it.

Use tools already available in the development environment to generate the PDF.

Do not install additional dependencies merely to create the report unless explicitly authorized.

At completion, provide:

### Workspace Assessment Summary

- Repositories reviewed.
- Projects reviewed.
- Dependencies inventoried.
- Dependencies with available updates.
- Recommended package updates.
- Custom functionality replacement opportunities.
- Modernization opportunities.
- Security concerns.
- Performance improvements.
- User experience improvements.
- AI development workflow improvements.

### User Approval Summary

- Total proposed changes.
- Approved.
- Rejected.
- Deferred.
- Pending.
- More research required.

### Generated Deliverables

Provide exact file locations.

### Important Findings

Highlight the most important security, compatibility, performance, and modernization findings.

---

# FINAL OBJECTIVE

Do not approach this task as simply finding newer packages.

Treat it as a comprehensive opportunity to modernize the entire workspace and its development ecosystem.

Look for opportunities to:

- Update outdated dependencies.
- Replace inefficient custom implementations.
- Adopt better framework capabilities.
- Eliminate unnecessary code.
- Consolidate duplicate functionality.
- Improve application performance.
- Strengthen security.
- Improve end-user experience.
- Improve maintainability.
- Optimize developer tooling.
- Improve GitHub Copilot and AI agent effectiveness.
- Reduce unnecessary AI context and token consumption.
- Improve coding accuracy and consistency.
- Simplify future development and maintenance.

**Every proposed change must be individually presented through VS Code `askQuestions` and receive an explicit user decision.**

The PDF must clearly explain WHAT should improve, WHY it matters, and WHICH changes the user approved.

The Markdown document must provide the next AI coding agent with the technical information needed to understand HOW to plan and implement the approved improvements throughout the workspace.

**Complete the research, conduct the individual approval process, generate both documents, and do not implement any proposed changes.**