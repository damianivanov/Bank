# Bank Client

React + TypeScript + Vite frontend for the banking operations application.

Generated backend contracts come from `server/Bank.Web` through Reinforced.Typings:

```powershell
dotnet build .\server\Bank.Web\Bank.Web.csproj
cd .\client
npm run process-types
```
