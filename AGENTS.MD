# SGE-ERP — Instruções do Codex

## Projeto

ERP web de compras de materiais e contratação de serviços.

- Backend: .NET 10, ASP.NET Core Web API, EF Core e PostgreSQL.
- Frontend: Angular, TypeScript e Angular Material.
- Camadas: `SGE.API`, `SGE.Application`, `SGE.Domain`, `SGE.Infrastructure` e `SGE.Persistence`.
- Backend é a fonte de verdade das regras e permissões.
- Backend: `dotnet build backend/SGE.API/SGE.API.csproj`.
- Frontend: `npm run build` em `frontend`.

## Regras gerais

- Fazer somente alterações focadas; não refatorar fora do escopo.
- Reutilizar entidades, DTOs, services, repositories e padrões existentes.
- Preservar dados e compatibilidade histórica.
- Não editar migrations aplicadas; criar migration nova somente quando o schema mudar.
- Dinheiro sempre em `decimal`.
- Não corrigir warnings antigos fora do escopo.
- Não usar `npm audit fix --force`.
- Nunca usar `DROP`, `TRUNCATE`, recriar banco ou apagar dados sem autorização explícita.

## Empresa e obras

- A empresa padrão e obrigatória de toda PurchaseRequest é `Estrutural`.
- O usuário não escolhe Empresa; Material e Serviço recebem automaticamente o `CompanyId` da Estrutural.
- Work/Obra pode existir sem `CompanyId`.
- Obras podem ser criadas, editadas e inativadas pelo Administrador.
- Preservar registros históricos.

## Perfis oficiais

Existem somente quatro perfis de negócio, com estes nomes persistidos e exibidos:

- `Administrador` (`Admin` internamente): acesso total e aprovações operacionais/comerciais.
- `Almoxarife` (`Warehouse` internamente): cria solicitações Material/Serviço, visualiza solicitações e OCs, registra e visualiza recebimentos; não cria cotação, aprova, acessa financeiro, registra pagamentos ou administra usuários.
- `Compras` (`Buyer` internamente): cria solicitações, cria/edita cotações editáveis, visualiza OCs/recebimentos, cadastra/edita Material e Fornecedor; não aprova, acessa financeiro, registra pagamento/recebimento, administra usuários, Obras ou Unidades.
- `Financeiro` (`Finance` internamente): visualiza todos os módulos e executa/aprova somente ações financeiras; pode liberar Ordem de Serviço para execução quando as pré-condições do contrato forem atendidas; não aprova operações/comercialmente, registra recebimentos, altera cadastros operacionais ou administra usuários.

Não criar ou usar operacionalmente `Approver`, `Requester` ou roles duplicadas. Roles legadas podem ser reconhecidas apenas para normalização de compatibilidade, nunca como perfis oficiais. Não usar RoleId fixo nem usuário específico para autorizar.

## Solicitações e imutabilidade

Material: `PurchaseRequest → Quotation → aprovações do Administrador → PurchaseOrder → Financeiro/Recebimento`.

Serviço: `PurchaseRequest → aprovação do Administrador → ServiceOrder → contrato → liberação → execução/medições → financeiro`.

Criar e enviar são ações separadas: salvar cria `Draft` editável; o detalhe exibe `Excluir/Cancelar` e `Enviar solicitação` enquanto permitido; enviar avança o fluxo e remove o botão. Serviço pode ser cancelado/excluído enquanto permitido.

Depois de aprovado/finalizado, o backend bloqueia edição e exclusão destrutiva dos dados de origem; o frontend oculta/desabilita ações inválidas. Correções posteriores usam fluxo auditável.

## Dashboard

`GET /api/Dashboard` fornece o resumo inicial agregado conforme o perfil autenticado. A página inicial mostra cards, pendências e atividades recentes usando somente dados reais, sem expor valores financeiros para Almoxarife ou Compras. Financeiro e Administrador recebem os indicadores financeiros permitidos; cards e pendências navegam para os módulos existentes.

