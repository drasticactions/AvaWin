# AvaWin

AvaWin is an experimental Avalonia theme and toolkit that reproduces the look, metrics and controls of the Metro controls from the Windows 8.1 / Windows Phone 8.1 era. It is derived from the WinJS 3.x and 4.x builds to try and match what they do.

You can check out the AvaWin Gallery on https://drasticactions.github.io/AvaWin/

Issues and PRs are welcome, but note that this is a hobby project. If you find yourself depending on this, you should probably fork it.

## Getting started

```xml
<!-- App.axaml -->
<Application xmlns="https://github.com/avaloniaui"
             xmlns:win="https://github.com/avawin">
  <Application.Styles>
    <win:AvaWinTheme Platform="Auto" />        <!-- Desktop | Phone | Auto -->
    <win:AvaWinControlsTheme />                <!-- only when AvaWin.Controls is referenced -->
  </Application.Styles>
</Application>
```

```csharp
AppBuilder.Configure<App>().UsePlatformDetect().WithAvaWinFonts().StartWithClassicDesktopLifetime(args);
```

`xmlns:win="https://github.com/avawin"` covers `AvaWin`, `AvaWin.Controls` and `AvaWin.Animations`.

```xml
<StackPanel Spacing="12">
  <TextBlock Classes="win-type-x-large" Text="Hello" />
  <Button Classes="accent" Content="Submit" />
  <win:Rating UserRating="3" />
  <win:ListView ItemsSource="{Binding People}" TapBehavior="ToggleSelect" SelectionStyle="Filled" />
  <win:AppBar>
    <win:AppBarCommand Label="Add" Icon="Add" />
    <win:AppBarCommand Label="Delete" Icon="Delete" Section="Selection" />
  </win:AppBar>
</StackPanel>
```