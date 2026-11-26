---
agent: 'agent'
description: 'Perform a workspace-wide dependency audit, technology modernization assessment, and AI development workflow optimization. Generate a professional PDF report and a Spec Kit-ready Markdown specification.'
---

# Workspace Dependency, Technology Modernization & AI Development Optimization Audit

## 1. ROLE AND OBJECTIVE

Act as a Senior Software Architect, Principal Developer, Dependency Management Specialist, Cybersecurity Analyst, Performance Engineer, and AI Development Workflow Specialist.

Your responsibility is to perform a comprehensive assessment of the entire accessible VS Code workspace, including all relevant repositories, solutions, projects, applications, shared libraries, development tools, and AI-assisted development infrastructure.

This is NOT simply a package version checker.

You must investigate three major areas:

### A. Dependency Health and Updates
Identify all outdated dependencies, research their current versions, compare available releases against the actual existing implementations, and identify every necessary change throughout the workspace.

### B. Technology and Code Modernization
Identify opportunities to replace, improve, consolidate, or eliminate existing custom-written functionality, hardcoded implementations, outdated methods, and inefficient processes through better libraries, packages, framework-native capabilities, APIs, SDKs, or development tools.

### C. AI-Assisted Development Optimization
Investigate opportunities to improve the capabilities, accuracy, efficiency, speed, consistency, context awareness, and overall performance of GitHub Copilot and other AI development tools used within the workspace.

The ultimate goal is to improve:

- Application performance.
- Application security.
- Application stability.
- Code quality.
- Code organization.
- Maintainability.
- Scalability.
- End-user experience.
- Developer productivity.
- AI coding agent accuracy.
- AI context management.
- AI response efficiency.
- Build and testing performance.
- Development workflows.

You must generate two deliverables:

1. A professional PDF report for the user.
2. A technical Markdown specification intended for `/speckit-specify`.

Both documents must cover the findings, but their audiences and presentation must be different.

---

# 2. OPERATING RULES AND BOUNDARIES

Follow these rules throughout the assessment.

1. Perform a read-only assessment of existing application code and configuration.
2. Do not update packages or modify source code during the assessment.
3. Do not install new packages, extensions, MCP servers, SDKs, or tools.
4. Do not automatically replace existing implementations.
5. Research recommendations against the actual project architecture and requirements.
6. Do not introduce dependencies simply because they are popular or newer.
7. Prefer existing framework capabilities over additional dependencies when they provide equivalent or better functionality.
8. Prefer mature, actively maintained, secure, well-documented solutions.
9. Do not replace business-specific functionality with generic third-party implementations unless there is a demonstrable benefit.
10. Do not invent performance improvements, compatibility claims, affected files, or package versions.
11. Distinguish documented improvements from theoretical possibilities.
12. Do not expose secrets, credentials, tokens, connection strings, or other sensitive configuration values in either report.
13. Identify uncertainty instead of guessing.
14. Keep recommendations directly related to improving the workspace.
15. Generate reports only. Implementation requires a separate specification and approval process.

You are authorized to create the output directory and the two requested deliverables.

---

# PHASE 1 — WORKSPACE AND ENVIRONMENT DISCOVERY

## 1.1 Inspect Existing Instructions

Before analyzing the workspace, inspect the development environment and existing project guidance.

Review relevant files and resources, including:

- `AGENTS.md`
- `.github/copilot-instructions.md`
- `.github/instructions/`
- `.github/prompts/`
- `.github/agents/`
- `CLAUDE.md`
- `GEMINI.md`
- `README.md`
- `CONTRIBUTING.md`
- `agents.md`
- `specs/`
- `.specify/`
- `memory/`
- Serena configuration and instructions, if installed.
- MCP server configurations.
- Existing development documentation.
- Existing architecture documentation.
- Existing development conventions and coding standards.

Identify relevant existing instructions that influence how future AI agents interact with the workspace.

Do not modify existing AI instructions during this assessment.

## 1.2 Workspace Inventory

Determine:

- Workspace root.
- All accessible workspace folders.
- All relevant repositories.
- All solutions.
- All projects.
- All application types.
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
- Development tooling.

Inspect all relevant project and dependency configuration files.

Examples include:

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
- `Cargo.lock`
- `go.mod`
- `pom.xml`
- `build.gradle`
- Dockerfiles.
- CI/CD configuration.
- Other relevant manifests discovered during inspection.

