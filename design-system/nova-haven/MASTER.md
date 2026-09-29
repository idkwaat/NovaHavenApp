# Nova Haven — UI Design System

Status: active local UI baseline  
Updated: 2026-09-28

This file records the owner-approved homepage direction and the shared editorial system for the public site. It is a **manual baseline**, not a generated design-system artifact. UI/UX Pro Max local searches were run on 2026-09-28; the generic blue/Swiss recommendations did not fit Nova, so only the useful accessibility, responsive and typography guidance was applied.

## Product and art direction

Nova Haven is a Vietnamese Minecraft RPG world and community portal. The public site should feel like a lived-in game world with editorial content, not a generic SaaS dashboard or bright NPC menu. Keep the homepage's landscape photograph, centered timber Nova sign, clear Vietnamese heading, server address, and two real destinations. Grade the photo toward muted soil/olive tones without replacing it; the wordmark green should be restrained rather than neon. Credit Einar Storsul / Unsplash in the footer. Credit the separate News Minecraft image to Xbox México / CC BY 3.0. Use layered dark soil and timber surfaces, warm readable cream text, olive as the primary accent, muted teal as a secondary accent, and restrained copper/harvest details. Wiki, item catalog and news may vary subtly in hue, but the whole system remains dark and earthy. Do not use generated art, Wynncraft-owned assets/logos, invented gameplay claims, or generic gradient/glass dashboard styling.

Keep the current page compositions and component conventions. This baseline guides polish and new UI; it does not authorize a wholesale visual redesign or behavior/API changes.

## Color tokens

Use the CSS custom properties in `apps/web/app/wynn-parity.css` rather than introducing near-duplicate values:

| Role | Token | Value |
| --- | --- | --- |
| Main text on dark surfaces | `--nh-text` / `--nh-ink` | `#efe2ca` |
| Secondary text on dark surfaces | `--nh-muted` | `#d4c3a7` |
| Dark soil / reading surface | `--nh-paper` | `#30271f` |
| Raised timber card surface | `--nh-paper-light` | `#3b3026` |
| Muted olive accent | `--nh-green` | `#9a9b70` |
| Deep olive accent | `--nh-green-dark` | `#83845d` |
| Olive CTA | `--nh-cta-green` | `#5c6845` |
| Muted teal CTA | `--nh-cta-blue` | `#4b6159` |
| Soft teal accent | `--nh-blue` | `#91a499` |
| Timber outline | `--nh-line` | `#80664a` |
| Deep soil frame | `--nh-forest` | `#211d19` |
| Olive-brown / library surface | `--nh-sage` | `#312b22` |
| Teal-brown / news surface | `--nh-sky` | `#332d26` |
| Secondary text on dark surfaces | `--nh-page-muted` | `#cbbb9f` |
| Copper editorial accent | `--nh-copper` | `#c18d64` |
| Harvest-gold accent | `--nh-harvest` | `#d1ae71` |
| Muted berry/rust accent | `--nh-berry` | `#bf816a` |

The toolbar and footer use deep soil with warm timber outlines. Cards and filters use adjacent but distinguishable dark-brown/olive/teal surfaces; harvest and rust are accents, not large luminous blocks. Keep text/background contrast at WCAG AA (4.5:1 for normal text), including muted text and hover states. Avoid neon green, bright blue, full-screen near-black slabs with no tonal variation, or decorative gold as small body text.

## Type and content

- UI and Vietnamese body copy: Arial with Segoe UI/Tahoma fallbacks.
- Editorial serif is reserved for existing tile/aside headings; Impact/Arial Black is reserved for the Nova sign wordmark.
- Keep Vietnamese diacritics intact; avoid synthetic font styles and global smoothing hacks.
- Body text should be at least 16px with line-height at least 1.5. Keep labels and metadata at 12px or larger.
- Use sentence case for Vietnamese content. Keep overlines short and secondary to the page title.

## Layout and interaction

- Preserve the 1320px home/editorial band and the 1240px public-library maximum; detail reading columns stay near 1000px. Keep page-specific compositions and collapse filters/feed layouts at tablet widths; verify the existing 900px / 760px / 600px / 460px / 380px breakpoints.
- At narrow widths, collapse multi-column content without horizontal page scrolling; the home hero sign, title, server line, and actions must remain legible and reachable.
- Interactive controls and links need at least a 44×44px target where practical, visible keyboard focus, and a clear hover/active state. Do not rely on hover alone.
- Respect `prefers-reduced-motion`; motion should clarify interaction, not decorate every element.
- Icon-only controls require an accessible name. Keep the navigation drawer/backdrop behavior and the server address centered in the toolbar.

## Verification checklist

For public UI changes, check: text contrast; focus visibility and keyboard operation; 44px touch targets; Vietnamese wrapping; 320–390px mobile layout without horizontal overflow; desktop hierarchy; reduced-motion handling; API-unavailable/empty states; and image attribution. Run the relevant Node tests and Next.js typecheck. Treat browser screenshots as visual evidence, not as a substitute for behavior tests.

## UI/UX Pro Max review

The local `ui-ux-pro-max` skill is available. The 2026-09-28 design-system search returned a generic Swiss/blue palette, which was rejected because it conflicts with the approved Minecraft-world direction. The applicable search findings—visible keyboard focus, 44px controls, readable Vietnamese type, responsive image/layout handling and reduced motion—are retained here. Wynncraft's current public homepage was inspected as a visual reference; use broad composition cues only and never copy its brand or artwork.
