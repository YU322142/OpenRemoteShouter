namespace RemoteShouter.Services;

public static class WebUiHtml
{
    public static string Build(string nonce)
    {
        return $$"""
<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>OpenRemoteShouter</title>
  <style nonce="{{nonce}}">
    :root {
      color-scheme: light;
      font-family: "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", system-ui, sans-serif;
      background: #f5f7fb;
      color: #1f2937;
      --line: #d7dde8;
      --muted: #667085;
      --panel: #ffffff;
      --panel-soft: #f8fafc;
      --text: #1f2937;
      --teal: #087f8c;
      --teal-dark: #05616b;
      --blue: #2563eb;
      --green: #15803d;
      --amber: #b45309;
      --rose: #be123c;
      --violet: #6d28d9;
      --indigo: #4f46e5;
      --magenta: #c026d3;
      --orange: #ea580c;
      --emerald: #047857;
      --cyan-dark: #0e7490;
      --blue-dark: #1e40af;
      --green-dark: #047857;
      --amber-dark: #b45309;
      --rose-dark: #be123c;
      --violet-dark: #6d28d9;
      --indigo-dark: #4f46e5;
      --magenta-dark: #c026d3;
      --orange-dark: #ea580c;
      --emerald-dark: #047857;
      --danger: #b42318;
      --shadow: 0 18px 44px rgba(15, 23, 42, .08);
    }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      min-height: 100vh;
      background:
        linear-gradient(180deg, #eef4f8 0, #f5f7fb 360px),
        #f5f7fb;
      /* Fluent UI v3 light theme tokens.  Keep the send button's custom
         gradient below, while every other control uses the Fluent palette. */
      --colorBrandBackground: #0f6cbd;
      --colorBrandBackgroundHover: #115ea3;
      --colorBrandBackgroundPressed: #0c3b5e;
      --colorBrandBackgroundSelected: #0f548c;
      --colorBrandForeground1: #0f6cbd;
      --colorBrandForeground2: #115ea3;
      --colorBrandForeground2Hover: #0f548c;
      --colorBrandForeground2Pressed: #0a2e4a;
      --colorCompoundBrandBackground: #0f6cbd;
      --colorCompoundBrandBackgroundHover: #115ea3;
      --colorCompoundBrandBackgroundPressed: #0f548c;
      --colorCompoundBrandForeground1: #0f6cbd;
      --colorCompoundBrandForeground1Hover: #115ea3;
      --colorCompoundBrandForeground1Pressed: #0f548c;
      --colorCompoundBrandStroke: #0f6cbd;
      --colorCompoundBrandStrokeHover: #115ea3;
      --colorCompoundBrandStrokePressed: #0f548c;
      --colorNeutralForegroundOnBrand: #ffffff;
      --colorNeutralForeground1: #242424;
      --colorNeutralForeground1Hover: #242424;
      --colorNeutralForeground1Pressed: #242424;
      --colorNeutralForeground2: #424242;
      --colorNeutralForeground2Hover: #242424;
      --colorNeutralForeground2Pressed: #242424;
      --colorNeutralForeground3: #616161;
      --colorNeutralForeground3Hover: #424242;
      --colorNeutralForeground4: #707070;
      --colorNeutralForegroundDisabled: #bdbdbd;
      --colorNeutralForegroundInverted: #ffffff;
      --colorNeutralForegroundStaticInverted: #ffffff;
      --colorNeutralBackground1: #ffffff;
      --colorNeutralBackground1Hover: #f5f5f5;
      --colorNeutralBackground1Pressed: #e0e0e0;
      --colorNeutralBackground2: #fafafa;
      --colorNeutralBackground3: #f5f5f5;
      --colorNeutralBackground4: #f0f0f0;
      --colorNeutralBackgroundDisabled: #f0f0f0;
      --colorNeutralBackgroundInverted: #292929;
      --colorNeutralCardBackground: #fafafa;
      --colorNeutralStrokeAccessible: #616161;
      --colorNeutralStrokeAccessibleHover: #575757;
      --colorNeutralStrokeAccessiblePressed: #4d4d4d;
      --colorNeutralStroke1: #d1d1d1;
      --colorNeutralStroke1Hover: #c7c7c7;
      --colorNeutralStroke1Pressed: #b3b3b3;
      --colorNeutralStroke2: #e0e0e0;
      --colorNeutralStroke3: #f0f0f0;
      --colorNeutralStrokeDisabled: #e0e0e0;
      --colorNeutralStrokeOnBrand: #ffffff;
      --colorNeutralStrokeSubtle: #e0e0e0;
      --colorTransparentBackground: transparent;
      --colorTransparentBackgroundHover: transparent;
      --colorTransparentBackgroundPressed: transparent;
      --colorTransparentStroke: transparent;
      --colorStrokeFocus1: #ffffff;
      --colorStrokeFocus2: #000000;
      --colorSubtleBackground: transparent;
      --colorSubtleBackgroundHover: #f5f5f5;
      --colorSubtleBackgroundPressed: #e0e0e0;
      --colorPaletteRedForeground1: #bc2f32;
      --colorPaletteRedForeground2: #751d1f;
      --colorPaletteRedBackground1: #fdf6f6;
      --colorPaletteRedBackground2: #f1bbbc;
      --colorPaletteRedBorder2: #d13438;
      --colorStatusSuccessBackground1: #f1faf1;
      --colorStatusSuccessBackground2: #9fd89f;
      --colorStatusSuccessBackground3: #107c10;
      --colorStatusSuccessForeground1: #0e700e;
      --colorStatusSuccessBorder1: #9fd89f;
      --colorStatusSuccessBorder2: #107c10;
      --colorStatusDangerBackground1: #fdf3f4;
      --colorStatusDangerBackground2: #eeacb2;
      --colorStatusDangerBackground3: #c50f1f;
      --colorStatusDangerForeground1: #b10e1c;
      --colorStatusDangerBorder1: #eeacb2;
      --colorStatusDangerBorder2: #c50f1f;
      --borderRadiusNone: 0;
      --borderRadiusSmall: 2px;
      --borderRadiusMedium: 4px;
      --borderRadiusLarge: 6px;
      --borderRadiusXLarge: 8px;
      --borderRadiusCircular: 10000px;
      --strokeWidthThin: 1px;
      --strokeWidthThick: 2px;
      --spacingHorizontalXXS: 2px;
      --spacingHorizontalXS: 4px;
      --spacingHorizontalS: 8px;
      --spacingHorizontalMNudge: 10px;
      --spacingHorizontalM: 12px;
      --spacingHorizontalL: 16px;
      --spacingVerticalXXS: 2px;
      --spacingVerticalXS: 4px;
      --spacingVerticalS: 8px;
      --spacingVerticalM: 12px;
      --spacingVerticalL: 16px;
      --fontFamilyBase: "Segoe UI", "Microsoft YaHei", "PingFang SC", system-ui, sans-serif;
      --fontSizeBase100: 10px;
      --fontSizeBase200: 12px;
      --fontSizeBase300: 14px;
      --fontSizeBase400: 16px;
      --fontSizeBase500: 20px;
      --fontSizeBase600: 24px;
      --fontWeightRegular: 400;
      --fontWeightSemibold: 600;
      --fontWeightBold: 700;
      --lineHeightBase200: 16px;
      --lineHeightBase300: 20px;
      --lineHeightBase400: 22px;
      --durationUltraFast: 50ms;
      --durationFast: 150ms;
      --durationNormal: 200ms;
      --curveAccelerateMid: cubic-bezier(1, 0, 1, 1);
      --curveDecelerateMid: cubic-bezier(0, 0, 0, 1);
      --shadow2: 0 0 2px rgba(0, 0, 0, .12), 0 1px 2px rgba(0, 0, 0, .14);
      --shadow4: 0 0 2px rgba(0, 0, 0, .12), 0 2px 4px rgba(0, 0, 0, .14);
      --shadow8: 0 0 2px rgba(0, 0, 0, .12), 0 4px 8px rgba(0, 0, 0, .14);
    }
    input, select { font: inherit; }
    .send-button {
      position: relative;
      overflow: hidden;
      isolation: isolate;
      --colorBrandBackground: transparent;
      --colorBrandBackgroundHover: rgba(255, 255, 255, .12);
      --colorBrandBackgroundPressed: rgba(0, 0, 0, .12);
      --colorNeutralForegroundOnBrand: #fff;
      background: linear-gradient(110deg, var(--teal-dark), var(--teal), #22d3ee, var(--teal));
      background-size: 240% 100%;
      box-shadow: 0 8px 18px rgba(8, 127, 140, .22);
      transition: transform .16s ease, box-shadow .16s ease;
      animation: sendGradient 7s ease-in-out infinite;
    }
    .send-button::after {
      content: "";
      position: absolute;
      inset: 0;
      z-index: -1;
      background: linear-gradient(100deg, transparent 18%, rgba(255, 255, 255, .34) 48%, transparent 78%);
      transform: translateX(-120%);
      transition: transform .45s ease;
    }
    .send-button:hover { transform: translateY(-1px); box-shadow: 0 12px 24px rgba(8, 127, 140, .28); }
    .send-button:hover::after { transform: translateX(120%); }
    .send-button.sending { cursor: progress; animation-duration: 1.2s; }
    .send-button.sending::after { transform: translateX(120%); transition-duration: 1.2s; }
    .send-stage { border-radius: 6px; }
    .shout-card {
      transform-origin: 50% 0;
      will-change: transform, opacity;
    }
    .shout-card.card-flight-out {
      pointer-events: none;
      animation: shoutCardExit .42s cubic-bezier(.76, 0, .9, .34) forwards;
    }
    .shout-card.card-flight-in {
      animation: shoutCardEnter .62s cubic-bezier(.08, .78, .12, 1) both;
    }
    .result-row {
      display: block;
    }
    .shout-feedback-stage {
      min-height: 320px;
      display: grid;
      place-items: center;
      align-content: center;
      gap: 14px;
      padding: 42px 24px;
      text-align: center;
    }
    .shout-feedback-stage.feedback-visible {
      animation: feedbackFadeIn .42s cubic-bezier(.08, .78, .12, 1) both;
    }
    .shout-feedback-stage.feedback-leaving {
      pointer-events: none;
      animation: feedbackFadeOut .22s cubic-bezier(.76, 0, .9, .34) both;
    }
    .shout-feedback-stage h3 {
      margin: 0;
      color: #242424;
      font-size: 28px;
      line-height: 36px;
      font-weight: 600;
    }
    .shout-feedback-stage p {
      max-width: 520px;
      margin: 0;
      color: #616161;
      font-size: 15px;
      line-height: 22px;
    }
    .feedback-actions {
      display: flex;
      justify-content: center;
      gap: 8px;
      margin-top: 8px;
    }
    .feedback-actions fluent-button { min-width: 112px; }
    .account-divider {
      height: 1px;
      margin: 4px 0;
      background: #e0e0e0;
    }
    .account-form fluent-button[type="submit"] { justify-self: start; }
    .user-profile-fields {
      display: grid;
      grid-template-columns: minmax(160px, 1fr) minmax(150px, .65fr);
      gap: 10px;
      min-width: 0;
    }
    .field-note {
      margin-top: 6px;
      color: var(--muted);
      font-size: 12px;
      line-height: 1.4;
    }
    fluent-button.send-button { width: 100%; min-height: 48px; }
    fluent-text-input, fluent-textarea, fluent-dropdown {
      width: 100%;
      max-width: none !important;
      inline-size: 100%;
      max-inline-size: none !important;
    }
    fluent-textarea {
      display: block;
      width: 100% !important;
      inline-size: 100% !important;
      min-inline-size: 0;
      --inline-size: 100%;
      --block-size: 220px;
      --min-block-size: 220px;
    }
    fluent-field,
    fluent-dropdown { width: 100%; min-width: 0; }
    fluent-dropdown {
      --listbox-max-height: min(360px, calc(100vh - 32px));
    }
    fluent-dropdown > fluent-listbox {
      max-width: min(320px, calc(100vw - 24px));
      max-height: min(360px, calc(100vh - 32px));
      overflow-y: auto;
    }
    fluent-slider {
      width: 100%;
      min-width: 0;
    }
    /* Keep numeric precision while hiding Fluent's optional step markers so
       the rail reads as one continuous control. */
    fluent-slider[step] { --step-rate: 100000% !important; }
    .checkbox-field {
      width: max-content;
      min-width: 0;
      min-height: 40px;
      align-self: end;
      justify-self: start;
    }
    .checkbox-field fluent-checkbox { flex: 0 0 auto; }
    /* Fluent positions listboxes in the top layer; a viewport-relative
       min-width makes the popup stretch across the browser. */
    .switch-field { align-self: center; min-height: 40px; }
    .slider-setting { display: grid; gap: 4px; min-width: 0; }
    .slider-heading {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      min-height: 20px;
    }
    .slider-heading label { margin: 0; }
    fluent-message-bar { display: block; }
    @keyframes shoutCardExit {
      0% { opacity: 1; transform: translateY(0) scale(1); }
      28% { opacity: .98; transform: translateY(-3vh) scale(.998); }
      62% { opacity: .72; transform: translateY(-30vh) scale(.985); }
      100% { opacity: 0; transform: translateY(-120vh) scale(.96); }
    }
    @keyframes shoutCardEnter {
      0% { opacity: 0; transform: translateX(76vw) scale(.965); }
      18% { opacity: .34; transform: translateX(38vw) scale(.978); }
      46% { opacity: .76; transform: translateX(11vw) scale(.991); }
      76% { opacity: .96; transform: translateX(1.8vw) scale(.998); }
      100% { opacity: 1; transform: translateX(0) scale(1); }
    }
    @keyframes feedbackFadeIn {
      0% { opacity: 0; }
      100% { opacity: 1; }
    }
    @keyframes feedbackFadeOut {
      0% { opacity: 1; }
      100% { opacity: 0; }
    }
    @keyframes sendGradient {
      0%, 100% { background-position: 0% 50%; }
      50% { background-position: 100% 50%; }
    }
    @media (prefers-reduced-motion: reduce) {
      *, *::before, *::after {
        animation-duration: .01ms !important;
        animation-iteration-count: 1 !important;
        scroll-behavior: auto !important;
        transition-duration: .01ms !important;
      }
    }
    label:not([slot="label"]) {
      display: block;
      margin-bottom: 7px;
      color: #344054;
      font-size: 14px;
      font-weight: 700;
    }
    .shell {
      width: min(1180px, calc(100% - 32px));
      margin: 0 auto;
      padding: 26px 0 34px;
    }
    .topbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 18px;
      min-height: 58px;
      margin-bottom: 18px;
    }
    .brand {
      display: flex;
      align-items: center;
      gap: 12px;
      min-width: 0;
    }
    .mark {
      display: grid;
      place-items: center;
      width: 38px;
      height: 38px;
      position: relative;
      flex: 0 0 auto;
      overflow: hidden;
      border-radius: 50%;
      color: transparent;
      /* Two clear breaks create the reference mark's large main arc and
         smaller independent arc on the right. */
      background: conic-gradient(
        from 0deg,
        #1f1f1f 0deg 45deg,
        transparent 45deg 55deg,
        #1f1f1f 55deg 125deg,
        transparent 125deg 135deg,
        #1f1f1f 135deg 360deg);
    }
    .mark::after {
      content: "";
      position: absolute;
      inset: 24%;
      border-radius: 50%;
      background: #f5f5f5;
    }
    h1 {
      margin: 0;
      font-size: 24px;
      letter-spacing: 0;
      line-height: 1.15;
    }
    .subtle { color: var(--muted); }
    .layout {
      display: grid;
      grid-template-columns: 300px 1fr;
      gap: 18px;
      align-items: start;
    }
    .layout.account-only {
      display: block;
    }
    .layout.account-only #detailsAside {
      width: min(680px, 100%);
      margin: 0 auto;
    }
    .panel {
      border: 1px solid var(--line);
      border-radius: 8px;
      background: var(--panel);
      box-shadow: var(--shadow);
    }
    .panel-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      padding: 16px 18px;
      border-bottom: 1px solid var(--line);
    }
    .panel-header h2 {
      margin: 0;
      font-size: 16px;
      letter-spacing: 0;
    }
    .panel-body { padding: 18px; }
    .stack {
      display: grid;
      gap: 14px;
    }
    .grid-2 {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 14px;
    }
    .grid-3 {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 14px;
    }
    .row {
      display: flex;
      align-items: center;
      gap: 10px;
      flex-wrap: wrap;
    }
    .row.between { justify-content: space-between; }
    .segmented {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      border: 1px solid var(--line);
      border-radius: 6px;
      overflow: hidden;
      background: #f2f5f9;
    }
    .segmented input { position: absolute; opacity: 0; pointer-events: none; }
    .segmented label {
      margin: 0;
      min-height: 42px;
      display: grid;
      place-items: center;
      color: #475467;
      cursor: pointer;
      font-weight: 800;
    }
    .segmented input:checked + label {
      color: #fff;
      background: var(--teal);
    }
    .switch {
      display: inline-flex;
      align-items: center;
      gap: 10px;
      min-height: 34px;
      color: #344054;
      font-weight: 700;
    }
    .switch input {
      width: 42px;
      height: 24px;
      appearance: none;
      border-radius: 999px;
      background: #cbd5e1;
      position: relative;
      outline: none;
      cursor: pointer;
    }
    .switch input::after {
      content: "";
      position: absolute;
      left: 3px;
      top: 3px;
      width: 18px;
      height: 18px;
      border-radius: 999px;
      background: #fff;
      transition: transform .16s ease;
    }
    .switch input:checked { background: var(--teal); }
    .switch input:checked::after { transform: translateX(18px); }
    input[type="range"] {
      width: 100%;
      accent-color: var(--teal);
    }
    .range-line {
      display: grid;
      grid-template-columns: 1fr auto;
      gap: 12px;
      align-items: center;
    }
    .range-value {
      min-width: 58px;
      text-align: right;
      color: #344054;
      font-weight: 800;
    }
    .theme-row {
      display: grid;
      grid-template-columns: 22px 1fr;
      gap: 10px;
      align-items: center;
    }
    .swatch {
      width: 18px;
      height: 18px;
      border-radius: 6px;
      border: 1px solid rgba(15, 23, 42, .16);
    }
    .swatch.cyan { background: var(--teal); }
    .swatch.blue { background: var(--blue); }
    .swatch.green { background: var(--green); }
    .swatch.amber { background: var(--amber); }
    .swatch.rose { background: var(--rose); }
    .swatch.violet { background: var(--violet); }
    .swatch.indigo { background: var(--indigo); }
    .swatch.magenta { background: var(--magenta); }
    .swatch.orange { background: var(--orange); }
    .swatch.emerald { background: var(--emerald); }
    .swatch.cyan-dark { background: var(--cyan-dark); }
    .swatch.blue-dark { background: var(--blue-dark); }
    .swatch.green-dark { background: var(--green-dark); }
    .swatch.amber-dark { background: var(--amber-dark); }
    .swatch.rose-dark { background: var(--rose-dark); }
    .swatch.violet-dark { background: var(--violet-dark); }
    .swatch.indigo-dark { background: var(--indigo-dark); }
    .swatch.magenta-dark { background: var(--magenta-dark); }
    .swatch.orange-dark { background: var(--orange-dark); }
    .swatch.emerald-dark { background: var(--emerald-dark); }
    .notice {
      min-height: 36px;
      padding: 10px 12px;
      border-radius: 6px;
      color: #344054;
      background: #eef6f7;
      border: 1px solid #c6e4e8;
      font-weight: 700;
    }
    .notice.error {
      color: #912018;
      background: #fff1f0;
      border-color: #f4b8b2;
    }
    .hidden { display: none !important; }
    .auth-wrap {
      width: min(440px, calc(100% - 32px));
      margin: 9vh auto 40px;
    }
    .auth-brand { margin-bottom: 16px; }
    .auth-wrap .panel { box-shadow: 0 28px 80px rgba(15, 23, 42, .14); }
    .meta-list {
      display: grid;
      gap: 10px;
      color: #475467;
      font-size: 14px;
    }
    .meta-list div {
      display: grid;
      gap: 3px;
      padding-bottom: 10px;
      border-bottom: 1px solid #edf1f6;
    }
    .meta-list strong { color: #1f2937; }
    .url-list {
      display: grid;
      gap: 8px;
      margin: 0;
      padding: 0;
      list-style: none;
    }
    .url-list button,
    .url-list fluent-button {
      width: 100%;
      text-align: left;
      overflow-wrap: anywhere;
    }
    .users {
      display: grid;
      gap: 10px;
    }
    .user-row {
      display: grid;
      grid-template-columns: minmax(150px, .8fr) minmax(260px, 1.4fr) auto;
      gap: 10px;
      align-items: center;
      padding: 12px;
      border: 1px solid var(--line);
      border-radius: 8px;
      background: var(--panel-soft);
    }
    .badges {
      display: flex;
      gap: 6px;
      flex-wrap: wrap;
      align-items: center;
    }
    .theme-badge {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      min-height: 24px;
      padding: 3px 8px;
      border: 1px solid var(--line);
      border-radius: 999px;
      background: var(--panel);
      color: var(--text);
      font-size: 12px;
      font-weight: 600;
    }
    .theme-badge .swatch {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      border: 0;
    }
    .badge {
      display: inline-flex;
      min-height: 24px;
      align-items: center;
      padding: 3px 8px;
      border-radius: 999px;
      background: #e8edf4;
      color: #475467;
      font-size: 12px;
      font-weight: 800;
    }
    .user-edit-fields {
      display: grid;
      grid-template-columns: minmax(140px, 1fr) minmax(120px, .7fr);
      gap: 8px;
      min-width: 0;
    }
    .user-edit-fields fluent-field { min-width: 0; }
    .user-row-actions { justify-content: flex-end; }
    .badge.admin { background: #e0f2fe; color: #075985; }
    .badge.disabled { background: #fee2e2; color: #991b1b; }
    .tabs {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }
    .tab {
      color: #344054;
      background: #e8edf4;
    }
    .tab.active {
      color: #fff;
      background: var(--teal);
    }
    .toggle-block { align-content: end; }
    .app-actions {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 8px;
      min-width: 0;
    }
    .app-actions fluent-button,
    .app-actions fluent-badge { white-space: nowrap; }
    .page-view { display: none; }
    .page-view.active { display: block; }
    .layout.shout-layout,
    .layout.single-page { grid-template-columns: 1fr; }
    .page-links {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      flex-wrap: wrap;
      margin-top: 18px;
      padding: 4px 2px 0;
    }
    .page-link {
      min-height: 34px;
      padding: 7px 10px;
      color: #155e75;
      background: transparent;
      border: 1px solid transparent;
      font-size: 13px;
    }
    .page-link:hover,
    .page-link.active {
      color: var(--teal-dark);
      background: #e6f7f8;
      border-color: #b9e1e5;
    }
    .debug-settings {
      border-top: 1px solid var(--line);
      padding-top: 14px;
    }
    .debug-toggle {
      justify-content: flex-start;
      width: 100%;
      padding-inline: 0;
      color: #155e75;
      font-weight: 800;
      text-align: left;
    }
    .debug-settings-body { padding-top: 14px; }
    .settings-actions { display: flex; gap: 8px; flex-wrap: wrap; }
    .settings-actions button,
    .settings-actions fluent-button { width: auto; }
    .settings-file { display: none; }
    .result-row.hidden { display: none; }
    fluent-button.danger {
      --colorNeutralForeground1: #b42318;
      --colorNeutralForeground2: #b42318;
      --colorTransparentBackgroundHover: #fff1f0;
      --colorTransparentBackgroundPressed: #ffe4e1;
    }
    @media (max-width: 900px) {
      .layout { grid-template-columns: 1fr; }
      .grid-3 { grid-template-columns: 1fr; }
      .user-row { grid-template-columns: 1fr; }
      .topbar { align-items: stretch; flex-direction: column; gap: 12px; }
      .app-actions { justify-content: stretch; }
      .app-actions fluent-badge { flex: 0 0 auto; }
    }
    @media (max-width: 620px) {
      .shell { width: min(100% - 20px, 1180px); padding-top: 16px; }
      .grid-2 { grid-template-columns: 1fr; }
      .panel-body { padding: 14px; }
      fluent-textarea {
        --block-size: 160px;
        --min-block-size: 160px;
      }
      .app-actions { display: grid; grid-template-columns: auto auto auto; gap: 6px; }
      .app-actions fluent-badge { min-width: 0; overflow: hidden; text-overflow: ellipsis; }
      .app-actions fluent-button { width: auto; padding-inline: 10px; min-width: 0; }
      .page-links { justify-content: stretch; }
      .page-link { flex: 1 1 0; }
      .tabs button { width: auto; }
      .user-edit-fields { grid-template-columns: 1fr; }
    }

    /* Fluent 3 page chrome. The send stage above intentionally keeps its
       custom rising animation; the surrounding shell follows Fluent tokens. */
    :root {
      font-family: "Segoe UI", "Microsoft YaHei", "PingFang SC", system-ui, sans-serif;
      background: #f5f5f5;
      color: #242424;
      --line: #d1d1d1;
      --muted: #616161;
      --panel: #ffffff;
      --panel-soft: #fafafa;
      --text: #242424;
      --teal: #0f6cbd;
      --teal-dark: #115ea3;
      --danger: #b10e1c;
      --shadow: 0 0 2px rgba(0, 0, 0, .12), 0 4px 8px rgba(0, 0, 0, .14);
    }
    body {
      background: #f5f5f5;
      color: #242424;
    }
    .brand { gap: 10px; }
    .auth-wrap {
      width: min(480px, calc(100% - 32px));
      margin: 10vh auto 40px;
    }
    .auth-wrap .panel {
      box-shadow: 0 0 2px rgba(0, 0, 0, .12), 0 14px 28px rgba(0, 0, 0, .14);
    }
    .mark {
      width: 36px;
      height: 36px;
      border-radius: 50%;
    }
    h1 {
      font-size: 22px;
      font-weight: 600;
    }
    .subtle { color: #616161; }
    .panel {
      border-color: #d1d1d1;
      border-radius: 4px;
      background: #fff;
      box-shadow: var(--shadow);
    }
    .panel-header {
      min-height: 56px;
      padding: 16px 20px;
      background: #fff;
    }
    .panel-header h2 {
      font-size: 20px;
      font-weight: 600;
      color: #242424;
    }
    .panel-body { padding: 20px; }
    .meta-list { color: #424242; }
    .meta-list strong { color: #242424; }
    .user-row {
      border-radius: 4px;
      border-color: #e0e0e0;
      background: #fafafa;
    }
    .page-links {
      justify-content: flex-start;
      gap: 0;
      margin-top: 20px;
      padding: 0;
      border-bottom: 1px solid #d1d1d1;
    }
    .page-link {
      min-height: 40px;
      padding: 10px 16px;
      border: 0;
      border-bottom: 2px solid transparent;
      border-radius: 0;
      color: #424242;
      background: transparent;
      font-size: 14px;
      font-weight: 600;
    }
    .page-links fluent-button.page-link {
      flex: 0 0 auto;
      min-width: 96px;
      --colorNeutralForeground1: #424242;
      --colorNeutralForeground1Hover: #242424;
      --colorNeutralForeground1Pressed: #242424;
      --colorNeutralForegroundDisabled: #bdbdbd;
      --colorTransparentBackground: transparent;
      --colorTransparentBackgroundHover: #f5f5f5;
      --colorTransparentBackgroundPressed: #e0e0e0;
      --colorCompoundBrandForeground1: #0f6cbd;
      --colorCompoundBrandForeground1Hover: #115ea3;
      --colorCompoundBrandForeground1Pressed: #0f548c;
    }
    .page-link:hover,
    .page-link.active,
    .page-link[selected] {
      color: #0f6cbd;
      background: #f5f5f5;
      border-color: #0f6cbd;
    }
    .debug-settings { border-top-color: #e0e0e0; }
    .debug-toggle {
      color: #0f6cbd;
      font-weight: 600;
      --colorNeutralForeground1: #0f6cbd;
      --colorNeutralForeground1Hover: #115ea3;
      --colorNeutralForeground1Pressed: #0f548c;
      --colorTransparentBackground: transparent;
      --colorTransparentBackgroundHover: #f5f5f5;
      --colorTransparentBackgroundPressed: #e0e0e0;
    }
    .field-note { color: #616161; }
    .auth-wrap {
      width: min(520px, calc(100% - 32px));
      margin: 9vh auto 40px;
    }
    .auth-brand {
      align-items: center;
      min-height: 52px;
      margin-bottom: 20px;
    }
    .auth-brand .mark {
      flex: 0 0 48px;
      width: 48px;
      height: 48px;
    }
    .auth-brand h1 {
      font-size: 26px;
      line-height: 32px;
    }
    .auth-brand .subtle {
      margin-top: 2px;
      font-size: 16px;
    }
    .auth-wrap .panel { overflow: hidden; }
    .auth-wrap .panel-header {
      min-height: 72px;
      padding: 20px 24px;
      align-items: center;
    }
    .auth-wrap .panel-header h2 {
      margin: 0;
      font-size: 24px;
      line-height: 32px;
    }
    .auth-wrap .panel-body { padding: 24px; }
    .auth-wrap #authForm { gap: 20px; }
    .auth-wrap #authForm > div {
      display: grid;
      gap: 8px;
      min-width: 0;
    }
    .auth-wrap #authForm label {
      margin: 0;
      font-size: 14px;
      line-height: 20px;
    }
    .auth-wrap #authForm fluent-text-input,
    .auth-wrap #authForm fluent-message-bar { width: 100%; }
    .auth-actions {
      display: flex !important;
      flex-direction: row;
      justify-content: flex-end;
      gap: 8px !important;
      width: 100%;
    }
    .auth-actions fluent-button { min-width: 96px; }
    .status-address-note {
      margin: 0;
      padding: 10px 12px;
      border-left: 3px solid #0f6cbd;
      background: #f5f5f5;
      color: #424242;
      font-size: 13px;
      line-height: 20px;
    }
    fluent-field > fluent-text-input,
    fluent-field > fluent-textarea,
    fluent-field > fluent-dropdown {
      width: 100%;
    }
    /* Keep native Fluent button geometry and interaction tokens.  Only the
       send action has a custom visual treatment by design. */
    .settings-actions fluent-button {
      flex: 0 0 132px;
      min-width: 132px;
    }
    @media (max-width: 620px) {
      .shell { width: min(100% - 20px, 1200px); }
      .panel-body { padding: 16px; }
      .page-links { overflow-x: auto; }
      .page-link { flex: 0 0 auto; }
      .auth-wrap { width: min(100% - 20px, 480px); margin-top: 32px; }
      .auth-brand .mark { flex-basis: 40px; width: 40px; height: 40px; }
      .auth-brand h1 { font-size: 20px; line-height: 26px; }
      .auth-wrap .panel-header,
      .auth-wrap .panel-body { padding: 16px; }
      .auth-wrap .panel-header h2 { font-size: 20px; line-height: 28px; }
    }
  </style>
</head>
<body>
  <div id="authView" class="auth-wrap hidden">
    <div class="brand auth-brand">
      <div class="mark" aria-hidden="true"></div>
      <div>
        <h1>OpenRemoteShouter</h1>
        <div class="subtle">远程喊话控制台</div>
      </div>
    </div>
    <section class="panel">
      <div class="panel-header">
        <h2 id="authTitle">登录</h2>
        <fluent-badge id="authStateBadge" appearance="tint" color="informative" shape="rounded" size="medium">账户</fluent-badge>
      </div>
      <div class="panel-body">
        <form id="authForm" class="stack" novalidate>
          <div>
            <label for="authUsername">用户名</label>
            <fluent-text-input id="authUsername" name="username" type="text" autocomplete="username" maxlength="32" appearance="outline"></fluent-text-input>
          </div>
          <div id="displayNameField">
            <label for="authDisplayName">显示名称</label>
            <fluent-text-input id="authDisplayName" name="displayName" type="text" maxlength="48" appearance="outline"></fluent-text-input>
            <div class="field-note">此显示名称会显示在客户端的喊话标题上。</div>
          </div>
          <div id="setupTokenField" class="hidden">
            <label for="authSetupToken">可信中转令牌（远程初始化时填写）</label>
            <fluent-text-input id="authSetupToken" name="setupToken" type="password" autocomplete="one-time-code" maxlength="512" appearance="outline"></fluent-text-input>
          </div>
          <div>
            <label for="authPassword">密码</label>
            <fluent-text-input id="authPassword" name="password" type="password" autocomplete="current-password" appearance="outline"></fluent-text-input>
          </div>
          <div class="auth-actions">
            <fluent-button id="authRetry" appearance="outline" class="secondary hidden" type="button">重新检查</fluent-button>
            <fluent-button id="authSubmit" appearance="primary" type="submit">登录</fluent-button>
          </div>
          <fluent-message-bar id="authMessage" class="notice hidden" intent="info"></fluent-message-bar>
        </form>
      </div>
    </section>
  </div>

  <div id="appView" class="shell hidden">
    <header class="topbar">
      <div class="brand">
        <div class="mark" aria-hidden="true"></div>
        <div>
          <h1>OpenRemoteShouter</h1>
          <div class="subtle" id="userLine">未登录</div>
        </div>
      </div>
      <div class="app-actions">
        <fluent-badge id="serverStatusBadge" appearance="tint" color="informative" shape="rounded" size="medium">读取状态</fluent-badge>
        <fluent-button appearance="outline" class="secondary" id="refreshButton" type="button">刷新</fluent-button>
        <fluent-button appearance="outline" class="secondary" id="logoutButton" type="button">退出登录</fluent-button>
      </div>
    </header>

    <main id="layout" class="layout shout-layout">
      <aside id="detailsAside" class="stack">
        <section id="statusPanel" class="panel hidden">
          <div class="panel-header"><h2>服务状态</h2></div>
          <div class="panel-body stack">
            <div class="meta-list">
              <div><span>状态</span><strong id="statusPageText">-</strong></div>
              <div><span>端口</span><strong id="portText">-</strong></div>
              <div><span>语音后端</span><strong id="speechBackendText">-</strong></div>
              <div><span>日志文件</span><strong id="logPathText">-</strong></div>
            </div>
            <p id="statusAddressNote" class="status-address-note">本机地址仅供运行软件的电脑访问，不在此处显示。远程访问请使用已配置的域名或中转地址。</p>
            <ul id="urlList" class="url-list"></ul>
          </div>
        </section>

        <section id="accountPanel" class="panel hidden">
          <div class="panel-header"><h2>账户</h2></div>
          <div class="panel-body stack">
            <div class="meta-list">
              <div><span>当前用户</span><strong id="currentUserText">-</strong></div>
              <div><span>权限</span><strong id="currentRoleText">-</strong></div>
            </div>
            <form id="profileForm" class="stack account-form">
              <fluent-field label-position="above" weight="semibold">
                <label slot="label" for="profileDisplayName">显示名称</label>
                <fluent-text-input slot="input" id="profileDisplayName" type="text" maxlength="48" appearance="outline"></fluent-text-input>
              </fluent-field>
              <fluent-field label-position="above" weight="semibold">
                <label slot="label" for="profileTheme">主题色</label>
                <div slot="input" class="theme-row">
                  <span id="profileThemeSwatch" class="swatch cyan"></span>
                  <fluent-dropdown id="profileTheme" appearance="outline" value="cyan">
                    <fluent-listbox>
                      <fluent-option value="cyan" selected>青色</fluent-option>
                      <fluent-option value="blue">蓝色</fluent-option>
                      <fluent-option value="green">绿色</fluent-option>
                      <fluent-option value="amber">琥珀</fluent-option>
                      <fluent-option value="rose">玫瑰</fluent-option>
                      <fluent-option value="violet">紫色</fluent-option>
                      <fluent-option value="indigo">靛蓝</fluent-option>
                      <fluent-option value="magenta">品红</fluent-option>
                      <fluent-option value="orange">橙色</fluent-option>
                      <fluent-option value="emerald">翠绿</fluent-option>
                    </fluent-listbox>
                  </fluent-dropdown>
                </div>
              </fluent-field>
              <fluent-button appearance="primary" type="submit">保存资料</fluent-button>
            </form>
            <fluent-message-bar id="profileMessage" class="notice hidden" intent="info"></fluent-message-bar>
            <div class="account-divider"></div>
            <form id="passwordForm" class="stack">
              <div>
                <label for="currentPassword">当前密码</label>
                <fluent-text-input id="currentPassword" type="password" autocomplete="current-password" appearance="outline"></fluent-text-input>
              </div>
              <div>
                <label for="newPassword">新密码</label>
                <fluent-text-input id="newPassword" type="password" autocomplete="new-password" appearance="outline"></fluent-text-input>
              </div>
              <fluent-button appearance="outline" class="secondary" type="submit">修改密码</fluent-button>
            </form>
          </div>
        </section>
      </aside>

      <section id="mainContent" class="stack">
        <section id="shoutCard" class="panel shout-card">
          <div class="panel-header">
            <h2 id="contentTitle">喊话</h2>
          </div>
          <div id="shoutPanel" class="panel-body">
            <form id="shoutForm" class="stack">
              <div>
                <label for="message">内容</label>
            <fluent-textarea id="message" class="message-input" maxlength="3000" required autofocus resize="vertical" appearance="outline" aria-label="喊话内容"></fluent-textarea>
              </div>
              <div id="sendStage" class="send-stage">
                <fluent-button id="sendButton" class="send-button" appearance="primary" type="submit">发送</fluent-button>
              </div>
              <div id="resultRow" class="result-row hidden">
                <fluent-message-bar id="result" class="notice hidden" intent="info"></fluent-message-bar>
              </div>

              <section id="debugSettings" class="debug-settings">
                <fluent-button id="debugToggle" class="debug-toggle" appearance="subtle" type="button" aria-expanded="false">调试设置</fluent-button>
                <div id="debugSettingsBody" class="debug-settings-body stack hidden">
                  <div>
                    <fluent-field label-position="above" weight="semibold">
                      <label slot="label" for="voiceName">EdgeTTS 说话人</label>
                      <fluent-dropdown slot="input" id="voiceName" appearance="outline" placeholder="选择语音">
                        <fluent-listbox id="voiceListbox">
                          <fluent-option value="zh-CN-XiaoyiNeural" selected>中文 · Xiaoyi</fluent-option>
                          <fluent-option value="zh-CN-XiaoxiaoNeural">中文 · Xiaoxiao</fluent-option>
                          <fluent-option value="zh-CN-YunxiNeural">中文 · Yunxi</fluent-option>
                        </fluent-listbox>
                      </fluent-dropdown>
                    </fluent-field>
                  </div>
                  <div class="grid-3">
                    <fluent-field class="switch-field" label-position="after">
                      <fluent-switch slot="input" id="topmost" checked></fluent-switch>
                      <label slot="label" for="topmost">置顶显示</label>
                    </fluent-field>
                    <fluent-field class="switch-field" label-position="after">
                      <fluent-switch slot="input" id="speechEnabled" checked></fluent-switch>
                      <label slot="label" for="speechEnabled">语音播报</label>
                    </fluent-field>
                    <div class="slider-setting">
                      <div class="slider-heading">
                        <label for="speechRate">语速</label>
                        <span id="rateValue" class="range-value">0%</span>
                      </div>
                      <fluent-slider id="speechRate" min="-100" max="100" step="1" value="0"></fluent-slider>
                    </div>
                  </div>
                  <div class="slider-setting">
                    <div class="slider-heading">
                      <label for="speechVolume">音量</label>
                      <span id="volumeValue" class="range-value">100%</span>
                    </div>
                    <fluent-slider id="speechVolume" min="0" max="1" step="0.01" value="1"></fluent-slider>
                  </div>
                  <div class="settings-actions">
                    <fluent-button appearance="outline" class="secondary" type="button" id="closeButton">关闭当前显示</fluent-button>
                    <fluent-button appearance="outline" class="secondary" type="button" id="exportSettingsButton">导出设置</fluent-button>
                    <fluent-button appearance="outline" class="secondary" type="button" id="importSettingsButton">导入设置</fluent-button>
                    <input id="settingsImportFile" class="settings-file" type="file" accept="application/json,.json">
                  </div>
                </div>
              </section>
            </form>
          </div>

          <div id="usersPanel" class="panel-body hidden">
            <div id="adminUsersContent" class="stack admin-only">
              <fluent-message-bar id="usersMessage" class="notice hidden" intent="info"></fluent-message-bar>
              <form id="createUserForm" class="grid-3">
                <div>
                  <label for="newUsernameInput">用户名</label>
                  <fluent-text-input id="newUsernameInput" type="text" maxlength="32" appearance="outline"></fluent-text-input>
                </div>
                <div>
                  <label for="newDisplayNameInput">显示名称</label>
                  <fluent-text-input id="newDisplayNameInput" type="text" maxlength="48" appearance="outline"></fluent-text-input>
                  <div class="field-note">此显示名称会显示在客户端的喊话标题上。</div>
                </div>
                <div>
                  <label for="newUserPasswordInput">初始密码</label>
                  <fluent-text-input id="newUserPasswordInput" type="password" appearance="outline"></fluent-text-input>
                </div>
                <fluent-field label-position="above" weight="semibold">
                  <label slot="label" for="newUserTheme">主题色</label>
                  <fluent-dropdown slot="input" id="newUserTheme" appearance="outline" value="cyan">
                    <fluent-listbox>
                      <fluent-option value="cyan" selected>青色</fluent-option>
                      <fluent-option value="blue">蓝色</fluent-option>
                      <fluent-option value="green">绿色</fluent-option>
                      <fluent-option value="amber">琥珀</fluent-option>
                      <fluent-option value="rose">玫瑰</fluent-option>
                      <fluent-option value="violet">紫色</fluent-option>
                      <fluent-option value="indigo">靛蓝</fluent-option>
                      <fluent-option value="magenta">品红</fluent-option>
                      <fluent-option value="orange">橙色</fluent-option>
                      <fluent-option value="emerald">翠绿</fluent-option>
                    </fluent-listbox>
                  </fluent-dropdown>
                </fluent-field>
                <fluent-field class="checkbox-field" label-position="after">
                  <fluent-checkbox slot="input" id="newUserAdminInput" size="large"></fluent-checkbox>
                  <label slot="label" id="newUserAdminLabel" for="newUserAdminInput">管理员</label>
                </fluent-field>
                <fluent-button appearance="primary" type="submit">创建用户</fluent-button>
              </form>
              <div id="usersList" class="users"></div>
            </div>
          </div>
        </section>

        <section id="shoutFeedbackPanel" class="shout-feedback-stage hidden" aria-live="polite">
          <h3 id="shoutFeedbackTitle">发送成功</h3>
          <p id="shoutFeedbackText">消息已发送，显示端正在播放。</p>
          <div class="feedback-actions">
            <fluent-button id="cancelDisplayButton" appearance="outline" type="button">撤回</fluent-button>
            <fluent-button id="continueShoutButton" class="hidden" appearance="primary" type="button">再发一条</fluent-button>
          </div>
        </section>
      </section>
    </main>
    <nav id="pageLinks" class="page-links" aria-label="页面导航">
      <fluent-button appearance="subtle" class="page-link active" data-page="shout" type="button" aria-current="page">喊话</fluent-button>
      <fluent-button appearance="subtle" class="page-link" data-page="users" type="button">账户与用户</fluent-button>
      <fluent-button appearance="subtle" class="page-link" data-page="status" type="button">服务状态</fluent-button>
    </nav>
  </div>

  <script type="module" nonce="{{nonce}}" src="/assets/fluentui-web-components-all.min.js"></script>
  <script type="module" nonce="{{nonce}}" src="/assets/fluentui-web-light-theme.min.js"></script>
  <script type="module" nonce="{{nonce}}">
    const defaultSettings = {
      mode: 'fullscreen',
      topmost: true,
      speechEnabled: true,
      voiceName: 'zh-CN-XiaoyiNeural',
      speechRate: 0,
      speechVolume: 1
    };
    const themeCatalog = [
      ['cyan', '青色 · 明亮'], ['cyan-dark', '青色 · 深色'],
      ['blue', '蓝色 · 明亮'], ['blue-dark', '蓝色 · 深色'],
      ['green', '绿色 · 明亮'], ['green-dark', '绿色 · 深色'],
      ['amber', '琥珀 · 明亮'], ['amber-dark', '琥珀 · 深色'],
      ['rose', '玫瑰 · 明亮'], ['rose-dark', '玫瑰 · 深色'],
      ['violet', '紫色 · 明亮'], ['violet-dark', '紫色 · 深色'],
      ['indigo', '靛蓝 · 明亮'], ['indigo-dark', '靛蓝 · 深色'],
      ['magenta', '品红 · 明亮'], ['magenta-dark', '品红 · 深色'],
      ['orange', '橙色 · 明亮'], ['orange-dark', '橙色 · 深色'],
      ['emerald', '翠绿 · 明亮'], ['emerald-dark', '翠绿 · 深色']
    ];
    const state = { user: null, csrfToken: null, setupRequired: false, remoteSetupEnabled: false, authUnavailable: false, users: [], voices: [], settings: { ...defaultSettings }, currentPage: 'shout', applyingSettings: false, cardTransitioning: false, feedbackCard: null, themeAvailability: [] };
    const $ = id => document.getElementById(id);
    const fluentReady = window.customElements
      ? Promise.race([
          Promise.all([
            'fluent-button',
            'fluent-badge',
            'fluent-checkbox',
            'fluent-dropdown',
            'fluent-field',
            'fluent-listbox',
            'fluent-message-bar',
            'fluent-option',
            'fluent-slider',
            'fluent-switch',
            'fluent-text-input',
            'fluent-textarea'
          ].map(tag => customElements.whenDefined(tag))),
          // A missing optional registration must never block auth, status,
          // voice, or user data from loading. The local bundle normally wins
          // this race immediately; the timeout is a defensive fallback.
          new Promise(resolve => window.setTimeout(resolve, 1500))
        ])
      : Promise.resolve();

    async function api(path, options = {}) {
      const headers = { ...(options.headers || {}) };
      if (options.body && !headers['Content-Type']) headers['Content-Type'] = 'application/json; charset=utf-8';
      if (options.method && options.method !== 'GET' && state.csrfToken) headers['X-OpenRemoteShouter-CSRF'] = state.csrfToken;
      const response = await fetch(path, { ...options, headers, credentials: 'same-origin' });
      const body = await response.json().catch(() => ({ ok: false, error: '响应格式错误。' }));
      if (!response.ok || body.ok === false) {
        const error = new Error(body.error || '请求失败。');
        error.status = response.status;
        throw error;
      }
      return body;
    }

    function showNotice(element, text, isError = false) {
      element.textContent = text;
      element.classList.toggle('error', isError);
      if (element.matches('fluent-message-bar')) {
        element.setAttribute('intent', isError ? 'error' : 'success');
      }
      element.classList.remove('hidden');
      if (element.id === 'result') $('resultRow').classList.remove('hidden');
    }

    function getThemeAwareErrorMessage(error, fallback) {
      if (error?.status === 409) {
        return '主题色已被其他用户使用，请选择其他主题色后重试。';
      }
      return error?.message || fallback;
    }

    function populateThemeDropdown(dropdown, selectedValue = 'cyan') {
      const listbox = dropdown?.querySelector('fluent-listbox');
      if (!listbox) return;
      dropdown.multiple = false;
      listbox.multiple = false;
      listbox.innerHTML = '';
      for (const [value, label] of themeCatalog) {
        const option = document.createElement('fluent-option');
        option.value = value;
        option.setAttribute('value', value);
        option.textContent = label;
        // Let the Fluent listbox own selection state. Setting selected on
        // several options while the component is upgrading can leave stale
        // entries in selectedOptions and make the dropdown display two
        // themes at once.
        option.selected = false;
        listbox.appendChild(option);
      }
      const options = Array.from(listbox.options || listbox.querySelectorAll('fluent-option'));
      const selectedIndex = Math.max(0, options.findIndex(option => option.value === selectedValue));
      normalizeSingleDropdown(dropdown, options[selectedIndex]?.value || selectedValue);
      dropdown.dataset.currentTheme = options[selectedIndex]?.value || selectedValue;
      if (!dropdown.dataset.singleSelectionBound) {
        dropdown.dataset.singleSelectionBound = 'true';
        dropdown.addEventListener('click', () => {
          queueMicrotask(() => normalizeSingleDropdown(dropdown, dropdown.dataset.currentTheme || 'cyan'));
        });
        dropdown.addEventListener('change', () => {
          // The change event is emitted after Fluent has processed the click.
          // Queueing one microtask lets us remove any stale selection left by
          // a previously upgraded component instance.
          queueMicrotask(() => {
            const value = dropdown.value || dropdown.dataset.currentTheme || 'cyan';
            dropdown.dataset.currentTheme = value;
            normalizeSingleDropdown(dropdown, value);
          });
        });
      }
    }

    function normalizeSingleDropdown(dropdown, preferredValue = '') {
      const listbox = dropdown?.querySelector('fluent-listbox');
      if (!listbox) return;
      dropdown.multiple = false;
      listbox.multiple = false;
      dropdown.removeAttribute('multiple');
      listbox.removeAttribute('multiple');
      const options = Array.from(listbox.options || listbox.querySelectorAll('fluent-option'));
      if (!options.length) return;
      const preferred = options.find(option => String(option.value) === String(preferredValue));
      const current = listbox.selectedOptions?.[listbox.selectedOptions.length - 1];
      const selected = preferred || current || options[0];
      options.forEach(option => {
        option.selected = false;
        option.defaultSelected = false;
        option.removeAttribute('selected');
        option.removeAttribute('default-selected');
      });
      const selectedIndex = options.indexOf(selected);
      if (selectedIndex < 0) return;
      if (typeof listbox.selectOption === 'function') listbox.selectOption(selectedIndex);
      selected.selected = true;
      selected.defaultSelected = true;
      selected.setAttribute('selected', '');
      selected.setAttribute('default-selected', '');
      // Always use the Fluent dropdown setter, even when the reflected
      // attribute already has the same value. The setter also refreshes the
      // component's internal displayValue used by its visible control.
      dropdown.value = selected.value;
      const syncVisibleValue = () => {
        const liveOptions = Array.from(listbox.options || listbox.querySelectorAll('fluent-option'));
        const liveIndex = Math.max(0, liveOptions.findIndex(option => String(option.value) === String(selected.value)));
        liveOptions.forEach(option => {
          const isSelected = option === liveOptions[liveIndex];
          option.selected = isSelected;
          option.defaultSelected = isSelected;
          option.toggleAttribute('selected', isSelected);
          option.toggleAttribute('default-selected', isSelected);
        });
        if (typeof dropdown.selectOption === 'function') dropdown.selectOption(liveIndex, false);
        const text = liveOptions[liveIndex]?.textContent?.trim() || selected.value;
        if (dropdown.control) dropdown.control.value = text;
        dropdown.setAttribute('value', selected.value);
      };
      // Dynamic user rows can be normalized before Fluent finishes its
      // listbox slotchange callback. Repeat after upgrade/layout so the
      // currently selected theme is visibly rendered in the closed control.
      queueMicrotask(syncVisibleValue);
      window.requestAnimationFrame(syncVisibleValue);
      window.setTimeout(syncVisibleValue, 60);
      window.setTimeout(syncVisibleValue, 240);
    }

    function populateThemeDropdowns() {
      populateThemeDropdown($('profileTheme'), state.user?.theme || 'cyan');
      populateThemeDropdown($('newUserTheme'), 'cyan');
    }

    function hideNotice(element) {
      element.classList.add('hidden');
      element.textContent = '';
      if (element.id === 'result') $('resultRow').classList.add('hidden');
    }

    function wait(milliseconds) {
      return new Promise(resolve => window.setTimeout(resolve, milliseconds));
    }

    async function flyShoutCardOut() {
      const card = $('shoutCard');
      card.classList.remove('card-flight-in', 'card-flight-out');
      void card.offsetWidth;
      card.classList.add('card-flight-out');
      await wait(420);
    }

    async function flyShoutCardIn() {
      const card = $('shoutCard');
      card.classList.remove('hidden');
      card.classList.remove('card-flight-out', 'card-flight-in');
      void card.offsetWidth;
      card.classList.add('card-flight-in');
      await wait(620);
      card.classList.remove('card-flight-in');
    }

    function showComposeCard(notice = '', isError = false) {
      state.feedbackCard = null;
      $('shoutCard').classList.remove('hidden');
      $('contentTitle').textContent = '喊话';
      $('shoutPanel').classList.remove('hidden');
      $('shoutFeedbackPanel').classList.add('hidden');
      if (notice) showNotice($('result'), notice, isError);
      else hideNotice($('result'));
    }

    function showFeedbackPage(title, text, canWithdraw) {
      state.feedbackCard = { title, text, canWithdraw };
      const feedback = $('shoutFeedbackPanel');
      $('shoutCard').classList.add('hidden');
      feedback.classList.remove('hidden', 'feedback-visible', 'feedback-leaving');
      $('shoutFeedbackTitle').textContent = title;
      $('shoutFeedbackText').textContent = text;
      $('cancelDisplayButton').classList.toggle('hidden', !canWithdraw);
      $('continueShoutButton').classList.remove('hidden');
      void feedback.offsetWidth;
      feedback.classList.add('feedback-visible');
    }

    async function fadeFeedbackPageOut() {
      const feedback = $('shoutFeedbackPanel');
      feedback.classList.remove('feedback-visible', 'feedback-leaving');
      void feedback.offsetWidth;
      feedback.classList.add('feedback-leaving');
      await wait(220);
      feedback.classList.add('hidden');
      feedback.classList.remove('feedback-leaving');
    }

    function setAuthState(authState) {
      state.authUnavailable = false;
      state.setupRequired = !!authState.setupRequired;
      state.remoteSetupEnabled = !!authState.remoteSetupEnabled;
      state.user = authState.user || null;
      state.csrfToken = authState.csrfToken || null;
      hideNotice($('authMessage'));
      renderShell();
    }

    function renderShell() {
      const authed = !!state.user;
      $('authView').classList.toggle('hidden', authed);
      $('appView').classList.toggle('hidden', !authed);
      if (!authed) renderAuth();
      if (authed) renderApp();
    }

    function renderAuth() {
      const unavailable = state.authUnavailable;
      $('authTitle').textContent = unavailable ? '账户服务不可用' : (state.setupRequired ? '创建管理员' : '登录');
      $('authSubmit').textContent = unavailable ? '暂不可用' : (state.setupRequired ? '完成初始化' : '登录');
      $('authStateBadge').textContent = unavailable ? '请检查服务' : (state.setupRequired ? '创建管理员' : '账户登录');
      $('authStateBadge').setAttribute('color', unavailable ? 'danger' : (state.setupRequired ? 'success' : 'informative'));
      $('displayNameField').classList.toggle('hidden', !state.setupRequired || unavailable);
      $('setupTokenField').classList.toggle('hidden', !state.setupRequired || !state.remoteSetupEnabled || unavailable);
      $('authPassword').autocomplete = state.setupRequired ? 'new-password' : 'current-password';
      // A relay may inject the token server-side, so keep this field optional
      // in the browser and let the endpoint validate the header.
      $('authSetupToken').required = false;
      $('authSubmit').disabled = unavailable;
      $('authRetry').classList.toggle('hidden', !unavailable);
      for (const input of $('authForm').querySelectorAll('fluent-text-input, fluent-textarea, fluent-dropdown, fluent-checkbox, fluent-switch, fluent-slider')) input.disabled = unavailable;
    }

    function showAuthUnavailable(message) {
      state.authUnavailable = true;
      state.setupRequired = false;
      state.user = null;
      state.csrfToken = null;
      renderShell();
      showNotice($('authMessage'), message, true);
    }

    async function loadAuthState() {
      $('authRetry').disabled = true;
      try {
        const body = await api('/api/auth/state');
        setAuthState(body.state);
      } catch (error) {
        showAuthUnavailable(error.status === 503
          ? '账户数据库无法读取。请检查数据目录、accounts.json 文件权限和日志，然后重启程序。'
          : '无法读取账户状态。请确认服务正在运行后重试。');
      } finally {
        $('authRetry').disabled = false;
      }
    }

    async function renderApp() {
      await fluentReady;
      await new Promise(resolve => window.requestAnimationFrame(resolve));
      $('userLine').textContent = `${state.user.displayName} · ${state.user.username}`;
      $('currentUserText').textContent = state.user.displayName;
      $('currentRoleText').textContent = state.user.isAdmin ? '管理员' : '普通用户';
      populateThemeDropdowns();
      setControlValue('profileDisplayName', state.user.displayName);
      setControlValue('profileTheme', state.user.theme || 'cyan');
      $('profileThemeSwatch').className = `swatch ${state.user.theme || 'cyan'}`;
      $('adminUsersContent').classList.toggle('hidden', !state.user.isAdmin);
      const accountLink = $('pageLinks').querySelector('[data-page="users"]');
      accountLink.classList.remove('hidden');
      accountLink.textContent = state.user.isAdmin ? '账户与用户' : '账户';
      $('serverStatusBadge').textContent = '读取中';
      try {
        loadTeacherSettings();
      } catch (error) {
        state.settings = normalizeSettings(defaultSettings);
        applySettingsToControls();
      }

      // Switch to the requested page before any status, voice, or user API
      // calls.  Those data requests can finish in the background without
      // holding the visible shell on a half-beat of blank space.
      setPage(state.currentPage);

      try {
        await loadStatus();
      } catch (error) {
        if (error.status === 401) return;
        showNotice($('result'), error.message || '状态加载失败。', true);
      }
      const voicesResult = await Promise.resolve().then(() => loadVoices()).catch(error => error);
      if (voicesResult instanceof Error) {
        if (voicesResult.status === 401) {
          forceLogin();
          return;
        }
        showNotice($('result'), voicesResult.message || '语音列表加载失败。', true);
      }

      if (state.user.isAdmin) {
        try {
          await loadUsers();
        } catch (error) {
          if (error.status === 401) {
            forceLogin();
            return;
          }
          showNotice($('result'), error.message || '用户列表加载失败。', true);
        }
      }

      await loadThemeAvailability();

    }

    async function loadStatus() {
      try {
        const status = await api('/api/status');
        const isRunning = Boolean(status.isRunning ?? status.IsRunning);
        $('serverStatusBadge').textContent = isRunning ? '运行中' : '已停止';
        $('statusPageText').textContent = isRunning ? '运行中' : '已停止';
        $('serverStatusBadge').setAttribute('color', isRunning ? 'success' : 'danger');
        $('portText').textContent = status.port ?? status.Port ?? '-';
        $('speechBackendText').textContent = status.speechBackend ?? status.SpeechBackend ?? '-';
        $('logPathText').textContent = (status.logFilePath ?? status.LogFilePath) || '仅管理员可见';
        $('urlList').innerHTML = '';
        const isLocalOnlyUrl = value => {
          try {
            const hostname = new URL(value).hostname.toLowerCase();
            return hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '[::1]' || hostname === '::1';
          } catch {
            return false;
          }
        };
        const publicUrls = (status.urls ?? status.Urls ?? []).filter(url => !isLocalOnlyUrl(url));
        if (!isLocalOnlyUrl(window.location.origin) && !publicUrls.includes(window.location.origin)) {
          publicUrls.unshift(window.location.origin);
        }
        $('statusAddressNote').textContent = publicUrls.length > 0
          ? '仅显示可从当前网络访问的地址。本机 localhost / 127.0.0.1 地址已隐藏。'
          : '本机 localhost / 127.0.0.1 地址仅供运行软件的电脑使用，已隐藏。远程访问请使用已配置的域名或 FRP 中转地址。';
        $('urlList').classList.toggle('hidden', publicUrls.length === 0);
        for (const url of publicUrls) {
          const li = document.createElement('li');
          const button = document.createElement('fluent-button');
          button.type = 'button';
          button.setAttribute('appearance', 'subtle');
          button.textContent = url;
          button.addEventListener('click', async () => navigator.clipboard?.writeText(url));
          li.appendChild(button);
          $('urlList').appendChild(li);
        }
      } catch (error) {
        if (error.status === 401) {
          forceLogin();
          throw error;
        }
        $('serverStatusBadge').textContent = '状态异常';
        $('statusPageText').textContent = '状态异常';
        $('serverStatusBadge').setAttribute('color', 'danger');
      }
    }

    async function loadVoices() {
      const body = await api('/api/voices');
      state.voices = Array.isArray(body) ? body : (body.voices || body.Voices || []);
      // Keep the built-in choices visible when a remote EdgeTTS voice lookup
      // returns an empty payload. The API still replaces them when available.
      if (!state.voices.length) return;
      const listbox = $('voiceListbox');
      listbox.multiple = false;
      listbox.innerHTML = '';
      for (const voice of state.voices) {
        const option = document.createElement('fluent-option');
        const shortName = voice.shortName ?? voice.ShortName ?? '';
        option.value = shortName;
        option.setAttribute('value', shortName);
        option.textContent = shortName.replace('zh-CN-', '').replace('Neural', '');
        option.selected = shortName === state.settings.voiceName;
        listbox.appendChild(option);
      }
      await new Promise(resolve => window.requestAnimationFrame(() => resolve()));
      const availableVoiceNames = state.voices.map(voice => voice.shortName ?? voice.ShortName ?? '');
      setControlValue('voiceName', state.settings.voiceName);
      if (!availableVoiceNames.includes(state.settings.voiceName)) {
        state.settings.voiceName = availableVoiceNames[0] || defaultSettings.voiceName;
        setControlValue('voiceName', state.settings.voiceName);
        saveTeacherSettings();
      }
    }

    async function loadUsers() {
      const body = await api('/api/users');
      state.users = body.users || [];
      const list = $('usersList');
      list.innerHTML = '';
      const pendingControls = [];
      for (const user of state.users) {
        const row = document.createElement('div');
        row.className = 'user-row';
        row.dataset.username = user.username;
        const isCurrentUser = user.username.toLowerCase() === state.user.username.toLowerCase();
        const adminDisabled = isCurrentUser && user.isAdmin ? ' disabled' : '';
        const enabledDisabled = isCurrentUser ? ' disabled' : '';
        const userTheme = String(user.theme || 'cyan');
        const themeOptions = themeCatalog.map(([value, label]) =>
          `<fluent-option value="${escapeHtml(value)}"${value === userTheme ? ' selected' : ''}>${escapeHtml(label)}</fluent-option>`
        ).join('');
        row.innerHTML = `
          <div class="user-row-info">
            <strong>${escapeHtml(user.displayName)}</strong>
            <div class="subtle">${escapeHtml(user.username)}</div>
            <div class="badges">
              ${user.isAdmin
                ? '<fluent-badge appearance="tint" color="informative" size="small">管理员</fluent-badge>'
                : '<fluent-badge appearance="tint" color="subtle" size="small">普通用户</fluent-badge>'}
              ${user.isEnabled
                ? '<fluent-badge appearance="tint" color="success" size="small">启用</fluent-badge>'
                : '<fluent-badge appearance="tint" color="danger" size="small">禁用</fluent-badge>'}
              <span class="theme-badge" title="此主题色会用于该账户发送的客户端喊话">
                <span class="swatch ${escapeHtml(userTheme)}"></span>${escapeHtml(themeLabel(userTheme))}
              </span>
            </div>
          </div>
          <div class="user-edit-fields">
            <fluent-field label-position="above">
              <label slot="label">显示名称</label>
              <fluent-text-input slot="input" data-field="display-name" type="text" maxlength="48" appearance="outline"></fluent-text-input>
            </fluent-field>
            <fluent-field label-position="above">
              <label slot="label">主题色</label>
              <fluent-dropdown slot="input" data-field="theme" appearance="outline" value="${escapeHtml(userTheme)}">
                <fluent-listbox>
                  ${themeOptions}
                </fluent-listbox>
              </fluent-dropdown>
            </fluent-field>
          </div>
          <div class="row user-row-actions">
            <fluent-button appearance="primary" data-action="save-profile" type="button">保存资料</fluent-button>
            <fluent-button appearance="outline" class="secondary" data-action="toggle-admin" type="button"${adminDisabled}>${isCurrentUser && user.isAdmin ? '当前管理员' : (user.isAdmin ? '取消管理员' : '设为管理员')}</fluent-button>
            <fluent-button appearance="outline" class="secondary" data-action="toggle-enabled" type="button"${enabledDisabled}>${user.isEnabled ? '禁用' : '启用'}</fluent-button>
            <fluent-button appearance="outline" class="danger" data-action="delete" type="button">删除</fluent-button>
          </div>`;
        pendingControls.push({ row, user });
        row.querySelector('[data-action="save-profile"]').addEventListener('click', () => {
          const displayName = String(row.querySelector('[data-field="display-name"]').value || '').trim();
          const theme = readControlValue(row.querySelector('[data-field="theme"]'), user.theme || 'cyan');
          updateUser(user.username, { displayName, theme });
        });
        if (!isCurrentUser || !user.isAdmin) {
          row.querySelector('[data-action="toggle-admin"]').addEventListener('click', () => updateUser(user.username, { isAdmin: !user.isAdmin }));
        }
        if (!isCurrentUser) {
          row.querySelector('[data-action="toggle-enabled"]').addEventListener('click', () => updateUser(user.username, { isEnabled: !user.isEnabled }));
        }
        row.querySelector('[data-action="delete"]').addEventListener('click', () => deleteUser(user.username));
        list.appendChild(row);
      }
      await new Promise(resolve => window.requestAnimationFrame(resolve));
      for (const { row, user } of pendingControls) {
        populateThemeDropdown(row.querySelector('[data-field="theme"]'), user.theme || 'cyan');
        setControlValue(row.querySelector('[data-field="display-name"]'), user.displayName);
      }
    }

    async function loadThemeAvailability() {
      const body = await api('/api/account/themes');
      state.themeAvailability = body.themes || [];
      const availability = new Map(state.themeAvailability.map(item => [item.value, item.available !== false]));
      const ownTheme = state.user?.theme || 'cyan';
      for (const option of document.querySelectorAll('#profileTheme fluent-option')) {
        option.disabled = !availability.get(option.value) && option.value !== ownTheme;
      }
      for (const option of document.querySelectorAll('#newUserTheme fluent-option')) {
        option.disabled = availability.get(option.value) === false || option.value === ownTheme;
      }
      const firstAvailable = themeCatalog.find(([value]) => availability.get(value) !== false)?.[0] || 'cyan';
      if (availability.get(readControlValue('newUserTheme', '')) === false) {
        setControlValue('newUserTheme', firstAvailable);
      }
    }

    async function updateUser(username, payload) {
      try {
        await api(`/api/users/${encodeURIComponent(username)}`, { method: 'PUT', body: JSON.stringify(payload) });
        await loadUsers();
        await loadThemeAvailability();
      } catch (error) {
        showNotice($('usersMessage'), getThemeAwareErrorMessage(error, '用户资料保存失败。'), true);
        if (error?.status === 409) {
          await loadThemeAvailability().catch(() => {});
        }
      }
    }

    async function deleteUser(username) {
      try {
        await api(`/api/users/${encodeURIComponent(username)}`, { method: 'DELETE' });
        await loadUsers();
        await loadThemeAvailability();
      } catch (error) {
        showNotice($('profileMessage'), error.message || '用户删除失败。', true);
      }
    }

    function forceLogin() {
      state.user = null;
      state.csrfToken = null;
      state.currentPage = 'shout';
      renderShell();
    }

    function setPage(page) {
      if (page === 'users' && !state.user) page = 'shout';
      state.currentPage = page;
      const isShout = page === 'shout';
      const isUsers = page === 'users';
      const isStatus = page === 'status';
      const accountOnly = isUsers && !state.user?.isAdmin;
      $('layout').classList.toggle('shout-layout', isShout);
      $('layout').classList.toggle('single-page', isStatus);
      $('layout').classList.toggle('account-only', accountOnly);
      $('detailsAside').classList.toggle('hidden', isShout);
      $('mainContent').classList.toggle('hidden', isStatus || accountOnly);
      const hasFeedbackCard = !!state.feedbackCard;
      if (!isShout) {
        $('shoutCard').classList.remove('card-flight-in', 'card-flight-out');
      }
      $('shoutPanel').classList.toggle('hidden', !isShout || hasFeedbackCard);
      $('shoutFeedbackPanel').classList.toggle('hidden', !isShout || !hasFeedbackCard);
      $('shoutCard').classList.toggle('hidden', isShout && hasFeedbackCard);
      // The account page is available to every signed-in user, but the
      // user-management editor is an administrator-only surface.
      $('usersPanel').classList.toggle('hidden', !isUsers || !state.user?.isAdmin);
      $('statusPanel').classList.toggle('hidden', !isStatus);
      $('accountPanel').classList.toggle('hidden', !isUsers);
      $('contentTitle').textContent = isUsers
        ? (state.user?.isAdmin ? '账户与用户' : '账户')
        : (state.feedbackCard?.title || '喊话');
      document.querySelectorAll('.page-link').forEach(button => {
        const selected = button.dataset.page === page;
        button.classList.toggle('active', selected);
        if (selected) button.setAttribute('aria-current', 'page');
        else button.removeAttribute('aria-current');
      });
    }

    function escapeHtml(value) {
      return String(value).replace(/[&<>"']/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch]));
    }

    function themeLabel(value) {
      return themeCatalog.find(([theme]) => theme === value)?.[1] || value || '未设置';
    }

    function syncRanges() {
      const rate = readNumberControl('speechRate', defaultSettings.speechRate);
      const volume = readNumberControl('speechVolume', defaultSettings.speechVolume);
      syncSliderPosition('speechRate', rate);
      syncSliderPosition('speechVolume', volume);
      $('rateValue').textContent = `${rate > 0 ? '+' : ''}${rate}%`;
      $('volumeValue').textContent = `${Math.round(volume * 100)}%`;
    }

    function syncSliderPosition(id, value) {
      const control = $(id);
      if (!control) return;
      const min = Number(control.min);
      const max = Number(control.max);
      if (!Number.isFinite(min) || !Number.isFinite(max) || max <= min) return;
      const percentage = Math.max(0, Math.min(100, ((Number(value) - min) / (max - min)) * 100));
      // fluent-slider renders its track and thumb in shadow DOM and keeps the
      // two positions in its public `position` style binding. Updating the
      // host variables alone is not enough because the component's own inline
      // style takes precedence inside the shadow tree.
      const position = `--slider-thumb: ${percentage}%; --slider-progress: ${percentage}%`;
      try { control.position = position; } catch (_) { /* keep the CSS fallback below */ }
      try { control.setSliderPosition?.(); } catch (_) { /* public helper is optional */ }
      control.style.setProperty('--slider-thumb', `${percentage}%`);
      control.style.setProperty('--slider-progress', `${percentage}%`);
    }

    function readControlValue(id, fallback = '') {
      const control = typeof id === 'string' ? $(id) : id;
      if (!control) return fallback;
      const value = control.value;
      if (value !== undefined && value !== null && String(value) !== '') return String(value);
      const attributeValue = control.getAttribute?.('value');
      if (attributeValue !== null && attributeValue !== undefined && attributeValue !== '') return String(attributeValue);
      const selected = control.querySelector?.('[selected], [current-selected]');
      const selectedValue = selected?.value ?? selected?.getAttribute?.('value');
      return selectedValue !== undefined && selectedValue !== null && String(selectedValue) !== ''
        ? String(selectedValue)
        : fallback;
    }

    function readNumberControl(id, fallback) {
      const control = typeof id === 'string' ? $(id) : id;
      const valueAsNumber = Number(control?.valueAsNumber);
      if (Number.isFinite(valueAsNumber)) return valueAsNumber;
      const value = Number(readControlValue(id, ''));
      return Number.isFinite(value) ? value : fallback;
    }

    function readCheckedControl(id, fallback = false) {
      const value = (typeof id === 'string' ? $(id) : id)?.checked;
      return typeof value === 'boolean' ? value : fallback;
    }

    function setControlValue(id, value) {
      const control = typeof id === 'string' ? $(id) : id;
      if (!control) return;
      const normalized = String(value);
      if (control.matches?.('fluent-dropdown')) {
        normalizeSingleDropdown(control, normalized);
      }
      try { control.value = normalized; } catch (_) { /* fallback to the attribute below */ }
      if (control.matches?.('fluent-slider')) {
        const numeric = Number(normalized);
        if (Number.isFinite(numeric)) {
          try { control.valueAsNumber = numeric; } catch (_) { /* component may still be upgrading */ }
        }
      }
      if (control.getAttribute?.('value') !== normalized) control.setAttribute('value', normalized);
    }

    function setControlChecked(id, value) {
      const control = typeof id === 'string' ? $(id) : id;
      if (!control) return;
      const checked = Boolean(value);
      try { control.checked = checked; } catch (_) { /* fallback to the attribute below */ }
      if (checked) control.setAttribute('checked', '');
      else control.removeAttribute('checked');
    }

    function getSettingsCookie(username) {
      if (!username) return {};
      const cookieName = `ors_teacher_settings_${encodeURIComponent(username)}`;
      const entry = document.cookie.split('; ').find(item => item.startsWith(`${cookieName}=`));
      if (!entry) return {};
      try { return JSON.parse(decodeURIComponent(entry.slice(entry.indexOf('=') + 1))) || {}; }
      catch (_) { return {}; }
    }

    function saveTeacherSettings() {
      if (!state.user) return;
      const cookieName = `ors_teacher_settings_${encodeURIComponent(state.user.username)}`;
      document.cookie = `${cookieName}=${encodeURIComponent(JSON.stringify(state.settings))}; Max-Age=31536000; Path=/; SameSite=Lax`;
    }

    function applySettingsToControls() {
      const settings = state.settings;
      state.applyingSettings = true;
      try {
        setControlChecked('topmost', settings.topmost);
        setControlChecked('speechEnabled', settings.speechEnabled);
        setControlValue('voiceName', settings.voiceName);
        setControlValue('speechRate', settings.speechRate);
        setControlValue('speechVolume', settings.speechVolume);
        syncRanges();
        window.requestAnimationFrame(syncRanges);
      } finally {
        state.applyingSettings = false;
      }
    }

    function loadTeacherSettings() {
      const saved = state.user ? getSettingsCookie(state.user.username) : null;
      state.settings = normalizeSettings(saved || defaultSettings);
      applySettingsToControls();
    }

    function normalizeSettings(value) {
      const rate = Number(value.speechRate);
      const volume = Number(value.speechVolume);
      return {
        ...defaultSettings,
        mode: 'fullscreen',
        topmost: value.topmost !== false,
        speechEnabled: value.speechEnabled !== false,
        voiceName: typeof value.voiceName === 'string' ? value.voiceName : defaultSettings.voiceName,
        speechRate: Number.isFinite(rate) ? Math.max(-100, Math.min(100, rate)) : defaultSettings.speechRate,
        speechVolume: Number.isFinite(volume) ? Math.max(0, Math.min(1, volume)) : defaultSettings.speechVolume
      };
    }

    function captureTeacherSettings() {
      state.settings = {
        mode: 'fullscreen',
        topmost: readCheckedControl('topmost', defaultSettings.topmost),
        speechEnabled: readCheckedControl('speechEnabled', defaultSettings.speechEnabled),
        voiceName: readControlValue('voiceName', defaultSettings.voiceName),
        speechRate: readNumberControl('speechRate', defaultSettings.speechRate),
        speechVolume: readNumberControl('speechVolume', defaultSettings.speechVolume)
      };
      saveTeacherSettings();
    }

    function importTeacherSettings(value) {
      if (!value || typeof value !== 'object') throw new Error('设置文件格式不正确。');
      state.settings = normalizeSettings(value);
      applySettingsToControls();
      saveTeacherSettings();
    }

    $('authForm').addEventListener('submit', async event => {
      event.preventDefault();
      if (state.authUnavailable) return;
      hideNotice($('authMessage'));
      const username = String($('authUsername').value || '').trim();
      const displayName = String($('authDisplayName').value || '').trim();
      const password = String($('authPassword').value || '');
      if (!username || !password) {
        showNotice($('authMessage'), '请输入用户名和密码。', true);
        return;
      }
      if (state.setupRequired && !displayName) {
        showNotice($('authMessage'), '请输入显示名称。该名称会显示在客户端上。', true);
        return;
      }
      const payload = {
        username,
        displayName,
        password
      };
      const headers = {};
      if (state.setupRequired && state.remoteSetupEnabled && $('authSetupToken').value) {
        headers['X-OpenRemoteShouter-Setup-Token'] = $('authSetupToken').value;
      }
      try {
        const body = await api(state.setupRequired ? '/api/auth/setup' : '/api/auth/login', {
          method: 'POST',
          headers,
          body: JSON.stringify(payload)
        });
        setAuthState(body.state);
        $('authPassword').value = '';
        $('authSetupToken').value = '';
      } catch (error) {
        showNotice($('authMessage'), error.message, true);
      }
    });

    $('authRetry').addEventListener('click', loadAuthState);

    $('logoutButton').addEventListener('click', async () => {
      try {
        await api('/api/auth/logout', { method: 'POST' });
        forceLogin();
      } catch (error) {
        if (error.status === 401) {
          forceLogin();
        } else {
          showNotice($('result'), error.message, true);
        }
      }
    });

    $('refreshButton').addEventListener('click', renderApp);

    $('passwordForm').addEventListener('submit', async event => {
      event.preventDefault();
      try {
        await api('/api/auth/password', {
          method: 'POST',
          body: JSON.stringify({ currentPassword: $('currentPassword').value, newPassword: $('newPassword').value })
        });
        $('currentPassword').value = '';
        $('newPassword').value = '';
        forceLogin();
      } catch (error) {
        alert(error.message);
      }
    });

    $('profileForm').addEventListener('submit', async event => {
      event.preventDefault();
      const displayName = String($('profileDisplayName').value || '').trim();
      const theme = readControlValue('profileTheme', state.user?.theme || 'cyan');
      if (!displayName) {
        showNotice($('profileMessage'), '显示名称不能为空。', true);
        return;
      }
      try {
        const body = await api('/api/account/profile', {
          method: 'PUT',
          body: JSON.stringify({ displayName, theme })
        });
        state.user = body.user;
        $('userLine').textContent = `${state.user.displayName} · ${state.user.username}`;
        $('currentUserText').textContent = state.user.displayName;
        $('profileDisplayName').value = state.user.displayName;
        $('profileThemeSwatch').className = `swatch ${state.user.theme}`;
        showNotice($('profileMessage'), '资料已保存。');
      } catch (error) {
        showNotice($('profileMessage'), getThemeAwareErrorMessage(error, '资料保存失败。'), true);
        if (error?.status === 409) {
          await loadThemeAvailability().catch(() => {});
        }
      }
    });

    $('profileTheme').addEventListener('change', () => {
      $('profileThemeSwatch').className = `swatch ${readControlValue('profileTheme', 'cyan')}`;
    });

    $('shoutForm').addEventListener('submit', async event => {
      event.preventDefault();
      if (state.cardTransitioning) return;
      state.cardTransitioning = true;
      hideNotice($('result'));
      $('sendButton').disabled = true;
      $('sendButton').classList.add('sending');
      $('sendButton').textContent = '发送中...';
      const payload = {
        title: '',
        message: $('message').value,
        mode: 'fullscreen',
        theme: state.user?.theme || 'cyan',
        durationSeconds: 10,
        topmost: readCheckedControl('topmost', defaultSettings.topmost),
        speechEnabled: readCheckedControl('speechEnabled', defaultSettings.speechEnabled),
        voiceName: readControlValue('voiceName', defaultSettings.voiceName),
        speechRate: readNumberControl('speechRate', defaultSettings.speechRate),
        speechVolume: readNumberControl('speechVolume', defaultSettings.speechVolume)
      };
      const request = api('/api/shout', { method: 'POST', body: JSON.stringify(payload) })
        .then(value => ({ ok: true, value }), error => ({ ok: false, error }));
      try {
        await flyShoutCardOut();
        const outcome = await request;
        if (outcome.ok) {
          showFeedbackPage('发送成功', '消息已发送，显示端正在播放。', true);
        } else {
          showComposeCard(outcome.error.message || '发送失败。', true);
          await flyShoutCardIn();
        }
      } finally {
        $('sendButton').disabled = false;
        $('sendButton').classList.remove('sending');
        $('sendButton').textContent = '发送';
        state.cardTransitioning = false;
      }
    });

    $('cancelDisplayButton').addEventListener('click', async () => {
      if (state.cardTransitioning) return;
      state.cardTransitioning = true;
      $('cancelDisplayButton').disabled = true;
      $('cancelDisplayButton').textContent = '撤回中...';
      try {
        await fadeFeedbackPageOut();
        const request = api('/api/close', { method: 'POST' })
          .then(value => ({ ok: true, value }), error => ({ ok: false, error }));
        const outcome = await request;
        if (outcome.ok) {
          showFeedbackPage('已撤回', '显示端已关闭这条消息。', false);
        } else {
          showFeedbackPage('撤回失败', outcome.error.message || '无法撤回当前显示。', true);
        }
      } finally {
        $('cancelDisplayButton').disabled = false;
        $('cancelDisplayButton').textContent = '撤回';
        state.cardTransitioning = false;
      }
    });

    $('continueShoutButton').addEventListener('click', async () => {
      if (state.cardTransitioning) return;
      state.cardTransitioning = true;
      try {
        await fadeFeedbackPageOut();
        showComposeCard();
        await flyShoutCardIn();
      } finally {
        state.cardTransitioning = false;
      }
    });

    $('closeButton').addEventListener('click', async () => {
      try {
        await api('/api/close', { method: 'POST' });
        showNotice($('result'), '已关闭当前显示。');
      } catch (error) {
        showNotice($('result'), error.message, true);
      }
    });

    $('createUserForm').addEventListener('submit', async event => {
      event.preventDefault();
      hideNotice($('usersMessage'));
      const submitButton = $('createUserForm').querySelector('fluent-button[type="submit"]');
      submitButton.disabled = true;
      try {
        const username = readControlValue('newUsernameInput').trim();
        const displayName = readControlValue('newDisplayNameInput').trim();
        const password = readControlValue('newUserPasswordInput');
        const theme = readControlValue('newUserTheme', 'cyan');
        const isAdmin = readCheckedControl('newUserAdminInput', false);
        if (!username || !displayName || !password) {
          showNotice($('usersMessage'), '请填写用户名、显示名称和初始密码。', true);
          return;
        }
        await api('/api/users', {
          method: 'POST',
          body: JSON.stringify({ username, displayName, password, theme, isAdmin })
        });
        setControlValue('newUsernameInput', '');
        setControlValue('newDisplayNameInput', '');
        setControlValue('newUserPasswordInput', '');
        setControlChecked('newUserAdminInput', false);
        await loadUsers();
        await loadThemeAvailability();
        showNotice($('usersMessage'), '用户已创建。');
      } catch (error) {
        showNotice($('usersMessage'), getThemeAwareErrorMessage(error, '用户创建失败。'), true);
        if (error?.status === 409) {
          await loadThemeAvailability().catch(() => {});
        }
      } finally {
        submitButton.disabled = false;
      }
    });

    // Form-associated custom elements do not consistently receive a click
    // from a slotted label in every browser. Keep the visible Fluent label
    // fully clickable without relying on native label forwarding.
    $('newUserAdminLabel').addEventListener('click', event => {
      event.preventDefault();
      $('newUserAdminInput').click();
    });

    document.querySelectorAll('.page-link').forEach(button => {
      button.addEventListener('click', () => setPage(button.dataset.page));
    });

    $('debugToggle').addEventListener('click', () => {
      const body = $('debugSettingsBody');
      const expanded = body.classList.toggle('hidden') === false;
      $('debugToggle').setAttribute('aria-expanded', expanded ? 'true' : 'false');
    });

    for (const id of ['speechRate', 'speechVolume', 'topmost', 'speechEnabled', 'voiceName']) {
      const control = $(id);
      const syncSettings = () => {
        if (state.applyingSettings) return;
        syncRanges();
        captureTeacherSettings();
      };
      control.addEventListener('input', syncSettings);
      control.addEventListener('change', syncSettings);
      control.addEventListener('keyup', syncSettings);
      control.addEventListener('pointerup', () => window.setTimeout(syncSettings, 0));
    }
    $('exportSettingsButton').addEventListener('click', () => {
      captureTeacherSettings();
      const blob = new Blob([JSON.stringify(state.settings, null, 2)], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `openremoteshouter-${state.user?.username || 'teacher'}-settings.json`;
      link.click();
      URL.revokeObjectURL(url);
    });
    $('importSettingsButton').addEventListener('click', () => $('settingsImportFile').click());
    $('settingsImportFile').addEventListener('change', async event => {
      const file = event.target.files?.[0];
      if (!file) return;
      try {
        importTeacherSettings(JSON.parse(await file.text()));
        showNotice($('result'), '设置已导入。');
      } catch (error) {
        showNotice($('result'), error.message || '设置导入失败。', true);
      } finally {
        event.target.value = '';
      }
    });

    // The Fluent module is bundled and loaded immediately before this module.
    // Waiting for the exact registered tag names prevents pre-upgrade property
    // writes from shadowing the components' value and checked accessors.
    (async function boot() {
      try {
        await fluentReady;
        await loadAuthState();
      } catch (error) {
        showAuthUnavailable(error?.message || '页面初始化失败，请刷新后重试。');
      }
    })();
  </script>
</body>
</html>
""";
    }
}
