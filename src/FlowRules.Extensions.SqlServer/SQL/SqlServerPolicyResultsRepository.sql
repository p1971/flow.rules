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
--:setvar flowUserPassword XXXXXXXXXXXXXXXXXXXXXXXX
 
:setvar flowSchema flowrules
:setvar flowUserName flowrulesuser
:setvar flowDatabaseName flowEngine
:setvar flowUserPassword test!23!XVCa

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
    CREATE USER [$(flowUserName)] FOR LOGIN [$(flowUserName)] 
END
GO

DROP TABLE IF EXISTS [$(flowSchema)].[FlowRulesRuleResult]
GO

DROP TABLE IF EXISTS [$(flowSchema)].[FlowRulesPolicyResult]
GO

DROP TABLE IF EXISTS [$(flowSchema)].[FlowRulesRequest];
GO

DROP SCHEMA IF EXISTS [$(flowSchema)]
GO

CREATE SCHEMA [$(flowSchema)]
GO

CREATE TABLE [$(flowSchema)].[FlowRulesRequest]
(
	[Id] INT IDENTITY(1,1) NOT NULL, 
    [FlowExecutionId] uniqueidentifier NOT NULL,     
    [PolicyId] NVARCHAR(250) NOT NULL, 
    [Request] NVARCHAR(MAX) NOT NULL,    
    [CreatedAt] DATETIME2 CONSTRAINT DF_FlowRulesRequest_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_FlowRulesRequest] PRIMARY KEY CLUSTERED ([Id])
)
GO

CREATE TABLE [$(flowSchema)].[FlowRulesPolicyResult]
(
	[Id] INT IDENTITY(1,1) NOT NULL, 
    [FlowRulesRequest_Id] INT NOT NULL,
    [PolicyName] NVARCHAR(250) NOT NULL, 
    [Passed] BIT NOT NULL, 
    [Message] NVARCHAR(MAX) NULL,
    [Version] VARCHAR(10) NULL,
    [CreatedAt] DATETIME2 CONSTRAINT DF_FlowRulesPolicyResult_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_FlowRulesPolicyResult] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_FlowRulesPolicyResult_FlowRulesRequest_Id] FOREIGN KEY (FlowRulesRequest_Id) REFERENCES [$(flowSchema)].[FlowRulesRequest]([Id])
)
GO

CREATE TABLE [$(flowSchema)].[FlowRulesRuleResult]
(
	[Id] INT IDENTITY(1,1) NOT NULL, 
    [FlowRulesPolicyResult_Id] INT NOT NULL,
    [RuleId] VARCHAR(250) NOT NULL,
    [RuleName] VARCHAR(250) NOT NULL,
    [RuleDescription] VARCHAR(250) NULL,
    [Passed] BIT NOT NULL, 
    [Message] NVARCHAR(MAX) NULL,
    [Elapsed] TIME NOT NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 CONSTRAINT DF_FlowRulesRuleResult_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_FlowRulesRuleResult] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_FlowRulesRuleResult_FlowRulesPolicyResult] FOREIGN KEY ([FlowRulesPolicyResult_Id]) REFERENCES [$(flowSchema)].[FlowRulesPolicyResult]([Id])
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
