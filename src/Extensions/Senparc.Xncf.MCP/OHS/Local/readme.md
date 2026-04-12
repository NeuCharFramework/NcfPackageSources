[中文版](readme.cn.md)

# Local branch

The Local branch handles communication between modules or domains within the same system. It exposes interfaces for collaboration while keeping components decoupled and extensible. Local typically runs inside the same system or service without crossing system boundaries.

In Local, `AppService` handles inter-module communication. NCF uses Dynamic WebApi to expose `AppService` methods as Web API endpoints automatically, so you do not need to hand-write Web API services in the Remote branch.
