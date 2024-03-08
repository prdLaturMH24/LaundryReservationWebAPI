namespace ReservationWebAPI.Proxies
{
    public interface IMachineApiProxy
    {
        Task<string> LockMachineAsync(string machineNumber);
        Task<string> UnlockMachineAsync(string machineNumber);
        Task<IEnumerable<Models.Machine>> GetMachinesAsync();
    }
}