Administradores e Financeiro também podem acessar `GET /api/Dashboard/work-costs`, com filtros reais de obra e período. A visão Gastos por Obra calcula contratado, pago e pendente a partir de OCs, OSs e pagamentos registrados, sem tratar medição como pagamento. Compras e Almoxarife não recebem esse endpoint nem dados financeiros equivalentes.

## Material

- Compras cria/edita cotações enquanto editáveis; não aprova.
- Administrador escolhe vencedor e aprova a cotação.
- PurchaseOrder não possui aprovação operacional própria.
- Financeiro/Admin executam ações financeiras; pagamentos podem ser parciais e nunca ultrapassam a OC.
- O histórico de pagamentos mostra valor, data/hora real, forma, responsável e anexos vinculados ao Payment.
- Almoxarife/Admin registram recebimentos, que podem ser parciais.
- Anexos de orçamento pertencem ao fornecedor correspondente, usam relação 1:N, podem ser adicionados/removidos em cotação editável e depois são somente leitura/download.
- Anexos de pagamento pertencem ao Payment específico, preservam histórico e nunca substituem documentos silenciosamente.
- Cada solicitação possui no máximo a cotação existente; não oferecer "Criar cotação" novamente.
- Orçamentos permanecem editáveis enquanto a cotação não tiver aprovação efetiva; após aprovação, cotação, orçamentos e anexos ficam imutáveis.
- Cada proposta de fornecedor na cotação de material possui seu próprio `FreightValue`; total da proposta = subtotal dos itens + frete, sem rateio do frete entre itens.
- Ao gerar a Ordem de Compra, preservar o frete da proposta escolhida. O total da OC e os compromissos financeiros consideram itens + frete; o recebimento físico continua baseado somente nas quantidades dos materiais.
- Pagamentos de antecipação aceitam múltiplos anexos vinculados ao pagamento específico.
- Ordens de Compra e Ordens de Serviço possuem PDF oficial para download, respeitando autenticação e permissões.

## Serviço e medições

- Serviço não usa Quotation ou PurchaseOrder; contrato existe antes da liberação.
- `ServiceMeasurement` é a entidade oficial.
- Medições válidas não exigem aprovação individual após OS aprovada/liberada.
- Registrar quantidade executada e valor financeiro; exibir contratado, medição, acumulado, restante e percentuais físico/financeiro pago.
- Nunca ultrapassar quantidade contratada ou `ContractedValue`.
- Preservar medições históricas e estados antigos.
- `ServiceMeasurement` aceita múltiplos anexos próprios, com download posterior e sem compartilhamento entre medições.
- Execução e financeiro são independentes; antecipação exige solicitação/aprovação e `TotalPaid` nunca ultrapassa `ContractedValue`.

## Recebimentos

Separar status da OC, financeiro e recebimento. Usar: Aguardando recebimento, Recebimento parcial, Recebido integralmente e Recebimento com divergência.

Recebimento parcial válido não é automaticamente divergência. Divergência deve preservar e mostrar esperado, recebido, diferença e item, com destaque visual persistente.

Toda nova operação de recebimento exige Nota Fiscal anexada no mesmo registro, inclusive recebimentos parciais. A NF permanece vinculada ao Receipt e disponível para download; recebimentos históricos sem NF são preservados.

## Anexos, erros e cadastros

- Separar anexos de orçamento, pagamento, NF/recebimento, contrato e Serviço.
- Não armazenar múltiplos arquivos em string; usar relação 1:N.
- Quando aplicável, aceitar PDF, JPG, JPEG, PNG, DOC, DOCX, XLS e XLSX, máximo 10 MB por arquivo.
- O frontend nunca mostra stack trace, SQL, paths, exceções internas ou classes técnicas; exibir mensagem amigável e manter detalhes somente nos logs.
- Cadastros: Material, Fornecedor, Obra, Usuário e Unidade de Medida.
- Unidade utilizada deve ser inativada, não apagada destrutivamente.
- Usuário: a única interface de gerenciamento é `Administração → Usuários`; fluxo `Cadastrar usuário → Salvar`; seletor mostra apenas os quatro perfis oficiais.
- Listagens mostram mais recentes primeiro e o responsável/criador quando disponível.
- Ações ficam agrupadas no cabeçalho/detalhe.

