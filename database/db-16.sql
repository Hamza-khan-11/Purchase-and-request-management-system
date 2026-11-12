--CREATE DATABASE Step0Test;
--SELECT name FROM sys.databases;


USE PurchaseApprovalDb;
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES ORDER BY TABLE_NAME;

SELECT name, is_unique, filter_definition
FROM sys.indexes
WHERE object_id = OBJECT_ID('Vendors') AND name IS NOT NULL;



USE PurchaseApprovalDb;
SELECT OBJECT_NAME(object_id) AS TableName, name
FROM sys.indexes
WHERE is_unique = 1 AND name LIKE 'IX_%'
ORDER BY TableName;


USE PurchaseApprovalDb;
SELECT 'Users' AS TableName, COUNT(*) AS TotalRows FROM Users
UNION ALL SELECT 'Vendors', COUNT(*) FROM Vendors
UNION ALL SELECT 'Products', COUNT(*) FROM Products
UNION ALL SELECT 'VendorProducts', COUNT(*) FROM VendorProducts
UNION ALL SELECT 'PurchaseRequests', COUNT(*) FROM PurchaseRequests
UNION ALL SELECT 'RequestItems', COUNT(*) FROM RequestItems
UNION ALL SELECT 'Approvals', COUNT(*) FROM Approvals
UNION ALL SELECT 'PurchaseOrders', COUNT(*) FROM PurchaseOrders;

SELECT Email, Role, LEFT(PasswordHash, 20) AS HashStart FROM Users;

SELECT v.Code, v.Name, COUNT(*) AS Delivered,
       SUM(CASE WHEN po.DeliveredDate <= po.PromisedDeliveryDate THEN 1 ELSE 0 END) AS OnTime
FROM PurchaseOrders po JOIN Vendors v ON v.Id = po.VendorId
WHERE po.Status = 'Delivered'
GROUP BY v.Code, v.Name ORDER BY v.Code;



USE PurchaseApprovalDb;
SELECT TOP 10 Id, UserId, Action, EntityName, Timestamp FROM AuditLogs ORDER BY Id DESC;



SELECT 
    TABLE_SCHEMA, 
    TABLE_NAME, 
    TABLE_TYPE
FROM PurchaseApprovalDb.INFORMATION_SCHEMA.TABLES
ORDER BY TABLE_SCHEMA ;