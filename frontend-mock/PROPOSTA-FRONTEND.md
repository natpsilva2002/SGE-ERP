# Proposta de evolução visual do frontend SGE-ERP

Documento de referência para uma futura aplicação das melhorias no frontend Angular.

> Este documento descreve uma proposta visual baseada no mock de avaliação. Não representa uma alteração implementada no sistema real.

## Objetivo

Evoluir a experiência atual do SGE-ERP sem criar um redesign completo. A interface deve continuar reconhecível para quem já utiliza o sistema, mas com:

- hierarquia de informação mais clara;
- maior facilidade para identificar pendências;
- navegação lateral mais organizada;
- tabelas e detalhes mais compactos;
- uso consistente de estados, ações e anexos;
- melhor leitura em desktop, notebook e tablet.

## Direção visual

O SGE deve parecer um painel operacional corporativo: confiável, sóbrio, compacto e fácil de escanear.

Evitar:

- aparência de landing page;
- gradientes decorativos;
- cards excessivamente grandes;
- sombras fortes;
- excesso de cantos arredondados;
- uso de muitas cores sem função;
- animações que não ajudam na compreensão.

## Tokens visuais

### Cores base

| Token | Valor | Uso |
|---|---|---|
| `primary` | `#175CD3` | Navegação, links e ações principais |
| `primaryStrong` | `#124AA8` | Hover e estados ativos fortes |
| `background` | `#F4F6F8` | Fundo geral da aplicação |
| `surface` | `#FFFFFF` | Cards, tabelas e painéis |
| `mutedSurface` | `#EEF2F5` | Fundos auxiliares e estados neutros |
| `border` | `#D8E0E7` | Divisórias e contornos |
| `text` | `#18222D` | Texto principal |
| `mutedText` | `#627386` | Texto auxiliar e metadados |
| `danger` | `#B42318` | Erros e urgência alta |

### Acentos semânticos

As cores adicionais devem aparecer em pequenas áreas funcionais, nunca como decoração global.

| Token | Valor | Uso |
|---|---|---|
| `teal` | `#0F766E` | Recebimento, execução e estados concluídos |
| `tealSoft` | `#E6F5F2` | Fundo de status concluído |
| `amber` | `#D98B0B` | Aprovação pendente, vencimento e atenção |
| `amberSoft` | `#FFF4D6` | Fundo de status de atenção |
| `violet` | `#6956A8` | Ordens de Serviço e medições |
| `violetSoft` | `#F0EDFA` | Fundo de estado neutro específico |

Regras de uso:

- azul continua sendo a cor de ação e navegação;
- vermelho fica reservado para risco, erro ou urgência;
- âmbar indica atenção, mas não erro;
- verde-petróleo indica avanço, recebimento ou conclusão;
- violeta diferencia Serviço/Medições sem competir com o azul;
- o significado nunca deve depender apenas da cor: usar texto, ícone ou marcador junto.

### Tipografia e formas

- Fonte: `Inter, Segoe UI, Roboto, Arial, sans-serif`.
- Texto operacional compacto, com boa legibilidade.
- Controles: raio de `6px`.
- Superfícies: raio de `8px`.
- Badges de status: formato de cápsula.
- Preferir bordas a sombras; usar sombra somente para indicar interação ou agrupamento.

## Shell e navegação

### Sidebar

Organizar os módulos reais em dois grupos:

**Operação**

- Início
- Solicitações
- Cotações
- Ordens de Compra
- Ordens de Serviço
- Recebimentos

**Gestão**

- Financeiro
- Cadastros
- Administração

Comportamentos esperados:

- item ativo com fundo azul-claro e indicador lateral;
- contador somente quando representar uma pendência útil;
- perfil logado no rodapé com nome e perfil;
- sidebar recolhível em tablet;
- estado ativo também deve ser percebido por texto e foco, não somente por cor.

### Cabeçalho

O cabeçalho da página deve conter:

- breadcrumb simples;
- título e descrição curta no conteúdo;
- notificações;
- usuário/perfil quando fizer sentido;
- opção discreta para visualizar anotações da proposta apenas no mock, nunca no sistema final.

## Início

### Abas

Manter no cabeçalho de Início duas abas discretas:

- **Visão Geral**
- **Gastos por Obra**

As abas não devem parecer uma nova navegação global. Elas pertencem ao contexto de Início.

### Cards de resumo

Usar cards menores, clicáveis quando houver destino real, com:

- rótulo;
- número principal;
- informação auxiliar;
- acento de cor semântico na borda superior.

Indicadores previstos para Administrador:

- solicitações em andamento;
- cotações aguardando aprovação;
- OCs em aberto;
- OSs em execução;
- informação financeira permitida.

O backend continua sendo a fonte de verdade para autorização e valores exibidos.

### Pendências

Esta é a seção de maior prioridade visual da página.

Cada item deve mostrar:

- documento e ação necessária;
- tipo ou contexto;
- obra, fornecedor ou responsável;
- tempo desde a pendência ou vencimento;
- link da ação principal.

Recomendação de ordenação:

1. urgência alta;
2. vencimento mais próximo;
3. pendências mais antigas.

A ação principal deve ser textual e direta, por exemplo: `Analisar cotação`, `Registrar recebimento` ou `Ver ordem de serviço`.

### Atividades recentes

Deve ser visualmente mais leve que Pendências:

