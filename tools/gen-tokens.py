#!/usr/bin/env python3
"""Generates the Layer 2 token files from one data table:

    src/AvaWin.Theme/Accents/BaseResources.xaml   (Desktop values, both variants)
    src/AvaWin.Theme/Accents/PhoneMetrics.xaml    (Phone overrides only)

Edit the tables here and run the script again. Do not edit the generated XAML by hand.

Brush entries: (key, light, dark, phone_light, phone_dark). A value is either a literal #AARRGGBB,
a palette/accent key name (rendered as {DynamicResource Key}), or None (phone: same as desktop).
Literal pairs that equal a Layer 1 palette pair are emitted as references to that palette key so the
ColorPaletteResources override surface reaches them.
"""
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "src", "AvaWin.Theme", "Accents")

PALETTE = [
    ("SystemBaseHighColor", "#FF000000", "#FFFFFFFF"),
    ("SystemBaseMediumHighColor", "#CC000000", "#CCFFFFFF"),
    ("SystemBaseMediumColor", "#99000000", "#99FFFFFF"),
    ("SystemBaseMediumLowColor", "#66000000", "#66FFFFFF"),
    ("SystemBaseLowColor", "#33000000", "#33FFFFFF"),
    ("SystemAltHighColor", "#FFFFFFFF", "#FF1D1D1D"),
    ("SystemAltMediumHighColor", "#CCFFFFFF", "#CC1D1D1D"),
    ("SystemAltMediumColor", "#99FFFFFF", "#991D1D1D"),
    ("SystemAltMediumLowColor", "#66FFFFFF", "#661D1D1D"),
    ("SystemAltLowColor", "#33FFFFFF", "#331D1D1D"),
    ("SystemChromeLowColor", "#FFF0F0F0", "#FF000000"),
    ("SystemChromeMediumColor", "#FFDEDEDE", "#FFDEDEDE"),
    ("SystemChromeHighColor", "#FF7B7B7B", "#FF7B7B7B"),
    ("SystemChromeAltLowColor", "#FF2A2A2A", "#FF2A2A2A"),
    ("SystemChromeDisabledHighColor", "#66CACACA", "#00000000"),
    ("SystemChromeDisabledLowColor", "#FF929292", "#FF7E7E7E"),
    ("SystemChromeGrayColor", "#FF808080", "#FF808080"),
    ("SystemChromeBlackHighColor", "#FF000000", "#FF000000"),
    ("SystemChromeBlackMediumColor", "#CC000000", "#CC000000"),
    ("SystemChromeBlackMediumLowColor", "#66000000", "#66000000"),
    ("SystemChromeBlackLowColor", "#33000000", "#33000000"),
    ("SystemChromeWhiteColor", "#FFFFFFFF", "#FFFFFFFF"),
    ("SystemListLowColor", "#21000000", "#21FFFFFF"),
    ("SystemListMediumColor", "#4D000000", "#4DFFFFFF"),
    ("SystemErrorTextColor", "#FFFF8033", "#FFFF8033"),
]
PALETTE_BY_PAIR = {}
for k, l, d in PALETTE:
    PALETTE_BY_PAIR.setdefault((l, d), k)

ACCENT = "SystemAccentColor"
L1 = "SystemAccentColorLight1"
L2 = "SystemAccentColorLight2"
L3 = "SystemAccentColorLight3"
ONDARK = "WinAccentOnDarkColor"
ONDARK_HOVER = "WinAccentOnDarkHoverColor"
ONDARK_PRESSED = "WinAccentOnDarkPressedColor"
ONDARK_VIVID = "WinAccentOnDarkVividColor"