Exclude irrelevant generated directories, dependency caches, build artifacts, and unrelated personal files.

If the VS Code workspace contains multiple roots, assess each relevant root. Do not claim to have inspected inaccessible repositories or directories.

## 1.3 Dependency and Project Relationships

Map the relationships between projects.

Identify:

- Project references.
- Shared libraries.
- Shared dependencies.
- Centralized dependency management.
- Duplicate dependencies.
- Version inconsistencies.
- Cross-project interfaces.
- Shared configuration.
- Dependencies that affect multiple applications.

Create an internal dependency map to use throughout the audit.

---

# PHASE 2 — COMPLETE PACKAGE AND DEPENDENCY AUDIT

## 2.1 Inventory All Dependencies

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

Determine which dependencies are actively used and which appear redundant, unused, or obsolete.

Do not confuse a declared package version with its actual resolved version.

## 2.2 Research Available Updates

For every relevant package, determine:

- Current declared version.
- Current resolved version.
- Latest stable version.
- Latest compatible version.
- Recommended version.
- Major, minor, and patch updates.
- Deprecated versions.
- End-of-support status.
- Known vulnerabilities.
- Breaking changes.
- Required migrations.
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

Use current research rather than relying exclusively on cached package metadata or previous AI knowledge.

Record the research date and authoritative source URLs.

Never automatically recommend preview, alpha, beta, nightly, or release-candidate versions for production applications.

---

# PHASE 3 — HARDCODED FUNCTIONALITY AND MODERNIZATION OPPORTUNITY AUDIT

This phase is essential.

Do not limit your assessment to packages already installed in the project.

Look for functionality currently implemented manually that could be replaced, simplified, or improved through a suitable package, library, framework feature, SDK, service, or built-in platform capability.

The goal is to identify opportunities to make the application better, not simply increase the number of dependencies.

## 3.1 Inspect Existing Custom Implementations

Analyze the existing source code for custom-built functionality.

Investigate applicable areas such as:

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
- Database access.
- SQL execution.
- Object-relational mapping.
- Data validation.
- Data serialization.
- Data deserialization.
- Data mapping.
- Data transformation.
- Bulk operations.
- Transaction management.
- Connection pooling.

### User Interface and User Experience
- Custom controls.
- UI component implementations.
- Data grids.
- Search functionality.
- Filtering.
- Sorting.
- Pagination.
- Form validation.
- Notifications.
- Progress indicators.
- Keyboard shortcuts.
- Accessibility features.
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
- Manual testing procedures.
- Custom static analysis.
- Duplicate validation tools.
- Documentation generation.
- Code generation.
- Git automation.
- Release automation.
- Dependency management scripts.

The above are investigation categories, not instructions to replace every existing implementation.

Only recommend changes supported by actual source-code evidence.

## 3.2 Identify Replacement and Improvement Opportunities

For each meaningful custom implementation, determine whether a better alternative exists.

Evaluate:

1. Existing custom implementation.
2. Existing framework-native alternative.
3. Available third-party libraries.
4. Available SDKs or official APIs.
5. Existing packages that already provide similar functionality.
6. Whether a complete replacement or partial integration would be better.

For each candidate, compare:

- Application performance.
- Memory utilization.
- Security.
- Reliability.
- Compatibility.
- Development complexity.
- Maintenance burden.
- Code readability.
- Testing capabilities.
- Extensibility.
- User experience.
- Long-term support.
- Licensing.
- Package size.
- Additional transitive dependencies.

Determine whether adopting the alternative would genuinely improve the application.

### Important: Framework Features vs. Third-Party Packages

Before recommending a package, investigate whether the current programming language or framework already provides an equivalent feature.

Prefer native capabilities when they:

- Perform equally well or better.
- Require fewer dependencies.
- Improve security.
- Reduce maintenance.
- Fit existing architecture.

Do not introduce an external package solely to replace a few lines of efficient, well-written code.

### 3.3 Identify Hardcoded Logic That Should Be Modernized

Investigate whether existing code contains:

- Hardcoded configuration.
- Duplicated business logic.
- Repeated utility methods.
- Manual implementations of standard algorithms.
- Repeated validation logic.
- Manually implemented common patterns.
- Duplicated infrastructure.
- Inconsistent implementations across projects.
- Configuration values embedded in source files.
- Application-specific functionality that would benefit from reusable abstractions.

