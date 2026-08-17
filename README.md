*Recommended Markdown Viewer: [Markdown Editor](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.MarkdownEditor2)*

## Getting Started

Browse and address `TODO:` comments in `View -> Task List` to learn the codebase and understand next steps for turning the generated code into production code.

Explore the [WinUI Gallery](https://www.microsoft.com/store/productId/9P3JFPWWDZRC) to learn about available controls and design patterns.

Relaunch Template Studio to modify the project by right-clicking on the project in `View -> Solution Explorer` then selecting `Add -> New Item (Template Studio)`.

## External Integrations

### Tables Ready

- API key: `5a657e2e-c68e-484c-9249-9db7049a5ada`

## Publishing

For projects with MSIX packaging, right-click on the application project and select `Package and Publish -> Create App Packages...` to create an MSIX package.

For projects without MSIX packaging, follow the [deployment guide](https://docs.microsoft.com/windows/apps/windows-app-sdk/deploy-unpackaged-apps) or add the `Self-Contained` Feature to enable xcopy deployment.

## CI Pipelines

See [README.md](https://github.com/microsoft/TemplateStudio/blob/main/docs/WinUI/pipelines/README.md) for guidance on building and testing projects in CI pipelines.

## WinUI 3 Dynamic Scaling & Accessibility Standouts

- Hardcoded pixel boundaries for layout-critical Width or Height values are forbidden for page, card, list, and form containers. Use fluid sizing (Auto and star columns/rows) with bounded constraints such as MinWidth, MinHeight, MaxWidth, and MaxHeight.
- Interactive overlay elements (such as floating action buttons) must define AdaptiveTrigger-backed visual states and must pair each overlay footprint with a matching bottom list/content padding offset so scrollable content always clears the overlay.
- Text-heavy layouts must be resilient to 150%+ text scaling by using ThemeResource-backed text styles, explicit MaxWidth constraints for identity and header text, and TextTrimming set to CharacterEllipsis when no-wrap behavior is required.
- Overflow-prone detail regions should use scrolling containers or wrapping text patterns so users on low-resolution displays can still reach all interactive and informational elements.

## Changelog

See [releases](https://github.com/microsoft/TemplateStudio/releases) and [milestones](https://github.com/microsoft/TemplateStudio/milestones).

## Feedback

Bugs and feature requests should be filed at https://aka.ms/templatestudio.
