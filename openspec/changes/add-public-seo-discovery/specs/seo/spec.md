# Public SEO discovery

## Purpose

Make the local public website discoverable without exposing Admin routes or unpublished content.

## Requirements

- The website MUST publish `robots.txt` that allows public routes, disallows `/admin`, and points to `/sitemap.xml`.
- The website MUST publish a sitemap containing stable public routes and only slugs returned by published-only public API list endpoints.
- Sitemap generation MUST tolerate the local API being unavailable by returning the stable public routes instead of failing the website build.
- Root metadata MUST define a configurable site origin, canonical URL and OpenGraph defaults. Published Wiki detail metadata MUST define its canonical URL and article OpenGraph data.

### Scenario: API is available

- **WHEN** sitemap generation reads the public list endpoints.
- **THEN** it includes public module detail URLs and their published timestamps where available.
- **AND** it does not include `/admin` or any admin API path.

### Scenario: API is unavailable

- **WHEN** a local developer builds or requests the sitemap while the API is offline.
- **THEN** sitemap generation still returns the stable public routes.
