# Preparação do SGE-ERP para Railway

Esta configuração prepara o monorepo, mas não cria serviços, não publica deploy e não aplica migrations.

## Serviços e build context

Crie três serviços no mesmo projeto/ambiente Railway: PostgreSQL, API e frontend. Para os serviços de API e frontend, mantenha o diretório raiz do repositório como source/build context (não defina `/backend` ou `/frontend` como root directory). Configure o Dockerfile de cada serviço:

- API: `backend/Dockerfile`
- Frontend: `frontend/Dockerfile`

O Railway detecta Dockerfiles na raiz da fonte ou aceita caminho configurado por serviço. O monorepo precisa do root context porque ambos os Dockerfiles copiam seus arquivos a partir dele. Veja [Dockerfiles Railway](https://docs.railway.com/builds/dockerfiles) e [Monorepos Railway](https://docs.railway.com/deployments/monorepo).

## Variáveis

Cadastre as variáveis de API no serviço backend. Prefira a variável interna `DATABASE_URL` do Postgres por referência Railway para manter o tráfego privado entre serviços. A API também aceita `ConnectionStrings__DefaultConnection` ou as variáveis `PGHOST`, `PGPORT`, `PGUSER`, `PGPASSWORD` e `PGDATABASE`.

O frontend roda no navegador do usuário, então `API_URL` deve apontar para o domínio **público HTTPS** da API, terminado em `/api`. Configure `Cors__AllowedOrigins` com o domínio público HTTPS do frontend, sem caminho. A política de produção não usa origem curinga.

Configure um bucket S3-compatible privado para os anexos em produção. A aplicação suporta Cloudflare R2 e Railway Bucket por meio do endpoint S3 e mantém o armazenamento local em Development. Não configure credenciais reais em arquivos versionados.

| Serviço | Variável | Valor |
| --- | --- | --- |
| API | `DATABASE_URL` | Referência Railway: `${{Postgres.DATABASE_URL}}` (ajuste o nome do serviço Postgres) |
| API | `Jwt__Key` | Segredo aleatório próprio, mínimo 32 bytes; não reutilizar a chave local |
| API | `Jwt__Issuer` | `SGE.API` |
| API | `Jwt__Audience` | `SGE.Client` |
| API | `Jwt__ExpirationMinutes` | `60` (ou política aprovada) |
| API | `Cors__AllowedOrigins` | `https://<dominio-publico-do-frontend>`; origens múltiplas separadas por vírgula |
| API | `Storage__Provider` | `S3` |
| API | `Storage__Endpoint` | Endpoint S3 HTTPS do provedor (R2: `https://<account-id>.r2.cloudflarestorage.com`) |
| API | `Storage__Bucket` | Nome do bucket privado |
| API | `Storage__AccessKey` | Credencial secreta com acesso somente ao bucket necessário |
| API | `Storage__SecretKey` | Segredo correspondente, cadastrado como variável secreta |
| API | `Storage__Region` | Região do bucket; para Cloudflare R2, `auto` |
| API | `Storage__UsePathStyle` | `true` para R2 e endpoints compatíveis; ajuste conforme o provedor |
| API | `BootstrapAdmin__Email` | E-mail do primeiro Administrador |
| API | `BootstrapAdmin__Password` | Senha inicial única com ao menos 16 caracteres |
| Frontend | `API_URL` | `https://<dominio-publico-da-api>/api` |

`PORT` é fornecida pela Railway. O backend Kestrel e o Nginx do frontend escutam essa porta; não cadastre um valor fixo para ela. Configure o healthcheck da API como `/health`. Railway fornece variáveis e referências entre serviços: [variáveis](https://docs.railway.com/variables) e [PostgreSQL](https://docs.railway.com/databases/postgresql).

Para um banco local novo sem Administrador inicial, use User Secrets em Development (não salve a senha em `appsettings*.json` nem `.env` rastreado):

```powershell
dotnet user-secrets init --project backend/SGE.API
dotnet user-secrets set "BootstrapAdmin:Email" "admin@seu-dominio.local" --project backend/SGE.API
dotnet user-secrets set "BootstrapAdmin:Password" "<senha-local-unica-com-16-ou-mais-caracteres>" --project backend/SGE.API
```

Em uma instalação local já existente, o seed preserva usuários e só consulta essas variáveis se ainda não houver perfil Administrador.

Depois que o primeiro Administrador tiver sido criado e o login confirmado, remova `BootstrapAdmin__Password` das variáveis do serviço e faça um redeploy/restart. O seed é idempotente e só usa essa senha se ainda não houver usuário com perfil Administrador.

## Banco limpo e migrations

Crie um PostgreSQL novo para produção; não restaure nele o dump do banco local e não aponte a API de produção para o banco de desenvolvimento. As migrations existentes criam schema e dados estruturais (perfis/unidades/empresa Estrutural e transformações de compatibilidade); não existe cópia automática de transações locais.

A API **não executa migrations automaticamente**. Antes de aplicá-las em qualquer banco Railway:

1. Confirme que o alvo é o PostgreSQL novo do ambiente correto e obtenha um backup/snapshot antes de atualizar um banco que já tenha dados.
2. Gere e revise o SQL idempotente; confirme cada operação de alteração de schema e de dados:

   ```powershell
   railway run --service api powershell -NoProfile -Command '$env:ASPNETCORE_ENVIRONMENT = "Migration"; dotnet ef migrations script --idempotent --project backend/SGE.Persistence/SGE.Persistence.csproj --startup-project backend/SGE.API/SGE.API.csproj --output railway-migrations.sql'
   ```

3. Somente após revisão e autorização explícita, aplique as migrations pendentes:

   ```powershell
   railway run --service api powershell -NoProfile -Command '$env:ASPNETCORE_ENVIRONMENT = "Migration"; dotnet ef database update --project backend/SGE.Persistence/SGE.Persistence.csproj --startup-project backend/SGE.API/SGE.API.csproj'
   ```

O ambiente `Migration` evita executar o bootstrap do app durante a ferramenta EF. O comando usa variáveis Railway do serviço e a implementação prioriza `DATABASE_URL` de ambiente sobre o connection string local ignorado pelo Git. Requer .NET 10 SDK e `dotnet-ef` instalado na máquina. A Railway documenta `railway run` para executar comandos locais com variáveis do serviço e para migrations; isso é um procedimento manual, não configurado como pre-deploy automático.

Depois de aplicar migrations e antes de abrir o frontend, inicie o serviço API com os valores de bootstrap configurados. Na primeira inicialização pós-migration, a aplicação cria somente os perfis oficiais que faltarem, garante a empresa Estrutural e cria o Administrador inicial com a senha de ambiente. Não existe senha administrativa fixa no código. Após isso, remova o segredo de bootstrap.

## Armazenamento de anexos

Os controladores usam `IFileStorage` para upload, download, verificação de existência e exclusão. `Storage__Provider=Local` grava em disco durante Development; o caminho pode ser definido por `UPLOADS_PATH` e, por padrão, é `backend/SGE.API/uploads`. A implementação local continua reconhecendo os caminhos históricos `uploads/<categoria>/<arquivo>`, sem mover nem apagar arquivos existentes.

Em produção, o provedor é S3 (`Storage__Provider=S3`) e exige endpoint HTTPS, bucket, access key, secret key e região. As chaves persistidas no campo de caminho existente são geradas pela aplicação, únicas e sem o nome original do arquivo; metadados e banco não armazenam o conteúdo binário. Buckets devem permanecer privados, sem ACL pública. Os downloads continuam passando pelos endpoints autenticados e suas permissões existentes; a aplicação não entrega URLs públicas nem pré-assinadas.

Categorias de armazenamento incluem NF/recebimentos, orçamentos/cotações, contratos, anexos da OS, pagamentos, medições e adendos. PDFs oficiais gerados sob demanda continuam sendo respostas em memória e não são persistidos como anexos.

Não existe migração ou cópia automática entre disco local e bucket. Registros históricos locais continuam acessíveis no ambiente de desenvolvimento que contém os arquivos. Antes de apontar um banco com anexos existentes para outro provedor, planeje e valide a cópia dos objetos com as mesmas chaves; esta alteração não migra arquivos nem modifica registros.

Antes de produção, defina com o operador a retenção e o backup do bucket (incluindo recuperação de exclusões, versionamento/replicação quando suportados e teste de restauração). O banco e os objetos precisam de backups coordenados para manter registros e anexos consistentes. Não há política de retenção ou cópia automática configurada nesta preparação.

## Frontend e PDF

O frontend injeta `API_URL` em runtime, sem gravar o domínio de produção no bundle. No `ng serve`, a configuração local continua usando `http://localhost:5261/api`. O Nginx encaminha rotas desconhecidas para `index.html`, permitindo refresh direto em `/app` e subrotas. PDF usa Liberation Sans/Arial com resolução cross-platform; a imagem backend instala `fonts-liberation`.

## Limites desta preparação

Nada foi publicado no Railway. A conexão, credenciais, endpoint do bucket e domínio do projeto ainda precisam ser fornecidos/configurados pelo operador; não validei conexão real com um bucket S3. As verificações locais usam diretório temporário e serviço falso de metadados, sem escrever no PostgreSQL. A imagem Docker depende de Docker Engine disponível para a etapa local de build.
