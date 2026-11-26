# Extension change request: csharp-xaml-graph-incompatible-023

Run this with `/speckit-companion-specify`.

csharp-xaml-graph-incompatible-023 could not be profiled: the extension found 3 structures it cannot handle, before any convention was asked.

## The analysis reads C# and XAML, and found no C# files in the files it reads, so there are no types to draw (0 XAML files read).

Structure: no C# files
Evidence: 0 C# files, 0 XAML files

### Read a repository that declares no C#.

The extension would accept a repository whose types are not C# and place them on the graph, instead of stopping because the C# file count is zero.

Evidence:
- 0 C# files
- 0 XAML files

### Say which languages the analysis reads, before asking about conventions.

The setup prompt would name the languages the analysis understands, so a repository built in another language is told what would be needed rather than asked questions it cannot answer.

Evidence:
- 0 C# files
- 0 XAML files

## The analysis reads C# and XAML, and found no XAML files in the files it reads, so no view-to-view-model binding can be drawn (0 C# files read).

Structure: no XAML files
Evidence: 0 XAML files, 0 C# files

### Read a repository that declares no XAML.

The extension would draw the C# dependency graph for a repository with no XAML, and treat the missing view layer as absent rather than as a reason to refuse the whole repository.

Evidence:
- 0 XAML files
- 0 C# files

## The analysis reads .csproj projects and C# sources, and this repository is built as a .fsproj project (Sample.fsproj), which it does not read.

Structure: Sample.fsproj
Evidence: 1 .fsproj project

### Read the project kinds the analysis refuses.

The extension would read .fsproj, .vbproj and .vcxproj projects, or accept a repository built as one without a C# project beside it.

Evidence:
- 1 .fsproj project

## What was not changed

Nothing in csharp-xaml-graph-incompatible-023 was created or changed. This file is a request against the extension.
