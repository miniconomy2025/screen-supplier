Feature: Purchase Order Queue Service

@Queue
Scenario: Enqueue a purchase order
	Given the queue is empty
	When I enqueue purchase order 1
	Then the queue count should be 1

@Queue
Scenario: Enqueue multiple purchase orders
	Given the queue is empty
	When I enqueue purchase order 1
	And I enqueue purchase order 2
	And I enqueue purchase order 3
	Then the queue count should be 3

@Queue
Scenario: Process queue successfully
	Given the queue processing is enabled
	And there is a purchase order with id 1 in status "requires_payment_supplier"
	And the command for purchase order 1 will succeed
	And purchase order 1 is already in the queue
	When I process the queue
	Then the queue should be empty after processing
	And the command should have been executed for purchase order 1

@Queue
Scenario: Process queue when processing is disabled
	Given the queue processing is disabled
	And purchase order 1 is already in the queue
	When I process the queue
	Then the queue should not be processed

@Queue
Scenario: Process queue with failed command that should retry
	Given the queue processing is enabled
	And the maximum retries is set to 3
	And there is a purchase order with id 1 in status "requires_payment_supplier"
	And the command for purchase order 1 will fail with retry
	And purchase order 1 is already in the queue
	When I process the queue
	Then purchase order 1 should be re-enqueued
	And the command should have been executed for purchase order 1

@Queue
Scenario: Process queue with failed command that should not retry
	Given the queue processing is enabled
	And there is a purchase order with id 1 in status "requires_payment_supplier"
	And the command for purchase order 1 will fail without retry
	And purchase order 1 is already in the queue
	When I process the queue
	Then the queue should be empty after processing
	And the command should have been executed for purchase order 1

@Queue
Scenario: Populate queue from database with active orders
	Given there is a purchase order with id 1 in status "requires_payment_supplier"
	And there is a purchase order with id 2 in status "requires_delivery"
	And there is a purchase order with id 3 in status "waiting_delivery"
	And the queue is empty
	When I populate the queue from database
	Then the queue count should be 3

@Queue
Scenario: Populate queue from database excludes delivered orders
	Given there is a purchase order with id 1 in status "requires_payment_supplier"
	And there is a purchase order with id 2 in status "delivered"
	And the queue is empty
	When I populate the queue from database
	Then the queue count should be 1

@Queue
Scenario: Get queue count
	Given the queue is empty
	When I enqueue purchase order 1
	And I enqueue purchase order 2
	And I get the queue count
	Then the queue count should be 2

@Queue
Scenario: Process empty queue
	Given the queue is empty
	And the queue processing is enabled
	When I process the queue
	Then the queue should be empty after processing

@Queue
Scenario: Process queue with terminal status order
	Given the queue processing is enabled
	And there is a purchase order with id 1 in status "delivered"
	And the command for purchase order 1 will succeed
	And purchase order 1 is already in the queue
	When I process the queue
	Then the queue should be empty after processing
