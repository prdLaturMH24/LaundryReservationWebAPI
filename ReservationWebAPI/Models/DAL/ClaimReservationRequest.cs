using System.ComponentModel.DataAnnotations;

namespace ReservationWebAPI.Models.DAL
{
    public class ClaimReservationRequest
    {
        [EmailAddress]
        public string Email { get; set; }
        [Phone]
        public string CellPhoneNumber { get; set; }
        [Length(5, 5)]
        public string Pin { get; set; }
    }
}
