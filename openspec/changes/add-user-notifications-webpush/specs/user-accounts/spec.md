# User accounts

## Requirements

- Registration accepts a valid email and password, creates an unconfirmed standard user, and never assigns the Admin role.
- Confirmation requires a valid Identity email-confirmation token. Sign-in is refused until the address is confirmed.
- Login and logout mutations require antiforgery validation and use the existing HttpOnly Identity cookie; no browser localStorage session token is allowed.
- An authenticated caller can retrieve only their own identity summary. Admin APIs remain restricted to the Admin role.
- Development without configured SMTP exposes confirmation messages only through a Development-only local outbox. Production must not expose this endpoint or silently claim a message was delivered.