Distinguish between legitimate application-specific logic and unnecessarily duplicated infrastructure.

Do not recommend externalizing business rules simply because they are hardcoded.

Never propose exposing secrets or moving sensitive configuration into an unsecured location.

### 3.4 Replacement Opportunity Classification

Assign one of the following classifications to each identified opportunity:

- **REPLACE:** Existing custom implementation should be replaced by a better-supported alternative.
- **MODERNIZE:** Existing functionality should be improved without necessarily replacing the underlying implementation.
- **ADD:** New functionality would provide meaningful benefits.
- **CONSOLIDATE:** Multiple implementations should be unified.
- **REMOVE:** Redundant or unnecessary functionality should be eliminated.
- **NATIVE ALTERNATIVE:** Existing functionality can be improved using built-in framework capabilities.
- **KEEP:** Existing implementation is already appropriate.
- **INVESTIGATE:** More research or testing is required.

For every opportunity, identify the exact source files, methods, classes, components, and other relevant code locations.

Explain why the existing implementation should or should not be changed.

---

# PHASE 4 — AI DEVELOPMENT ENVIRONMENT AND COPILOT OPTIMIZATION

Investigate opportunities to improve the AI-assisted development environment used for this workspace.

This assessment should focus on making GitHub Copilot and other coding agents more accurate, efficient, consistent, and productive.

Do not limit this phase to application runtime packages.

## 4.1 AI Context and Repository Understanding

Evaluate:

- Repository instructions.
- `AGENTS.md` organization.
- Copilot instruction files.
- Path-specific instructions.
- Custom prompt files.
- Custom agents.
- Reusable skills.
- Serena integration.
- Code indexing.
- Symbol navigation.
- Semantic code search.
- Documentation indexing.
- Project architecture documentation.
- Existing memory and context management.
- Rules for selecting relevant files.
- Duplicate or conflicting AI instructions.

Identify opportunities to reduce unnecessary context consumption while improving the quality of information provided to coding agents.

Determine whether the current instruction architecture makes it difficult for an AI agent to understand the entire workspace.

Recommend improvements that increase consistency across projects.

## 4.2 MCP Servers and AI Integrations

Investigate MCP servers and integrations that could improve development workflows.

Consider applicable categories:

- Microsoft Learn documentation.
- Official SDK documentation.
- Context7 or equivalent current documentation retrieval.
- Serena and semantic code navigation.
- GitHub integration.
- Git history analysis.
- Database development tools.
- Testing and debugging tools.
- Browser automation.
- API documentation.
- Architecture visualization.
- Dependency research.
- Security analysis.
- Code quality analysis.

Identify:

- Currently installed MCP servers.
- Available but unused integrations.
- Missing integrations.
- Redundant integrations.
- Potential performance improvements.
- Permission requirements.
- Configuration requirements.

Prefer free or open-source options where appropriate, but evaluate paid solutions when they offer meaningful advantages.

For every proposed MCP server, research:

- Official project and repository.
- Maintenance activity.
- Compatibility.
- Security history.
- Required permissions.
- Data access.
- Whether it executes external code.
- Configuration complexity.
- Resource consumption.
- Actual usefulness to this workspace.

Do not recommend a server solely because it is popular.

Be especially cautious with tools that provide unrestricted filesystem, terminal, credential, or network access.

## 4.3 VS Code Extensions and Development Tools

Inspect the available information about installed VS Code extensions and relevant development tools.

Identify opportunities involving:

- C# and .NET tooling.
- Language servers.
- Code navigation.
- Static analysis.
- Code quality.
- Automated formatting.
- Documentation.
- Testing.
- Debugging.
- Security scanning.
- Dependency management.
- Git and GitHub integration.
- AI-assisted development.
- Build optimization.

Identify useful features from tools that may already be installed but are not being used effectively.

Look for missing extensions that provide demonstrable benefits.

Do not recommend overlapping extensions that introduce unnecessary resource usage or conflicting behavior.

## 4.4 Improving AI Coding Performance

Evaluate opportunities to improve:

- Code understanding.
- Context selection.
- Repository-wide reasoning.
- Multi-project awareness.
- Accuracy of code changes.
- Tool selection.
- Documentation retrieval.
- API version accuracy.
- Test generation.
- Debugging.
- Error diagnosis.
- Token efficiency.
- Response latency.
- Consistency between agents.
- Autonomous task execution.

