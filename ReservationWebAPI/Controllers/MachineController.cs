using Microsoft.AspNetCore.Mvc;
using ReservationWebAPI.Interfaces;

namespace ReservationWebAPI.Controllers
{
    public class MachineController : BaseController
    {
        private readonly IMachineRepository _machineRepository;
        public MachineController(IMachineRepository machineRepository)
        {
            _machineRepository = machineRepository;
        }

        [HttpGet]
        [Route("[controller]/list")]
        public async Task<IActionResult> GetAllMachinesAsync()
        {
            var machines = await _machineRepository.GetAllMachinesAsync();
            if (machines.Count() == 0)
            {
                return NoContent();
            }
            return Ok(machines);
        }

        [HttpPost]
        [Route("[controller]/lock")]
        public async Task<ActionResult<bool>> LockMachineAsync(string? machineNumber)
        {
            if (machineNumber == null) throw new ArgumentNullException(nameof(machineNumber));
            var machineDetails = await _machineRepository.GetMachineByNumberAsync(machineNumber);
            if (machineDetails == null)
            {
                return Ok(false);
            }
            //check if machine is already locked then return false => i.e, Machine can not be locked as already locked.
            if (machineDetails.IsLocked)
            {
                return Ok(false);
            }
            var isLockedSuccessfully = await _machineRepository.ToggleMachineLockAsync(machineDetails);
            if (!isLockedSuccessfully)
            {
                return Ok(isLockedSuccessfully);
            }
            return Ok(isLockedSuccessfully);
        }
        
        [HttpPost]
        [Route("[controller]/unlock")]
        public async Task<ActionResult<bool>> UnlockMachineAsync(string? machineNumber)
        {
            if (machineNumber == null) throw new ArgumentNullException(nameof(machineNumber));
            var machineDetails = await _machineRepository.GetMachineByNumberAsync(machineNumber);
            if (machineDetails == null)
            {
                return Ok(false);
            }
            //check if machine is already unlocked then return false => i.e, Machine can not be unlocked as already unlocked.
            if (!machineDetails.IsLocked)
            {
                return Ok(false);
            }
            var isUnlockedSuccessfully = await _machineRepository.ToggleMachineLockAsync(machineDetails);
            if (!isUnlockedSuccessfully)
            {
                return Ok(isUnlockedSuccessfully);
            }
            return Ok(isUnlockedSuccessfully);
        }
    }
}
