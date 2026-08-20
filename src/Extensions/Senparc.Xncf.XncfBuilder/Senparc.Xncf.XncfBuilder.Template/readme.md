# Senparc.Xncf.XncfBuilder.Template

`Senparc.Xncf.XncfBuilder.Template` is the .NET template package used to scaffold an NCF XNCF module.

## Features

- Creates a ready-to-customize XNCF project with registration, Areas, application services, optional database entities, and migration folders.
- Replaces organization and module-name placeholders throughout the generated project.
- Includes template-local guidance files for ACL, domain, OHS, and database conventions.
- Works with the `Senparc.Xncf.XncfBuilder` module for in-application generation workflows.

## Installation and usage

```bash
dotnet new install Senparc.Xncf.XncfBuilder.Template
dotnet new xncf -n MyProject --force --UseDatabase true
```

Use `dotnet new uninstall Senparc.Xncf.XncfBuilder.Template` to remove the installed template package.

## Key template contract

- Template short name: `xncf`.
- `--UseDatabase true` includes the database model and migration structure.
- Organization and module placeholders are replaced during template instantiation; review namespaces, package IDs, UIDs, and database names after generation.

The package is a scaffolding artifact, not a runtime module. Generated code must be reviewed, tested, and secured before publication.
