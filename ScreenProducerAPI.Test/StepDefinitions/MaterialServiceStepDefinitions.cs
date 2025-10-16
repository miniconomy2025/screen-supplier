using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Reqnroll;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScreenProducerAPI.Test.StepDefinitions;

[Binding]
public sealed class MaterialServiceStepDefinitions
{
    private ScreenContext _context = null!;
    private MaterialService _materialService = null!;
    private bool _addResult;
    private bool _consumeResult;
    private bool _hasSufficientResult;
    private Material? _retrievedMaterial;
    private List<Material>? _allMaterials;
    private decimal _averageCost;

    [BeforeScenario]
    public void SetupContext()
    {
        var options = new DbContextOptionsBuilder<ScreenContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ScreenContext(options);
        _materialService = new MaterialService(_context);
    }

    [Given(@"there is no existing material named ""(.*)""")]
    public void GivenThereIsNoExistingMaterialNamed(string materialName)
    {
        // Materials list is already empty or doesn't contain this material
        var existing = _context.Materials.Where(m => m.Name.ToLower() == materialName.ToLower()).ToList();
        _context.Materials.RemoveRange(existing);
        _context.SaveChanges();
    }

    [Given(@"there is an existing material named ""(.*)"" with quantity (.*)")]
    public void GivenThereIsAnExistingMaterialNamedWithQuantity(string materialName, int quantity)
    {
        var material = new Material
        {
            Name = materialName,
            Quantity = quantity
        };
        _context.Materials.Add(material);
        _context.SaveChanges();
    }

    [Given(@"there are purchase orders for ""(.*)"" with unit prices (.*)")]
    public void GivenThereArePurchaseOrdersForWithUnitPrices(string materialName, string unitPricesStr)
    {
        var material = _context.Materials.FirstOrDefault(m => m.Name.ToLower() == materialName.ToLower());
        var unitPrices = unitPricesStr.Split(',').Select(int.Parse).ToArray();

        foreach (var price in unitPrices)
        {
            var purchaseOrder = new PurchaseOrder
            {
                RawMaterialId = material?.Id,
                RawMaterial = material,
                UnitPrice = price,
                OrderDate = DateTime.UtcNow,
                OrderID = 1,
                Quantity = 100,
                BankAccountNumber = "TEST",
                Origin = "test"
            };
            _context.PurchaseOrders.Add(purchaseOrder);
        }
        _context.SaveChanges();
    }

    [When(@"I add (.*) units of ""(.*)"" material")]
    public async Task WhenIAddUnitsOfMaterial(int quantity, string materialName)
    {
        _addResult = await _materialService.AddMaterialAsync(materialName, quantity);
    }

    [When(@"I consume (.*) units of ""(.*)"" material")]
    public async Task WhenIConsumeUnitsOfMaterial(int quantity, string materialName)
    {
        _consumeResult = await _materialService.ConsumeMaterialAsync(materialName, quantity);
    }

    [When(@"I check if there are sufficient ""(.*)"" materials with required quantity (.*)")]
    public async Task WhenICheckIfThereAreSufficientMaterialsWithRequiredQuantity(string materialName, int requiredQuantity)
    {
        _hasSufficientResult = await _materialService.HasSufficientMaterialsAsync(materialName, requiredQuantity);
    }

    [When(@"I get the material named ""(.*)""")]
    public async Task WhenIGetTheMaterialNamed(string materialName)
    {
        _retrievedMaterial = await _materialService.GetMaterialAsync(materialName);
    }

    [When(@"I get all materials")]
    public async Task WhenIGetAllMaterials()
    {
        _allMaterials = await _materialService.GetAllMaterialsAsync();
    }

    [When(@"I get the average cost per kg for ""(.*)""")]
    public async Task WhenIGetTheAverageCostPerKgFor(string materialName)
    {
        _averageCost = await _materialService.GetAverageCostPerKgAsync(materialName);
    }

    [Then(@"the add material operation should succeed")]
    public void ThenTheAddMaterialOperationShouldSucceed()
    {
        _addResult.Should().BeTrue();
    }

    [Then(@"the add material operation should fail")]
    public void ThenTheAddMaterialOperationShouldFail()
    {
        _addResult.Should().BeFalse();
    }

    [Then(@"the material ""(.*)"" should have quantity (.*)")]
    public void ThenTheMaterialShouldHaveQuantity(string materialName, int expectedQuantity)
    {
        var material = _context.Materials.FirstOrDefault(m => m.Name.ToLower() == materialName.ToLower());
        material.Should().NotBeNull();
        material!.Quantity.Should().Be(expectedQuantity);
    }

    [Then(@"a new material ""(.*)"" should be created")]
    public void ThenANewMaterialShouldBeCreated(string materialName)
    {
        var material = _context.Materials.FirstOrDefault(m => m.Name.ToLower() == materialName.ToLower());
        material.Should().NotBeNull();
    }

    [Then(@"the consume material operation should succeed")]
    public void ThenTheConsumeMaterialOperationShouldSucceed()
    {
        _consumeResult.Should().BeTrue();
    }

    [Then(@"the consume material operation should fail")]
    public void ThenTheConsumeMaterialOperationShouldFail()
    {
        _consumeResult.Should().BeFalse();
    }

    [Then(@"the sufficient materials check should return true")]
    public void ThenTheSufficientMaterialsCheckShouldReturnTrue()
    {
        _hasSufficientResult.Should().BeTrue();
    }

    [Then(@"the sufficient materials check should return false")]
    public void ThenTheSufficientMaterialsCheckShouldReturnFalse()
    {
        _hasSufficientResult.Should().BeFalse();
    }

    [Then(@"the retrieved material should not be null")]
    public void ThenTheRetrievedMaterialShouldNotBeNull()
    {
        _retrievedMaterial.Should().NotBeNull();
    }

    [Then(@"the retrieved material should be null")]
    public void ThenTheRetrievedMaterialShouldBeNull()
    {
        _retrievedMaterial.Should().BeNull();
    }

    [Then(@"the retrieved material should have name ""(.*)""")]
    public void ThenTheRetrievedMaterialShouldHaveName(string expectedName)
    {
        _retrievedMaterial.Should().NotBeNull();
        _retrievedMaterial!.Name.Should().Be(expectedName);
    }

    [Then(@"all materials should contain (.*) items")]
    public void ThenAllMaterialsShouldContainItems(int expectedCount)
    {
        _allMaterials.Should().NotBeNull();
        _allMaterials!.Count.Should().Be(expectedCount);
    }

    [Then(@"the average cost should be (.*)")]
    public void ThenTheAverageCostShouldBe(decimal expectedCost)
    {
        _averageCost.Should().Be(expectedCost);
    }

    [Then(@"the average cost should be default value (.*)")]
    public void ThenTheAverageCostShouldBeDefaultValue(int defaultValue)
    {
        _averageCost.Should().Be(defaultValue);
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
    }
}