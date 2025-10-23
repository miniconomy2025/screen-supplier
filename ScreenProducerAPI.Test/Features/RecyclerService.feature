Feature: Recycler Service

@RecyclerService
Scenario: Get materials successfully returns list of materials
	Given the recycler service is available
	When I get materials from the recycler service
	Then the materials operation should succeed
	And the materials list should not be empty
	And all materials should have valid properties

@RecyclerService
Scenario: Get materials fails when service is unavailable
	Given the recycler service is unavailable
	When I get materials from the recycler service
	Then the materials operation should throw RecyclerServiceException
	And the exception message should contain "Recycler service unavailable"

@RecyclerService
Scenario: Get materials fails when service times out
	Given the recycler service times out
	When I get materials from the recycler service
	Then the materials operation should throw RecyclerServiceException
	And the exception message should contain "Recycler service timeout"

@RecyclerService
Scenario: Get materials fails when response format is invalid
	Given the recycler service returns invalid materials response
	When I get materials from the recycler service
	Then the materials operation should throw RecyclerServiceException
	And the exception message should contain "Invalid response format"

@RecyclerService
Scenario: Create order successfully
	Given the recycler service is available
	And I have a valid recycler order request with company "TestCompany"
	And the order has item "Sand" with quantity 100
	When I create a recycler order
	Then the create order operation should succeed
	And the order response should have an order ID
	And the order response should have an account number

@RecyclerService
Scenario: Create order fails with insufficient stock
	Given the recycler service returns insufficient stock error
	And I have a valid recycler order request with company "INSUFFICIENT_STOCK_COMPANY"
	And the order has item "Sand" with quantity 100
	When I create a recycler order
	Then the create order operation should throw InsufficientStockException

@RecyclerService
Scenario: Create order fails when service is unavailable
	Given the recycler service is unavailable
	And I have a valid recycler order request with company "NETWORK_ERROR_COMPANY"
	And the order has item "Sand" with quantity 100
	When I create a recycler order
	Then the create order operation should throw RecyclerServiceException
	And the exception message should contain "Recycler service unavailable"

@RecyclerService
Scenario: Create order fails when service times out
	Given the recycler service times out
	And I have a valid recycler order request with company "TIMEOUT_COMPANY"
	And the order has item "Sand" with quantity 100
	When I create a recycler order
	Then the create order operation should throw RecyclerServiceException
	And the exception message should contain "Recycler service timeout"

@RecyclerService
Scenario: Create order fails with invalid response format
	Given the recycler service returns invalid order creation response
	And I have a valid recycler order request with company "TestCompany"
	And the order has item "Sand" with quantity 100
	When I create a recycler order
	Then the create order operation should throw RecyclerServiceException
	And the exception message should contain "Invalid response format"

@RecyclerService
Scenario: Get orders successfully returns list of orders
	Given the recycler service is available
	And there are existing recycler orders
	When I get all recycler orders
	Then the get orders operation should succeed
	And the orders list should not be empty
	And all orders should have valid properties

@RecyclerService
Scenario: Get orders returns empty list when no orders exist
	Given the recycler service is available
	And there are no existing recycler orders
	When I get all recycler orders
	Then the get orders operation should succeed
	And the orders list should be empty

@RecyclerService
Scenario: Get orders fails when service is unavailable
	Given the recycler service is unavailable
	When I get all recycler orders
	Then the get orders operation should throw RecyclerServiceException
	And the exception message should contain "Recycler service unavailable"

@RecyclerService
Scenario: Get orders fails when service times out
	Given the recycler service times out
	When I get all recycler orders
	Then the get orders operation should throw RecyclerServiceException
	And the exception message should contain "Recycler service timeout"

@RecyclerService
Scenario: Get orders fails with invalid response format
	Given the recycler service returns invalid orders response
	When I get all recycler orders
	Then the get orders operation should throw RecyclerServiceException
	And the exception message should contain "Invalid response format"

@RecyclerService
Scenario: Get order by number successfully
	Given the recycler service is available
	And there is an existing recycler order with number "RECYC-12345678"
	When I get recycler order by number "RECYC-12345678"
	Then the get order by number operation should succeed
	And the order detail should have order number "RECYC-12345678"
	And the order detail should have valid items

@RecyclerService
Scenario: Get order by number returns not found
	Given the recycler service is available
	And there is no recycler order with number "RECYC-NOTFOUND"
	When I get recycler order by number "RECYC-NOTFOUND"
	Then the get order by number operation should throw DataNotFoundException
	And the exception message should contain "Recycler order RECYC-NOTFOUND"

@RecyclerService
Scenario: Get order by number fails when service is unavailable
	Given the recycler service is unavailable
	When I get recycler order by number "RECYC-12345678"
	Then the get order by number operation should throw RecyclerServiceException
	And the exception message should contain "Recycler service unavailable"

@RecyclerService
Scenario: Get order by number fails when service times out
	Given the recycler service times out
	When I get recycler order by number "RECYC-12345678"
	Then the get order by number operation should throw RecyclerServiceException
	And the exception message should contain "Recycler service timeout"

@RecyclerService
Scenario: Get order by number fails with invalid response format
	Given the recycler service returns invalid order detail response
	And there is an existing recycler order with number "RECYC-12345678"
	When I get recycler order by number "RECYC-12345678"
	Then the get order by number operation should throw RecyclerServiceException
	And the exception message should contain "Invalid response format"
