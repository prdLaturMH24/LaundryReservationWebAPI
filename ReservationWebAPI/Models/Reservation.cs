using System.ComponentModel.DataAnnotations;

namespace ReservationWebAPI.Models
{
    public class Reservation
    {
        public Reservation() { }
        public int ReservationId { get; set; }
        public DateTime? ReservationDateTime { get; set; }
        [EmailAddress]
        public string Email { get; set; } = "test@test.com";
        [Phone]
        public string CellPhoneNumber { get; set; } = "";
        public string? Pin {  get; set; }
        public bool IsUsed { get; set; }
        public bool IsCanceled { get; set; }
        public int MachineId { get; set; }
        public Machine? Machine { get; set; }

        public Reservation( 
            DateTime reservationDateTime, 
            string email, 
            string cellPhoneNumber, 
            string? pin, 
            bool isUsed,
            bool isCanceled,
            int machineId)
        {
            if(isCanceled && isUsed) throw new InvalidOperationException();
            if(String.IsNullOrEmpty(pin)) throw new ArgumentNullException(nameof(pin));
            ReservationDateTime = reservationDateTime;
            Email = email;
            CellPhoneNumber = cellPhoneNumber;
            Pin = pin;
            IsUsed = isUsed;
            IsCanceled = isCanceled;
            MachineId = machineId;
        }
    }
}
