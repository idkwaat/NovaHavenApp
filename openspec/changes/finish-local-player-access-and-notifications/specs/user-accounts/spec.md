# User accounts

## Requirements

- Registration accepts a valid unique email and password, creates a usable standard user immediately, and never assigns the Admin role.
- Email confirmation and email delivery are not prerequisites for registration or sign-in. Existing unconfirmed accounts remain able to sign in.
- Registration, login and logout mutations require antiforgery validation and use the existing HttpOnly Identity cookie; browser localStorage must not contain a session token.
- An authenticated caller can retrieve only their own identity summary. Admin APIs remain restricted to the Admin role.
- Password policy, lockout behavior, UUID identity and PostgreSQL persistence remain consistent with the existing Identity configuration.

## Scenarios

### Scenario: Register and sign in without mail

- **WHEN** a visitor submits a valid unused email and password with a valid CSRF token
- **THEN** the API creates a non-admin account that can sign in immediately without sending email

### Scenario: Existing unconfirmed account signs in

- **WHEN** an existing account has a valid password but `EmailConfirmed` is false
- **THEN** sign-in is not blocked solely because the email is unconfirmed

### Scenario: Invalid credentials and CSRF

- **WHEN** a request has invalid credentials or lacks a valid CSRF token for a mutation
- **THEN** the API rejects it without issuing an authenticated cookie
