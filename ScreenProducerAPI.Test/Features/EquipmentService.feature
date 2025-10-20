Feature: Equipment Service
  Tests for adding, starting, stopping, and managing equipment production.

  Scenario: Initialize equipment parameters successfully
    Given there are no existing equipment parameters
    When I initialize the equipment parameters with input sand 5, copper 10, output screens 20, and weight 100
    Then the initialization should be successful
