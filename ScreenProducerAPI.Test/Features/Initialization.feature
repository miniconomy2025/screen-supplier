Feature: Bank Integration Service Initialization

@Bank
Scenario: Initialize when no account, loan, or notification URL exists
	Given the bank integration service has no existing account
	And the bank integration service has no existing loan
	And the bank integration service has no notification URL setup
	When the bank integration service initializes
	Then the account should be created successfully
	And the loan should be taken successfully
	And the notification URL should be set to true

@Bank
Scenario: Initialize when account exists but no loan
	Given the bank integration service has an existing account
	And the bank integration service has no existing loan
	And the bank integration service has no notification URL setup
	When the bank integration service initializes
	Then the account should remain as existing
	And the loan should be taken successfully
	And the notification URL should be set to true

@Bank
Scenario: Initialize when account and loan exist
	Given the bank integration service has an existing account
	And the bank integration service has an existing loan
	And the bank integration service has no notification URL setup
	When the bank integration service initializes
	Then the account should remain as existing
	And the loan should remain as existing
	And the notification URL should be set to true

@Bank
Scenario: Initialize when account creation fails
	Given the bank integration service has no existing account
	And the bank account creation will fail
	And the bank integration service has no existing loan
	And the bank integration service has no notification URL setup
	When the bank integration service initializes
	Then the account creation should fail
	And no loan should be attempted
	And the notification URL should be set to true

@Bank
Scenario: Initialize when loan fails but account succeeds
	Given the bank integration service has no existing account
	And the bank integration service has no existing loan
	And the loan taking will fail
	And the bank integration service has no notification URL setup
	When the bank integration service initializes
	Then the account should be created successfully
	And the loan should fail
	And the notification URL should be set to true