Consider whether reusable agents, skills, prompts, instruction files, MCP servers, or specialized development tools could improve these areas.

Identify ways to prevent AI agents from:

- Repeatedly reading irrelevant files.
- Reimplementing existing functionality.
- Using deprecated APIs.
- Guessing package versions.
- Making inconsistent architectural decisions.
- Duplicating utilities.
- Generating unnecessary code.
- Missing existing project conventions.

Where possible, recommend measurable improvements rather than subjective claims.

Do not assume that introducing more AI tools automatically improves AI performance.

## 4.5 AI Workflow Recommendations

Create a dedicated section for improvements to the AI development environment.

Include:

- Current limitation.
- Recommended improvement.
- Tool or integration involved.
- Expected benefit.
- Required configuration.
- Security implications.
- Estimated effort.
- Compatibility.
- Supporting documentation.

Keep application runtime dependencies, development dependencies, and AI workflow tools in separate categories.

---

# PHASE 5 — DEEP IMPACT AND COMPATIBILITY ANALYSIS

For every recommended package update or modernization opportunity, analyze the actual workspace implementation.

## 5.1 Compatibility

Check compatibility with:

- Current framework versions.
- Target frameworks.
- Runtime versions.
- SDKs.
- Compilers.
- Existing dependencies.
- Other projects.
- Operating systems.
- Deployment environments.
- Existing architecture.

Identify any required framework, runtime, SDK, or package upgrades.

## 5.2 File-Level Impact

Identify every file that would need modification.

For each file, record:

- Repository-relative path.
- Relevant class, method, component, or configuration section.
- Current implementation.
- Recommended change.
- Reason for the change.
- Associated package or tool.
- Whether the change is mandatory or optional.

Use actual code references and line numbers where practical.

Do not invent line numbers or implementation details.

## 5.3 Performance Analysis

Investigate measurable benefits involving:

- Startup time.
- Execution speed.
- CPU consumption.
- Memory consumption.
- Database performance.
- Network efficiency.
- UI responsiveness.
- Build performance.
- Test execution.
- Application resource consumption.

Identify opportunities where existing code can be optimized without introducing additional dependencies.

Distinguish benchmarks, documented performance changes, and estimated improvements.

## 5.4 Security Analysis

Investigate:

- CVEs.
- Vulnerable packages.
- Outdated encryption.
- Unsafe APIs.
- Insecure configuration.
- Vulnerable transitive dependencies.
- Insecure custom implementations.
- Unsupported packages.
- Supply-chain risks.
- Package ownership and maintenance.
- License compatibility.

Explain security benefits and possible new risks introduced by proposed replacements.

## 5.5 Maintainability and Organization

Evaluate:

- Code duplication.
- Shared utility opportunities.
- Project separation.
- Dependency consistency.
- Centralized configuration.
- Architecture.
- Naming conventions.
- Abstraction quality.
- Testability.
- Documentation.
- Long-term maintainability.

Prioritize improvements that reduce unnecessary complexity.

---

# PHASE 6 — RESEARCH AND CANDIDATE EVALUATION

Every proposed new package, replacement, framework feature, or development tool must undergo an evaluation.

Use authoritative documentation and current research.

For every candidate, record:

| Category | Information |
|---|---|
| Name | Package, tool, framework, or capability |
| Type | Runtime, development, AI, security, etc. |
| Current Solution | Existing implementation |
| Proposed Alternative | Recommended replacement |
| Latest Version | Current stable version when applicable |
| License | License and compatibility |
| Maintenance | Project activity and support |
| Security | Known vulnerabilities and security practices |
| Compatibility | Workspace compatibility |
| Performance | Documented or estimated benefits |
| Complexity | Integration and maintenance impact |
| Dependencies | Additional packages or tools |
| Risks | Migration and operational concerns |
| Recommendation | Adopt, replace, pilot, keep, etc. |

Compare at least two reasonable alternatives when meaningful alternatives exist.

Include the native framework solution as a candidate where applicable.

### Candidate Evaluation Rules

Evaluate whether the proposed solution:

- Solves an actual problem.
- Provides functionality unavailable or inefficiently implemented today.
- Reduces maintenance.
- Improves security.
- Improves performance.
- Improves development productivity.
- Improves user experience.
- Fits the existing architecture.
- Justifies its additional complexity.