# (section, [(key, light, dark, phone_light, phone_dark), ...])
BRUSHES = [
("Text", [
    ("WinTextBrush", "#FF000000", "#FFFFFFFF", "#DE000000", "#FFFFFFFF"),
    ("WinTextSecondaryBrush", "#99000000", "#99FFFFFF", None, None),
    ("WinTextTertiaryBrush", "#4D000000", "#4DFFFFFF", None, None),
    ("WinTextDisabledBrush", "#66000000", "#66FFFFFF", "#4D000000", "#4DFFFFFF"),
    ("WinTextHoverBrush", "#CC000000", "#CCFFFFFF", None, None),
    ("WinTextPressedBrush", "#66000000", "#66FFFFFF", None, None),
    ("WinLinkBrush", "WinLinkColor", "WinLinkOnDarkColor", None, None),
    ("WinLinkHoverBrush", ("WinLinkColor", 0.8), ("WinLinkOnDarkColor", 0.8), None, None),
    ("WinLinkPressedBrush", ("WinLinkColor", 0.6), ("WinLinkOnDarkColor", 0.6), None, None),
    ("WinPlaceholderBrush", "#99000000", "#99000000", "#40000000", "#40000000"),
    ("WinPlaceholderDisabledBrush", "#38000000", "#38FFFFFF", None, None),
    ("WinTextSelectionBrush", "WinTextSelectionColor", "WinTextSelectionColor", ACCENT, ACCENT),
    ("WinTextSelectionForegroundBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinErrorBrush", "SystemErrorTextColor", "SystemErrorTextColor", None, None),
]),
("Surfaces", [
    ("WinPageBackgroundBrush", "#FFFFFFFF", "#FF1D1D1D", None, "#FF000000"),
    ("WinChromeBackgroundBrush", "#FFF0F0F0", "#FF000000", None, None),
    ("WinOverlayBackgroundBrush", "#FFFFFFFF", "#FF000000", None, None),
    ("WinOverlayBorderBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinPaneBackgroundBrush", "#FFFFFFFF", "#FF000000", None, None),
    ("WinPaneBorderBrush", "#3D000000", "#3DFFFFFF", None, None),
    ("WinTooltipBackgroundBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinTooltipBorderBrush", "#FF808080", "#FF808080", None, None),
    ("WinTooltipForegroundBrush", "#99000000", "#99000000", None, None),
    ("WinSeparatorBrush", "#FF7B7B7B", "#FF7B7B7B", None, None),
    ("WinFocusOutlineBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinOverlayDimBrush", "#66000000", "#66000000", None, None),
]),
("Buttons", [
    ("WinButtonBackgroundBrush", "#B3B6B6B6", "#00000000", "#00000000", "#00000000"),
    ("WinButtonBorderBrush", "#33000000", "#FFFFFFFF", "#FF000000", "#FFFFFFFF"),
    ("WinButtonForegroundBrush", "#FF000000", "#FFFFFFFF", "#DE000000", "#FFFFFFFF"),
    ("WinButtonBackgroundHoverBrush", "#D1CDCDCD", "#21FFFFFF", "#00000000", "#00000000"),
    ("WinButtonBorderHoverBrush", "#73A4A4A4", "#FFFFFFFF", "#FF000000", "#FFFFFFFF"),
    ("WinButtonForegroundHoverBrush", "#FF000000", "#FFFFFFFF", "#DE000000", "#FFFFFFFF"),
    ("WinButtonBackgroundPressedBrush", "#FF000000", "#FFFFFFFF", ACCENT, ACCENT),
    ("WinButtonBorderPressedBrush", "#FF000000", "#FFFFFFFF", ACCENT, ACCENT),
    ("WinButtonForegroundPressedBrush", "#FFFFFFFF", "#FF000000", "#FFFFFFFF", "#FFFFFFFF"),
    ("WinButtonBackgroundDisabledBrush", "#66CACACA", "#00000000", "#00000000", "#00000000"),
    ("WinButtonBorderDisabledBrush", "#14000000", "#66FFFFFF", "#66000000", "#66FFFFFF"),
    ("WinButtonForegroundDisabledBrush", "#66000000", "#66FFFFFF", "#4D000000", "#4DFFFFFF"),
    ("WinAccentButtonBackgroundBrush", ACCENT, ACCENT, None, None),
    ("WinAccentButtonBorderBrush", "#00000000", "#00000000", None, None),
    ("WinAccentButtonForegroundBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinAccentButtonBackgroundHoverBrush", L1, L1, None, None),
    ("WinAccentButtonBorderHoverBrush", "#00000000", "#FFFFFFFF", None, None),
    ("WinAccentButtonForegroundHoverBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinAccentButtonBackgroundPressedBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinAccentButtonBorderPressedBrush", "#00000000", "#00000000", None, None),
    ("WinAccentButtonForegroundPressedBrush", "#FFFFFFFF", "#FF000000", None, None),
    ("WinAccentButtonBackgroundDisabledBrush", "#66CACACA", "#00000000", None, None),
    ("WinAccentButtonBorderDisabledBrush", "#14000000", "#66FFFFFF", None, None),
    ("WinAccentButtonForegroundDisabledBrush", "#66000000", "#66FFFFFF", None, None),
    ("WinButtonHoverWashBrush", "#21000000", "#21FFFFFF", None, None),
    ("WinToggleButtonBackgroundCheckedHoverBrush", "#FF212121", "#FFDEDEDE", None, None),
    ("WinToggleButtonBorderIndeterminateBrush", ACCENT, ACCENT, None, None),
]),
("Inputs", [
    ("WinInputBackgroundBrush", "#CCFFFFFF", "#CCFFFFFF", "#33000000", "#CCFFFFFF"),
    ("WinInputBorderBrush", "#45000000", "#00000000", "#00000000", "#00000000"),
    ("WinInputForegroundBrush", "#FF000000", "#FF000000", "#DE000000", "#FF000000"),
    ("WinInputBackgroundHoverBrush", "#DEFFFFFF", "#DEFFFFFF", "#33000000", "#CCFFFFFF"),
    ("WinInputBorderHoverBrush", "#70000000", "#00000000", "#00000000", "#00000000"),
    ("WinInputBackgroundFocusedBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinInputBorderFocusedBrush", "#45000000", "#00000000", ACCENT, ACCENT),
    ("WinSelectBorderFocusedBrush", "#99000000", "#00000000", ACCENT, ACCENT),
    ("WinInputBackgroundDisabledBrush", "#66CACACA", "#00000000", "#00000000", "#00000000"),
    ("WinInputBorderDisabledBrush", "#26000000", "#66FFFFFF", None, None),
    ("WinInputForegroundDisabledBrush", "#66000000", "#66FFFFFF", "#4D000000", "#4DFFFFFF"),
    ("WinCheckGlyphBrush", "#FF000000", "#FF000000", "#FF000000", "#FFFFFFFF"),
    ("WinCheckBackgroundPressedBrush", "#FF000000", "#FFFFFFFF", ACCENT, ACCENT),
    ("WinCheckGlyphPressedBrush", "#FFFFFFFF", "#FF000000", "#FFFFFFFF", "#FFFFFFFF"),
    ("WinCheckBorderBrush", "#45000000", "#00000000", "#FF000000", "#FFFFFFFF"),
    ("WinCheckBackgroundBrush", "#CCFFFFFF", "#CCFFFFFF", "#00000000", "#00000000"),
    ("WinCheckBorderHoverBrush", "#70000000", "#00000000", "#FF000000", "#FFFFFFFF"),
    ("WinCheckBackgroundHoverBrush", "#DEFFFFFF", "#DEFFFFFF", "#00000000", "#00000000"),
    ("WinCheckBorderDisabledBrush", "#26000000", "#66FFFFFF", "#4D000000", "#4DFFFFFF"),
    ("WinCheckBackgroundDisabledBrush", "#66CACACA", "#00000000", "#00000000", "#00000000"),
    ("WinCheckGlyphDisabledBrush", "#66000000", "#66FFFFFF", "#4D000000", "#4DFFFFFF"),
    ("WinDropDownItemBackgroundHoverBrush", "#FFDEDEDE", "#FFDEDEDE", "#FFFFFFFF", "#FFFFFFFF"),
    ("WinDropDownItemBackgroundSelectedBrush", ACCENT, ACCENT, "#00000000", "#00000000"),
    ("WinDropDownItemForegroundSelectedBrush", "#FFFFFFFF", "#FFFFFFFF", ACCENT, ACCENT),
    ("WinDropDownItemBackgroundSelectedHoverBrush", L1, L1, "#00000000", "#00000000"),
    ("WinDropDownItemBackgroundSelectedDisabledBrush", "#8C000000", "#66FFFFFF", None, None),
    ("WinDropDownItemForegroundSelectedDisabledBrush", "#99FFFFFF", "#99000000", None, None),
    ("WinDropDownItemForegroundBrush", "#FF000000", "#FF000000", None, None),
    ("WinDropDownItemForegroundDisabledBrush", "#66000000", "#66000000", None, None),
    ("WinDropDownBackgroundBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinDropDownBorderBrush", "#45000000", "#00000000", None, None),
]),
("Lists and selection", [
    ("WinListItemBackgroundBrush", "#FFFFFFFF", "#FF1D1D1D", None, "#FF000000"),
    ("WinListItemBackdropBrush", "#3B9B9B9B", "#3B9B9B9B", None, None),
    ("WinListItemHoverOutlineBrush", "#4D000000", "#4DFFFFFF", "#00000000", "#00000000"),
    ("WinListItemFilledHoverBrush", "#4D000000", "#4DFFFFFF", None, None),
    ("WinSelectionBorderBrush", ACCENT, ACCENT, None, None),
    ("WinSelectionBorderHoverBrush", L1, L1, ACCENT, ACCENT),
    ("WinSelectionFillBrush", ACCENT, ACCENT, None, None),
    ("WinSelectionFillHoverBrush", L1, L1, ACCENT, ACCENT),
    ("WinSelectionForegroundBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinSelectionCheckmarkBackgroundBrush", ACCENT, ACCENT, None, None),
    ("WinSelectionCheckmarkBackgroundHoverBrush", L1, L1, ACCENT, ACCENT),
    ("WinSelectionCheckmarkBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinSelectionHintBrush", ACCENT, "#FFFFFFFF", None, None),
    ("WinListFocusOutlineBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinGroupHeaderForegroundBrush", "#FF000000", "#FFFFFFFF", "#DE000000", "#FFFFFFFF"),
]),
("Progress and slider", [
    ("WinProgressTrackBrush", "#33000000", "#59FFFFFF", "#18000000", "#1AFFFFFF"),
    ("WinProgressFillBrush", ACCENT, ONDARK, ACCENT, ACCENT),
    ("WinProgressIndeterminateBrush", ACCENT, ONDARK_VIVID, ACCENT, ACCENT),
    ("WinProgressIndeterminateTrackBrush", "#59000000", "#59FFFFFF", None, None),
    ("WinSliderTrackBrush", "#1A000000", "#29FFFFFF", None, None),
    ("WinSliderTrackHoverBrush", "#26000000", "#2EFFFFFF", None, None),
    ("WinSliderFillBrush", ACCENT, ONDARK, ACCENT, ACCENT),
    ("WinSliderFillHoverBrush", L1, ONDARK_HOVER, ACCENT, ACCENT),
    ("WinSliderFillDisabledBrush", "#33000000", "#3BFFFFFF", None, None),
    ("WinSliderThumbBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinSliderThumbDisabledBrush", "#FF929292", "#FF7E7E7E", None, None),
    ("WinTickBrush", "#80000000", "#80FFFFFF", None, None),
]),
("ToggleSwitch", [
    ("WinToggleTrackBorderBrush", "#59000000", "#59FFFFFF", None, None),
    ("WinToggleTrackBorderDisabledBrush", "#33000000", "#33FFFFFF", None, None),
    ("WinToggleThumbBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinToggleThumbDisabledBrush", "#FF929292", "#FF7E7E7E", None, None),
    ("WinToggleOffFillBrush", "#59000000", "#42FFFFFF", None, None),
    ("WinToggleOffFillHoverBrush", "#4A000000", "#4AFFFFFF", None, None),
    ("WinToggleOffFillPressedBrush", "#42000000", "#59FFFFFF", None, None),
    ("WinToggleOffFillDisabledBrush", "#1F000000", "#1FFFFFFF", None, None),
    ("WinToggleOnFillBrush", ACCENT, ONDARK, ACCENT, ACCENT),
    ("WinToggleOnFillHoverBrush", L1, ONDARK_HOVER, ACCENT, ACCENT),
    ("WinToggleOnFillPressedBrush", L2, ONDARK_PRESSED, ACCENT, ACCENT),
    ("WinToggleOnFillDisabledBrush", "#1F000000", "#FF7E7E7E", None, None),
]),
("Menus", [
    ("WinMenuItemBackgroundHoverBrush", "#FFDEDEDE", "#FFDEDEDE", None, None),
    ("WinMenuItemBackgroundPressedBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinMenuItemForegroundPressedBrush", "#FFFFFFFF", "#FF000000", None, None),
    ("WinMenuItemForegroundHoverBrush", "#FF000000", "#FF000000", None, None),
    ("WinMenuItemForegroundDisabledBrush", "#1A000000", "#1AFFFFFF", None, None),
]),
("ScrollBar", [
    ("WinScrollBarThumbBrush", "#FF7B7B7B", "#FF7B7B7B", None, None),
    ("WinScrollBarThumbHoverBrush", "#99000000", "#99FFFFFF", None, None),
    ("WinScrollBarThumbPressedBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinScrollBarTrackBrush", "#FFF0F0F0", "#FF000000", None, None),
]),
("Suggestion flyouts and spinners", [
    ("WinSuggestionFlyoutBackgroundBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinSuggestionFlyoutBorderBrush", "#FF2A2A2A", "#FF2A2A2A", None, None),
    ("WinSuggestionForegroundBrush", "#FF000000", "#FF000000", None, None),
    ("WinSuggestionBackgroundHoverBrush", "#FFE5E5E5", "#FFE5E5E5", None, None),
    ("WinSpinnerButtonBackgroundHoverBrush", "#21000000", "#21000000", None, None),
    ("WinSpinnerButtonBackgroundPressedBrush", "#FF000000", "#FF000000", None, None),
    ("WinSpinnerButtonForegroundPressedBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
]),
("AppBar / commands", [
    ("WinCommandForegroundBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinCommandForegroundPressedBrush", "#FFFFFFFF", "#FF000000", None, None),
    ("WinCommandForegroundDisabledBrush", "#66000000", "#66FFFFFF", None, None),
    ("WinCommandForegroundSelectedBrush", "#FFFFFFFF", "#FF000000", None, None),
    ("WinCommandForegroundSelectedPressedBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinCommandRingBackgroundBrush", "#00000000", "#00000000", None, None),
    ("WinCommandRingBorderBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinCommandRingBackgroundHoverBrush", "#21000000", "#21FFFFFF", None, None),
    ("WinCommandRingBackgroundPressedBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinCommandRingBorderDisabledBrush", "#66000000", "#66FFFFFF", None, None),
    ("WinCommandRingBackgroundSelectedBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinCommandRingBackgroundSelectedHoverBrush", "#FF212121", "#FFDEDEDE", None, None),
    ("WinCommandRingBackgroundSelectedDisabledBrush", "#66000000", "#66FFFFFF", None, None),
    ("WinCommandLabelForegroundDisabledBrush", "#66000000", "#66FFFFFF", None, None),
    ("WinAppBarInvokeButtonBackgroundHoverBrush", "#D1CDCDCD", "#21FFFFFF", None, None),
    ("WinBackButtonBackgroundHoverBrush", "#21000000", "#21FFFFFF", None, None),
    ("WinBackButtonBackgroundPressedBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinBackButtonForegroundPressedBrush", "#FFFFFFFF", "#FF000000", None, None),
    ("WinSettingsFlyoutBackgroundBrush", "#FFFFFFFF", "#FF000000", None, None),
    ("WinSettingsFlyoutBorderBrush", "#3D000000", "#3DFFFFFF", None, None),
    ("WinNotificationBorderBrush", "#FF000000", "#FFFFFFFF", None, None),
]),
("ContentDialog (the panel, and the backdrop drawn at 60% opacity)", [
    ("WinContentDialogBackgroundBrush", "#FFF2F2F2", "#FF2B2B2B", None, None),
    ("WinContentDialogBackdropBrush", "#FFFFFFFF", "#FF000000", None, None),
]),
("NavBar (also used by PipsPager)", [
    ("WinNavPageIndicatorBrush", "#4D000000", "#66FFFFFF", None, None),
    ("WinNavPageIndicatorCurrentBrush", "#A8000000", "#CCFFFFFF", None, None),
    ("WinNavPageIndicatorHoverBrush", "#70000000", "#99FFFFFF", None, None),
    ("WinNavPageIndicatorPressedBrush", "#94000000", "#B3FFFFFF", None, None),
    ("WinNavArrowBackgroundBrush", "#FFE7E7E7", "#FF4A4A4A", None, None),
    ("WinNavArrowForegroundBrush", "#FF5C5C5C", "#FF1E1E1E", None, None),
    ("WinNavArrowBackgroundHoverBrush", "#FFDADADA", "#FFCACACA", None, None),
    ("WinNavArrowForegroundHoverBrush", "#FF000000", "#FF000000", None, None),
    ("WinNavArrowBackgroundPressedBrush", "#FF5D5D5D", "#FF1E1E1E", None, None),
    ("WinNavArrowForegroundPressedBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinNavCommandBackgroundBrush", "#17000000", "#1CFFFFFF", None, None),
    ("WinNavCommandBackgroundHoverBrush", "#4FC7C7C7", "#38FFFFFF", None, None),
    ("WinNavCommandBackgroundPressedBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinNavCommandForegroundPressedBrush", "#FFFFFFFF", "#FF000000", None, None),
    ("WinHubHeaderHoverBrush", "#CC000000", "#CCFFFFFF", None, None),
    ("WinFlipViewNavButtonBackgroundBrush", "#59D5D5D5", "#59D5D5D5", None, None),
    ("WinFlipViewNavButtonForegroundBrush", "#99000000", "#99000000", None, None),
    ("WinFlipViewNavButtonBackgroundHoverBrush", "#F0D7D7D7", "#F0D7D7D7", None, None),
    ("WinFlipViewNavButtonForegroundHoverBrush", "#FF000000", "#FF000000", None, None),
    ("WinFlipViewNavButtonBackgroundPressedBrush", "#BD292929", "#BD292929", None, None),
    ("WinFlipViewNavButtonForegroundPressedBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinSemanticZoomButtonBackgroundBrush", "#54D8D8D8", "#54D8D8D8", None, None),
    ("WinSemanticZoomButtonBackgroundHoverBrush", "#FFD8D8D8", "#FFD8D8D8", None, None),
    ("WinSemanticZoomButtonBackgroundPressedBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinSemanticZoomButtonForegroundBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinSemanticZoomButtonForegroundPressedBrush", "#FFFFFFFF", "#FF000000", None, None),
    ("WinSemanticZoomZoomedOutBackgroundBrush", "#00000000", "#00000000", "#ABFFFFFF", "#AB000000"),
    ("WinRatingUserBrush", ACCENT, ONDARK, None, None),
    ("WinRatingTentativeBrush", L2, L3, None, None),
    ("WinRatingAverageBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinRatingEmptyBrush", "#59000000", "#59FFFFFF", None, None),
    ("WinSearchBoxBorderBrush", "#45000000", "#CCFFFFFF", None, None),
    ("WinSearchBoxBackgroundBrush", "#CCFFFFFF", "#CCFFFFFF", None, None),
    ("WinSearchBoxForegroundBrush", "#99000000", "#99000000", None, None),
    ("WinSearchBoxBackgroundHoverBrush", "#DEFFFFFF", "#DEFFFFFF", None, None),
    ("WinSearchBoxBorderHoverBrush", "#45000000", "#DEFFFFFF", None, None),
    ("WinSearchBoxBorderFocusedBrush", "#FF2A2A2A", "#FF2A2A2A", None, None),
    ("WinSearchBoxBackgroundFocusedBrush", "#CCFFFFFF", "#FFFFFFFF", None, None),
    ("WinSearchBoxButtonForegroundBrush", "#99000000", "#99000000", None, None),
    ("WinSearchBoxButtonBackgroundHoverBrush", L1, L1, None, None),
    ("WinSearchBoxButtonBackgroundPressedBrush", "#FF000000", "#FF000000", None, None),
    ("WinSearchBoxButtonForegroundPressedBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinSearchBoxButtonBackgroundFocusedBrush", ACCENT, ACCENT, None, None),
    ("WinSearchBoxButtonForegroundFocusedBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinSearchBoxBackgroundDisabledBrush", "#66CACACA", "#00000000", None, None),
    ("WinSearchBoxBorderDisabledBrush", "#26000000", "#66FFFFFF", None, None),
    ("WinSearchBoxForegroundDisabledBrush", "#38000000", "#66FFFFFF", None, None),
    ("WinSearchBoxFlyoutBackgroundBrush", "#FFFFFFFF", "#FFFFFFFF", None, None),
    ("WinSearchBoxFlyoutBorderBrush", "#FF2A2A2A", "#FF2A2A2A", None, None),
    ("WinSearchBoxSuggestionForegroundBrush", "#FF000000", "#FF000000", None, None),
    ("WinSearchBoxSuggestionBackgroundHoverBrush", "#FFE5E5E5", "#FFE5E5E5", None, None),
    ("WinSearchBoxSuggestionBackgroundSelectedBrush", ACCENT, ACCENT, None, None),
    ("WinSearchBoxSuggestionBackgroundSelectedHoverBrush", L1, L1, None, None),
    ("WinSearchBoxSuggestionSeparatorBrush", "#FF7A7A7A", "#FF7A7A7A", None, None),
    ("WinSearchBoxHighlightBrush", ACCENT, ACCENT, None, None),
    ("WinSearchBoxHighlightSelectedBrush", "#FFA38BDA", "#FFA38BDA", None, None),
    ("WinHubHeaderPressedBrush", "#66000000", "#66FFFFFF", None, None),
    ("WinPivotNavButtonBackgroundBrush", "#59D5D5D5", "#4CD5D5D5", None, None),
    ("WinPivotNavButtonForegroundBrush", "#99000000", "#99FFFFFF", None, None),
    ("WinPivotNavButtonBackgroundHoverBrush", "#F0D7D7D7", "#EF313131", None, None),
    ("WinPivotNavButtonForegroundHoverBrush", "#FF000000", "#FFFFFFFF", None, None),
    ("WinPivotNavButtonBackgroundPressedBrush", "#BD292929", "#BFBFBFBF", None, None),
    ("WinPivotNavButtonForegroundPressedBrush", "#FFFFFFFF", "#FF000000", None, None),
]),
("Utility", [
    ("WinTransparentBrush", "#00000000", "#00000000", None, None),
]),
("Status", [
    ("WinStatusInfoBrush", ACCENT, ACCENT, None, None),
    ("WinStatusSuccessBrush", "#FF128B44", "#FF128B44", None, None),
    ("WinStatusWarningBrush", "#FFFFC316", "#FFFFC316", None, None),
    ("WinStatusErrorBrush", "#FFE81123", "#FFE81123", None, None),
    ("WinWindowCloseHoverBrush", "#FFE81123", "#FFE81123", None, None),
    ("WinWindowClosePressedBrush", "#FFF1707A", "#FFF1707A", None, None),
    ("WinCaptionButtonHoverBrush", "#21000000", "#21FFFFFF", None, None),
    ("WinCaptionButtonPressedBrush", "#4D000000", "#4DFFFFFF", None, None),
]),
]

# Geometry: doubles, thicknesses and more. (key, type, desktop, phone-or-None)
GEOMETRY = [
    ("WinControlBorderThickness", "Thickness", "2", None),
    ("WinInputBorderThickness", "Thickness", "2", "3"),
    ("WinControlCornerRadius", "CornerRadius", "0", None),
    ("WinButtonPadding", "Thickness", "8,4", "6,0"),
    ("WinButtonMinHeight", "x:Double", "32", "39"),
    ("WinButtonMinWidth", "x:Double", "90", "108"),
    ("WinTextBoxPadding", "Thickness", "8,0", "8,10"),
    ("WinTextBoxMinHeight", "x:Double", "28", "38"),
    ("WinTextBoxMinWidth", "x:Double", "64", None),
    ("WinTextAreaPadding", "Thickness", "8,4,17,4", "8,4,17,10"),
    ("WinTextAreaMinHeight", "x:Double", "39", "53"),
    ("WinCheckBoxSize", "x:Double", "21", "26"),
    ("WinRadioButtonSize", "x:Double", "23", "26"),
    ("WinRadioButtonDotSize", "x:Double", "9", "11"),
    ("WinCheckBoxContentMargin", "Thickness", "5,0,0,0", "8,0,0,0"),
    ("WinComboBoxMinHeight", "x:Double", "32", "38"),
    ("WinComboBoxMinWidth", "x:Double", "80", None),
    ("WinComboBoxPadding", "Thickness", "8,4", "8,3,8,4"),
    ("WinComboBoxChevronPadding", "Thickness", "6,0", None),
    ("WinComboBoxChevronVisible", "x:Boolean", "True", "False"),
    ("WinProgressBarWidth", "x:Double", "180", "207"),
    ("WinProgressBarWidthMedium", "x:Double", "280", "207"),
    ("WinProgressBarHeight", "x:Double", "6", None),
    ("WinProgressBarIndeterminateHeight", "x:Double", "4", None),
    ("WinProgressRingSize", "x:Double", "20", None),
    ("WinProgressRingSizeMedium", "x:Double", "40", None),
    ("WinProgressRingSizeLarge", "x:Double", "60", None),
    ("WinSliderWidth", "x:Double", "280", "0"),
    ("WinSliderHeightVertical", "x:Double", "191", "272"),
    ("WinSliderTrackThickness", "x:Double", "11", "10"),
    ("WinSliderThumbWidth", "x:Double", "11", "11"),
    ("WinSliderThumbHeight", "x:Double", "11", "18"),
    ("WinSliderThumbWidthVertical", "x:Double", "11", "24"),
    ("WinSliderThumbHeightVertical", "x:Double", "11", "12"),
    ("WinToolTipPadding", "Thickness", "10,6,10,7", None),
    ("WinToolTipMaxWidth", "x:Double", "380", None),
    ("WinFlyoutPadding", "Thickness", "20,25,20,20", None),
    ("WinFlyoutMinWidth", "x:Double", "26", None),
    ("WinFlyoutMaxWidth", "x:Double", "466", None),
    ("WinFlyoutOffset", "x:Double", "5", None),
    ("WinMenuPadding", "Thickness", "0,5", None),
    ("WinMenuItemPadding", "Thickness", "20,10,20,12", None),
    ("WinMenuItemMinHeight", "x:Double", "40", None),
    ("WinMenuMinHeight", "x:Double", "38", None),
    ("WinMenuSeparatorMargin", "Thickness", "20,9,20,10", None),
    ("WinFocusOutlineThickness", "Thickness", "1", None),
    ("WinListFocusOutlineThickness", "Thickness", "2", None),
    ("WinListItemHoverOutlineThickness", "Thickness", "3", None),
    ("WinListSelectionBorderThickness", "Thickness", "4", None),
    ("WinListItemPadding", "Thickness", "10,7", None),
    ("WinListItemMinHeight", "x:Double", "40", None),
    ("WinListBoxPadding", "Thickness", "0,0,24,0", "0"),
    ("WinScrollBarThickness", "x:Double", "3", None),
    ("WinScrollBarExpandedThickness", "x:Double", "12", "3"),
    ("WinScrollBarButtonSize", "x:Double", "17", "0"),
    ("WinZeroThickness", "Thickness", "0", None),
    ("WinCheckBoxMinHeight", "x:Double", "21", "26"),
    ("WinRadioButtonMinHeight", "x:Double", "23", "26"),
    ("WinToggleTrackWidth", "x:Double", "50", None),
    ("WinToggleTrackHeight", "x:Double", "19", None),
    ("WinToggleThumbWidth", "x:Double", "12", None),
    ("WinToggleThumbHeight", "x:Double", "19", None),
    ("WinToggleFillWidth", "x:Double", "35", None),
    ("WinToggleFillHeight", "x:Double", "13", None),
    ("WinToggleKnobTravel", "x:Double", "38", None),
    ("WinToggleHeaderMaxWidth", "x:Double", "470", None),
    ("WinToggleHeaderMargin", "Thickness", "0,10,0,7", None),
    ("WinToggleValueMinWidth", "x:Double", "65", None),
    ("WinToggleValueMargin", "Thickness", "0,0,20,0", None),
    ("WinSliderHorizontalHeight", "x:Double", "11", "18"),
    ("WinSliderVerticalWidth", "x:Double", "11", "24"),
    ("WinProgressRingDotSize", "x:Double", "3", None),
    ("WinProgressRingDotSizeMedium", "x:Double", "5", None),
    ("WinProgressRingDotSizeLarge", "x:Double", "7", None),
    ("WinSuggestionListMaxHeight", "x:Double", "272", None),
    ("WinSuggestionListPadding", "Thickness", "0,5,0,10", None),
    ("WinSuggestionItemPadding", "Thickness", "18,9,18,11", None),
    ("WinSuggestionItemMinHeight", "x:Double", "40", None),
    ("WinComboBoxItemPadding", "Thickness", "8,4", None),
    ("WinIconSize", "x:Double", "20", None),
    ("WinMenuBarItemPadding", "Thickness", "12,6", None),
    ("WinMenuBarHeight", "x:Double", "40", None),
    ("WinMenuSeparatorHeight", "x:Double", "1", None),
    ("WinFlyoutMaxHeight", "x:Double", "758", None),
    ("WinCaptionButtonWidth", "x:Double", "46", None),
    ("WinCaptionButtonHeight", "x:Double", "32", None),
    ("WinScrollBarGlyphFontSize", "x:Double", "10.5", None),
    ("WinHorizontalMenuMinWidth", "x:Double", "32", None),
    ("WinHorizontalMenuItemPadding", "Thickness", "12,6", None),
    ("WinHorizontalMenuItemMargin", "Thickness", "0", None),
    ("WinMenuScrollerMargin", "Thickness", "0", None),
    ("WinMenuChevronMargin", "Thickness", "12,0,0,0", None),
    ("WinScrollThumbCollapsedTransformVertical", "TransformOperations", "scaleX(0.25)", "none"),
    ("WinScrollThumbCollapsedTransformHorizontal", "TransformOperations", "scaleY(0.25)", "none"),
    ("WinPivotHeaderUnselectedOpacityValue", "x:Double", "0.2", None),
    ("WinPivotHeaderUnselectedOpacityDarkValue", "x:Double", "0.4", None),
    ("WinTabItemPadding", "Thickness", "9,0", None),
    ("WinTabItemMargin", "Thickness", "0", None),
    ("WinGroupBoxPadding", "Thickness", "8", None),
    ("WinSplitViewOpenPaneLength", "x:Double", "345", None),
    ("WinSplitViewCompactPaneLength", "x:Double", "48", None),
    ("WinTreeViewItemMinHeight", "x:Double", "40", None),
    ("WinRefreshIndicatorSize", "x:Double", "20", None),
    ("WinCalendarDayButtonSize", "x:Double", "40", None),
    ("WinAppBarMinHeight", "x:Double", "80", None),
    ("WinAppBarReducedHeight", "x:Double", "60", None),
    ("WinAppBarMinimalHeight", "x:Double", "25", None),
    ("WinAppBarEllipsisWidth", "x:Double", "56", None),
    ("WinAppBarEllipsisHeight", "x:Double", "21", None),
    ("WinAppBarEllipsisFontSize", "x:Double", "19", None),
    ("WinCommandRingSize", "x:Double", "40", None),
    ("WinCommandRingBorderThickness", "Thickness", "2", None),
    ("WinCommandRingCornerRadius", "CornerRadius", "20", None),
    ("WinCommandWidth", "x:Double", "100", None),
    ("WinCommandPadding", "Thickness", "0,8", None),
    ("WinCommandIconMargin", "Thickness", "28,0", None),
    ("WinCommandIconMarginReduced", "Thickness", "8,0", None),
    ("WinCommandLabelMaxWidth", "x:Double", "88", None),
    ("WinCommandLabelMargin", "Thickness", "0,5,0,-1", None),
    ("WinCommandGlyphFontSize", "x:Double", "18.5", None),
    ("WinCommandLabelFontSize", "x:Double", "12", None),
    ("WinCommandSeparatorMargin", "Thickness", "30,10,29,30", None),
    ("WinCommandSeparatorMarginReduced", "Thickness", "10,10,9,10", None),
    ("WinCommandContentPadding", "Thickness", "30,8", None),
    ("WinCommandContentPaddingReduced", "Thickness", "10,8", None),
    ("WinSettingsFlyoutNarrowWidth", "x:Double", "345", None),
    ("WinSettingsFlyoutWideWidth", "x:Double", "645", None),
    ("WinSettingsFlyoutHeaderPadding", "Thickness", "40,32,40,0", None),
    ("WinSettingsFlyoutHeaderHeight", "x:Double", "48", None),
    ("WinSettingsFlyoutTitlePadding", "Thickness", "40,0,0,0", None),
    ("WinSettingsFlyoutContentPadding", "Thickness", "40,33,40,0", None),
    ("WinBackButtonSize", "x:Double", "41", None),
    ("WinBackButtonSizeSmall", "x:Double", "30", None),
    ("WinBackButtonCornerRadius", "CornerRadius", "20.5", None),
    ("WinBackButtonCornerRadiusSmall", "CornerRadius", "15", None),
    ("WinBackButtonGlyphFontSize", "x:Double", "18.5", None),
    ("WinBackButtonGlyphFontSizeSmall", "x:Double", "10.5", None),
    ("WinNavigationBarHeight", "x:Double", "48", None),
    ("WinNavBarMinHeight", "x:Double", "60", None),
    ("WinNavBarCommandWidth", "x:Double", "210", None),
    ("WinNavBarCommandMargin", "Thickness", "5", None),
    ("WinNavBarCommandMarginVertical", "Thickness", "30,10", None),
    ("WinNavBarCommandPadding", "Thickness", "10,5", None),
    ("WinNavBarCommandIconSize", "x:Double", "40", None),
    ("WinNavBarCommandIconFontSize", "x:Double", "26.5", None),
    ("WinNavBarCommandLabelFontSize", "x:Double", "14.5", None),
    ("WinNavBarSplitButtonWidth", "x:Double", "40", None),
    ("WinNavBarSplitButtonFontSize", "x:Double", "14.5", None),
    ("WinNavBarArrowWidth", "x:Double", "17", None),
    ("WinNavBarArrowMargin", "Thickness", "0,20", None),
    ("WinNavBarArrowFontSize", "x:Double", "21.5", None),
    ("WinNavBarViewportPadding", "Thickness", "0,15", None),
    ("WinNavBarViewportPaddingVertical", "Thickness", "0,10", None),
    ("WinNavBarVerticalMaxHeight", "x:Double", "250", None),
    ("WinNavBarPageIndicatorWidth", "x:Double", "40", None),
    ("WinNavBarPageIndicatorHeight", "x:Double", "4", None),
    ("WinNavBarPageIndicatorMargin", "Thickness", "2.5,5,2.5,11", None),
    ("WinListViewItemMarginHorizontal", "Thickness", "5,10,5,0", None),
    ("WinListViewItemMarginVertical", "Thickness", "7,10,24,0", None),
    ("WinListViewItemMarginGrid", "Thickness", "5", None),
    ("WinListViewGroupLeaderMargin", "x:Double", "70", None),
    ("WinListViewGroupHeaderPadding", "Thickness", "2,10,10,10", None),
    ("WinListViewCheckmarkSize", "x:Double", "40", None),
    ("WinListViewCheckmarkFontSize", "x:Double", "14.67", None),
    ("WinListViewCheckmarkPadding", "Thickness", "2", None),
    ("WinListViewPhoneSelectionOffset", "x:Double", "41", None),
    ("WinListViewPhoneCheckSize", "x:Double", "26", None),
    ("WinListViewPhoneCheckBorderThickness", "Thickness", "2.5", None),
    ("WinListViewPhoneCheckMargin", "Thickness", "-26,12,0,0", None),
    ("WinListViewProgressSize", "x:Double", "60", None),
    ("WinFlipViewNavButtonWidth", "x:Double", "69", None),
    ("WinFlipViewNavButtonHeight", "x:Double", "39", None),
    ("WinFlipViewNavButtonFontSize", "x:Double", "21.5", None),
    ("WinFlipViewDefaultHeight", "x:Double", "400", None),
    ("WinSemanticZoomButtonSize", "x:Double", "25", None),
    ("WinSemanticZoomButtonMargin", "Thickness", "0,0,4,21", None),
    ("WinSemanticZoomButtonFontSize", "x:Double", "14.5", None),
    ("WinSemanticZoomDefaultHeight", "x:Double", "400", None),
    ("WinRatingStarSize", "x:Double", "28", None),
    ("WinRatingStarWidth", "x:Double", "40", None),
    ("WinRatingStarPadding", "Thickness", "6,0", None),
    ("WinRatingStarSizeSmall", "x:Double", "14", None),
    ("WinRatingStarWidthSmall", "x:Double", "20", None),
    ("WinRatingStarPaddingSmall", "Thickness", "3,0", None),
    ("WinSearchBoxWidth", "x:Double", "266", None),
    ("WinSearchBoxHeight", "x:Double", "28", None),
    ("WinSearchBoxButtonWidth", "x:Double", "28", None),
    ("WinSearchBoxButtonFontSize", "x:Double", "20", None),
    ("WinSearchBoxInputPadding", "Thickness", "6,0,28,0", None),
    ("WinSearchBoxFlyoutPadding", "Thickness", "0,5,0,10", None),
    ("WinSearchBoxFlyoutMaxHeight", "x:Double", "272", None),
    ("WinSearchBoxFlyoutOffset", "x:Double", "2", None),
    ("WinSearchBoxQueryPadding", "Thickness", "18,9,18,11", None),
    ("WinSearchBoxResultHeight", "x:Double", "60", None),
    ("WinSearchBoxResultImagePadding", "Thickness", "0,10,10,10", None),
    ("WinSearchBoxResultImageSize", "x:Double", "40", None),
    ("WinSearchBoxSeparatorPadding", "Thickness", "18,0", None),
    ("WinSearchBoxSeparatorHeight", "x:Double", "40", None),
    ("WinHubSectionPadding", "Thickness", "40,0", None),
    ("WinHubSectionPaddingVertical", "Thickness", "40,14", None),
    ("WinHubSurfacePadding", "Thickness", "80,0", None),
    ("WinHubSurfacePaddingVertical", "Thickness", "0,15", None),
    ("WinHubHeaderMargin", "Thickness", "0,133,0,4", None),
    ("WinHubHeaderMarginVertical", "Thickness", "0,5,0,4", None),
    ("WinHubHeaderFontSize", "x:Double", "26.5", None),
    ("WinHubHeaderPadding", "Thickness", "0,0,3,0", None),
    ("WinHubChevronMargin", "Thickness", "7,0,0,0", None),
    ("WinHubChevronFontSize", "x:Double", "20", None),
    ("WinHubProgressTop", "Thickness", "0,10,0,0", None),
    ("WinPivotHeadersHeight", "x:Double", "72", None),
    ("WinPivotHeaderFontSize", "x:Double", "60", None),
    ("WinPivotHeaderPadding", "Thickness", "9,0", None),
    ("WinPivotTitleMargin", "Thickness", "18,12,0,-8", None),
    ("WinPivotTitleFontSize", "x:Double", "18.5", None),
    ("WinPivotItemPadding", "Thickness", "19,0", None),
    ("WinPivotItemTopOffset", "Thickness", "0,28,0,0", None),
    ("WinPivotNavButtonWidth", "x:Double", "18", None),
    ("WinPivotNavButtonHeight", "x:Double", "40", None),
    ("WinPivotNavButtonMargin", "Thickness", "0,30,0,0", None),
    ("WinPivotNavButtonFontSize", "x:Double", "21.5", None),
    ("WinDatePickerMinWidth", "x:Double", "296", None),
    ("WinOpaque", "x:Double", "1", None),
    ("WinZeroDouble", "x:Double", "0", None),
    ("WinZeroGridLength", "GridLength", "0", None),
    ("WinControlBorderWidth", "x:Double", "2", None),
    ("WinIsPhone", "x:Boolean", "False", "True"),
    ("WinIsDesktop", "x:Boolean", "True", "False"),
    ("WinPressedScale", "x:Double", "0.975", None),
    ("WinPointerDownDuration", "x:TimeSpan", "0:0:0.167", None),
    ("WinShortDuration", "x:TimeSpan", "0:0:0.1", None),
]

# Type ramp: (name, desktop_size, desktop_weight, phone_size, phone_weight, lineheight_ratio)
TYPE_RAMP = [
    ("XXLarge", 56, "Light", 37.5, "Light", 1.1429),
    ("XLarge", 26.5, "Light", 32, "Light", 1.2),
    ("Large", 14.5, "SemiBold", 22.5, "Normal", 1.3636),
    ("Medium", 14.5, "Normal", 20, "Normal", 1.3636),
    ("Small", 14.5, "SemiLight", 13.5, "SemiBold", 1.3636),
    ("XSmall", 14.5, "SemiLight", 13.5, "SemiBold", 1.3636),
    ("XXSmall", 12, "Normal", 13.5, "SemiBold", 1.6667),
]
# Named sizes: (name, desktop_size, desktop_weight, phone_size, phone_weight)
NAMED_TYPE = [
    ("Body", 14.5, "SemiLight", 13.5, "SemiBold"),
    ("Button", 14.5, "SemiBold", 18.5, "SemiBold"),
    ("Input", 14.5, "Normal", 21.5, "Normal"),
    ("Select", 14.5, "Normal", 20, "Normal"),
    ("CommandLabel", 12, "Normal", 12, "Normal"),
    ("CommandGlyph", 18.5, "Normal", 18.5, "Normal"),
    ("Header", 26.5, "Light", 32, "Light"),
    ("PivotHeader", 60, "Normal", 60, "Normal"),
    ("PivotTitle", 18.5, "SemiBold", 18.5, "SemiBold"),
    ("NavGlyph", 21.5, "Normal", 21.5, "Normal"),
    ("Tooltip", 12, "Normal", 13.5, "SemiBold"),
    ("Caption", 12, "Normal", 13.5, "SemiBold"),
    ("ToggleHeader", 16, "SemiLight", 16, "SemiLight"),
    ("ListGroupHeader", 26.5, "Light", 32, "Light"),
]

FONTS = [
    ("WinFontFamily", "fonts:AvaWin#Selawik, Segoe UI, Segoe WP, sans-serif"),
    ("WinSymbolFontFamily", "fonts:AvaWin#Symbols, Segoe UI Symbol, Segoe MDL2 Assets"),
    ("WinMonospaceFontFamily", "Consolas, Menlo, monospace"),
]


def fmt(v):
    return ("%g" % v) if isinstance(v, (int, float)) else v


def brush_xml(key, value, indent):
    pad = " " * indent
    if isinstance(value, tuple):
        ref, opacity = value
        return f'{pad}<SolidColorBrush x:Key="{key}" Color="{{DynamicResource {ref}}}" Opacity="{opacity}" />'
    if value.startswith("#"):
        return f'{pad}<SolidColorBrush x:Key="{key}" Color="{value}" />'
    return f'{pad}<SolidColorBrush x:Key="{key}" Color="{{DynamicResource {value}}}" />'


def resolve_pair(light, dark):
    """Map a literal pair onto a palette key when one matches exactly."""
    if isinstance(light, str) and isinstance(dark, str) and light.startswith("#") and dark.startswith("#"):
        k = PALETTE_BY_PAIR.get((light, dark))
        if k:
            return k, k
    return light, dark


def value_xml(key, typ, value, indent):
    pad = " " * indent
    return f'{pad}<{typ} x:Key="{key}">{value}</{typ}>'


def emit_base():
    o = []
    o.append("<!-- Layer 2: the semantic brushes, geometry, fonts and type ramp, with the Desktop values.")
    o.append("     GENERATED by tools/gen-tokens.py. Do not edit this file. Edit the script and run it again.")
    o.append("     The Phone overrides live in PhoneMetrics.xaml. Every color key is defined in BOTH variants. -->")
    o.append('<ResourceDictionary xmlns="https://github.com/avaloniaui"')
    o.append('                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">')
    o.append("  <ResourceDictionary.ThemeDictionaries>")
    for variant, idx in (("Default", 1), ("Dark", 2)):
        o.append(f'    <ResourceDictionary x:Key="{variant}">')
        for section, rows in BRUSHES:
            o.append(f"      <!-- {section} -->")
            for row in rows:
                key = row[0]
                light, dark = resolve_pair(row[1], row[2])
                o.append(brush_xml(key, light if idx == 1 else dark, 6))
        o.append("    </ResourceDictionary>")
    o.append("  </ResourceDictionary.ThemeDictionaries>")
    o.append("")
    o.append("  <!-- Fonts -->")
    for k, v in FONTS:
        o.append(f'  <FontFamily x:Key="{k}">{v}</FontFamily>')
    o.append("")
    o.append("  <!-- The type ramp. 1pt = 1.3333 DIP -->")
    for name, size, weight, _, _, ratio in TYPE_RAMP:
        o.append(value_xml(f"WinFontSize{name}", "x:Double", fmt(size), 2))
        o.append(value_xml(f"WinFontWeight{name}", "FontWeight", weight, 2))
        o.append(value_xml(f"WinLineHeight{name}", "x:Double", fmt(round(size * ratio * 2) / 2), 2))
    for name, size, weight, _, _ in NAMED_TYPE:
        o.append(value_xml(f"WinFontSize{name}", "x:Double", fmt(size), 2))
        o.append(value_xml(f"WinFontWeight{name}", "FontWeight", weight, 2))
    o.append("")
    o.append("  <!-- Geometry -->")
    for key, typ, desktop, _ in GEOMETRY:
        o.append(value_xml(key, typ, desktop, 2))
    o.append("</ResourceDictionary>")
    o.append("")
    return "\n".join(o)


def emit_phone():
    o = []
    o.append("<!-- The Phone overlay: the same keys as BaseResources.xaml, with the phone values.")
    o.append("     GENERATED by tools/gen-tokens.py. AvaWinTheme searches it first when Platform is Phone. -->")
    o.append('<ResourceDictionary xmlns="https://github.com/avaloniaui"')
    o.append('                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">')
    o.append("  <ResourceDictionary.ThemeDictionaries>")
    for variant, idx in (("Default", 3), ("Dark", 4)):
        o.append(f'    <ResourceDictionary x:Key="{variant}">')
        for section, rows in BRUSHES:
            emitted = False
            for row in rows:
                key = row[0]
                pl, pd = row[3], row[4]
                if pl is None and pd is None:
                    continue
                light = pl if pl is not None else row[1]
                dark = pd if pd is not None else row[2]
                light, dark = resolve_pair(light, dark)
                if not emitted:
                    o.append(f"      <!-- {section} -->")
                    emitted = True
                o.append(brush_xml(key, light if idx == 3 else dark, 6))
        o.append("    </ResourceDictionary>")
    o.append("  </ResourceDictionary.ThemeDictionaries>")
    o.append("")
    o.append("  <!-- Type ramp (phone) -->")
    for name, _, _, size, weight, ratio in TYPE_RAMP:
        o.append(value_xml(f"WinFontSize{name}", "x:Double", fmt(size), 2))
        o.append(value_xml(f"WinFontWeight{name}", "FontWeight", weight, 2))
        o.append(value_xml(f"WinLineHeight{name}", "x:Double", fmt(round(size * ratio * 2) / 2), 2))
    for name, _, _, size, weight in NAMED_TYPE:
        o.append(value_xml(f"WinFontSize{name}", "x:Double", fmt(size), 2))
        o.append(value_xml(f"WinFontWeight{name}", "FontWeight", weight, 2))
    o.append("")
    o.append("  <!-- Geometry (phone) -->")
    for key, typ, _, phone in GEOMETRY:
        if phone is not None:
            o.append(value_xml(key, typ, phone, 2))
    o.append("</ResourceDictionary>")
    o.append("")
    return "\n".join(o)


def main():
    keys = [r[0] for _, rows in BRUSHES for r in rows]
    assert len(keys) == len(set(keys)), "duplicate brush key"
    with open(os.path.join(OUT, "BaseResources.xaml"), "w", encoding="utf-8") as f:
        f.write(emit_base())
    with open(os.path.join(OUT, "PhoneMetrics.xaml"), "w", encoding="utf-8") as f:
        f.write(emit_phone())
    print(f"{len(keys)} brushes, {len(GEOMETRY)} geometry keys")


if __name__ == "__main__":
    main()
