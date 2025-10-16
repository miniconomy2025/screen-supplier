Feature: Reorder Service

@Reorder
Scenario: Auto reorder is disabled
	Given auto reorder is disabled in configuration
	When the reorder service checks and processes reorders
	Then auto reorder should be disabled in the result
	And no orders should be created

@Reorder
Scenario: Screen stock limit reached, skip reorders
	Given auto reorder is enabled in configuration
	And screen stock check is enabled
	And available screens in stock is 1500
	And max screens before stop ordering is 1000
	When the reorder service checks and processes reorders
	Then no orders should be created
	And the service should log screen stock limit reached

@Reorder
Scenario: No machines available, emergency equipment order
	Given auto reorder is enabled in configuration
	And screen stock check is disabled
	And there are 0 working machines available
	And equipment needs reorder
	And emergency machine can be afforded
	When the reorder service checks and processes reorders
	Then an equipment order should be created
	And it should be logged as emergency equipment order

@Reorder
Scenario: Create material reorders when needed
	Given auto reorder is enabled in configuration
	And screen stock check is disabled
	And there are 2 working machines available
	And sand needs reorder
	And copper needs reorder
	And equipment does not need reorder
	And materials can be afforded
	When the reorder service checks and processes reorders
	Then a sand order should be created
	And a copper order should be created
	And no equipment order should be created

@Reorder
Scenario: Create equipment order when conditions are met
	Given auto reorder is enabled in configuration
	And screen stock check is disabled
	And there are 2 working machines available
	And sand does not need reorder
	And copper does not need reorder
	And equipment needs reorder
	And new machine should be ordered based on materials and finances
	When the reorder service checks and processes reorders
	Then an equipment order should be created

@Reorder
Scenario: Skip equipment order when insufficient materials
	Given auto reorder is enabled in configuration
	And screen stock check is disabled
	And there are 2 working machines available
	And sand does not need reorder
	And copper does not need reorder
	And equipment needs reorder
	And new machine should not be ordered due to insufficient materials
	When the reorder service checks and processes reorders
	Then no equipment order should be created
	And the service should log skipping equipment order

@Reorder
Scenario: Material supplier not available or insufficient funds
	Given auto reorder is enabled in configuration
	And screen stock check is disabled
	And there are 2 working machines available
	And sand needs reorder
	And material supplier is not available for sand
	When the reorder service checks and processes reorders
	Then no sand order should be created
	And the service should log no suitable supplier found