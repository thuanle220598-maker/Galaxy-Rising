# P0.7 UI/UX Presentation Pass Design

## Goal

Turn the existing portrait vertical slice from functional prototype UI into a coherent pixel sci-fi command deck without changing combat, progression, economy, save data, or navigation flow.

## Visual Direction

- Keep the existing dark space arena and code-native pixel art.
- Use cyan for information, amber for rewards and primary actions, red for danger, and teal for progression.
- Replace flat boxes with framed panels, accent rails, layered backgrounds, resource chips, portraits, and meters.
- Keep text uppercase and compact. Important actions must be readable within a few seconds.
- Use native Unity UI only. Do not add packages or imported production art.

## Battle/Home

- Preserve the current vertical order: stage header, battlefield, hero bar, primary action, navigation.
- Split the resource summary into readable chips for Power, Gold, XP, Materials, and Tickets.
- Turn each hero entry into a portrait card with selection state, HP meter, and energy meter.
- Strengthen the challenge action and combat status hierarchy without covering the arena.
- Add subtle code-generated stars, rails, borders, shadows, and button press states.

## Secondary Screens

- Squad, Heroes, Summon, and Activities reuse the same frame, header, panel, and button language.
- Preserve every existing action and layout constraint.
- Hero and squad choices use rarity/selection accents and clearer grouping.
- Summon keeps the existing rates and history while presenting the action area as the visual focus.

## Interaction

- Rebuilt screens remain fully opaque; no whole-screen fade may reveal Battle/Home.
- Buttons use native color-tint states for hover, press, selected, and disabled feedback.
- Primary actions may pulse subtly, but navigation and content must remain stable.
- Touch targets remain at least 44 design pixels.

## Constraints

- Reference resolution remains `360 x 640`, portrait `9:16`.
- Safe-area behavior remains unchanged.
- No new gameplay systems, currencies, save fields, packages, or asset-import pipeline.
- Keep implementation in the existing runtime UI flow; add only small helpers needed by multiple screens.
- Run Unity once after code and tests are complete.

## Acceptance Criteria

- Battle/Home visibly contains resource chips, five portrait hero cards, HP/energy meters, framed sections, and responsive button states.
- Secondary screens use the same visual language instead of plain full-screen boxes.
- Opening or rebuilding any overlay never exposes the Battle/Home screen underneath.
- Existing onboarding, save, stage, safe-area, and soak behavior remains intact.
- The combined Play Mode suite passes.
