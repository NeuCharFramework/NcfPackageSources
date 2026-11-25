## Template_OrgName.Xncf.Template_XncfName

> XNCF template project used to scaffold modules that embed into NeuCharFramework (NCF).

Template_Description

## EventBus internal round-trip check

After you enable the sample Function, you can run the **EventBus internal round-trip check**. It uses
`IEventBusRequestClient` inside the current XNCF process to send a request, handle it, produce a correlated response, and wait for completion—verifying that the host EventBus registration and assembly scanning work as expected.

This check does not read the database, configuration, files, user profiles, or other modules. It uses a 5-second timeout and does not add HTTP, SSE, or other public endpoints. EventBus is an in-process mechanism and must not be treated as a security boundary between modules.