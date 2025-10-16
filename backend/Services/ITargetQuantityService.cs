using ScreenProducerAPI.Models;

namespace ScreenProducerAPI.Services;

public interface ITargetQuantityService
{
    Task<InventoryStatus> GetInventoryStatusAsync();
}