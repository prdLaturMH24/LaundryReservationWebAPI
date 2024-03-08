using ReservationWebAPI.Models;

namespace ReservationWebAPI.Proxies
{
    public class MachineApiResponsePayload
    {
        public List<Machine> Machines { get; set; }
        public MachineApiResponsePayload() { }
    }
}
