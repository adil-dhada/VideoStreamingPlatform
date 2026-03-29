# Identity Microservice

## Role
Manages User registration, login verification, and JSON Web Token (JWT) provisioning.

## Sub-Projects
Based on Clean Architecture:
- `Identity.Domain`: The User aggregate and specific Password hashing domain definitions.
- `Identity.Application`: Handlers for `RegisterUserCommand`, `LoginUserCommand`, formatting outputs.
- `Identity.Infrastructure`: Database mappings to SQL Server for Identity DbContext and BCrypt hashing implementations.
- `Identity.API`: The HTTP entrypoint controller containing REST endpoints.

## Impact on Solution
This is the core security cornerstone. It issues stateless 15-minute JWT tokens used by the Gateway or downstream APIs to identify requests instantly without bouncing back to a database every time.
