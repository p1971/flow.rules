
-- This needs to be run in sqlcmd mode
--
-- You can either use an existing database or create a new one
--
-- The db objects can be created as follows:
-- sqlcmd -d [databasename] -v flowSchema="[schemaname]" -v flowUserPassword="[userpassword]"
-- replacing databasename, schemaname and userpassword with appropriate values.
--
-- For example
-- sqlcmd -U sa -P zzzzzzz -S server,port -v flowUserPassword="XXXXXXXXX" -i SqlServerStateRepository.sql

-- Instead of passing the parameters on the cmd line you can uncomment the below lines
-- :setvar flowUserPassword XXXXXXXXXXXXXXXXX
 
:setvar flowSchema flowrules
:setvar flowUserName flowrulesuser
:setvar flowDatabaseName flowrules

IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '$(flowDatabaseName)')
BEGIN
    CREATE DATABASE [$(flowDatabaseName)]
END
GO

USE [$(flowDatabaseName)]
GO

DROP USER IF EXISTS $(flowUserName);
GO

IF EXISTS
    (SELECT name
     FROM sys.database_principals
     WHERE name = '$(flowUserName)')
BEGIN
    DROP LOGIN $(flowUserName);
END
GO

IF NOT EXISTS (SELECT name FROM sys.sql_logins WHERE name='$(flowUserName)')
BEGIN
    CREATE LOGIN $(flowUserName) WITH PASSWORD = '$(flowUserPassword)'
END
GO

IF NOT EXISTS
    (SELECT name
     FROM sys.database_principals
     WHERE name = '$(flowUserName)')
BEGIN
    CREATE USER [$(flowUserName)] FOR LOGIN [$(flowUserName)] WITH DEFAULT_SCHEMA=[$(flowSchema)] 
END
GO

DROP TABLE IF EXISTS [$(flowSchema)].[Rule];
GO

DROP TABLE IF EXISTS [$(flowSchema)].[Policy];
GO

DROP TABLE IF EXISTS [$(flowSchema)].[RuleResult]
GO

DROP TABLE IF EXISTS [$(flowSchema)].[PolicyResult]
GO

DROP TABLE IF EXISTS [$(flowSchema)].[Request];
GO

DROP SCHEMA IF EXISTS [$(flowSchema)]
GO

CREATE SCHEMA [$(flowSchema)]
GO

CREATE TABLE [$(flowSchema)].[Policy]
(
	[Id] INT IDENTITY(1,1) NOT NULL, 
    [PolicyId] VARCHAR(20) NOT NULL,     
    [PolicyVersion] VARCHAR(20) NOT NULL, 
    [CreatedAt] DATETIME2 CONSTRAINT DF_Policy_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Policy] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Policy_PolicyId_PolicyVersion] UNIQUE (PolicyId, PolicyVersion) 
)
GO

CREATE TABLE [$(flowSchema)].[Rule]
(
	[Id] INT IDENTITY(1,1) NOT NULL, 
    [Policy_Id] INT NOT NULL,     
    [RuleId] VARCHAR(20) NOT NULL,         
    [Name] VARCHAR(20) NOT NULL,         
    [Source] VARCHAR(MAX) NOT NULL,         
    [CreatedAt] DATETIME2 CONSTRAINT DF_Rule_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Rule] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Rule_RuleId_Name] UNIQUE (RuleId, Policy_Id, Name),
    CONSTRAINT [FK_Rule_Policy_Id] FOREIGN KEY (Policy_Id) REFERENCES [$(flowSchema)].[Policy]([Id])
)
GO

CREATE TABLE [$(flowSchema)].[Request]
(
	[Id] INT IDENTITY(1,1) NOT NULL, 
    [FlowExecutionId] uniqueidentifier NOT NULL,     
    [CorrelationId] varchar(200) NOT NULL,     
    [PolicyId] VARCHAR(20) NOT NULL, 
    [Request] NVARCHAR(MAX) NOT NULL,    
    [CreatedAt] DATETIME2 CONSTRAINT DF_Request_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Request] PRIMARY KEY CLUSTERED ([Id])
)
GO

CREATE TABLE [$(flowSchema)].[PolicyResult]
(
	[Id] INT IDENTITY(1,1) NOT NULL, 
    [Request_Id] INT NOT NULL,
    [PolicyName] NVARCHAR(50) NOT NULL, 
    [Passed] BIT NOT NULL, 
    [Version] VARCHAR(20) NULL,
    [CreatedAt] DATETIME2 CONSTRAINT DF_PolicyResult_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_PolicyResult] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_PolicyResult_Request_Id] FOREIGN KEY (Request_Id) REFERENCES [$(flowSchema)].[Request]([Id])
)
GO

CREATE TABLE [$(flowSchema)].[RuleResult]
(
	[Id] INT IDENTITY(1,1) NOT NULL, 
    [PolicyResult_Id] INT NOT NULL,
    [RuleId] VARCHAR(20) NOT NULL,
    [RuleName] VARCHAR(50) NOT NULL,
    [RuleDescription] VARCHAR(250) NULL,
    [Passed] BIT NOT NULL, 
    [Message] NVARCHAR(MAX) NULL,
    [Elapsed] TIME NOT NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 CONSTRAINT DF_RuleResult_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_RuleResult] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_RuleResult_PolicyResult] FOREIGN KEY ([PolicyResult_Id]) REFERENCES [$(flowSchema)].[PolicyResult]([Id])
)
GO

GRANT SELECT, INSERT, UPDATE ON SCHEMA:: [$(flowSchema)] TO [$(flowUserName)]
GO

SELECT 
    schema_name = schema_name(t.schema_id),
    table_name = t.name,
    t.create_date,
    t.modify_date
FROM sys.tables t
WHERE schema_name(t.schema_id) = '$(flowSchema)'
ORDER BY table_name;
GO