Recommend adoption only when the overall benefit justifies implementation and long-term maintenance costs.

---

# PHASE 7 — PRIORITIZATION AND RECOMMENDATIONS

Assign a priority:

- **CRITICAL:** Serious security, stability, or operational issue.
- **HIGH:** Significant improvement or important compatibility requirement.
- **MEDIUM:** Meaningful improvement with manageable risk.
- **LOW:** Optional enhancement or minor improvement.

Assign a recommendation:

- **RECOMMENDED:** Strong evidence of meaningful benefits.
- **RECOMMENDED WITH CHANGES:** Benefits are substantial but require migration.
- **PILOT:** Promising, but should be tested before broader adoption.
- **OPTIONAL:** Useful improvement, but not essential.
- **DEFER:** Not suitable for immediate adoption.
- **KEEP CURRENT:** Existing implementation is preferable.
- **INVESTIGATE:** More information is required.

Also estimate:

- Implementation complexity.
- Affected projects.
- Affected files.
- Migration risk.
- Testing requirements.
- Long-term maintenance impact.

Do not select the newest or most popular technology simply because it is newer or more popular.

---

# PHASE 8 — WORKSPACE-WIDE MODERNIZATION ROADMAP

Analyze all recommendations together.

Identify:

- Shared files that need modifications.
- Packages that must be updated together.
- Projects requiring framework upgrades.
- Common functionality that should be consolidated.
- Multiple implementations that can be replaced with one shared solution.
- AI tooling improvements that affect every repository.
- Changes that could conflict.
- Changes that should be performed in a particular order.

Create a unified roadmap that minimizes redundant work.

Group related changes into logical phases.

For example:

1. Critical security and compatibility changes.
2. Shared dependency updates.
3. Framework and SDK changes.
4. Custom functionality replacements.
5. Performance improvements.
6. User experience improvements.
7. AI development environment improvements.
8. Testing and final validation.

Adapt the actual sequence to the findings.

---

# PHASE 9 — TESTING AND VALIDATION STRATEGY

Determine how every proposed update and modernization should be validated.

Include applicable requirements for:

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

Identify existing tests that need modification.

Identify missing tests.

Define measurable acceptance criteria wherever possible.

Include baseline comparisons when performance improvements are claimed.

---

# PHASE 10 — PDF DELIVERABLE

Generate:

`Dependency_Update_and_Modernization_Report.pdf`

This document is intended for the end user.

## Audience and Writing Style

Assume the reader understands their applications and general computer terminology but does not necessarily understand programming languages, packages, frameworks, or implementation details.

Use accessible language.

Avoid unnecessary programming jargon.

Explain technical concepts in terms of their effect on the application, its security, performance, maintenance, and user experience.

## Required PDF Structure

### 1. Cover Page

Include:

- Workspace name.
- Dependency Update & Technology Modernization Assessment.
- Research date.
- Report version.
- Number of repositories reviewed.
- Number of projects reviewed.
- Number of dependencies reviewed.
- Number of update opportunities.
- Number of modernization opportunities.

### 2. Executive Summary

Explain:

- Overall workspace health.
- Dependency update status.
- Security concerns.
- Performance opportunities.
- Code modernization opportunities.
- User experience opportunities.
- AI development workflow opportunities.
- Most valuable recommended improvements.

### 3. Dependency Update Overview

Provide a table containing:

- Package.
- Current version.
- Latest stable version.
- Recommended version.
- Projects affected.
- Priority.
- Recommendation.

### 4. Individual Dependency Assessments

For every package with a meaningful update:

- Package name.
- Current version.
- Available versions.
- Recommended version.
- Projects affected.
- Files needing updates.
- Why the update matters.
- Performance benefits.
- Security improvements.
- Compatibility considerations.
- Application changes.
- Risks.
- Estimated effort.

### 5. Custom Functionality and Modernization Opportunities

Explain which existing application features could benefit from replacement or modernization.

For each opportunity include:

- Existing functionality.
- Recommended alternative.
- Files and application areas affected.
- Why the current implementation could be improved.
- Performance benefits.
- Security benefits.
- Maintainability benefits.
- User experience benefits.
- Risks.
- Implementation effort.

Explain why the proposed replacement is better in practical terms.

### 6. Development Tools and AI Optimization

Document opportunities to improve:

