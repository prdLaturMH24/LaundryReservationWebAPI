using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework.Internal;
using ReservationWebAPI.Controllers;
using ReservationWebAPI.Interfaces;
using ReservationWebAPI.Models.DAL;
using ReservationWebAPI.Models;
using ReservationWebAPI.Proxies;
using System.Net;
using System.Text.Json;

namespace ReservationWebAPI.Core.Tests
{
    [TestFixture]
    public class ReservationControllerTests
    {
        private Mock<IReservationRepository> _reservationRepositoryMock;
        private Mock<IMachineApiProxy> _machineApiProxyMock;
        private ReservationController _controller;

        [SetUp]
        public void SetUp()
        {
            _reservationRepositoryMock = new Mock<IReservationRepository>();
            _machineApiProxyMock = new Mock<IMachineApiProxy>();
            _controller = new ReservationController(_reservationRepositoryMock.Object, _machineApiProxyMock.Object);
        }

        #region CreateReservationRequest

        [Test]
        public async Task CreateReservationAsync_WhenRequestIsValid_ReturnsCreated201Result()
        {
            // Arrange
            var reservationRequest = new ReservationRequest
            {
                ReservationDateTime = new DateTime(2024,02,04),
                Email = "test@example.com",
                CellPhoneNumber = "1234567890"
            };

            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Reservation?)null);

            var unlockedMachines = new List<Machine>
                {
                    new Machine { IsLocked = false,MachineId=1,MachineNumber="M000" },
                    new Machine { IsLocked = true,MachineId=2,MachineNumber="M001" },
                };

            _machineApiProxyMock.Setup(proxy => proxy.GetMachinesAsync())
                .ReturnsAsync(unlockedMachines);
            _reservationRepositoryMock.Setup(repo => repo.GenerateRandomPin())
                .Returns("123456");
            _reservationRepositoryMock.Setup(repo => repo.AddReservationAsync(It.IsAny<Reservation>()))
                .ReturnsAsync(true);
            _machineApiProxyMock.Setup(proxy => proxy.LockMachineAsync(It.IsAny<string>()))
                .Returns(Task.FromResult("true"));

            var expectedReservationResponse = new ReservationResponse
            { 
                MachineNumber = "M000",
                Pin = "123456"
            };