- linha do tempo curta;
- marcador discreto;
- documento ou evento;
- responsável e data/hora;
- sem competir com as ações pendentes.

### Ações rápidas

Exibir somente ações frequentes e realmente úteis, como `Criar solicitação`. Evitar transformar a área em um painel de atalhos.

## Gastos por Obra

Visão gerencial para Administrador e Financeiro, usando dados reais quando aplicada ao sistema.

### Filtros

- Obra;
- período;
- botão `Aplicar filtros`;
- filtros compactos e alinhados em uma única barra quando houver espaço.

### Indicadores

- total contratado;
- total pago;
- total pendente.

Não tratar medição como pagamento. Os valores devem ser calculados a partir de OCs, OSs e pagamentos registrados conforme as regras do backend.

### Visualizações

Manter gráficos simples e operacionais:

- evolução dos gastos;
- Material x Serviço;
- maiores custos por fornecedor;
- detalhamento por OC/OS;
- pagamentos recentes.

Evitar dashboards analíticos complexos ou excesso de gráficos. Tabelas continuam sendo a fonte principal de leitura detalhada.

## Listagens

Todas as listagens devem manter:

- registros mais recentes primeiro;
- filtros compactos;
- busca quando houver volume relevante;
- status com badge textual;
- responsável/criador quando disponível;
- ações agrupadas e reduzidas;
- rolagem horizontal em telas estreitas sem criar segunda rolagem da página.

Listas representadas no mock:

- Solicitações;
- Ordens de Compra;
- Cotações;
- Recebimentos;
- Cadastros;
- Administração.

## Padrão de detalhe

Usar a seguinte hierarquia:

1. cabeçalho com número, título, status e ações principais;
2. resumo;
3. informações principais;
4. itens ou medições;
5. informações complementares;
6. anexos;
7. histórico.

### Cabeçalho de detalhe

As ações principais devem ficar no cabeçalho, com no máximo uma ou duas ações destacadas. Ações secundárias ficam em `Mais ações` ou em controles próximos, sem poluir tabelas.

### Anexos

O frontend deve manter o contexto do anexo:

- orçamento ligado ao fornecedor da cotação;
- pagamento ligado ao Payment específico;
- NF ligada ao Receipt;
- contrato ligado à OS/contrato;
- medição ligada à ServiceMeasurement.

Após aprovação ou finalização, os anexos devem ser exibidos como somente leitura/download, respeitando as regras do backend.

### Histórico

Exibir eventos com:

- ação realizada;
- responsável;
- data/hora;
- estado resultante quando relevante.

Não expor stack trace, SQL, caminhos internos ou classes técnicas.

## Estados e feedback

Implementar padrões reutilizáveis para:

- carregamento;
- erro amigável;
- vazio com orientação;
- sucesso;
- confirmação de ação;
- modal;
- toast;
- estado desabilitado por regra de negócio.

Mensagens devem descrever o que aconteceu e qual é o próximo passo. A ação do botão e a mensagem de retorno devem usar o mesmo verbo.

## Responsividade

### Desktop e notebook

- sidebar fixa;
- cards em quatro ou cinco colunas conforme largura;
- pendências e atividades em duas colunas;
- tabelas com densidade compacta.

### Tablet

- sidebar recolhível;
- cards em duas ou três colunas;
- detalhes podem passar para uma coluna;
- filtros quebram linha sem perder a ordem visual.

### Acessibilidade

- foco visível em links, botões e campos;
- contraste suficiente;
- status não depender somente da cor;
- navegação por teclado;
- respeitar `prefers-reduced-motion`;
- textos e ações com nomes claros.

## Interações demonstradas no mock

O mock representa os seguintes comportamentos:

- alternância entre Visão Geral e Gastos por Obra;
- navegação pelo menu;
- abertura de Solicitação, OC e OS;
- modais de ação;
- expansão de detalhes;
- aplicação simulada de filtros;
- menu responsivo;
- painel de anotações da proposta.

As anotações da proposta e os dados usados no mock são exclusivamente demonstrativos e não devem ser levados para o sistema final.

## Roteiro para futura aplicação no Angular

1. Validar esta direção visual com usuários do Administrador e Financeiro.
2. Conferir os tokens globais existentes em `frontend/src/styles.css`.
3. Extrair componentes compartilhados para shell, cabeçalho, tabs, cards, tabelas, badges, filtros, detalhe, anexos e histórico.
4. Aplicar primeiro na página `Início` e validar com dados reais do endpoint de Dashboard.
5. Aplicar o padrão às listagens e detalhes, preservando rotas e nomenclaturas atuais.
6. Conectar cada ação à permissão de backend correspondente.
7. Validar estados de carregamento, erro, vazio e `403`.
8. Testar desktop, notebook, tablet, teclado e contraste.
9. Executar o build do frontend somente após a aplicação real das mudanças.

## Critérios de aceitação

- Usuário identifica pendências sem abrir cada módulo.
- A ação principal de cada detalhe é evidente no cabeçalho.
- Cores adicionais têm função semântica e não criam ruído.
- Financeiro e Gastos por Obra respeitam o perfil autenticado.
- Nenhum dado financeiro é exposto para perfis não autorizados.
- Anexos permanecem vinculados ao registro correto.
- Listagens continuam compactas e legíveis.
- O layout funciona em desktop, notebook e tablet.
- O mock permanece isolado e não altera o frontend Angular atual.
