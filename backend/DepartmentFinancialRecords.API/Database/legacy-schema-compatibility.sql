-- One-time compatibility migration for the existing department_financial_records schema.
-- Legacy columns are retained so their original values remain available.

ALTER TABLE Students
    ADD COLUMN FirstName LONGTEXT NULL,
    ADD COLUMN LastName LONGTEXT NULL,
    ADD COLUMN ContactNumber LONGTEXT NULL,
    ADD COLUMN Email VARCHAR(256) NULL,
    ADD COLUMN IsActive TINYINT(1) NULL;

UPDATE Students
SET FirstName = TRIM(SUBSTRING_INDEX(FullName, ' ', 1)),
    LastName = TRIM(IF(LOCATE(' ', TRIM(FullName)) = 0, '', SUBSTRING(TRIM(FullName), LOCATE(' ', TRIM(FullName)) + 1))),
    ContactNumber = '',
    Email = '',
    IsActive = CASE WHEN LOWER(TRIM(Status)) = 'active' THEN 1 ELSE 0 END,
    RfidUid = CASE WHEN TRIM(COALESCE(RfidUid, '')) = '' THEN LEFT(COALESCE(Rfid, ''), 128) ELSE RfidUid END;

ALTER TABLE Students
    MODIFY COLUMN FirstName LONGTEXT NOT NULL,
    MODIFY COLUMN LastName LONGTEXT NOT NULL,
    MODIFY COLUMN ContactNumber LONGTEXT NOT NULL,
    MODIFY COLUMN Email VARCHAR(256) NOT NULL,
    MODIFY COLUMN IsActive TINYINT(1) NOT NULL;

ALTER TABLE AttendanceEvents
    ADD COLUMN Title VARCHAR(200) NULL,
    ADD COLUMN Description LONGTEXT NULL;

UPDATE AttendanceEvents
SET Title = LEFT(EventName, 200), Description = '';

ALTER TABLE AttendanceEvents
    MODIFY COLUMN Title VARCHAR(200) NOT NULL,
    MODIFY COLUMN Description LONGTEXT NOT NULL;

ALTER TABLE AttendanceRecords
    ADD COLUMN RecordedAt DATETIME(6) NULL,
    ADD COLUMN Remarks LONGTEXT NULL,
    ADD COLUMN LegacyStatus LONGTEXT NULL;

UPDATE AttendanceRecords
SET RecordedAt = CheckIn,
    Remarks = '',
    LegacyStatus = Status,
    Status = CASE LOWER(TRIM(Status))
        WHEN 'present' THEN '0'
        WHEN 'absent' THEN '1'
        WHEN 'late' THEN '2'
        WHEN 'excused' THEN '3'
        ELSE NULL
    END;

ALTER TABLE AttendanceRecords
    MODIFY COLUMN Status INT NOT NULL,
    MODIFY COLUMN RecordedAt DATETIME(6) NOT NULL,
    MODIFY COLUMN Remarks LONGTEXT NOT NULL;

ALTER TABLE Fines
    ADD COLUMN Category LONGTEXT NULL,
    ADD COLUMN DateIssued DATETIME(6) NULL,
    ADD COLUMN Remarks LONGTEXT NULL;

UPDATE Fines
SET Category = Reason,
    DateIssued = UTC_TIMESTAMP(6),
    Remarks = '';

ALTER TABLE Fines
    MODIFY COLUMN Category LONGTEXT NOT NULL,
    MODIFY COLUMN DateIssued DATETIME(6) NOT NULL,
    MODIFY COLUMN Remarks LONGTEXT NOT NULL;

ALTER TABLE Collections
    ADD COLUMN AmountPaid DECIMAL(65,30) NULL,
    ADD COLUMN PaymentDate DATETIME(6) NULL,
    ADD COLUMN CollectorName LONGTEXT NULL,
    ADD COLUMN ReceiptNumber LONGTEXT NULL,
    ADD COLUMN Category LONGTEXT NULL;

UPDATE Collections
SET AmountPaid = Amount,
    PaymentDate = CollectionDate,
    CollectorName = '',
    ReceiptNumber = '',
    Category = CollectionType;

