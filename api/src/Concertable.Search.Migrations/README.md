# Concertable.Search.Migrations

This executable applies Search's projection migrations and the pinned platform messaging inbox migrations
to `SearchDb`, then exits. Set `ConnectionStrings__SearchDb` to the target PostgreSQL connection string.

The job is idempotent. Runtime startup migration remains in place until the standalone AppHost and deployment
topology invoke this artifact before Search Web and Workers start.
