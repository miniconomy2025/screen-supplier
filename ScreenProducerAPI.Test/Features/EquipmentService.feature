Feature: Equipment Service
  Tests for adding, starting, stopping, and managing equipment production.

  Scenario: Initialize equipment parameters successfully
    Given there are no existing equipment parameters
    When I initialize the equipment parameters with input sand 5, copper 10, output screens 20, and weight 100
    Then the initialization should be successful

  Scenario: Add new equipment successfully
    Given valid equipment parameters exist
    When I add equipment for purchase order 1
    Then the equipment should be added successfully

  Scenario: Start production when materials are sufficient
    Given valid equipment parameters exist
    And there are available machines
    And sufficient materials are available
    When I start production
    Then some machines should start producing

