1. Courierly's merchant portal parses { "error": "..." } from 404 responses and { "message", "code" } from 409s. You want RFC 9457 Problem Details on every error. How do you ship that without breaking the portal? Name the steps in order.

   These are the steps I would take to introduce RFC 9457 Problem Details on every error without breaking the portal:
   1. I will determine every error responses the API are using first.
   2. I will create tests to verify those determined error responses are all working as is.
   3. Start adding code to implement RFC 9457 while still preserving the existing error message, meaning both error messaging are now being returned to the response. Adjust the tests accordingly on the expected error output.
   4. Start migrating the usage from old error response to the new 1 error at a time.

2. A Service Bus-triggered Function updates a shipment and then calls a merchant callback. What three things make it safe when the same message is delivered twice? What happens to the message if the handler throws, versus if it catches the exception and only logs it?

   1 that I can think of would be adding and idempotency id to identify duplicate deliveries. If the handler throws, I believe azure functions considers the invocation as failed. If there is a try catch and only logs the exception, azure function will consider it as a successful invocation.

3. Two status messages for the same shipment arrive out of order (Delivered, then an older InTransit). Name two different mechanisms that stop the older one from winning, and one trade-off of each.

   The use of adding a sequence indicator could prevent older status to be processed. Tradeoff could be that the reliability or tracking of the correct sequence to be use will a bit difficult to handle. There should also be a validation that the incoming status is an allowed transistion. Tradeoff could be not so reliable in handling stale events.

4. A carrier partner calls your webhook endpoint. How do you prove a request really came from that partner, and how do you stop someone replaying a valid request they captured?

   Implement a 1-time authentication logic.

5. A signed-in user of tenant A changes /api/v1/shipments/1042 to /api/v1/shipments/1043, which belongs to tenant B. Where exactly is that stopped in an ASP.NET Core + EF Core API, and what automated test proves it?

   I would validate the authenticated users identity context (like JWT token) extract its tenant id (or all allowed tenant id) then check it to the database if the user is allowed to access that specific tenant's data. If the 1043 shipment belongs to the tenant the the user can access, then the system would allow the user to access it.

6. Where do the SQL connection string, the carrier webhook secret and the merchant API keys live (a) on your dev machine and (b) in Azure? Name two things that must never appear in logs.

   In my dev machine, I will add it as environment variable. In azure, I would store it in Azure Key Vault
