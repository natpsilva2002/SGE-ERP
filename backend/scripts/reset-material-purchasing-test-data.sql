/*
========================================================
DEVELOPMENT ONLY
REMOVE DADOS TRANSACIONAIS DO FLUXO MATERIAL
NAO EXECUTAR EM PRODUCAO
========================================================

Este script limpa dados transacionais de teste do fluxo de compras
de MATERIAL no banco de desenvolvimento.

Remove, quando relacionados a PurchaseRequests com Type = 1 (Material):
- ApprovalHistories e Approvals de PurchaseRequest/Quotation
- ReceiptItems e Receipts de PurchaseOrders geradas por Quotations Material
- Payments de PurchaseOrders geradas por Quotations Material
- PurchaseOrderItems e PurchaseOrders geradas por Quotations Material
- QuotationItems e Quotations das PurchaseRequests Material
- PurchaseRequestItems e PurchaseRequests Material

Preserva:
- Users, Roles
- Works, Companies
- Items/Materials, Categories
- Suppliers
- ServiceOrders, ServiceMeasurements e contratos de servico
- __EFMigrationsHistory

Protecao de Service:
- Nao apaga PurchaseRequests Service (Type = 2).
- Se existir ServiceOrder vinculada a uma PurchaseRequest Material,
  o script falha antes de executar deletes para evitar perda silenciosa.

Ordem:
- Filhos -> pais, respeitando Foreign Keys.
- Tudo roda dentro de uma unica transacao PostgreSQL.
*/

BEGIN;

CREATE TEMP TABLE _reset_material_pr_ids ON COMMIT DROP AS
SELECT pr."Id"
FROM purchase_requests pr
WHERE pr."Type" = 1;

CREATE TEMP TABLE _reset_material_quotation_ids ON COMMIT DROP AS
SELECT q."Id"
FROM quotations q
JOIN _reset_material_pr_ids pr ON pr."Id" = q."PurchaseRequestId";

CREATE TEMP TABLE _reset_material_purchase_order_ids ON COMMIT DROP AS
SELECT po."Id"
FROM "PurchaseOrders" po
JOIN _reset_material_quotation_ids q ON q."Id" = po."QuotationId";

CREATE TEMP TABLE _reset_material_purchase_order_item_ids ON COMMIT DROP AS
SELECT poi."Id"
FROM "PurchaseOrderItems" poi
JOIN _reset_material_purchase_order_ids po ON po."Id" = poi."PurchaseOrderId";

CREATE TEMP TABLE _reset_material_receipt_ids ON COMMIT DROP AS
SELECT r."Id"
FROM "Receipts" r
JOIN _reset_material_purchase_order_ids po ON po."Id" = r."PurchaseOrderId";

CREATE TEMP TABLE _reset_material_approval_ids ON COMMIT DROP AS
SELECT a."Id"
FROM "Approvals" a
WHERE a."PurchaseRequestId" IN (SELECT "Id" FROM _reset_material_pr_ids)
   OR a."QuotationId" IN (SELECT "Id" FROM _reset_material_quotation_ids);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM "ServiceOrders" so
        JOIN _reset_material_pr_ids pr ON pr."Id" = so."PurchaseRequestId"
    ) THEN
        RAISE EXCEPTION 'Limpeza abortada: existe ServiceOrder vinculada a PurchaseRequest Material.';
    END IF;
END $$;

SELECT 'BEFORE PurchaseRequests Material' AS item, count(*) AS count
FROM _reset_material_pr_ids
UNION ALL
SELECT 'BEFORE PurchaseRequestItems Material', count(*)
FROM purchase_request_items pri
JOIN _reset_material_pr_ids pr ON pr."Id" = pri."PurchaseRequestId"
UNION ALL
SELECT 'BEFORE Quotations Material', count(*)
FROM _reset_material_quotation_ids
UNION ALL
SELECT 'BEFORE QuotationItems Material', count(*)
FROM quotation_items qi
JOIN _reset_material_quotation_ids q ON q."Id" = qi."QuotationId"
UNION ALL
SELECT 'BEFORE PurchaseOrders Material', count(*)
FROM _reset_material_purchase_order_ids
UNION ALL
SELECT 'BEFORE PurchaseOrderItems Material', count(*)
FROM _reset_material_purchase_order_item_ids
UNION ALL
SELECT 'BEFORE Receipts Material', count(*)
FROM _reset_material_receipt_ids
UNION ALL
SELECT 'BEFORE ReceiptItems Material', count(*)
FROM "ReceiptItems" ri
WHERE ri."ReceiptId" IN (SELECT "Id" FROM _reset_material_receipt_ids)
   OR ri."PurchaseOrderItemId" IN (SELECT "Id" FROM _reset_material_purchase_order_item_ids)
