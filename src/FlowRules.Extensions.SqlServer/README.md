# Flow Rules

An implementation of the [IPolicyResultsRepository](https://github.com/p1971/flow.rules/blob/main/src/FlowRules.Engine/Interfaces/IPolicyResultsRepository.cs) for storing the results of a policy execution.

> [!WARNING]
> `SQL/SqlServerPolicyResultsRepository.sql` is an initial-creation script, not a migration. It drops and recreates the FlowRules tables, permanently deleting all existing audit history if it is run again.

See the [github repository](https://github.com/p1971/flow.rules) for further information.
