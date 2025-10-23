Feature: Product Service
  The Product Service manages screen products, including stock control,
  consumption, pricing updates, and stock summaries.

  @Product
  Scenario: Add screens when no product exists
    Given there are no products
    When I add 100 screens
    Then a new product should exist with quantity 100 and price 0

  @Product
  Scenario: Add screens when product already exists
    Given a product exists with quantity 50
    When I add 50 screens
    Then the product quantity should be 100

  @Product
  Scenario: Consume screens successfully
    Given a product exists with quantity 100
    When I consume 40 screens
    Then the remaining quantity should be 60

  @Product
  Scenario: Consume screens fails when insufficient quantity
    Given a product exists with quantity 30
    When I consume 50 screens
    Then the operation should return false
    And the product quantity should be 30

  @Product
  Scenario: Consume screens fails when product does not exist
    Given there are no products
    When I consume 10 screens
    Then the operation should return false

  @Product
  Scenario: Check available stock with reserved screens
    Given there are 100 screens produced
    And 40 are reserved for orders waiting payment or collection
    When I check available stock
    Then the result should be 60

  @Product
  Scenario: Update unit price successfully
    Given a product exists with stock and valid equipment parameters
    When I update the unit price
    Then the price should be greater than 0

  @Product
  Scenario: Get stock summary with reserved and available screens
    Given there are 100 total screens and 40 are reserved
    When I request a stock summary
    Then total produced should be 100
    And reserved should be 40
    And available should be 60