ALTER TABLE Collections
    MODIFY COLUMN AmountPaid DECIMAL(65,30) NOT NULL,
    MODIFY COLUMN PaymentDate DATETIME(6) NOT NULL,
    MODIFY COLUMN CollectorName LONGTEXT NOT NULL,
    MODIFY COLUMN ReceiptNumber LONGTEXT NOT NULL,
    MODIFY COLUMN Category LONGTEXT NOT NULL;

ALTER TABLE Collectibles
    ADD COLUMN IsPaid TINYINT(1) NULL;

UPDATE Collectibles
SET IsPaid = CASE WHEN LOWER(TRIM(Status)) IN ('paid', 'settled', 'true', '1') THEN 1 ELSE 0 END;

ALTER TABLE Collectibles
    MODIFY COLUMN IsPaid TINYINT(1) NOT NULL;

ALTER TABLE FundTransactions
    ADD COLUMN Source LONGTEXT NULL,
    ADD COLUMN Remarks LONGTEXT NULL,
    ADD COLUMN LegacyTransactionType LONGTEXT NULL;

UPDATE FundTransactions
SET LegacyTransactionType = TransactionType,
    Source = '',
    Remarks = Description,
    TransactionType = CASE LOWER(REPLACE(TRIM(TransactionType), ' ', ''))
        WHEN 'beginningbalance' THEN '0'
        WHEN 'addition' THEN '1'
        WHEN 'deduction' THEN '2'
        ELSE NULL
    END;

ALTER TABLE FundTransactions
    MODIFY COLUMN TransactionType INT NOT NULL,
    MODIFY COLUMN Source LONGTEXT NOT NULL,
    MODIFY COLUMN Remarks LONGTEXT NOT NULL;

ALTER TABLE Disbursements
    ADD COLUMN DateReleased DATETIME(6) NULL,
    ADD COLUMN DocumentPath LONGTEXT NULL,
    ADD COLUMN IsApproved TINYINT(1) NULL,
    ADD COLUMN LegacyStatus LONGTEXT NULL;

UPDATE Disbursements
SET DateReleased = DisbursementDate,
    DocumentPath = '',
    LegacyStatus = Status,
    IsApproved = CASE WHEN LOWER(TRIM(Status)) IN ('approved', 'true', '1') THEN 1 ELSE 0 END;

ALTER TABLE Disbursements
    MODIFY COLUMN DateReleased DATETIME(6) NOT NULL,
    MODIFY COLUMN DocumentPath LONGTEXT NOT NULL,
    MODIFY COLUMN IsApproved TINYINT(1) NOT NULL;

ALTER TABLE ActivityLogs
    ADD COLUMN UserName LONGTEXT NULL,
    ADD COLUMN Timestamp DATETIME(6) NULL,
    ADD COLUMN Details LONGTEXT NULL;

UPDATE ActivityLogs AS logs
JOIN Users AS users ON users.Id = logs.UserId
SET logs.UserName = users.Username,
    logs.Timestamp = logs.CreatedAt,
    logs.Details = '';

ALTER TABLE ActivityLogs
    MODIFY COLUMN UserName LONGTEXT NOT NULL,
    MODIFY COLUMN Timestamp DATETIME(6) NOT NULL,
    MODIFY COLUMN Details LONGTEXT NOT NULL;

ALTER TABLE Users
    ADD COLUMN IsActive TINYINT(1) NULL,
    ADD COLUMN LegacyRole LONGTEXT NULL;

UPDATE Users
SET LegacyRole = Role,
    IsActive = 1,
    Role = CASE LOWER(TRIM(Role))
        WHEN 'administrator' THEN '0'
        WHEN 'treasurer' THEN '1'
        WHEN 'officer' THEN '2'
        WHEN 'student' THEN '3'
        ELSE NULL
    END;

ALTER TABLE Users
    MODIFY COLUMN Role INT NOT NULL,
    MODIFY COLUMN IsActive TINYINT(1) NOT NULL;