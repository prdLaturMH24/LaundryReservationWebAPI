namespace ReservationWebAPI.Models
{
    public class Machine
    {
        public int MachineId { get; set; }
        public string? MachineNumber { get; set; }
        public bool IsLocked {  get; set; }


        public Machine() { }
        public Machine(int machineId, string machineNumber, bool isLocked)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(machineId);
            if (String.IsNullOrEmpty(machineNumber)) throw new ArgumentNullException(nameof(machineNumber));
            MachineId = machineId;
            MachineNumber = machineNumber;
            IsLocked = isLocked;
        }
    }
}
