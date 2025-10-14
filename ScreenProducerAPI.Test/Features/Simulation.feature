Feature: Simulation Time Service

@Simulation
Scenario: Start simulation and check initial state
	Given the simulation time service is not running
	When I start the simulation with epoch time 1609459200000 and not resuming
	Then the simulation should be running
	And the current simulation day should be 0

@Simulation
Scenario: Resume simulation that was previously running
	Given the simulation time service is not running
	When I start the simulation with epoch time 1609459200000 and resuming
	Then the simulation should be running

@Simulation
Scenario: Calculate current simulation day based on elapsed time
	Given the simulation started 240000 milliseconds ago
	When I check the current simulation day
	Then the current simulation day should be 2

@Simulation
Scenario: Get simulation date time for day zero
	Given the simulation started and is on day 0
	When I get the simulation date time
	Then the simulation date time should be 2050-01-01

@Simulation
Scenario: Get simulation date time for specific day
	Given the simulation started and is on day 5
	When I get the simulation date time
	Then the simulation date time should be 2050-01-06

@Simulation
Scenario: Stop running simulation
	Given the simulation is running
	When I stop the simulation
	Then the simulation should not be running

@Simulation
Scenario: Get current day when simulation not running
	Given the simulation time service is not running
	When I check the current simulation day
	Then the current simulation day should be 0

@Simulation
Scenario: Destroy simulation cleans up and stops
	Given the simulation is running
	When I destroy the simulation
	Then the simulation should not be running