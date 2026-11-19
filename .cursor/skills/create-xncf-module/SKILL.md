---
name: create-xncf-module
description: Scaffold a NeuCharFramework XNCF with dotnet new XNCF from the Senparc.Xncf.XncfBuilder.Template NuGet package, generate its GUID and database prefix, align package references with the host, and add it beside Senparc.Web. Use when the user asks to create, initialize, or integrate an XNCF module, including on a published NCF site that has only Senparc.Web and NuGet assemblies.
---

# Create and Integrate an XNCF

Read and follow immediately:

- `skills/create-xncf-module/SKILL.md`
- `skills/develop-xncf-modules/references/create-and-integrate.md`

Use `skills/develop-xncf-modules/scripts/prepare_xncf.py` to generate the GUID and commands. Obtain the template and base libraries from NuGet; do not use directories from the source repository, and never use an all-zero GUID.