UNION ALL
SELECT 'BEFORE Payments Material', count(*)
FROM "Payments" p
JOIN _reset_material_purchase_order_ids po ON po."Id" = p."PurchaseOrderId"
UNION ALL
SELECT 'BEFORE Approvals Material PR/Quotation', count(*)
FROM _reset_material_approval_ids
UNION ALL
SELECT 'BEFORE ApprovalHistories Material PR/Quotation', count(*)
FROM "ApprovalHistories" ah
JOIN _reset_material_approval_ids a ON a."Id" = ah."ApprovalId"
UNION ALL
SELECT 'PRESERVED Items', count(*) FROM items
UNION ALL
SELECT 'PRESERVED Suppliers', count(*) FROM suppliers
UNION ALL
SELECT 'PRESERVED Works', count(*) FROM works
UNION ALL
SELECT 'PRESERVED Users', count(*) FROM users
UNION ALL
SELECT 'PRESERVED Roles', count(*) FROM roles
UNION ALL
SELECT 'PRESERVED ServiceOrders', count(*) FROM "ServiceOrders"
UNION ALL
SELECT 'PRESERVED ServiceMeasurements', count(*) FROM "ServiceMeasurements"
UNION ALL
SELECT 'PRESERVED __EFMigrationsHistory', count(*) FROM "__EFMigrationsHistory"
ORDER BY item;

WITH deleted AS (
    DELETE FROM "ApprovalHistories" ah
    USING _reset_material_approval_ids a
    WHERE ah."ApprovalId" = a."Id"
    RETURNING 1
)
SELECT 'DELETE ApprovalHistories' AS operation, count(*) AS rows_affected FROM deleted;

WITH deleted AS (
    DELETE FROM "ReceiptItems" ri
    WHERE ri."ReceiptId" IN (SELECT "Id" FROM _reset_material_receipt_ids)
       OR ri."PurchaseOrderItemId" IN (SELECT "Id" FROM _reset_material_purchase_order_item_ids)
    RETURNING 1
)
SELECT 'DELETE ReceiptItems' AS operation, count(*) AS rows_affected FROM deleted;

WITH deleted AS (
    DELETE FROM "Receipts" r
    USING _reset_material_receipt_ids ids
    WHERE r."Id" = ids."Id"
    RETURNING 1
)
SELECT 'DELETE Receipts' AS operation, count(*) AS rows_affected FROM deleted;

WITH deleted AS (
    DELETE FROM "Payments" p
    USING _reset_material_purchase_order_ids po
    WHERE p."PurchaseOrderId" = po."Id"
    RETURNING 1
)
SELECT 'DELETE Payments' AS operation, count(*) AS rows_affected FROM deleted;

WITH deleted AS (
    DELETE FROM "PurchaseOrderItems" poi
    USING _reset_material_purchase_order_item_ids ids
    WHERE poi."Id" = ids."Id"
    RETURNING 1
)
SELECT 'DELETE PurchaseOrderItems' AS operation, count(*) AS rows_affected FROM deleted;

WITH deleted AS (
    DELETE FROM "PurchaseOrders" po
    USING _reset_material_purchase_order_ids ids
    WHERE po."Id" = ids."Id"
    RETURNING 1
)
SELECT 'DELETE PurchaseOrders' AS operation, count(*) AS rows_affected FROM deleted;

WITH deleted AS (
    DELETE FROM quotation_items qi
    USING _reset_material_quotation_ids q
    WHERE qi."QuotationId" = q."Id"
    RETURNING 1
)
SELECT 'DELETE quotation_items' AS operation, count(*) AS rows_affected FROM deleted;

WITH deleted AS (
    DELETE FROM "Approvals" a
    USING _reset_material_approval_ids ids
    WHERE a."Id" = ids."Id"
    RETURNING 1
)
SELECT 'DELETE Approvals' AS operation, count(*) AS rows_affected FROM deleted;

