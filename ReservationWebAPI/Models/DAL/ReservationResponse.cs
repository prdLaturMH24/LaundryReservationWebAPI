using System.ComponentModel.DataAnnotations;

namespace ReservationWebAPI.Models.DAL
{
    public class ReservationResponse
    {
        public string? MachineNumber { get; set; }
        [Length(5, 5)]
        public string? Pin { get; set; }
    }
}