## Segurança e testes

- Toda permissão existe no backend; esconder botão não é segurança.
- Operação não autorizada retorna `403` quando aplicável.
- Após backend: `dotnet build backend/SGE.API/SGE.API.csproj`.
- Após frontend: `npm run build` em `frontend`.
- Após mudanças no armazenamento de arquivos: `dotnet run --project backend/SGE.API.StorageChecks/SGE.API.StorageChecks.csproj -c Release` valida upload, metadado de chave, download, atributos de autorização, exclusão, compatibilidade local histórica e path traversal sem usar o banco real.
- Não corrigir warnings antigos fora do escopo.

## Resposta do Codex

Responder curto com implementação, migration, testes, builds, arquivos principais e pendências.

## Serviço — contrato e adendos

- Solicitações novas de Serviço registram necessidade, Obra e especificação; quantidade/unidade contratuais são definidas ao gerar a OS. Campos históricos das solicitações e OSs permanecem legíveis.
- Quantidade, unidade, prestador e condições contratuais da OS só podem ser ajustados diretamente enquanto aguardam contrato/liberação. Após a liberação, alterações contratuais são feitas por adendo.
- Pagamento e solicitação/aprovação de antecipação exigem OS liberada para execução; o backend bloqueia tentativas anteriores com mensagem amigável.
- Adendos pertencem à OS, preservam motivo, ajustes, responsável, instante, anexos próprios 1:N e snapshots antes/depois. Rascunho não afeta limites; somente aprovação por Administrador atualiza contrato. Financeiro pode consultar, mas não aprovar.
- Valor e quantidade contratuais atuais são os termos originais somados aos ajustes aprovados. Pagamentos e medições respeitam os limites atuais e preservam registros históricos.
- O PDF oficial da OS lista somente adendos aprovados, com valores/quantidades original e atual.

## Deploy Railway

- Produção usa o monorepo como build context e imagens `backend/Dockerfile` e `frontend/Dockerfile`; Railway não deve publicar dados do banco local.
- API recebe connection string via `ConnectionStrings__DefaultConnection` ou compatibilidade com `DATABASE_URL`/variáveis `PG*`; JWT e primeiro Administrador usam variáveis secretas, nunca valores no código.
- `Cors__AllowedOrigins` é obrigatório em produção e deve conter somente origens confiáveis; nunca usar `AllowAnyOrigin` em produção.
- Anexos usam `IFileStorage` para upload/download/existência/exclusão. Desenvolvimento usa `Storage__Provider=Local` (padrão), preservando leitura de caminhos históricos `uploads/<categoria>/<arquivo>` no diretório local; produção exige S3 privado via `Storage__Provider=S3`.
- Configurar produção com `Storage__Endpoint`, `Storage__Bucket`, `Storage__AccessKey`, `Storage__SecretKey`, `Storage__Region` e `Storage__UsePathStyle`; nunca versionar credenciais. S3 guarda objetos privados com chaves seguras/únicas, e downloads continuam nos endpoints autenticados com as permissões existentes.
- `UPLOADS_PATH` é apenas para o armazenamento local. Não mover arquivos nem registros durante a configuração inicial; histórico local permanece utilizável em Development. Cópia entre provedores exige plano explícito, preservando as chaves/categorias.
- Planejar retenção e backup de bucket junto com backup do banco e testar restauração; PDFs gerados sob demanda não são anexos persistidos.
- A API não aplica migrations no startup/pre-deploy. Revisar SQL idempotente e aplicar migrations manualmente, após backup e confirmação do banco alvo.
- O bootstrap de produção só cria perfis oficiais faltantes, garante a empresa Estrutural e cria o Administrador inicial quando ainda não existir, usando `BootstrapAdmin__Email`/`BootstrapAdmin__Password`; remover a senha do serviço depois do primeiro login.
- O frontend recebe `API_URL` em runtime; backend e frontend escutam `PORT`; `/health` é o healthcheck e deve retornar indisponível quando o banco não conecta.
- PDF deve resolver fontes Linux sem caminhos absolutos Windows; uploads e builds devem ser validados em containers Linux antes de autorizar deploy.
