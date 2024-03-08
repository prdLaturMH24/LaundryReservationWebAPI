using Microsoft.AspNetCore.Mvc;
using Moq;
using ReservationWebAPI.Controllers;
using ReservationWebAPI.Interfaces;

namespace ReservationWebAPI.Core.Tests
{
    public class MachineControllerTests
    {
        private Mock<IMachineRepository> _machineRepositoryMock;
        private MachineController _controller;

        [SetUp]
        public void SetUp()
        {
            _machineRepositoryMock = new Mock<IMachineRepository>();
            _controller = new MachineController(_machineRepositoryMock.Object);
        }
        [Test]
        public async Task GetAllMachinesAsync_ReturnsOkResultWithData()
        {
            // Arrange
            var machinesData = new List<Models.Machine>();

            machinesData =
            [
                new Models.Machine
                {
                    MachineId=1,
                    MachineNumber="M001",
                    IsLocked=false
                }
            ];

            _machineRepositoryMock.Setup(repo => repo.GetAllMachinesAsync()).ReturnsAsync(machinesData);


            // Act
            var result = await _controller.GetAllMachinesAsync();

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult?.Value, Is.InstanceOf<IEnumerable<Models.Machine>>());
            var machinesResult = okResult?.Value as IEnumerable<Models.Machine>;

            Assert.That(machinesResult, Is.EqualTo(machinesData));
        }

        [Test]
        public async Task GetAllMachinesAsync_ReturnsNoContentResult()
        {
            // Arrange
            _machineRepositoryMock.Setup(repo => repo.GetAllMachinesAsync()).ReturnsAsync(new List<Models.Machine>());

            // Act
            var result = await _controller.GetAllMachinesAsync();

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public void LockMachineAsync_WhenMachineNumberIsNull_ThrowsNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _controller.LockMachineAsync(null));

        }


        [Test]
        public async Task LockMachineAsync_WhenMachineDetailsIsNull_ReturnsOkWithFalse()
        {
            // Arrange
            _machineRepositoryMock.Setup(repo => repo.GetMachineByNumberAsync(It.IsAny<string>())).ReturnsAsync((Models.Machine?)null);

            // Act
            var result = await _controller.LockMachineAsync("123");

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(okResult, Is.InstanceOf<OkObjectResult>());

                Assert.That((bool)result.Value, Is.False);
            });
        }

        [Test]
        public async Task LockMachineAsync_WhenMachineIsAlreadyLocked_ReturnsOkWithFalse()
        {
            // Arrange
            var machineDetails = new Models.Machine { IsLocked = true };
            _machineRepositoryMock.Setup(repo => repo.GetMachineByNumberAsync(It.IsAny<string>())).ReturnsAsync(machineDetails);

            // Act
            var result = await _controller.LockMachineAsync("123");

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(okResult, Is.InstanceOf<OkObjectResult>());
                Assert.That((bool)result.Value, Is.False);
            });
        }

        [Test]
        public async Task LockMachineAsync_WhenMachineLockIsToggledSuccessfully_ReturnsOkWithTrue()
        {
            // Arrange
            var machineDetails = new Models.Machine { IsLocked = false };
            _machineRepositoryMock.Setup(repo => repo.GetMachineByNumberAsync(It.IsAny<string>())).ReturnsAsync(machineDetails);
            _machineRepositoryMock.Setup(repo => repo.ToggleMachineLockAsync(It.IsAny<Models.Machine>())).ReturnsAsync(true);

            // Act
            var result = await _controller.LockMachineAsync("123");

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(okResult, Is.InstanceOf<OkObjectResult>());
                Assert.That(Convert.ToBoolean(okResult?.Value), Is.True);
            });
        }

        [Test]
        public async Task LockMachineAsync_WhenMachineLockToggleFails_ReturnsOkWithFalse()
        {
            // Arrange
            var machineDetails = new Models.Machine { IsLocked = false };
            var machineRepositoryMock = new Mock<IMachineRepository>();
            _machineRepositoryMock.Setup(repo => repo.GetMachineByNumberAsync(It.IsAny<string>())).ReturnsAsync(machineDetails);
            _machineRepositoryMock.Setup(repo => repo.ToggleMachineLockAsync(It.IsAny<Models.Machine>())).ReturnsAsync(false);

            // Act
            var result = await _controller.LockMachineAsync("123");

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(okResult, Is.InstanceOf<OkObjectResult>());
                Assert.That(Convert.ToBoolean(okResult?.Value), Is.False);
            });
        }

        [Test]
        public Task UnlockMachineAsyncWhenMachineNumberIsNullThrowsNullException()
        {
            // Arrange
            var machineRepositoryMock = new Mock<IMachineRepository>();

            var controller = new MachineController(machineRepositoryMock.Object);

            // Act & Assert

            Assert.ThrowsAsync<ArgumentNullException>(async () => await _controller.UnlockMachineAsync(null));
            return Task.CompletedTask;
        }

        [Test]
        public async Task UnlockMachineAsync_WhenMachineDetailsIsNull_ReturnsOkWithFalse()
        {
            // Arrange
            _machineRepositoryMock.Setup(repo => repo.GetMachineByNumberAsync(It.IsAny<string>()))
                .ReturnsAsync((Models.Machine?)null);

            // Act
            var result = await _controller.UnlockMachineAsync("123");

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(okResult, Is.InstanceOf<OkObjectResult>());

                Assert.That((bool)result.Value, Is.False);
            });
        }

        [Test]
        public async Task UnlockMachineAsync_WhenMachineIsAlreadyUnLocked_ReturnsOkWithFalse()
        {
            // Arrange
            var machineDetails = new Models.Machine { IsLocked = false };
            _machineRepositoryMock.Setup(repo => repo.GetMachineByNumberAsync(It.IsAny<string>()))
                .ReturnsAsync(machineDetails);

            // Act
            var result = await _controller.LockMachineAsync("123");

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(okResult, Is.InstanceOf<OkObjectResult>());
                Assert.That((bool)result.Value, Is.False);
            });
        }

        [Test]
        public async Task UnlockMachineAsync_WhenMachineLockIsToggledSuccessfully_ReturnsOkWithTrue()
        {
            // Arrange
            var machineDetails = new Models.Machine { IsLocked = true };
            _machineRepositoryMock.Setup(repo => repo.GetMachineByNumberAsync(It.IsAny<string>()))
                .ReturnsAsync(machineDetails);
            _machineRepositoryMock.Setup(repo => repo.ToggleMachineLockAsync(It.IsAny<Models.Machine>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UnlockMachineAsync("123");

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(okResult, Is.InstanceOf<OkObjectResult>());
                Assert.That(Convert.ToBoolean(okResult?.Value), Is.True);
            });
        }

        [Test]
        public async Task UnlockMachineAsync_WhenMachineLockToggleFails_ReturnsOkWithFalse()
        {
            // Arrange
            var machineDetails = new Models.Machine { IsLocked = true };
            _machineRepositoryMock.Setup(repo => repo.GetMachineByNumberAsync(It.IsAny<string>()))
                .ReturnsAsync(machineDetails);
            _machineRepositoryMock.Setup(repo => repo.ToggleMachineLockAsync(It.IsAny<Models.Machine>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.LockMachineAsync("123");

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(okResult, Is.InstanceOf<OkObjectResult>());
                Assert.That(Convert.ToBoolean(okResult?.Value), Is.False);
            });
        }
    }
}
