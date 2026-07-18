-- Reviewed, read-only product query. Values are bound as named parameters only.
SELECT
    c.Id          AS Id,
    c.FullName    AS FullName,
    c.Email       AS Email,
    c.Status      AS Status
FROM dbo.Customers AS c
WHERE c.Id = @id;
