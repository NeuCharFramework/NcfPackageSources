# Anti-Corruption Layer (ACL)

In the NCF system, we use the Anti-Corruption Layer (ACL) pattern from Domain-Driven Design (DDD) to ensure smooth interaction with external systems. The ACL is responsible for data transformation, providing adapters, and isolating changes, thereby protecting our core domain logic from negative impacts of other contexts.

By using the Anti-Corruption Layer, we can ensure that different parts of the system remain independent, avoiding tight coupling while improving code maintainability and extensibility. Typically, the ACL contains a series of decoupled entities that conform to Anti-Corruption (AC) standards, as well as Repository classes, etc.