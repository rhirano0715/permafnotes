# Permafnotes

Permafnotes is a personal note management application built with Blazor WebAssembly. You can choose Microsoft Graph, local files, or a REST API as the backend. The app supports tagging as well as import and export functions.

## Structure

- **src/permafnotes** — Blazor WebAssembly frontend using AntDesign for the UI.
- **src/PermafnotesDomain** — Domain models and services for notes and tags.
- **src/PermafnotesRepositoryByFile** — Repository implementation that saves notes on Microsoft Graph or the local file system.
- **src/PermafnotesRepositoryByApi** — Repository implementation that communicates with an external Web API.
- **src/*Test** — NUnit unit tests.
- **scripts/** — PowerShell scripts for tasks like generating appsettings.
- **sql/** — Sample SQL scripts for creating tables and triggers.

## Build

Run the following with the .NET 8 SDK installed:

```bash
dotnet build src/permafnotes.sln
```

For development you can start the app with `dotnet run --project src/permafnotes`. Azure AD settings and the API base URI are configured via `appsettings.json` or environment variables. The script `scripts/create_appsettings.ps1` can generate a suitable configuration file.

## Test

```bash
dotnet test src/permafnotes.sln
```

## License

This repository is released under the MIT License.
