Feature: Screen Order Service

@ScreenOrder
Scenario: Create screen order successfully
	Given there is a product with price 100
	And there are 500 screens available in stock
	When I create a screen order with quantity 50
	Then the screen order should be created successfully
	And the created order should have quantity 50
	And the created order should have status "waiting_payment"

@ScreenOrder
Scenario: Create screen order fails when no product available
	Given there is no product available
	And there are 500 screens available in stock
	When I create a screen order with quantity 50
	Then an exception should be thrown with type "SystemConfigurationException"

@ScreenOrder
Scenario: Create screen order fails when insufficient stock
	Given there is a product with price 100
	And there are 30 screens available in stock
	When I create a screen order with quantity 50
	Then an exception should be thrown with type "InsufficientStockException"

@ScreenOrder
Scenario: Create screen order fails with invalid quantity
	Given there is a product with price 100
	And there are 500 screens available in stock
	When I create a screen order with quantity 0
	Then an exception should be thrown with type "InvalidRequestException"

@ScreenOrder
Scenario: Process payment confirmation with full payment
	Given there is a product with price 100
	And there is a screen order with id 1 in status "waiting_payment" with quantity 10 and unit price 100
	And there is a bank account
	When I process payment confirmation with amount 1000 for order 1
	Then the payment confirmation should be successful
	And the payment response should indicate order is fully paid
	And screen order 1 should have status "waiting_collection"

@ScreenOrder
Scenario: Process payment confirmation with partial payment
	Given there is a product with price 100
	And there is a screen order with id 1 in status "waiting_payment" with quantity 10 and unit price 100
	And there is a bank account
	When I process payment confirmation with amount 500 for order 1
	Then the payment confirmation should be successful
	And the payment response should indicate order has remaining balance
	And screen order 1 should have status "waiting_payment"

@ScreenOrder
Scenario: Process payment confirmation with multiple payments
	Given there is a product with price 100
	And there is a screen order with id 1 in status "waiting_payment" with quantity 10 and unit price 100
	And screen order 1 has amount paid 600
	And there is a bank account
	When I process payment confirmation with amount 400 for order 1
	Then the payment confirmation should be successful
	And the payment response should indicate order is fully paid
	And screen order 1 should have status "waiting_collection"

@ScreenOrder
Scenario: Process payment confirmation for non-existent order
	Given there is a bank account
	When I process payment confirmation with amount 1000 for order 999
	Then the payment confirmation should not be successful

@ScreenOrder
Scenario: Find screen order by id successfully
	Given there is a product with price 100
	And there is a screen order with id 1 in status "waiting_payment" with quantity 10 and unit price 100
	When I find screen order by id 1
	Then the retrieved order should not be null
	And the retrieved order should have id 1

@ScreenOrder
Scenario: Find screen order by id fails when not found
	Given there is a product with price 100
	When I find screen order by id 999
	Then an exception should be thrown with type "OrderNotFoundException"

@ScreenOrder
Scenario: Update screen order status successfully
	Given there is a product with price 100
	And there is a screen order with id 1 in status "waiting_payment" with quantity 10 and unit price 100
	When I update status of order 1 to "waiting_collection"
	Then the update status operation should succeed
	And screen order 1 should have status "waiting_collection"

@ScreenOrder
Scenario: Update screen order status fails for non-existent order
	When I update status of order 999 to "waiting_collection"
	Then the update status operation should fail

@ScreenOrder
Scenario: Update screen order payment successfully
	Given there is a product with price 100
	And there is a screen order with id 1 in status "waiting_payment" with quantity 10 and unit price 100
	When I update payment of order 1 to 500
	Then the update payment operation should succeed

@ScreenOrder
Scenario: Update quantity collected successfully
	Given there is a product with price 100
	And there is a screen order with id 1 in status "waiting_collection" with quantity 10 and unit price 100
	When I update quantity collected of order 1 by 5
	Then the update quantity collected operation should succeed
	And screen order 1 should have quantity collected 5

@ScreenOrder
Scenario: Update quantity collected marks order as collected
	Given there is a product with price 100
	And there is a screen order with id 1 in status "waiting_collection" with quantity 10 and unit price 100
	And screen order 1 has quantity collected 8
	When I update quantity collected of order 1 by 2
	Then the update quantity collected operation should succeed
	And screen order 1 should have quantity collected 10
	And screen order 1 should have status "collected"

@ScreenOrder
Scenario: Get active screen orders
	Given there is a product with price 100
	And there is a screen order with id 1 in status "waiting_payment" with quantity 10 and unit price 100
	And there is a screen order with id 2 in status "waiting_collection" with quantity 5 and unit price 100
	And there is a screen order with id 3 in status "collected" with quantity 3 and unit price 100
	When I get active screen orders
	Then the retrieved orders should contain 2 items

@ScreenOrder
Scenario: Get orders by status
	Given there is a product with price 100
	And there is a screen order with id 1 in status "waiting_payment" with quantity 10 and unit price 100
	And there is a screen order with id 2 in status "waiting_payment" with quantity 5 and unit price 100
	And there is a screen order with id 3 in status "waiting_collection" with quantity 3 and unit price 100
	When I get orders by status "waiting_payment"
	Then the retrieved orders should contain 2 items
