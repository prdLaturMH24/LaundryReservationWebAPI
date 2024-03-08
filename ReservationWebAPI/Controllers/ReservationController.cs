using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationWebAPI.Interfaces;
using ReservationWebAPI.Models;
using ReservationWebAPI.Models.DAL;
using ReservationWebAPI.Proxies;
using Serilog;
using System.Net;

namespace ReservationWebAPI.Controllers
{
    public class ReservationController : BaseController
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IMachineApiProxy _machineApiProxy;

        public ReservationController(
            IReservationRepository reservationRepository,
            IMachineApiProxy machineApiProxy
            )
        {
            _reservationRepository = reservationRepository;
            _machineApiProxy = machineApiProxy;
        }
        /// <summary>
        /// Get all Laundry Machine Reservations
        /// </summary>
        /// <returns>Lists of Reservations</returns>
        [HttpGet]
        [Route("[controller]/list")]
        [ProducesResponseType(typeof(IEnumerable<Reservation>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> GetReservationsAsync()
        {
            var reservations = await _reservationRepository.GetAllReservationsAsync();
            if (!reservations.Any())
            {
                return NoContent();
            }
            return Ok(reservations);
        }

        /// <summary>
        /// Add Laundry Machine Reservation
        /// </summary>
        /// <param name="reservationRequest"></param>
        /// <returns>Machine Number and 5 Digit Pin</returns>

        [HttpPost]
        [Route("[controller]/create")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ReservationResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> CreateReservationAsync([FromBody] ReservationRequest reservationRequest)
        {
            try
            {

                if (!ModelState.IsValid)
                {
                    return BadRequest("Reservation request body is not valid!");
                }

                var isReservationAvailable = await _reservationRepository
                    .GetReservationAsync(reservationRequest.Email, reservationRequest.CellPhoneNumber) != null
                    ? true
                    : false;

                if (isReservationAvailable)
                {
                    return BadRequest("Email or Phone Number is already used.");
                }

                var machine = await AssignUnlockedMachineAsync();
                var pin = _reservationRepository.GenerateRandomPin();

                var reservation = new Reservation(
                    reservationRequest.ReservationDateTime,
                    reservationRequest.Email,
                    reservationRequest.CellPhoneNumber,
                    pin,
                    false,
                    false,
                    machine.MachineId
                    );

                var isReservationCompleted = await _reservationRepository.AddReservationAsync(reservation);
                if (!isReservationCompleted)
                {
                    return Problem(detail: "Unable to add reservation.",
                        statusCode:(int)HttpStatusCode.InternalServerError,title:"Reservation Add Failed");
                }
                var machineNumber = machine.MachineNumber;
                var isLockedSuccess = await _machineApiProxy.LockMachineAsync(machineNumber);
                if (!Convert.ToBoolean(isLockedSuccess))
                {
                    return Problem("Unable to lock machine.");
                }

                string? machineNumber1 = machine.MachineNumber;
                var reservationResponse = new ReservationResponse()
                {
                    MachineNumber = machineNumber1,
                    Pin = pin,
                };

                Log.Information("Ended request to Reservation API.");
                return Created("api/Reservation/create",reservationResponse);
            }
            catch (DbUpdateException dbUpdateException)
            {
                Log.Error($"An Error occurred during Database Changes - {dbUpdateException.Message}");
                return Problem($"An Error occurred during Database Changes - {dbUpdateException.Message}");
            }
            catch(InvalidOperationException invalidOperationEx)
            {
                Log.Error($"An Error occurred during Database Changes - {invalidOperationEx.Message}");
                return Problem($"An Error occurred during Database Changes - {invalidOperationEx.Message}");
            }
            catch (Exception ex)
            {
                return Problem($"An Error occurred while processing your request - {ex.Message}");
            }
        }
      
        /// <summary>
        /// Claims Laundry Machine Reservation
        /// </summary>
        /// <param name="claimReservation"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/claim")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string),(int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> ClaimReservationAsync([FromBody] ClaimReservationRequest claimReservation)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Claim Request Body is not valid.");
            }

            var reservationDetails = await _reservationRepository.GetReservationAsync(claimReservation.Email, claimReservation.CellPhoneNumber);
            if (reservationDetails == null)
            {
                return NotFound("Reservation is not found.");
            }
            if (Convert.ToInt32(reservationDetails.Pin) != Convert.ToInt32(claimReservation.Pin))
            {
                return BadRequest("Entered Pin is invalid!");
            }

            if (reservationDetails.IsCanceled)
            {
                return BadRequest("Reservation cannot be claimed as it was cancelled previously!");
            }

            if (reservationDetails.IsUsed)
            {
                return BadRequest("Reservation cannot be claimed as it was already claimed!");
            }

            reservationDetails.IsUsed = true;
            var isReservationClaimed = await _reservationRepository.UpdateReservationAsync(reservationDetails);
            if (!isReservationClaimed)
            {
                return Problem("Unable to claim reservation.");
            }
            var machineNumber = reservationDetails.Machine.MachineNumber;
            var isUnlockSuccess = await _machineApiProxy.UnlockMachineAsync(machineNumber);
            if (!Convert.ToBoolean(isUnlockSuccess))
            {
                return Problem("Unable to unlock machine.");
            }
            return Ok("Reservation has been claimed successfully!");
        }

        /// <summary>
        /// Cancel Laundry Machine Reservation
        /// </summary>
        /// <param name="cancellationRequest"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/cancel")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> CancelReservationAsync([FromBody] CancelReservationRequest cancellationRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Cancellation Request Body is not valid!");
            }

            var reservation = await _reservationRepository.GetReservationAsync(cancellationRequest.Email, cancellationRequest.CellPhoneNumber);
            if (reservation == null)
            {
                return NotFound($"Reservation is not available for entered details!");
            }

            if (reservation.IsUsed)
            {
                return BadRequest("Reservation cannot be cancelled as it was claimed previously!");
            }
            if (reservation.IsCanceled)
            {
                return BadRequest("Reservation cannot be cancelled as it was already cancelled!");
            }
            reservation.IsCanceled = true;
            var isCancelled = await _reservationRepository.UpdateReservationAsync(reservation);
            if (!isCancelled)
            {
                return Problem("Error occurred while cancelling reservation.");
            }
            var machineNumber = reservation.Machine.MachineNumber;
            var isUnlockSuccess = await _machineApiProxy.UnlockMachineAsync(machineNumber);
            if (!Convert.ToBoolean(isUnlockSuccess))
            {
                return Problem("Unable to unlock machine.");
            }

            return Ok("Reservation has been cancelled successfully!");
        }

        /// <summary>
        /// Private Methods for Reservation Controller
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public async Task<Models.Machine> AssignUnlockedMachineAsync()
        {
            var machines = await _machineApiProxy.GetMachinesAsync();
            Func<Models.Machine, bool> isMachineUnlocked = machine => !machine.IsLocked;
            return SelectRandomMachine(machines, isMachineUnlocked);
        }

        [NonAction]
        private static T SelectRandomMachine<T>(IEnumerable<T> collection, Func<T, bool> condition)
        {
            var filteredCollection = collection.Where(condition);

            // Check if there's any data based on the condition
            if (filteredCollection.Any())
            {
                // Shuffle the collection using a random number
                var shuffledCollection = filteredCollection.OrderBy(x => Guid.NewGuid());

                // Select the first element from the shuffled collection
                return shuffledCollection.First();
            }

            // Return default value if no data is found
            return default;
        }
    }
}
