

using ReservationWebAPI.Models;

namespace ReservationWebAPI.Interfaces
{
    public interface IMachineRepository
    {
        Task<Machine> GetMachineByNumberAsync(string machineNumber);
        Task<IEnumerable<Machine>> GetAllMachinesAsync();
        Task<bool> ToggleMachineLockAsync(Machine machine);
        Task<bool> SaveChangesAsync();
    }
}
