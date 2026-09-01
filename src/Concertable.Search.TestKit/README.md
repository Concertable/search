# Concertable.Search.TestKit

Black-box HTTP helpers for tests that need to observe Search projection convergence through the public
autocomplete API. The package contains no Search runtime, persistence, repository, or Aspire implementation
and does not provision dependencies.

Construct `SearchTestClient` with an `HttpClient` whose base address is the Search service root. Consumers can
read autocomplete results directly or wait for a known artist, venue, or concert projection to become
observable by its producer-owned ID and name.
