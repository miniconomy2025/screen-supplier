Feature: Target Quantity Inventory Status
  To ensure proper stock management
  As a production system
  I want to calculate if materials and equipment need reordering

  Scenario: All materials and equipment are above reorder points
    Given the current quantities of sand, copper, and equipment are above their reorder points
    When I check the inventory status
    Then no item should need reordering

  Scenario: Some materials are below reorder points
    Given the current quantity of sand is below its reorder point
    And copper and equipment are above reorder points
    When I check the inventory status
    Then sand should need reordering
    And copper should not need reordering
    And equipment should not need reordering
