# Anti-Corruption Layer

In NeuCharFramework (NCF), we use the Domain-Driven Design (DDD) Anti-Corruption Layer (ACL) pattern to integrate safely with external systems. The ACL translates data, provides adapters, and isolates change so core domain logic is not affected by foreign bounded contexts.

An ACL keeps parts of the system independent, reduces tight coupling, and improves maintainability and extensibility. It typically contains anti-corruption entities and repository types that enforce the ACL contract.