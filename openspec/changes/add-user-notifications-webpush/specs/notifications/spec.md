# Notifications and Web Push

## Requirements

- An authenticated user can page through their own notifications, mark one read and mark all read. Another user's notification is indistinguishable from not found.
- Admin can create a validated announcement for confirmed users; it is persisted for each recipient before optional push is attempted.
- A user can register/remove only their own valid Web Push subscription. Subscription endpoints must be HTTPS and constrained to supported public push-service hosts.
- Web Push is reported as unavailable unless the server has valid VAPID configuration. When configured, the API sends encrypted payloads; expired subscriptions are removed and delivery failures do not remove inbox records.
- Next.js supports account sign-in/registration/confirmation, notification inbox and user-initiated browser push permission. Flutter supports account sign-in/registration and the same notification inbox; native push is not claimed without FCM setup.
- Notification payloads contain only a short title/body and same-site destination; no account secret or private profile data is sent in push payloads.