- GitHub Copilot.
- Coding agents.
- MCP servers.
- VS Code extensions.
- Repository instructions.
- Code navigation.
- Documentation retrieval.
- Testing and debugging tools.
- Developer productivity.

Explain how each improvement helps development quality, efficiency, or consistency.

### 7. Workspace-Wide Changes

Summarize common changes across all projects.

Identify shared files and dependencies.

### 8. Recommended Implementation Roadmap

Group the improvements into logical implementation phases.

Include priority, expected benefit, effort, and dependencies between phases.

### 9. Testing and Risk Assessment

Explain how to verify the improvements and what risks need consideration.

### 10. Final Recommendations

Provide a prioritized list of the highest-value improvements.

### 11. Research Sources

Include clickable links to authoritative sources.

## PDF Design Requirements

- US Letter paper, 8.5 × 11 inches.
- Professional report layout.
- Consistent typography.
- Clear section hierarchy.
- Page numbers.
- Headers and footers.
- Table of contents.
- Properly formatted tables.
- Clickable hyperlinks.
- Readable margins.
- No clipped or overflowing content.
- Avoid unnecessarily splitting related information across pages.
- Use subtle professional colors.
- Use clear priority indicators.
- Make the report suitable for printing and management review.

Do not make the PDF look like raw technical documentation.

---

# PHASE 11 — SPECKIT-READY MARKDOWN SPECIFICATION

Generate:

`DEPENDENCY_AND_MODERNIZATION_SPECIFICATION.md`

This document is intended for use with:

`/speckit-specify`

It must be a technically detailed document suitable for another AI development agent.

The purpose is to provide an implementation-ready starting point for a new Spec Kit feature specification.

## Required Markdown Structure

# Workspace Dependency and Technology Modernization Specification

## 1. Overview

Describe the modernization objectives and overall scope.

## 2. Workspace Context

Document repositories, projects, frameworks, architecture, runtimes, SDKs, and package management systems.

## 3. Existing Dependency Inventory

Document all identified dependencies, current declared versions, resolved versions, support status, and update availability.

## 4. Proposed Dependency Updates

For each update, document:

- Package name.
- Current version.
- Recommended version.
- Projects affected.
- Required changes.
- Compatibility requirements.
- Benefits.
- Breaking changes.
- Security impact.
- Migration considerations.
- Official sources.

## 5. Existing Custom Functionality Assessment

Document relevant custom-built implementations.

Include:

- File path.
- Class or method.
- Existing behavior.
- Current implementation limitations.
- Why modernization is being considered.
- Whether the implementation should remain or change.

## 6. Proposed Replacements and Modernizations

For every candidate, document:

- Existing solution.
- Proposed replacement.
- Package, framework, SDK, or tool.
- Recommended version where applicable.
- Alternative candidates.
- Comparison.
- Benefits.
- Risks.
- Compatibility.
- Dependencies.
- Estimated effort.
- Recommendation.

## 7. File-Level Change Requirements

Provide exact repository-relative paths.

For each affected file document:

- Existing implementation.
- Required change.
- Associated dependency or modernization.
- Reason.
- Mandatory or optional classification.
- Expected outcome.

## 8. Cross-Project Changes

Document shared libraries, common components, dependency consolidation, centralized configuration, and multi-project implications.

## 9. AI Development Environment Improvements

Document:

- Copilot instructions.
- Custom agents.
- Prompt files.
- Reusable skills.
- Serena integration.
- MCP servers.
- VS Code extensions.
- Documentation retrieval.
- Code navigation.
- AI context improvements.
- Security and permission requirements.

Separate currently installed tools from proposed additions.

## 10. Security Requirements

Document vulnerabilities, CVEs, package trust, license issues, and required security improvements.

## 11. Performance Requirements

Document measurable or documented performance opportunities.

Include baseline and expected outcomes where available.

## 12. User Experience and Maintainability Requirements

Document improvements to usability, accessibility, application consistency, maintainability, and developer workflows.

## 13. Architecture and Compatibility Requirements

Document framework, runtime, SDK, API, and dependency compatibility.

## 14. Implementation Plan

Create ordered implementation phases.

For each phase include:

- Objective.
- Projects involved.
- Dependencies involved.
- Files affected.
- Required changes.
- Prerequisites.
- Dependencies on other phases.
- Completion criteria.

## 15. Testing and Verification Strategy

