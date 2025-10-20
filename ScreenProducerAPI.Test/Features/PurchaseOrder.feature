Feature: Purchase Order Service

@PurchaseOrder
Scenario: Create purchase order successfully for material
	Given the database has order status "requires_payment_supplier"
	And the simulation time is "2025-01-15 10:30:00"
	When I create a purchase order with order ID 123, quantity 100, unit price 50, bank account "test-account", origin "test-supplier", material ID 1, and equipment order false
	Then the purchase order should be created successfully
	And the order should have order ID 123
	And the order should have quantity 100
	And the order should have unit price 50
	And the order should have bank account "test-account"
	And the order should have origin "test-supplier"
	And the order should have material ID 1
	And the order should not be an equipment order

@PurchaseOrder
Scenario: Fail to create purchase order when required status missing
	Given the database does not have order status "requires_payment_supplier"
	When I create a purchase order with order ID 789, quantity 50, unit price 25, bank account "test-account", origin "test-supplier", material ID 2, and equipment order false
	Then the purchase order should not be created
	And the result should be null

@PurchaseOrder
Scenario: Find purchase order by shipment ID successfully
	Given there is a purchase order with shipment ID 100
	When I find purchase order by shipment ID 100
	Then the purchase order should be found
	And the order should have shipment ID 100

@PurchaseOrder
Scenario: Find purchase order by shipment ID returns null when not found
	Given there is no purchase order with shipment ID 999
	When I find purchase order by shipment ID 999
	Then no purchase order should be found

@PurchaseOrder
Scenario: Update shipment ID successfully
	Given there is a purchase order with ID <orderID>
	When I update shipment ID to <shipmentID> for purchase order <orderID>
	Then the shipment ID update should succeed
	And the order should have shipment ID <shipmentID>

Examples:
	| orderID | shipmentID |
	| 1       | 201        |
	| 2       | 202        |
	| 3       | 203        |

@PurchaseOrder
Scenario: Update shipment ID fails when order not found
	Given there is no purchase order with ID 999
	When I update shipment ID to 500 for purchase order 999
	Then the shipment ID update should fail

@PurchaseOrder
Scenario Outline: Update delivery quantity and mark as delivered when fully delivered
	Given there is a purchase order with ID <orderID> and quantity <totalQuantity>
	And the database has order status "delivered"
	When I update delivery quantity by <deliveryQuantity> for purchase order <orderID>
	Then the delivery quantity update should succeed
	And the order should have delivered quantity <expectedDelivered>
	And the order should have status <expectedStatus>

Examples:
	| orderID | totalQuantity | deliveryQuantity | expectedDelivered | expectedStatus |
	| 1       | 100           | 50               | 50                | requires_payment_supplier |
	| 2       | 100           | 100              | 100               | delivered |
	| 3       | 100           | 150              | 150               | delivered |

@PurchaseOrder
Scenario: Update delivery quantity fails when order not found
	Given there is no purchase order with ID 999
	When I update delivery quantity by 50 for purchase order 999
	Then the delivery quantity update should fail

@PurchaseOrder
Scenario: Update order status fails when order not found
	Given there is no purchase order with ID 999
	And the database has order status "delivered"
	When I update status to "delivered" for purchase order 999
	Then the status update should fail

@PurchaseOrder
Scenario: Update order status fails when status not found
	Given there is a purchase order with ID 1
	And the database does not have order status "invalid_status"
	When I update status to "invalid_status" for purchase order 1
	Then the status update should fail

@PurchaseOrder
Scenario Outline: Update order shipping details successfully
	Given there is a purchase order with ID <orderID>
	When I update shipping details with bank account "<bankAccount>" and shipping price <shippingPrice> for purchase order <orderID>
	Then the shipping details update should succeed
	And the order should have shipper bank account "<bankAccount>"
	And the order should have shipping price <shippingPrice>

Examples:
	| orderID | bankAccount | shippingPrice |
	| 1       | shipper-123 | 25           |
	| 2       | logistics-456 | 50         |

@PurchaseOrder
Scenario: Update order shipping details fails when order not found
	Given there is no purchase order with ID 999
	When I update shipping details with bank account "test-shipper" and shipping price 30 for purchase order 999
	Then the shipping details update should fail

@PurchaseOrder
Scenario: Get purchase order by ID successfully
	Given there is a purchase order with ID 1
	When I get purchase order by ID 1
	Then the purchase order should be retrieved successfully

@PurchaseOrder
Scenario: Get purchase order by ID returns null when not found
	Given there is no purchase order with ID 999
	When I get purchase order by ID 999
	Then no purchase order should be retrieved

@PurchaseOrder
Scenario: Get active purchase orders excludes delivered orders
	Given there are purchase orders with various statuses
	When I get active purchase orders
	Then only non-delivered orders should be returned
	And delivered orders should not be included

@PurchaseOrder
Scenario: Get all orders includes all orders regardless of status
	Given there are purchase orders with various statuses
	When I get all orders
	Then all orders should be returned including delivered ones

@PurchaseOrder
Scenario: Get past orders returns limited results ordered by date
	Given there are multiple purchase orders with different dates
	When I get past orders for a specific date
	Then orders should be returned in descending date order
	And no more than 100 orders should be returned