# Demo instruction files

One file per page in the lab, scoped with `applyTo` to the two source files that implement it, so it loads only when that page is being read or changed.

Regenerate after any catalog or demo change: `WinUI3LearningLab.App.exe --export-instructions` (or run `scripts\verify.ps1`, which regenerates them). Total pages: 167.

## WinUI 3

### Basic input (14)

- [Button](BasicInput/ButtonDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.Button` (Starter)
- [CheckBox](BasicInput/CheckBoxDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.CheckBox` (Starter)
- [ColorPicker](BasicInput/ColorPickerDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ColorPicker` (Intermediate)
- [ComboBox](BasicInput/ComboBoxDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ComboBox` (Starter)
- [DropDownButton](BasicInput/DropDownButtonDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.DropDownButton` (Starter)
- [HyperlinkButton](BasicInput/HyperlinkButtonDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.HyperlinkButton` (Starter)
- [RadioButton](BasicInput/RadioButtonDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.RadioButton` (Starter)
- [RatingControl](BasicInput/RatingControlDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.RatingControl` (Starter)
- [RepeatButton](BasicInput/RepeatButtonDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.Primitives.RepeatButton` (Intermediate)
- [Slider](BasicInput/SliderDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.Slider` (Starter)
- [SplitButton](BasicInput/SplitButtonDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.SplitButton` (Intermediate)
- [ToggleButton](BasicInput/ToggleButtonDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.Primitives.ToggleButton` (Starter)
- [ToggleSplitButton](BasicInput/ToggleSplitButtonDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ToggleSplitButton` (Intermediate)
- [ToggleSwitch](BasicInput/ToggleSwitchDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ToggleSwitch` (Starter)

### Text (7)

- [AutoSuggestBox](Text/AutoSuggestBoxDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.AutoSuggestBox` (Intermediate)
- [NumberBox](Text/NumberBoxDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.NumberBox` (Intermediate)
- [PasswordBox](Text/PasswordBoxDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.PasswordBox` (Starter)
- [RichEditBox](Text/RichEditBoxDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.RichEditBox` (Advanced)
- [RichTextBlock](Text/RichTextBlockDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.RichTextBlock` (Intermediate)
- [TextBlock](Text/TextBlockDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.TextBlock` (Starter)
- [TextBox](Text/TextBoxDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.TextBox` (Starter)

### Collections (8)

- [FlipView](Collections/FlipViewDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.FlipView` (Intermediate)
- [GridView](Collections/GridViewDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.GridView` (Starter)
- [ItemsRepeater](Collections/ItemsRepeaterDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ItemsRepeater` (Advanced)
- [ItemsView](Collections/ItemsViewDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ItemsView` (Advanced)
- [ListView](Collections/ListViewDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ListView` (Starter)
- [Pull to refresh (RefreshContainer)](Collections/RefreshContainerDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.RefreshContainer` (Intermediate)
- [TableView (not in this SDK)](Collections/TableViewDemo.instructions.md) — `Concept only` (Intermediate)
- [TreeView](Collections/TreeViewDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.TreeView` (Intermediate)

### Date & time (4)

- [CalendarDatePicker](DateAndTime/CalendarDatePickerDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.CalendarDatePicker` (Starter)
- [CalendarView](DateAndTime/CalendarViewDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.CalendarView` (Intermediate)
- [DatePicker](DateAndTime/DatePickerDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.DatePicker` (Starter)
- [TimePicker](DateAndTime/TimePickerDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.TimePicker` (Starter)

### Dialogs & flyouts (4)

- [ContentDialog](DialogsAndFlyouts/ContentDialogDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ContentDialog` (Starter)
- [Flyout](DialogsAndFlyouts/FlyoutDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.Flyout` (Starter)
- [Popup](DialogsAndFlyouts/PopupDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.Primitives.Popup` (Advanced)
- [TeachingTip](DialogsAndFlyouts/TeachingTipDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.TeachingTip` (Intermediate)

### Menus & toolbars (10)

- [AppBarButton](MenusAndToolbars/AppBarButtonDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.AppBarButton` (Starter)
- [AppBarSeparator](MenusAndToolbars/AppBarSeparatorDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.AppBarSeparator` (Starter)
- [AppBarToggleButton](MenusAndToolbars/AppBarToggleButtonDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.AppBarToggleButton` (Starter)
- [CommandBar](MenusAndToolbars/CommandBarDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.CommandBar` (Intermediate)
- [CommandBarFlyout](MenusAndToolbars/CommandBarFlyoutDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.CommandBarFlyout` (Intermediate)
- [MenuBar](MenusAndToolbars/MenuBarDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.MenuBar` (Intermediate)
- [MenuFlyout](MenusAndToolbars/MenuFlyoutDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.MenuFlyout` (Starter)
- [StandardUICommand](MenusAndToolbars/StandardUICommandDemo.instructions.md) — `Microsoft.UI.Xaml.Input.StandardUICommand` (Intermediate)
- [SwipeControl](MenusAndToolbars/SwipeControlDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.SwipeControl` (Intermediate)
- [XamlUICommand](MenusAndToolbars/XamlUICommandDemo.instructions.md) — `Microsoft.UI.Xaml.Input.XamlUICommand` (Intermediate)

### Status & info (5)

- [InfoBadge](StatusAndInfo/InfoBadgeDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.InfoBadge` (Starter)
- [InfoBar](StatusAndInfo/InfoBarDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.InfoBar` (Starter)
- [ProgressBar](StatusAndInfo/ProgressBarDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ProgressBar` (Starter)
- [ProgressRing](StatusAndInfo/ProgressRingDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ProgressRing` (Starter)
- [ToolTip](StatusAndInfo/ToolTipDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ToolTip` (Starter)

### Scrolling (5)

- [AnnotatedScrollBar](Scrolling/AnnotatedScrollBarDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.AnnotatedScrollBar` (Intermediate)
- [PipsPager](Scrolling/PipsPagerDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.PipsPager` (Starter)
- [ScrollView](Scrolling/ScrollViewDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ScrollView` (Intermediate)
- [ScrollViewer](Scrolling/ScrollViewerDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ScrollViewer` (Starter)
- [SemanticZoom](Scrolling/SemanticZoomDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.SemanticZoom` (Advanced)

### Layout (11)

- [Border](Layout/BorderDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.Border` (Starter)
- [Canvas](Layout/CanvasDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.Canvas` (Starter)
- [Custom layouts (NonVirtualizingLayout)](Layout/LayoutPanelDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.NonVirtualizingLayout` (Advanced)
- [Expander](Layout/ExpanderDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.Expander` (Starter)
- [Grid](Layout/GridDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.Grid` (Starter)
- [RelativePanel](Layout/RelativePanelDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.RelativePanel` (Intermediate)
- [SplitView](Layout/SplitViewDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.SplitView` (Intermediate)
- [StackPanel](Layout/StackPanelDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.StackPanel` (Starter)
- [VariableSizedWrapGrid](Layout/VariableSizedWrapGridDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.VariableSizedWrapGrid` (Intermediate)
- [Viewbox](Layout/ViewboxDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.Viewbox` (Starter)
- [WrapPanel (Community Toolkit)](Layout/WrapPanelDemo.instructions.md) — `CommunityToolkit.WinUI.UI.Controls.WrapPanel` (Starter)

### Navigation (5)

- [BreadcrumbBar](Navigation/BreadcrumbBarDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.BreadcrumbBar` (Intermediate)
- [NavigationView](Navigation/NavigationViewDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.NavigationView` (Intermediate)
- [Pivot](Navigation/PivotDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.Pivot` (Intermediate)
- [SelectorBar](Navigation/SelectorBarDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.SelectorBar` (Starter)
- [TabView](Navigation/TabViewDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.TabView` (Advanced)

### Media (10)

- [AnimatedVisualPlayer](Media/AnimatedVisualPlayerDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer` (Advanced)
- [Camera preview (MediaCapture)](Media/CameraPreviewDemo.instructions.md) — `Windows.Media.Capture.MediaCapture` (Advanced)
- [Chart (no framework control)](Media/ChartDemo.instructions.md) — `Concept only` (Intermediate)
- [Image](Media/ImageDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.Image` (Starter)
- [Ink (InkCanvas is UWP-only)](Media/InkCanvasDemo.instructions.md) — `Windows.UI.Input.Inking.InkPresenter` (Advanced)
- [MapControl](Media/MapControlDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.MapControl` (Intermediate)
- [MediaPlayerElement](Media/MediaPlayerElementDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.MediaPlayerElement` (Intermediate)
- [PersonPicture](Media/PersonPictureDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.PersonPicture` (Starter)
- [Sound (MediaPlayer)](Media/SoundDemo.instructions.md) — `Windows.Media.Playback.MediaPlayer` (Intermediate)
- [WebView2](Media/WebView2Demo.instructions.md) — `Microsoft.UI.Xaml.Controls.WebView2` (Advanced)

### Styles & brushes (10)

- [AcrylicBrush](Styles/AcrylicDemo.instructions.md) — `Microsoft.UI.Xaml.Media.AcrylicBrush` (Intermediate)
- [AnimatedIcon](Styles/AnimatedIconDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.AnimatedIcon` (Intermediate)
- [Compact sizing](Styles/CompactSizingDemo.instructions.md) — `Concept only` (Intermediate)
- [IconElement](Styles/IconElementDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.IconElement` (Starter)
- [Line](Styles/LineDemo.instructions.md) — `Microsoft.UI.Xaml.Shapes.Line` (Starter)
- [RadialGradientBrush](Styles/RadialGradientBrushDemo.instructions.md) — `Microsoft.UI.Xaml.Media.RadialGradientBrush` (Intermediate)
- [Shape](Styles/ShapeDemo.instructions.md) — `Microsoft.UI.Xaml.Shapes.Shape` (Starter)
- [System backdrops (Mica / Acrylic)](Styles/SystemBackdropsDemo.instructions.md) — `Microsoft.UI.Composition.SystemBackdrops.MicaController` (Intermediate)
- [SystemBackdropElement](Styles/SystemBackdropElementDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.SystemBackdropElement` (Advanced)
- [ThemeShadow](Styles/ThemeShadowDemo.instructions.md) — `Microsoft.UI.Xaml.Media.ThemeShadow` (Intermediate)

### Motion (7)

- [Animation interop (Composition)](Motion/AnimationInteropDemo.instructions.md) — `Microsoft.UI.Xaml.Hosting.ElementCompositionPreview` (Advanced)
- [Connected animation](Motion/ConnectedAnimationDemo.instructions.md) — `Microsoft.UI.Xaml.Media.Animation.ConnectedAnimationService` (Advanced)
- [Easing functions](Motion/EasingFunctionsDemo.instructions.md) — `Microsoft.UI.Xaml.Media.Animation.EasingFunctionBase` (Starter)
- [Implicit and reposition transitions](Motion/ImplicitTransitionsDemo.instructions.md) — `Microsoft.UI.Xaml.Media.Animation.Transition` (Starter)
- [Page transitions](Motion/PageTransitionsDemo.instructions.md) — `Microsoft.UI.Xaml.Media.Animation.NavigationThemeTransition` (Starter)
- [ParallaxView](Motion/ParallaxViewDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.ParallaxView` (Advanced)
- [Theme transitions](Motion/ThemeTransitionsDemo.instructions.md) — `Microsoft.UI.Xaml.Media.Animation.ThemeTransition` (Starter)

### Windowing (4)

- [AppWindow](Windowing/AppWindowDemo.instructions.md) — `Microsoft.UI.Windowing.AppWindow` (Intermediate)
- [AppWindowTitleBar](Windowing/AppWindowTitleBarDemo.instructions.md) — `Microsoft.UI.Windowing.AppWindowTitleBar` (Advanced)
- [TitleBar](Windowing/TitleBarDemo.instructions.md) — `Microsoft.UI.Xaml.Controls.TitleBar` (Starter)
- [Window](Windowing/WindowDemo.instructions.md) — `Microsoft.UI.Xaml.Window` (Starter)

## Windows App SDK

### Platform services (9)

- [Accessibility & automation](Platform/AccessibilityDemo.instructions.md) — `Microsoft.UI.Xaml.Automation.AutomationProperties` (Starter)
- [App data & settings](Platform/AppDataDemo.instructions.md) — `Microsoft.Windows.Storage.ApplicationData` (Intermediate)
- [App lifecycle & single instance](Platform/AppLifecycleDemo.instructions.md) — `Microsoft.Windows.AppLifecycle.AppInstance` (Advanced)
- [Deployment & runtime check](Platform/DeploymentDemo.instructions.md) — `Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentManager` (Advanced)
- [DispatcherQueue](Platform/DispatcherQueueDemo.instructions.md) — `Microsoft.UI.Dispatching.DispatcherQueue` (Intermediate)
- [Drag and drop](Platform/DragDropDemo.instructions.md) — `Windows.ApplicationModel.DataTransfer.DataPackage` (Intermediate)
- [Resources & localization (MRT Core)](Platform/ResourcesDemo.instructions.md) — `Microsoft.Windows.ApplicationModel.Resources.ResourceLoader` (Intermediate)
- [Secondary windows](Platform/SecondaryWindowDemo.instructions.md) — `Microsoft.UI.Xaml.Window` (Intermediate)
- [Theme & accent colour](Platform/ThemeAndAccentDemo.instructions.md) — `Microsoft.UI.Xaml.Application` (Intermediate)

### Clipboard & pickers (3)

- [Clipboard](SystemServices/ClipboardDemo.instructions.md) — `Windows.ApplicationModel.DataTransfer.Clipboard` (Starter)
- [ContentIsland](SystemServices/ContentIslandDemo.instructions.md) — `Microsoft.UI.Content.ContentIsland` (Advanced)
- [Storage pickers](SystemServices/StoragePickersDemo.instructions.md) — `Microsoft.Windows.Storage.Pickers.FileOpenPicker` (Intermediate)

### Notifications & jump lists (3)

- [App notifications](Shell/AppNotificationDemo.instructions.md) — `Microsoft.Windows.AppNotifications.AppNotificationManager` (Intermediate)
- [Badge notifications](Shell/BadgeNotificationDemo.instructions.md) — `Microsoft.Windows.BadgeNotifications.BadgeNotificationManager` (Advanced)
- [JumpList](Shell/JumpListDemo.instructions.md) — `Microsoft.UI.StartScreen.JumpList` (Advanced)

## Community Toolkit 7.x

### Controls (13)

- [DataGrid](Toolkit/DataGridDemo.instructions.md) — `CommunityToolkit.WinUI.UI.Controls.DataGrid` (Advanced)
- [DockPanel](Toolkit/DockPanelDemo.instructions.md) — `CommunityToolkit.WinUI.UI.Controls.DockPanel` (Starter)
- [DropShadowPanel](Toolkit/DropShadowPanelDemo.instructions.md) — `CommunityToolkit.WinUI.UI.Controls.DropShadowPanel` (Intermediate)
- [GridSplitter](Toolkit/GridSplitterDemo.instructions.md) — `CommunityToolkit.WinUI.UI.Controls.GridSplitter` (Intermediate)
- [HeaderedContentControl](Toolkit/HeaderedContentControlDemo.instructions.md) — `CommunityToolkit.WinUI.UI.Controls.HeaderedContentControl` (Starter)
- [ImageCropper](Toolkit/ImageCropperDemo.instructions.md) — `CommunityToolkit.WinUI.UI.Controls.ImageCropper` (Advanced)
- [InAppNotification](Toolkit/InAppNotificationDemo.instructions.md) — `CommunityToolkit.WinUI.UI.Controls.InAppNotification` (Intermediate)
- [RangeSelector](Toolkit/RangeSelectorDemo.instructions.md) — `CommunityToolkit.WinUI.UI.Controls.RangeSelector` (Intermediate)
- [SettingsCard / SettingsExpander](Toolkit/SettingsCardDemo.instructions.md) — `CommunityToolkit.WinUI.Controls.SettingsCard` (Starter)
- [SwitchPresenter](Toolkit/SwitchPresenterDemo.instructions.md) — `CommunityToolkit.WinUI.UI.Controls.SwitchPresenter` (Intermediate)
- [TokenizingTextBox](Toolkit/TokenizingTextBoxDemo.instructions.md) — `CommunityToolkit.WinUI.UI.Controls.TokenizingTextBox` (Advanced)
- [UniformGrid](Toolkit/UniformGridDemo.instructions.md) — `CommunityToolkit.WinUI.UI.Controls.UniformGrid` (Starter)
- [WinUIEx](Toolkit/WinUIExDemo.instructions.md) — `WinUIEx.WindowEx` (Intermediate)

## Community Toolkit.Mvvm

### ObservableObject & commands (4)

- [ObservableProperty](Mvvm/ObservablePropertyDemo.instructions.md) — `CommunityToolkit.Mvvm.ComponentModel.ObservableObject` (Starter)
- [ObservableValidator](Mvvm/ObservableValidatorDemo.instructions.md) — `CommunityToolkit.Mvvm.ComponentModel.ObservableValidator` (Advanced)
- [RelayCommand](Mvvm/RelayCommandDemo.instructions.md) — `CommunityToolkit.Mvvm.Input.RelayCommand` (Starter)
- [WeakReferenceMessenger](Mvvm/MessengerDemo.instructions.md) — `CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger` (Intermediate)

## XAML Behaviors

### Triggers & actions (3)

- [ChangePropertyAction](Behaviors/ChangePropertyActionDemo.instructions.md) — `Microsoft.Xaml.Interactions.Core.ChangePropertyAction` (Starter)
- [EventTriggerBehavior](Behaviors/EventTriggerBehaviorDemo.instructions.md) — `Microsoft.Xaml.Interactivity.EventTriggerBehavior` (Intermediate)
- [InvokeCommandAction](Behaviors/InvokeCommandActionDemo.instructions.md) — `Microsoft.Xaml.Interactions.Core.InvokeCommandAction` (Intermediate)

## DevWinUI

### Controls (28)

- [BannerView](DevWinUI/BannerViewDemo.instructions.md) — `DevWinUI.BannerView` (Intermediate)
- [Barcode](DevWinUI/BarcodeDemo.instructions.md) — `DevWinUI.Base.Barcode` (Advanced)
- [CarouselView2](DevWinUI/CarouselViewDemo.instructions.md) — `DevWinUI.CarouselView2` (Intermediate)
- [ClockPicker](DevWinUI/ClockPickerDemo.instructions.md) — `DevWinUI.ClockPicker` (Intermediate)
- [ColorPalette](DevWinUI/ColorPaletteDemo.instructions.md) — `DevWinUI.ColorPalette` (Intermediate)
- [CopyButton](DevWinUI/CopyButtonDemo.instructions.md) — `DevWinUI.CopyButton` (Starter)
- [Countdown](DevWinUI/CountdownDemo.instructions.md) — `DevWinUI.Countdown` (Starter)
- [Divider](DevWinUI/DividerDemo.instructions.md) — `DevWinUI.Divider` (Starter)
- [Empty](DevWinUI/EmptyDemo.instructions.md) — `DevWinUI.Empty` (Starter)
- [FlexPanel](DevWinUI/FlexPanelDemo.instructions.md) — `DevWinUI.FlexPanel` (Intermediate)
- [Gravatar](DevWinUI/GravatarDemo.instructions.md) — `DevWinUI.Gravatar` (Starter)
- [Growl](DevWinUI/GrowlDemo.instructions.md) — `DevWinUI.Growl` (Intermediate)
- [InfoCard](DevWinUI/InfoCardDemo.instructions.md) — `DevWinUI.InfoCard` (Starter)
- [LiveGraph](DevWinUI/LiveGraphDemo.instructions.md) — `DevWinUI.LiveGraph` (Advanced)
- [PagerControl](DevWinUI/PagerControlDemo.instructions.md) — `DevWinUI.PagerControl` (Intermediate)
- [PinBox](DevWinUI/PinBoxDemo.instructions.md) — `DevWinUI.PinBox` (Intermediate)
- [ProgressButton](DevWinUI/ProgressButtonDemo.instructions.md) — `DevWinUI.ProgressButton` (Intermediate)
- [SelectorBarSegmented](DevWinUI/SegmentedDemo.instructions.md) — `DevWinUI.SelectorBarSegmented` (Starter)
- [Shimmer](DevWinUI/ShimmerDemo.instructions.md) — `DevWinUI.Shimmer` (Starter)
- [ShyHeader](DevWinUI/ShyHeaderDemo.instructions.md) — `DevWinUI.ShyHeader` (Intermediate)
- [Spoiler](DevWinUI/SpoilerDemo.instructions.md) — `DevWinUI.Spoiler` (Starter)
- [Stars](DevWinUI/StarsDemo.instructions.md) — `DevWinUI.Stars` (Starter)
- [StepBar](DevWinUI/StepBarDemo.instructions.md) — `DevWinUI.StepBar` (Intermediate)
- [StorageBar](DevWinUI/StorageBarDemo.instructions.md) — `DevWinUI.StorageBar` (Starter)
- [TileControl](DevWinUI/TileControlDemo.instructions.md) — `DevWinUI.TileControl` (Intermediate)
- [Timeline](DevWinUI/TimelineDemo.instructions.md) — `DevWinUI.Timeline` (Intermediate)
- [TransitioningContentControl](DevWinUI/TransitioningContentControlDemo.instructions.md) — `DevWinUI.TransitioningContentControl` (Intermediate)
- [Watermark](DevWinUI/WatermarkDemo.instructions.md) — `DevWinUI.Watermark` (Starter)

