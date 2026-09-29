---
version: alpha
colors:
  primary: "#175cd3"
  primaryStrong: "#124aa8"
  background: "#f4f6f8"
  surface: "#ffffff"
  mutedSurface: "#eef2f5"
  border: "#d8e0e7"
  text: "#18222d"
  mutedText: "#627386"
  danger: "#b42318"
typography:
  sans:
    fontFamily: "Inter, Segoe UI, Roboto, Arial, sans-serif"
    fontSize: "14px"
    lineHeight: "1.4"
rounded:
  control: "6px"
  surface: "8px"
spacing:
  page: "28px"
  section: "18px"
components:
  dashboard:
    register: "product-admin"
    density: "compact-readable"
    cards: "four-column desktop, stacked narrow"
---

## Overview

SGE-ERP é um produto operacional para compras, recebimentos, serviços e financeiro. A interface deve parecer um painel de operação confiável: leitura rápida, hierarquia clara e pouca decoração. O dashboard é uma visão de trabalho, não uma tela de BI.

## Colors

O azul é reservado para navegação, links e ações primárias. Superfícies permanecem brancas sobre um fundo cinza frio. Amarelo, vermelho, verde e azul-claro são semânticos e aparecem principalmente em indicadores de estado, não como decoração global.

## Typography

Usar a pilha Inter/Segoe UI/Roboto/Arial já adotada pelo aplicativo. Títulos têm contraste de escala e peso; textos operacionais permanecem compactos e legíveis.

## Layout

O conteúdo usa o espaçamento do shell existente. Cards de resumo devem ser compactos, clicáveis quando houver destino e responsivos. Seções longas mantêm rolagem natural do documento; não criar uma segunda rolagem da página.

## Elevation & Depth

Priorizar bordas e superfícies sobre sombras. Elevação sutil só deve indicar uma interação ou agrupamento, nunca transformar o dashboard em um mosaico de painéis pesados.

## Shapes

Controles usam raio de 6px e superfícies usam raio de 8px. Badges de estado podem usar formato de cápsula. Ações devem continuar reconhecíveis por texto, foco e contraste, não apenas por cor.

## Components

O dashboard usa os tokens CSS globais em `frontend/src/styles.css` e o mesmo cabeçalho, botão, link, estado vazio e feedback das telas de módulo. Pendências e atividades usam marcadores semânticos discretos; valores financeiros aparecem somente quando autorizados pelo backend.

A visão gerencial Gastos por Obra usa abas discretas no cabeçalho de Início, filtros compactos, cards de resumo e tabelas com rolagem horizontal em telas estreitas. Gráficos permanecem simples, com barras semânticas para Material e Serviço e sem transformar a tela operacional em BI.

## Do's and Don'ts

- Faça: mostre pendências reais, responsáveis e data/hora; preserve estados de carregamento, erro e vazio.
- Faça: use links e botões semânticos com foco visível.
- Não faça: invente números, exponha valores financeiros para Compras/Almoxarife ou transforme o dashboard em um relatório gráfico complexo.
- Não faça: introduza uma segunda linguagem visual para uma tela que pertence ao mesmo shell operacional.
