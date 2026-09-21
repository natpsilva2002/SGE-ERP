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
- Pagamentos de antecipação aceitam múltiplos anexos vinculados ao pagamento específico.

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
- Não corrigir warnings antigos fora do escopo.

## Resposta do Codex

Responder curto com implementação, migration, testes, builds, arquivos principais e pendências.
