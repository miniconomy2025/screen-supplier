Feature: Material Service

@Material
Scenario: Add material when material does not exist
	Given there is no existing material named "sand"
	When I add 100 units of "sand" material
	Then the add material operation should succeed
	And a new material "sand" should be created
	And the material "sand" should have quantity 100

@Material
Scenario: Add material when material already exists
	Given there is an existing material named "copper" with quantity 50
	When I add 75 units of "copper" material
	Then the add material operation should succeed
	And the material "copper" should have quantity 125

@Material
Scenario: Consume material successfully
	Given there is an existing material named "sand" with quantity 200
	When I consume 50 units of "sand" material
	Then the consume material operation should succeed
	And the material "sand" should have quantity 150

@Material
Scenario: Consume material fails when insufficient quantity
	Given there is an existing material named "copper" with quantity 30
	When I consume 50 units of "copper" material
	Then the consume material operation should fail
	And the material "copper" should have quantity 30

@Material
Scenario: Consume material fails when material does not exist
	Given there is no existing material named "aluminum"
	When I consume 10 units of "aluminum" material
	Then the consume material operation should fail

@Material
Scenario: Check sufficient materials returns true
	Given there is an existing material named "sand" with quantity 100
	When I check if there are sufficient "sand" materials with required quantity 50
	Then the sufficient materials check should return true

@Material
Scenario: Check sufficient materials returns false when quantity insufficient
	Given there is an existing material named "copper" with quantity 30
	When I check if there are sufficient "copper" materials with required quantity 50
	Then the sufficient materials check should return false

@Material
Scenario: Check sufficient materials returns false when material does not exist
	Given there is no existing material named "aluminum"
	When I check if there are sufficient "aluminum" materials with required quantity 10
	Then the sufficient materials check should return false

@Material
Scenario: Get material by name successfully
	Given there is an existing material named "sand" with quantity 100
	When I get the material named "sand"
	Then the retrieved material should not be null
	And the retrieved material should have name "sand"

@Material
Scenario: Get material by name returns null when not found
	Given there is no existing material named "aluminum"
	When I get the material named "aluminum"
	Then the retrieved material should be null

@Material
Scenario: Get all materials
	Given there is an existing material named "sand" with quantity 100
	And there is an existing material named "copper" with quantity 50
	When I get all materials
	Then all materials should contain 2 items

@Material
Scenario: Get average cost per kg with purchase history
	Given there is an existing material named "sand" with quantity 100
	And there are purchase orders for "sand" with unit prices 80,90,100,110,120
	When I get the average cost per kg for "sand"
	Then the average cost should be 100

@Material
Scenario: Get average cost per kg without purchase history
	Given there is no existing material named "sand"
	When I get the average cost per kg for "sand"
	Then the average cost should be default value 100
