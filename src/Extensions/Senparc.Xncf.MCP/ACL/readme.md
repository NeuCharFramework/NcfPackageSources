# Anti-Corruption Layer
  
In the NCF system, we use the Anti-Corruption Layer (ACL) pattern of Domain-Driven Design (DDD) to ensure smooth interaction with external systems. The anti-corruption layer is responsible for transforming data, providing adapters, and isolating changes, thus protecting our core domain logic from negative impacts in other contexts.
  
By using an anti-corrosion layer, we can ensure that various parts of the system remain independent and avoid tight coupling, while improving the maintainability and scalability of the code.
Generally speaking, ACL will contain a series of decoupled entities that comply with anti-corruption (AC) standards, as well as Repository classes, etc.