Document all required tests and validation procedures.

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
- [ ] Performance improvements are validated against the baseline where applicable.
- [ ] All relevant automated tests pass.
- [ ] AI development workflow changes are validated.
- [ ] No unapproved application changes are introduced.

## 17. Risks and Constraints

Document migration risks, technical limitations, deferred improvements, and compatibility uncertainties.

## 18. Out of Scope

Identify unrelated work and improvements that should not be included.

## 19. Decisions Required

Clearly identify any proposed changes requiring user approval.

Separate approved scope from optional recommendations and unresolved decisions.

## 20. Research References

Include authoritative URLs for package documentation, release notes, migration guides, security advisories, tools, and integration documentation.

---

# PHASE 12 — SPECKIT READINESS

The Markdown document must be useful as the input to `/speckit-specify`.

To achieve this:

1. Describe the desired outcomes rather than prescribing every implementation detail unnecessarily.
2. Document existing application behavior that must be preserved.
3. Clearly identify requirements and constraints.
4. Include all affected projects and shared components.
5. Provide enough technical evidence to prevent another agent from guessing.
6. Include specific acceptance criteria.
7. Distinguish mandatory changes from optional improvements.
8. Identify dependencies between implementation phases.
9. Include known risks and unresolved decisions.
10. Explicitly protect existing business logic and functionality.

Do not treat every suggested package or replacement as approved.

The specification must clearly identify which recommendations are ready for implementation and which require a decision.

Do not begin implementing the modernization work.

---

# PHASE 13 — FINAL QUALITY VALIDATION

Before completing the task, validate both files.

## PDF Validation

- PDF opens correctly.
- All pages render correctly.
- No clipped text.
- No broken tables.
- Links work.
- All important findings are included.
- Language is understandable to an end user.
- Recommendations are consistent.

## Markdown Validation

- Valid Markdown.
- All required sections exist.
- File paths are accurate.
- Package versions are verified.
- Technical recommendations are actionable.
- Acceptance criteria are testable.
- Spec Kit requirements are addressed.
- Unapproved changes are clearly identified.

## Cross-Document Validation

Compare the PDF and Markdown documents.

Verify that:

- Package counts match.
- Version recommendations match.
- Modernization opportunities match.
- Affected files match.
- Priorities match.
- Security findings match.
- Recommendations do not contradict each other.

---

# PHASE 14 — OUTPUT AND COMPLETION

Create this directory at the workspace root:

`Dependency_Assessment/`

Generate these files:

1. `Dependency_Update_and_Modernization_Report.pdf`
2. `DEPENDENCY_AND_MODERNIZATION_SPECIFICATION.md`

If the files already exist, preserve them and create appropriately versioned alternatives rather than silently overwriting them.

Use tools already available in the development environment to generate the PDF. Do not install additional dependencies just to create the report unless explicitly authorized.

If a genuine PDF generation limitation exists, report it rather than producing a broken or misleading file.

At completion, provide:

### Workspace Assessment Summary
- Repositories reviewed.
- Projects reviewed.
- Dependencies inventoried.
- Dependencies with available updates.
- Recommended package updates.
- Identified custom functionality replacements.
- Recommended modernization opportunities.
- Identified security concerns.
- Performance improvement opportunities.
- User experience improvements.
- AI development workflow improvements.

### Generated Deliverables
Provide the exact paths to both files.

### Important Findings
Highlight the highest-priority findings and any significant compatibility or security concerns.

---

# FINAL OBJECTIVE

Do not approach this task as simply finding newer packages.

Approach it as an opportunity to comprehensively evaluate whether the entire development workspace and its applications are using the best appropriate technologies and practices.

Look for opportunities to:

- Update outdated dependencies.
- Replace inefficient custom implementations.
- Adopt better framework capabilities.
- Eliminate unnecessary code.
- Consolidate duplicate functionality.
- Improve application performance.
- Strengthen security.
- Improve user experience.
- Improve maintainability.
- Optimize developer tooling.
- Improve GitHub Copilot and AI agent effectiveness.
- Reduce unnecessary AI context and token consumption.
- Improve coding accuracy and consistency.
- Simplify future development and maintenance.

Every recommendation must have a clear purpose, evidence, and explanation of its expected benefit.

The PDF must help the user understand WHAT should improve and WHY.

The Markdown document must give a subsequent AI agent the information needed to understand HOW to plan and implement those improvements across the entire workspace.

**Perform the complete assessment first. Do not implement the proposed changes.**