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

  Scenario: Retrieve non-existent production history by date
    When I get the production history for "2025-10-16"
    Then the result should be null

  Scenario: Store new daily production history
    Given no production history exists for "2025-10-17"
    And materials, products, and equipment exist in the system
    When I store daily production history for "2025-10-17" with 100 screens produced
    Then a new production history record should be created
    And it should record 100 screens produced

  Scenario: Update existing production history record
    Given a production history record exists for "2025-10-18"
    And materials, products, and equipment exist in the system
    When I store daily production history for "2025-10-18" with 150 screens produced
    Then the existing production history should be updated
    And it should record 150 screens produced
