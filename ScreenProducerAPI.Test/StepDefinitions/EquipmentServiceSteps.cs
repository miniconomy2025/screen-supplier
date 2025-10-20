using Microsoft.EntityFrameworkCore;
using Moq;
using Reqnroll;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Models.Responses;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using System;

[Binding]
public class EquipmentServiceSteps
{
    private ScreenContext _context;
    private Mock<IMaterialService> _mockMaterialService;
    private Mock<IProductService> _mockProductService;
    private EquipmentService _equipmentService;
    private bool _boolResult;
    private int _intResult;
    private MachineFailureResponse _failureResponse;

    public EquipmentServiceSteps()
    {
        var options = new DbContextOptionsBuilder<ScreenContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ScreenContext(options);

        _mockMaterialService = new Mock<IMaterialService>();
        _mockProductService = new Mock<IProductService>();

        _equipmentService = new EquipmentService(_context, _mockMaterialService.Object, _mockProductService.Object);
    }


}
