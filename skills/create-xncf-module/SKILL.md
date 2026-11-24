---
name: create-xncf-module
description: Scaffold a NeuCharFramework XNCF with dotnet new XNCF from the Senparc.Xncf.XncfBuilder.Template NuGet package, generate its GUID and database prefix, align package references with the host, and add it beside Senparc.Web. Use when the user asks to create, initialize, or integrate an XNCF module, including on a published NCF site that has only Senparc.Web and NuGet assemblies.
---

# Create and integrate an XNCF

Generate the project from the `Senparc.Xncf.XncfBuilder.Template` NuGet package in an NCF site that contains `Senparc.Web`. Do not use this repo's `tools/` or `src/` trees as the template or base-library source. Naming, GUID, database prefix, and package versions come from the script. Post-generation cleanup: [create-and-integrate.md](../develop-xncf-modules/references/create-and-integrate.md).

If the user only wants a plan, do not pass `--execute` or `--install-template`.

## 1. Confirm the host

`--root` is the **NCF site root**, not the package source repo. The site must contain `Senparc.Web/Senparc.Web.csproj`. A published site with only the Web project and NuGet assemblies is normal.

Decide:

- `OrgName`, `XncfName`: match `[A-Za-z][A-Za-z0-9]*`. Project name is `<OrgName>.Xncf.<XncfName>`.
- One-line description, menu name, icon.
- Capability flags: `--web`, `--database`, `--function`, `--webapi`. Do not enable `--sample` by default.
- Starting version defaults to `0.1.0`.
- Place the new project beside `Senparc.Web`; the script resolves paths from the host it finds.

For multiple modules, use [develop-xncf-modules](../develop-xncf-modules/SKILL.md) for boundary decisions first.

## 2. Generation plan

```bash
python3 skills/develop-xncf-modules/scripts/prepare_xncf.py \
  --root <NCF-site-root> \
  --org Senparc \
  --name Ordering \
  --description "Order lifecycle" \
  --web --database
```

Stdout is JSON. Use its `packages`, `templateInstall`, and `command`; do not invent paths.

- `packages` prefers versions already referenced or restored on the host `Senparc.Web`; query nuget.org only for missing Senparc packages.
- `templateInstall` installs `Senparc.Xncf.XncfBuilder.Template`. Do not substitute a local template path.
- Stop on `collisions`. Do not use the all-zero GUID or `WrongGuid`.
- If `Senparc.Web.csproj` is missing, the script requires `--output`. Confirm `--root` is the site root.

## 3. Install template and generate

After the user approves creation:

```bash
python3 skills/develop-xncf-modules/scripts/prepare_xncf.py \
  --root <NCF-site-root> \
  --org Senparc \
  --name Ordering \
  --description "Order lifecycle" \
  --web --database \
  --execute
```

`--execute` runs `templateInstall` first when the `XNCF` template is not on the machine. This updates the local `dotnet new` cache and downloads the template from NuGet.

The output directory must be new.

## 4. Immediate post-generation checks

```bash
rg -n "Template_|WrongGuid" <new-project-dir> -g '!**/bin/**' -g '!**/obj/**'
```

Then follow [create-and-integrate.md](../develop-xncf-modules/references/create-and-integrate.md):

1. Set csproj `<Version>` to match `Register.Version` and replace release notes with this module's genesis entry.
2. When database is enabled, replace the Color sample, including seed data, dead menus, and uninstall-delete-table demos.
3. Turn `ProjectReference` entries that point outside the site into `PackageReference` entries from JSON `packages`. Remove non-existent `OutputPath` and icon paths.
4. Add the project to the host solution's `XncfModules` and `dotnet add` it to `host.webProject`.
5. Keep template-generated `<ModuleName>Resource.cs` and all seven `.resx` files (neutral, `zh-CN`, `en`, `es`, `fr`, `ja`, `ru`). User-visible text must use resources—do not revert to hard-coded strings during cleanup. See [develop-and-maintain.md](../develop-xncf-modules/references/develop-and-maintain.md#%E6%9C%AC%E5%9C%B0%E5%8C%96).

## 5. Validation

```bash
dotnet restore <new-module.csproj>
dotnet build <new-module.csproj> --no-restore
dotnet build <host.webProject> --no-restore
```

Report separately: template install, project generation, package reference fixes, module compile, host compile. Generation success does not mean the module is installed in the site or that migrations ran.

Also verify localization: equal key sets across seven resource files; module name, menu, and Function metadata resolve in Chinese and English; new script strings go through the module's localization partial included from Razor.