WITH deleted AS (
    DELETE FROM quotations q
    USING _reset_material_quotation_ids ids
    WHERE q."Id" = ids."Id"
    RETURNING 1
)
SELECT 'DELETE quotations' AS operation, count(*) AS rows_affected FROM deleted;

WITH deleted AS (
    DELETE FROM purchase_request_items pri
    USING _reset_material_pr_ids pr
    WHERE pri."PurchaseRequestId" = pr."Id"
    RETURNING 1
)
SELECT 'DELETE purchase_request_items' AS operation, count(*) AS rows_affected FROM deleted;

WITH deleted AS (
    DELETE FROM purchase_requests pr
    USING _reset_material_pr_ids ids
    WHERE pr."Id" = ids."Id"
    RETURNING 1
)
SELECT 'DELETE purchase_requests Material' AS operation, count(*) AS rows_affected FROM deleted;

SELECT 'AFTER PurchaseRequests Material' AS item, count(*) AS count
FROM purchase_requests pr
WHERE pr."Type" = 1
UNION ALL
SELECT 'AFTER PurchaseRequests Service', count(*)
FROM purchase_requests pr
WHERE pr."Type" = 2
UNION ALL
SELECT 'AFTER PurchaseRequestItems Material scoped', count(*)
FROM purchase_request_items pri
WHERE pri."PurchaseRequestId" IN (SELECT "Id" FROM _reset_material_pr_ids)
UNION ALL
SELECT 'AFTER Quotations Material scoped', count(*)
FROM quotations q
WHERE q."Id" IN (SELECT "Id" FROM _reset_material_quotation_ids)
UNION ALL
SELECT 'AFTER QuotationItems Material scoped', count(*)
FROM quotation_items qi
WHERE qi."QuotationId" IN (SELECT "Id" FROM _reset_material_quotation_ids)
UNION ALL
SELECT 'AFTER PurchaseOrders Material scoped', count(*)
FROM "PurchaseOrders" po
WHERE po."Id" IN (SELECT "Id" FROM _reset_material_purchase_order_ids)
UNION ALL
SELECT 'AFTER PurchaseOrderItems Material scoped', count(*)
FROM "PurchaseOrderItems" poi
WHERE poi."Id" IN (SELECT "Id" FROM _reset_material_purchase_order_item_ids)
UNION ALL
SELECT 'AFTER Receipts Material scoped', count(*)
FROM "Receipts" r
WHERE r."Id" IN (SELECT "Id" FROM _reset_material_receipt_ids)
UNION ALL
SELECT 'AFTER ReceiptItems Material scoped', count(*)
FROM "ReceiptItems" ri
WHERE ri."ReceiptId" IN (SELECT "Id" FROM _reset_material_receipt_ids)
   OR ri."PurchaseOrderItemId" IN (SELECT "Id" FROM _reset_material_purchase_order_item_ids)
UNION ALL
SELECT 'AFTER Payments Material scoped', count(*)
FROM "Payments" p
WHERE p."PurchaseOrderId" IN (SELECT "Id" FROM _reset_material_purchase_order_ids)
UNION ALL
SELECT 'AFTER Approvals Material scoped', count(*)
FROM "Approvals" a
WHERE a."Id" IN (SELECT "Id" FROM _reset_material_approval_ids)
UNION ALL
SELECT 'AFTER ApprovalHistories Material scoped', count(*)
FROM "ApprovalHistories" ah
WHERE ah."ApprovalId" IN (SELECT "Id" FROM _reset_material_approval_ids)
UNION ALL
SELECT 'PRESERVED Items after', count(*) FROM items
UNION ALL
SELECT 'PRESERVED Suppliers after', count(*) FROM suppliers
UNION ALL
SELECT 'PRESERVED Works after', count(*) FROM works
UNION ALL
SELECT 'PRESERVED Users after', count(*) FROM users
UNION ALL
SELECT 'PRESERVED Roles after', count(*) FROM roles
UNION ALL
SELECT 'PRESERVED ServiceOrders after', count(*) FROM "ServiceOrders"
UNION ALL
SELECT 'PRESERVED ServiceMeasurements after', count(*) FROM "ServiceMeasurements"
UNION ALL
SELECT 'PRESERVED __EFMigrationsHistory after', count(*) FROM "__EFMigrationsHistory"
ORDER BY item;

COMMIT;
