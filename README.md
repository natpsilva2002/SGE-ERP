# SGE-ERP

ERP web para gestão de compras de materiais e contratação de serviços.

## Stack

- Backend: .NET 10, ASP.NET Core Web API, Entity Framework Core e PostgreSQL
- Frontend: Angular e TypeScript

## Estrutura

- `backend/SGE.API` — API HTTP
- `backend/SGE.Application` — casos de uso e DTOs
- `backend/SGE.Domain` — entidades e regras de domínio
- `backend/SGE.Infrastructure` — integrações e serviços técnicos
- `backend/SGE.Persistence` — contexto, configurações, repositories e migrations
- `frontend` — aplicação Angular

## Execução local

1. Configure o PostgreSQL e copie `backend/SGE.API/appsettings.example.json` para `appsettings.json`, preenchendo os valores locais.
2. Execute a API:

   ```powershell
   dotnet run --project backend/SGE.API --launch-profile http
   ```

3. Em outro terminal, execute o frontend:

   ```powershell
   cd frontend
   npm install
   npm start
   ```

O frontend fica disponível em `http://localhost:4200` e a API em `http://localhost:5261`.
