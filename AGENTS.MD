# SGE-ERP — Codex Instructions

## Projeto

ERP web para gestão de compras de materiais e contratação de serviços.

Stack:

- Backend: .NET 10 / C#
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Frontend: Angular
- TypeScript
- Angular Material
- Arquitetura em camadas:
  - SGE.API
  - SGE.Application
  - SGE.Domain
  - SGE.Infrastructure
  - SGE.Persistence

Antes de implementar uma feature, procure implementações equivalentes existentes e reutilize os padrões do projeto.

Não crie entidades, DTOs, services ou repositories paralelos quando já existir estrutura equivalente.

---

## Regra geral

Faça somente o que foi solicitado no prompt atual.

Não faça refatorações fora do escopo.

Não altere módulos que não fazem parte da tarefa.

Não corrija warnings antigos durante implementação de features.

Não use `npm audit fix --force`.

Não altere migrations antigas já aplicadas.

Preserve compatibilidade com dados existentes sempre que possível.

---

# MATERIAL

O fluxo atual de Material está funcional e NÃO deve ser alterado sem solicitação explícita.

Fluxo:

PurchaseRequest Material
→ Quotation
→ WaitingApproval
→ primeira aprovação
→ WaitingSecondApproval
→ segunda aprovação por usuário diferente
→ Quotation Approved
→ geração de exatamente uma PurchaseOrder
→ Financeiro e Recebimento seguem independentemente

## Cotação

As duas aprovações pertencem à QUOTATION.

NÃO implementar aprovação dupla na PurchaseOrder.

O mesmo usuário NÃO pode realizar as duas aprovações da cotação.

Somente após a segunda aprovação:

- Quotation = Approved
- gerar exatamente uma PurchaseOrder
- PurchaseOrder nasce Approved

## PurchaseOrder

PurchaseOrder NÃO possui aprovação operacional no fluxo atual.

PDF da OC deve estar disponível imediatamente após sua geração.

Financeiro e recebimento são independentes.

## Financeiro Material

PurchaseOrder possui autorização de pagamento antes do pagamento.

Depois:

Unpaid
→ PartiallyPaid
→ Paid

Pagamento nunca pode ultrapassar o valor da OC.

## Recebimento Material

Pode haver recebimentos parciais.

Recebimento é independente do pagamento.

NF pode ser anexada ao Receipt.

---

# SERVIÇOS

Serviço é um fluxo separado de Material.

NÃO reutilizar Quotation ou PurchaseOrder para ServiceOrder.

Fluxo atual:

PurchaseRequest Service
→ aprovação
→ ServiceOrder
→ contrato
→ release
→ execução
→ medições
→ conclusão

## ServiceOrder

A OS possui:

- PurchaseRequest origem
- Work
- Supplier/prestador
- ContractedValue
- PaymentCondition
- contrato
- status de execução
- status financeiro

Contrato deve existir antes do release.

## Medições

ServiceMeasurement é a entidade oficial.

NÃO criar entidade paralela de medição.

Fluxo:

Draft
→ WaitingApproval
→ Approved

ou:

WaitingApproval
→ Rejected

Medições podem ser parciais.

Uma ServiceOrder pode possuir várias medições.

Medições Draft + WaitingApproval + Approved consomem o saldo disponível.

Rejected não consome saldo.

Nunca permitir que o total comprometido ultrapasse ContractedValue.

Quando:

SUM(Approved Measurements) == ContractedValue

ServiceOrder → Completed

Execução e financeiro são independentes.

Uma OS pode estar Completed e ainda possuir pagamentos pendentes.

---

# FINANCEIRO DE SERVIÇOS

Ainda deve seguir o princípio:

somente valores de medições Approved podem ficar elegíveis para pagamento.

Pagamento não deve alterar o status de execução da ServiceOrder.

Não inventar regra financeira adicional sem solicitação explícita.

---

# PERFIS

Existe controle de acesso por roles.

A matriz definitiva de perfis será revisada posteriormente.

Não refatore permissões gerais durante outras features.

Preserve as permissões existentes, exceto quando o prompt atual solicitar explicitamente uma alteração.

---

# BANCO / MIGRATIONS

Antes de criar migration:

1. conferir entidade atual;
2. conferir configuração EF;
3. conferir migrations existentes;
4. evitar duplicar colunas/tabelas.

Se uma migration for necessária:

- criar migration nova;
- nunca editar migrations antigas aplicadas;
- preservar dados existentes;
- revisar a migration antes de aplicar.

---

# TESTES

Após alterações de backend:

dotnet build backend/SGE.API/SGE.API.csproj

Após alterações de frontend:

npm run build

Não corrigir warnings antigos fora do escopo.

Quando a feature possuir regra financeira ou mudança de status, testar também o fluxo real pela API quando possível.

---

# Segurança de alterações

NÃO:

- apagar dados sem autorização;
- executar DROP/TRUNCATE;
- recriar banco;
- alterar Material enquanto trabalha em Serviço;
- alterar Serviço enquanto trabalha em Material;
- modificar regras consolidadas sem solicitação explícita;
- fazer grandes refatorações fora do escopo.

Se encontrar inconsistência importante que exija mudar uma regra consolidada, informe antes de fazer a alteração.

---

# Resposta final

Ao terminar uma tarefa, seja objetivo.

Informe somente:

- o que foi implementado;
- arquivos principais alterados;
- migration criada, se houver;
- testes executados;
- resultado dos builds;
- problemas ou decisões pendentes.

Não produzir relatório extenso salvo se solicitado.