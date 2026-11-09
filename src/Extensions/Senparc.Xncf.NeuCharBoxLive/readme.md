## Senparc.Xncf.NeuCharBoxLive

NeuCharBox live AI demo: invite-only live sessions, as an XNCF module of the NCF host (`src/Senparc.Web`).

- `Register.cs` registers the live API (`NcbLive.Web`'s `AddNcbLive`) with `EfLiveStore` and the NeuChar chat client,
  plus `RetentionService` (visitor text blanked after 30 days).
- `Domain/Models/DatabaseModel`: the three tables (`Senparc_NeuCharBoxLive_InviteCode`, `_Application`, `_Run`).
- `Domain/Migrations/Sqlite`, `Domain/Migrations/SqlServer`: the `Init` migrations. Other databases: see `交接说明.md`.
- `Areas/Admin/Pages/NeuCharBoxLive`: the admin pages (Applications, Codes, Runs), inside the NCF admin.

Scaffolded with `dotnet new XNCF` (Senparc.Xncf.XncfBuilder.Template 0.14.0), then trimmed: no Color sample, no
template migrations, no Dm/MySql/Oracle/PostgreSQL contexts.
