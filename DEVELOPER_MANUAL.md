# Developer Manual

## Philosophy And Intent

NoorLocator is not only a directory application. It is a community service platform shaped by a manifesto-driven purpose: no follower of Ahlulbayt (AS) should feel disconnected from their community, no matter where they are in the world.

The system is intentionally designed around trust, clarity, and service.

## Why Moderation Exists

Moderation is a product principle, not an afterthought.

- Religious community data must be trustworthy before it becomes public.
- Center discovery should reduce confusion, not create more of it.
- Contributions are welcomed, but public accuracy must remain protected.

This is why center requests, language suggestions, manager requests, and most community edits flow through review states before affecting public data.

## Why Roles Are Structured This Way

The role model reflects a balance between openness and responsibility.

- Guests can discover public information without friction.
- Registered users can contribute useful community data.
- Managers can maintain official center-owned content such as majalis, announcements, and images.
- Admins moderate, approve, and protect platform integrity.

This balance keeps NoorLocator community-driven without becoming chaotic.

## Why The Language System Is Strict

Languages are intentionally stored as predefined structured data.

- It improves search quality.
- It supports multilingual diaspora communities more reliably.
- It prevents duplicate spellings and inconsistent free-text input.
- It enables future multilingual UI and content expansion.

Free-text language entry would weaken discovery, filtering, and trust, so the system uses a controlled language table with suggestion workflows.

## Why Announcements Are Manager-Controlled

Event announcements are treated as center-owned operational content.

- They come from verified center managers.
- They do not require admin approval for every normal update.
- They still respect ownership boundaries through manager-to-center assignments.
- Admins retain override and deletion power for safety.

This keeps center communication timely while preserving platform governance.

## Identity Layer

Phase 9 introduces a reusable identity layer through seeded `AppContent` records and `/api/content/about`.

That content powers:

- the public About page
- homepage purpose and mission sections
- manifesto-driven messaging in the product experience

The goal is to keep NoorLocator's vision visible both to users and to developers working on future phases.

## Attribution

Driven by موكب خدام أهل البيت (عليهم السلام), Copenhagen, Denmark.
