using System.ComponentModel.DataAnnotations;

namespace ReservationWebAPI.Models.DAL
{
    public class CancelReservationRequest
    {
        [EmailAddress]
        public string Email { get; set; }
        [Phone]
        public string CellPhoneNumber { get; set; }
    }
}
