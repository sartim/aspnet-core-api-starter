# Email actions

The full `user-service` profile includes optional password-reset and email
verification flows. The endpoints are safe to enable before selecting an email
provider: requests are accepted, tokens are stored hashed and expire after one
hour, and no message is delivered until `EMAIL_ACTION_BASE_URL` is configured.

## Provider boundary

`IEmailSender` is the provider-neutral seam. The default `NullEmailSender`
records that an action was requested without logging the token or link. Replace
it in the generated application's dependency injection setup with an SMTP,
transactional email, or platform mail adapter:

```csharp
builder.Services.AddSingleton<IEmailSender, YourEmailSender>();
```

Set the public frontend URL used to build links:

```dotenv
EMAIL_ACTION_BASE_URL=https://app.example.com/account
```

## Endpoints

- `POST /api/v1/auth/password-reset/request` accepts `{ "email": "..." }` and
  always returns `202 Accepted` to avoid account enumeration.
- `POST /api/v1/auth/password-reset/confirm` accepts a token and a new password.
- `POST /api/v1/auth/email-verification/request` requires authentication.
- `POST /api/v1/auth/email-verification/confirm` accepts a verification token.

Tokens are single-use, randomly generated, SHA-256 hashed at rest, and never
returned by the API. A provider should also apply delivery rate limits and
avoid including sensitive account details in email content.
