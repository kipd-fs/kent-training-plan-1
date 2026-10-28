# API consumers (as of the start of this plan)

These clients call the Courierly API in production today. Their parsing code is not ours to change on our schedule. Any change to response shapes must keep them working until they move to the new contract.

| Consumer | Owner | Calls | What it parses today |
|---|---|---|---|
| Merchant portal (web) | Merchant Experience team | `GET /api/shipments/{id}`, `GET /api/shipments`, `POST /api/shipments`, `POST /api/shipments/{id}/cancel` | On 404 reads `body.error`. On 409 reads `body.message` and switches on `body.code` (`DUPLICATE_REFERENCE`, `INVALID_STATE`). Any other non-2xx shows a generic error. |
| Rider mobile app (Android) | Mobile team, releases every 3–4 weeks through the Play Store | `GET /api/shipments`, `POST /api/shipments/{id}/cancel` | On 400 displays the raw response body as text. Older app versions stay installed for months. |
| Partner checkout plugin | External partner, Shopify app | `POST /api/quotes` | Reads `fee` from 200 responses. On 400 logs the raw body. |
| Carrier partners | Carrier integrations | `POST /api/webhooks/carrier` | Only looks at the status code. Retries on any non-2xx for up to 24 hours. |

Contacts: the merchant portal and mobile teams need two sprints' notice (four weeks) for a breaking change. The external partner needs 90 days.
