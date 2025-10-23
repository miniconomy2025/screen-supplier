Feature: Queue Commands
	As a system
	I want to execute queue commands for purchase orders
	So that orders can be processed through their lifecycle

@QueueCommands
Scenario: Process Supplier Payment Command succeeds
	Given there is a purchase order with id 1 in status "requires_payment_supplier"
	And the bank service will succeed for supplier payment
	When I execute ProcessSupplierPaymentCommand for purchase order 1
	Then the command result should be successful
	And the purchase order 1 status should be "requires_delivery"

@QueueCommands
Scenario: Process Supplier Payment Command fails
	Given there is a purchase order with id 2 in status "requires_payment_supplier"
	And the bank service will fail for supplier payment
	When I execute ProcessSupplierPaymentCommand for purchase order 2
	Then the command result should be failed
	And the purchase order 2 status should still be "requires_payment_supplier"

@QueueCommands
Scenario: Process Supplier Payment Command with exception
	Given there is a purchase order with id 3 in status "requires_payment_supplier"
	And the bank service will throw exception for supplier payment
	When I execute ProcessSupplierPaymentCommand for purchase order 3
	Then the command result should be failed
	And the error message should contain exception details

@QueueCommands
Scenario: Process Logistics Payment Command succeeds
	Given there is a purchase order with id 4 in status "requires_payment_delivery"
	And the purchase order 4 has shipment id 1001
	And the purchase order 4 has shipper bank account "SHIP-BANK-123"
	And the purchase order 4 has shipping price 500
	And the bank service will succeed for logistics payment
	When I execute ProcessLogisticsPaymentCommand for purchase order 4
	Then the command result should be successful
	And the purchase order 4 status should be "waiting_delivery"

@QueueCommands
Scenario: Process Logistics Payment Command fails
	Given there is a purchase order with id 5 in status "requires_payment_delivery"
	And the purchase order 5 has shipment id 1002
	And the purchase order 5 has shipper bank account "SHIP-BANK-456"
	And the purchase order 5 has shipping price 600
	And the bank service will fail for logistics payment
	When I execute ProcessLogisticsPaymentCommand for purchase order 5
	Then the command result should be failed
	And the purchase order 5 status should still be "requires_payment_delivery"

@QueueCommands
Scenario: Process Logistics Payment Command with exception
	Given there is a purchase order with id 6 in status "requires_payment_delivery"
	And the purchase order 6 has shipment id 1003
	And the purchase order 6 has shipper bank account "SHIP-BANK-789"
	And the purchase order 6 has shipping price 700
	And the bank service will throw exception for logistics payment
	When I execute ProcessLogisticsPaymentCommand for purchase order 6
	Then the command result should be failed
	And the error message should contain exception details

@QueueCommands
Scenario: Process Shipping Request Command succeeds for equipment order
	Given there is a purchase order with id 7 in status "requires_delivery"
	And the purchase order 7 is an equipment order
	And the equipment service has valid parameters
	And the logistics service will succeed for pickup request
	When I execute ProcessShippingRequestCommand for purchase order 7
	Then the command result should be successful
	And the purchase order 7 status should be "requires_payment_delivery"
	And the purchase order 7 should have shipment id
	And the purchase order 7 should have shipping details

@QueueCommands
Scenario: Process Shipping Request Command succeeds for raw material order
	Given there is a purchase order with id 8 in status "requires_delivery"
	And the purchase order 8 is a raw material order with material "Sand"
	And the equipment service has valid parameters
	And the logistics service will succeed for pickup request
	When I execute ProcessShippingRequestCommand for purchase order 8
	Then the command result should be successful
	And the purchase order 8 status should be "requires_payment_delivery"
	And the purchase order 8 should have shipment id
	And the purchase order 8 should have shipping details

@QueueCommands
Scenario: Process Shipping Request Command fails with invalid configuration
	Given there is a purchase order with id 9 in status "requires_delivery"
	And the purchase order 9 is an equipment order
	And the equipment service has no parameters
	When I execute ProcessShippingRequestCommand for purchase order 9
	Then the command result should be failed without retry
	And the error message should contain "Invalid purchase order configuration"

@QueueCommands
Scenario: Process Shipping Request Command fails with logistics exception
	Given there is a purchase order with id 10 in status "requires_delivery"
	And the purchase order 10 is an equipment order
	And the equipment service has valid parameters
	And the logistics service will throw exception for pickup request
	When I execute ProcessShippingRequestCommand for purchase order 10
	Then the command result should be failed
	And the error message should contain exception details

@QueueCommands
Scenario: NoOp Command for terminal state
	Given there is a purchase order with id 11 in status "delivered"
	When I execute NoOpCommand for purchase order 11
	Then the command result should be successful

@QueueCommands
Scenario: NoOp Command for waiting state
	Given there is a purchase order with id 12 in status "waiting_delivery"
	When I execute NoOpCommand for purchase order 12
	Then the command result should be successful

@QueueCommands
Scenario: Queue Command Factory creates ProcessSupplierPaymentCommand
	Given there is a purchase order with id 13 in status "requires_payment_supplier"
	When I create a command using the factory for purchase order 13
	Then the command should be of type ProcessSupplierPaymentCommand

@QueueCommands
Scenario: Queue Command Factory creates ProcessShippingRequestCommand
	Given there is a purchase order with id 14 in status "requires_delivery"
	When I create a command using the factory for purchase order 14
	Then the command should be of type ProcessShippingRequestCommand

@QueueCommands
Scenario: Queue Command Factory creates ProcessLogisticsPaymentCommand
	Given there is a purchase order with id 15 in status "requires_payment_delivery"
	When I create a command using the factory for purchase order 15
	Then the command should be of type ProcessLogisticsPaymentCommand

@QueueCommands
Scenario: Queue Command Factory creates NoOpCommand for terminal state
	Given there is a purchase order with id 16 in status "delivered"
	When I create a command using the factory for purchase order 16
	Then the command should be of type NoOpCommand

@QueueCommands
Scenario: Process Supplier Payment with calculated total amount
	Given there is a purchase order with id 17 in status "requires_payment_supplier"
	And the purchase order 17 has quantity 10 and unit price 100
	And the bank service will succeed for supplier payment
	When I execute ProcessSupplierPaymentCommand for purchase order 17
	Then the command result should be successful
	And the bank service should have been called with amount 1000

@QueueCommands
Scenario: Process Logistics Payment with correct description format
	Given there is a purchase order with id 18 in status "requires_payment_delivery"
	And the purchase order 18 has shipment id 2001
	And the purchase order 18 has shipper bank account "SHIP-BANK-999"
	And the purchase order 18 has shipping price 800
	And the bank service will succeed for logistics payment
	When I execute ProcessLogisticsPaymentCommand for purchase order 18
	Then the command result should be successful
	And the bank service should have been called with description "2001"
