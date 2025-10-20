Feature: Production History Management
  This feature verifies that the ProductionHistoryService
  correctly retrieves and stores daily production history.

  Background:
    Given a fresh in-memory database

  Scenario: Retrieve existing production history by date
    Given a production history record exists for "2025-10-15"
    When I get the production history for "2025-10-15"
    Then the result should not be null
    And the record date should be "2025-10-15"

 