            // Act
            var result = await _controller.CreateReservationAsync(reservationRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<CreatedResult>());
            var createdResult = (CreatedResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(createdResult.StatusCode, Is.EqualTo((int)HttpStatusCode.Created));
                Assert.That(JsonSerializer.Serialize(createdResult.Value), Is.EqualTo(JsonSerializer.Serialize(expectedReservationResponse)));
            });
        }

        [Test]
        public async Task CreateReservationAsync_WhenRequestBodyIsInvalid_ReturnsBadRequest()
        {
            // Arrange
            var reservationRequest = new ReservationRequest
            {
                ReservationDateTime = DateTime.Now,
                Email = null, // Invalid email
                CellPhoneNumber = "1234567890"
            };

            _controller.ModelState.AddModelError("Email","Email is not valid!");

            // Act
            var result = await _controller.CreateReservationAsync(reservationRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(badRequestResult.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(badRequestResult?.Value?.ToString(), Is.EqualTo((string)"Reservation request body is not valid!"));
            });
        }

        [Test]
        public async Task CreateReservationAsync_WhenReservationExists_ReturnsBadRequest()
        {
            // Arrange
            var reservationRequest = new ReservationRequest
            {
                ReservationDateTime = DateTime.Now,
                Email = "test@example.com",
                CellPhoneNumber = "1234567890"
            };
            var existingReservation = new Reservation
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890"
            };

            _reservationRepositoryMock.Setup(r => r.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(existingReservation);


            // Act
            var result = await _controller.CreateReservationAsync(reservationRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());

            var okResult = (BadRequestObjectResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(okResult.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(okResult?.Value?.ToString(), Is.EqualTo("Email or Phone Number is already used."));
            });
        }

        [Test]
        public async Task CreateReservationAsync_ValidRequest_WhenReservationDatabaseStaleFailed_ReturnsInternalServerError()
        {
            // Arrange
            var reservationRequest = new ReservationRequest
            {
                ReservationDateTime = DateTime.Now,
                Email = "test@example.com",
                CellPhoneNumber = "1234567890"
            };

            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Reservation?)null);

            var unlockedMachines = new List<Machine>
                {
                    new Machine { IsLocked = false,MachineId=1,MachineNumber="M000" },
                    new Machine { IsLocked = false,MachineId=2,MachineNumber="M001" },
                };

            _machineApiProxyMock.Setup(proxy => proxy.GetMachinesAsync())
                .ReturnsAsync(unlockedMachines);
            _reservationRepositoryMock.Setup(repo => repo.GenerateRandomPin())
                .Returns("123456");
            _reservationRepositoryMock.Setup(repo => repo.AddReservationAsync(It.IsAny<Reservation>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.CreateReservationAsync(reservationRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var problemDetails = (ObjectResult)result;
            Assert.That(problemDetails?.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task CreateReservationAsync_ValidRequest_WhenReservationDatabaseStaleSucceeds_ButMachineLockingFails_ReturnsInternalServerError()
        {
            // Arrange
            var reservationRequest = new ReservationRequest
            {
                ReservationDateTime = DateTime.Now,
                Email = "test@example.com",
                CellPhoneNumber = "1234567890"
            };

            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Reservation?)null);

            var unlockedMachines = new List<Machine>
                {
                    new Machine { IsLocked = false,MachineId=1,MachineNumber="M000" },
                    new Machine { IsLocked = false,MachineId=2,MachineNumber="M001" },
                };

            _machineApiProxyMock.Setup(proxy => proxy.GetMachinesAsync())
                .ReturnsAsync(unlockedMachines);
            _reservationRepositoryMock.Setup(repo => repo.GenerateRandomPin())
                .Returns("123456");
            _reservationRepositoryMock.Setup(repo => repo.AddReservationAsync(It.IsAny<Reservation>()))
                .ReturnsAsync(true);
            _machineApiProxyMock.Setup(proxy => proxy.LockMachineAsync(It.IsAny<string>()))
             .Returns(Task.FromResult("false"));

            // Act
            var result = await _controller.CreateReservationAsync(reservationRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var problemDetails = (ObjectResult)result;
            Assert.That(problemDetails?.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
        }

        #endregion

        #region ClaimReservationRequest
        [Test]
        public async Task ClaimReservationAsync_WhenRequestIsInvalid_ReturnsBadRequest()
        {
            //Arrange
            var claimRequest = new ClaimReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "12345",
                Pin = null
            };

            _controller.ModelState.AddModelError("Pin","Pin should not be null.");

            //Act
            var result  = await _controller.ClaimReservationAsync(claimRequest);


            //Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(badRequestResult.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(badRequestResult?.Value?.ToString(), Is.EqualTo("Claim Request Body is not valid."));
            });
        }

        [Test]
        public async Task ClaimReservationAsync_WhenRequestIsValid_WhenReservationNotAvailable_ReturnsNotFound()
        {
            //Arrange
            var claimRequest = new ClaimReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890",
                Pin = "12345"
            };

            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync((Reservation?)null);

            //Act
            var result = await _controller.ClaimReservationAsync(claimRequest);


            //Assert
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
            var notFoundResult = (NotFoundObjectResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(notFoundResult.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
                Assert.That(notFoundResult?.Value?.ToString(), Is.EqualTo("Reservation is not found."));
            });
        }

        [Test]
        public async Task ClaimReservationAsync_WhenRequestIsValid_WhenReservationAvailable_WhenPinInvalid_ReturnsBadRequest()
        {
            //Arrange
            var claimRequest = new ClaimReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890",
                Pin = "12345"
            };

            var existingReservation = new Reservation(
                new DateTime(2024,02,04),
                claimRequest.Email,
                claimRequest.CellPhoneNumber,
                "45678",
                false,
                false,
                1
                );

            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(existingReservation);

            //Act
            var result = await _controller.ClaimReservationAsync(claimRequest);

            //Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(badRequestResult.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(badRequestResult?.Value?.ToString(), Is.EqualTo("Entered Pin is invalid!"));
            });
        }

        [Test]
        public async Task ClaimReservationAsync_WhenRequestIsValid_WhenReservationAvailable_WhenPinIsValid_WhenReservationCanceledPrev_ReturnsBadRequest()
        {
            //Arrange
            var claimRequest = new ClaimReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890",
                Pin = "12345"
            };

            var existingReservation = new Reservation(
                new DateTime(2024, 02, 04),
                claimRequest.Email,
                claimRequest.CellPhoneNumber,
                "12345",
                false,
                true,
                1
                );

            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(existingReservation);

            //Act
            var result = await _controller.ClaimReservationAsync(claimRequest);

            //Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(badRequestResult.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(badRequestResult?.Value?.ToString(), Is.EqualTo("Reservation cannot be claimed as it was cancelled previously!"));
            });
        }

        [Test]
        public async Task ClaimReservationAsync_WhenRequestIsValid_WhenReservationAvailable_WhenPinIsValid_WhenReservationClaimedPrev_ReturnsBadRequest()
        {
            //Arrange
            var claimRequest = new ClaimReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890",
                Pin = "12345"
            };

            var existingReservation = new Reservation(
                new DateTime(2024, 02, 04),
                claimRequest.Email,
                claimRequest.CellPhoneNumber,
                "12345",
                true,
                false,
                1
                );

            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(existingReservation);

            //Act
            var result = await _controller.ClaimReservationAsync(claimRequest);

            //Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(badRequestResult.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(badRequestResult?.Value?.ToString(), Is.EqualTo("Reservation cannot be claimed as it was already claimed!"));
            });
        }

        [Test]
        public async Task ClaimReservationAsync_WhenRequestIsValid_WhenReservationAvailable_WhenPinIsValid_WhenReservationNotClaimedOrCanceled_ReturnsOkResult()
        {
            //Arrange
            var claimRequest = new ClaimReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890",
                Pin = "12345"
            };

            var lockedMachine = new Machine { IsLocked = true, MachineId = 1, MachineNumber = "M000" };

            var existingReservation = new Reservation(
                new DateTime(2024, 02, 04),
                claimRequest.Email,
                claimRequest.CellPhoneNumber,
                "12345",
                false,
                false,
                lockedMachine.MachineId
                );

            existingReservation.Machine = lockedMachine;

            //Mock reservation exists check
            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(existingReservation);
            //Mock reservation claim update
            _reservationRepositoryMock.Setup(repo => repo.UpdateReservationAsync(It.IsAny<Reservation>())).ReturnsAsync(true);

            //Mock machine unlock check
            _machineApiProxyMock.Setup(proxy => proxy.UnlockMachineAsync(It.IsAny<string>())).ReturnsAsync(await Task.FromResult("true"));

            //Act
            var result = await _controller.ClaimReservationAsync(claimRequest);

            //Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okRequestResult = (OkObjectResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(okRequestResult.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
                Assert.That(okRequestResult?.Value?.ToString(), Is.EqualTo("Reservation has been claimed successfully!"));
            });
        }

        [Test]
        public async Task ClaimReservationAsync_WhenRequestIsValid_WhenReservationAvailable_WhenPinIsValid_WhenReservationNotClaimedOrCanceled_ReturnsProblemResultWhenDbUpdateFails()
        {
            //Arrange
            var claimRequest = new ClaimReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890",
                Pin = "12345"
            };

            var lockedMachine = new Machine { IsLocked = true, MachineId = 1, MachineNumber = "M000" };

            var existingReservation = new Reservation(
                new DateTime(2024, 02, 04),
                claimRequest.Email,
                claimRequest.CellPhoneNumber,
                "12345",
                false,
                false,
                lockedMachine.MachineId
                );

            existingReservation.Machine = lockedMachine;

            //Mock reservation exists check
            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(existingReservation);
            //Mock reservation claim update
            _reservationRepositoryMock.Setup(repo => repo.UpdateReservationAsync(It.IsAny<Reservation>())).ReturnsAsync(false);

            //Act
            var result = await _controller.ClaimReservationAsync(claimRequest);

            //Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var _result = (ObjectResult)result;
            Assert.That(_result.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));

        }

        [Test]
        public async Task ClaimReservationAsync_WhenRequestIsValid_WhenReservationAvailable_WhenPinIsValid_WhenReservationNotClaimedOrCanceled_ReturnsProblemResultWhenMachineUnlockFails()
        {
            //Arrange
            var claimRequest = new ClaimReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890",
                Pin = "12345"
            };

            var lockedMachine = new Machine { IsLocked = true, MachineId = 1, MachineNumber = "M000" };

            var existingReservation = new Reservation(
                new DateTime(2024, 02, 04),
                claimRequest.Email,
                claimRequest.CellPhoneNumber,
                "12345",
                false,
                false,
                lockedMachine.MachineId
                );

            existingReservation.Machine = lockedMachine;

            //Mock reservation exists check
            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(existingReservation);
            //Mock reservation claim update
            _reservationRepositoryMock.Setup(repo => repo.UpdateReservationAsync(It.IsAny<Reservation>())).ReturnsAsync(true);

            //Mock machine unlock check
            _machineApiProxyMock.Setup(proxy => proxy.UnlockMachineAsync(It.IsAny<string>())).ReturnsAsync(await Task.FromResult("false"));

            //Act
            var result = await _controller.ClaimReservationAsync(claimRequest);

            //Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var _result = (ObjectResult)result;
            Assert.That(_result.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));

        }

        #endregion

        #region CancelReservationRequest
        [Test]
        public async Task CancelReservationAsync_WhenRequestIsInvalid_ReturnsBadRequest()
        {
            //Arrange
            var cancelRequest = new CancelReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = null
            };

            _controller.ModelState.AddModelError("CellPhoneNumber", "CellPhoneNumber should not be null.");

            //Act
            var result = await _controller.CancelReservationAsync(cancelRequest);


            //Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(badRequestResult.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(badRequestResult?.Value?.ToString(), Is.EqualTo("Cancellation Request Body is not valid!"));
            });
        }

        [Test]
        public async Task CancelReservationAsync_WhenRequestIsValid_WhenReservationNotAvailable_ReturnsNotFound()
        {
            //Arrange
            var cancelRequest = new CancelReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890"
            };

            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync((Reservation?)null);

            //Act
            var result = await _controller.CancelReservationAsync(cancelRequest);


            //Assert
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
            var notFoundResult = (NotFoundObjectResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(notFoundResult.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
                Assert.That(notFoundResult?.Value?.ToString(), Is.EqualTo("Reservation is not available for entered details!"));
            });
        }

        [Test]
        public async Task ClaimReservationAsync_WhenRequestIsValid_WhenReservationAvailable_WhenReservationClaimedPrev_ReturnsBadRequest()
        {
            //Arrange
            var cancelRequest = new CancelReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890"
            };

            var existingReservation = new Reservation(
                new DateTime(2024, 02, 04),
                cancelRequest.Email,
                cancelRequest.CellPhoneNumber,
                "12345",
                true,
                false,
                1
                );

            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(existingReservation);

            //Act
            var result = await _controller.CancelReservationAsync(cancelRequest);

            //Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(badRequestResult.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(badRequestResult?.Value?.ToString(), Is.EqualTo("Reservation cannot be cancelled as it was claimed previously!"));
            });
        }

        [Test]
        public async Task CancelReservationAsync_WhenRequestIsValid_WhenReservationAvailable_WhenReservationCanceledPrev_ReturnsBadRequest()
        {
            //Arrange
            var cancelRequest = new CancelReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890"
            };

            var existingReservation = new Reservation(
                new DateTime(2024, 02, 04),
                cancelRequest.Email,
                cancelRequest.CellPhoneNumber,
                "12345",
                false,
                true,
                1
                );

            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(existingReservation);

            //Act
            var result = await _controller.CancelReservationAsync(cancelRequest);

            //Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(badRequestResult.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(badRequestResult?.Value?.ToString(), Is.EqualTo("Reservation cannot be cancelled as it was already cancelled!"));
            });
        }

        [Test]
        public async Task CancelReservationAsync_WhenRequestIsValid_WhenReservationAvailable_WhenReservationNotClaimedOrCanceled_ReturnsOkResult()
        {
            //Arrange
            var cancelRequest = new CancelReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890"
            };

            var lockedMachine = new Machine { IsLocked = true, MachineId = 1, MachineNumber = "M000" };

            var existingReservation = new Reservation(
                new DateTime(2024, 02, 04),
                cancelRequest.Email,
                cancelRequest.CellPhoneNumber,
                "12345",
                false,
                false,
                lockedMachine.MachineId
                );

            existingReservation.Machine = lockedMachine;

            //Mock reservation exists check
            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(existingReservation);
            //Mock reservation claim update
            _reservationRepositoryMock.Setup(repo => repo.UpdateReservationAsync(It.IsAny<Reservation>())).ReturnsAsync(true);

            //Mock machine unlock check
            _machineApiProxyMock.Setup(proxy => proxy.UnlockMachineAsync(It.IsAny<string>())).ReturnsAsync(await Task.FromResult("true"));

            //Act
            var result = await _controller.CancelReservationAsync(cancelRequest);

            //Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okRequestResult = (OkObjectResult)result;
            Assert.Multiple(() =>
            {
                Assert.That(okRequestResult.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
                Assert.That(okRequestResult?.Value?.ToString(), Is.EqualTo("Reservation has been cancelled successfully!"));
            });
        }

        [Test]
        public async Task CancelReservationAsync_WhenRequestIsValid_WhenReservationAvailable_WhenReservationNotClaimedOrCanceled_ReturnsProblemResultWhenDbUpdateFails()
        {
            //Arrange
            var cancelRequest = new CancelReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890"
            };

            var lockedMachine = new Machine { IsLocked = true, MachineId = 1, MachineNumber = "M000" };

            var existingReservation = new Reservation(
                new DateTime(2024, 02, 04),
                cancelRequest.Email,
                cancelRequest.CellPhoneNumber,
                "12345",
                false,
                false,
                lockedMachine.MachineId
                );

            existingReservation.Machine = lockedMachine;

            //Mock reservation exists check
            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(existingReservation);
            //Mock reservation claim update
            _reservationRepositoryMock.Setup(repo => repo.UpdateReservationAsync(It.IsAny<Reservation>())).ReturnsAsync(false);

            //Act
            var result = await _controller.CancelReservationAsync(cancelRequest);

            //Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var _result = (ObjectResult)result;
            Assert.That(_result.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));

        }

        [Test]
        public async Task CancelReservationAsync_WhenRequestIsValid_WhenReservationAvailable_WhenReservationNotClaimedOrCanceled_ReturnsProblemResultWhenMachineUnlockFails()
        {
            //Arrange
            var cancelRequest = new CancelReservationRequest
            {
                Email = "test@example.com",
                CellPhoneNumber = "1234567890"
            };

            var lockedMachine = new Machine { IsLocked = true, MachineId = 1, MachineNumber = "M000" };

            var existingReservation = new Reservation(
                new DateTime(2024, 02, 04),
                cancelRequest.Email,
                cancelRequest.CellPhoneNumber,
                "12345",
                false,
                false,
                lockedMachine.MachineId
                );

            existingReservation.Machine = lockedMachine;

            //Mock reservation exists check
            _reservationRepositoryMock.Setup(repo => repo.GetReservationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(existingReservation);
            //Mock reservation claim update
            _reservationRepositoryMock.Setup(repo => repo.UpdateReservationAsync(It.IsAny<Reservation>())).ReturnsAsync(true);

            //Mock machine unlock check
            _machineApiProxyMock.Setup(proxy => proxy.UnlockMachineAsync(It.IsAny<string>())).ReturnsAsync(await Task.FromResult("false"));

            //Act
            var result = await _controller.CancelReservationAsync(cancelRequest);

            //Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var _result = (ObjectResult)result;
            Assert.That(_result.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));

        }

        #endregion
    }
}
