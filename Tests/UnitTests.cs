
using Microsoft.Extensions.Configuration;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Backend.Services;
using Backend.Models;
using Backend.AppDbContext;
using FluentValidation;
using Backend.DTOs;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers;
using Moq;
using Quartz;
using Quartz.Impl.Matchers;
using Microsoft.AspNetCore.Identity;




namespace Backend.Tests
{
    public class TestRunnerServiceTests
    {
        private readonly TestRunnerService _service;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<TestComparisonService> _testComparisonMock;
        private readonly ApplicationDbContext _context;



        public TestRunnerServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _configurationMock = new Mock<IConfiguration>();
            _userManagerMock = new Mock<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            _testComparisonMock = new Mock<TestComparisonService>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            _service = new TestRunnerService(
                _context,
                _httpClientFactoryMock.Object,
                _configurationMock.Object,
                _userManagerMock.Object,
                _testComparisonMock.Object);
        }

        [Fact]
        public async Task ExecuteApiTestAsync_SuccessfulExecution_ReturnsSuccessResult()
        {
            // Arrange
            var userId = "user1";
            var test = new ApiTest
            {
                Id = 1,
                Name = "Test API",
                Url = "https://api.example.com/test",
                Method = "GET",
                ExpectedStatusCode = 200,
                ExpectedResponse = "{\"success\":true}",
                CreatedByUserId = userId
            };

            var httpClient = new HttpClient(new MockHttpMessageHandler(
                HttpStatusCode.OK, "{\"success\":true}"));
            _httpClientFactoryMock.Setup(f => f.CreateClient()).Returns(httpClient);

            _testComparisonMock.Setup(c => c.CompareApiTest(
      It.IsAny<string>(), It.IsAny<string>(), 200, 200, out It.Ref<string>.IsAny))
      .Returns(true)
      .Callback((string expected, string actual, int expectedStatus, int actualStatus, out string error) => error = null);

            // Act
            var result = await _service.ExecuteApiTestAsync(test, userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Тест пройшов успішно", result.ErrorMessage);
            Assert.NotNull(result.Response);
            Assert.Equal(userId, result.ExecutedByUserId);
            Assert.Equal(test.Id, result.ApiTestId);
        }

        [Fact]
        public async Task ExecuteApiTestAsync_Timeout_ReturnsTimeoutError()
        {
            // Arrange
            var userId = "user1";
            var test = new ApiTest
            {
                Id = 1,
                Name = "Test API",
                Url = "https://api.example.com/test",
                Method = "GET",
                TimeoutSeconds = 1,
                ExpectedStatusCode = 200,
                CreatedByUserId = userId
            };

            var httpClient = new HttpClient(new MockHttpMessageHandler(
                HttpStatusCode.OK, "{\"success\":true}", delay: TimeSpan.FromSeconds(2)));
            _httpClientFactoryMock.Setup(f => f.CreateClient()).Returns(httpClient);

            // Act
            var result = await _service.ExecuteApiTestAsync(test, userId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("timed out", result.ErrorMessage);
        }

        [Fact]
        public async Task RunSingleSqlTestAsync_ScalarTest_SuccessfulExecution()
        {
            // Arrange
            var userId = "user1";
            var test = new SqlTest
            {
                Id = 1,
                Name = "Test Scalar SQL",
                SqlQuery = "SELECT 42 AS Result",
                TestType = SqlTestType.Scalar,
                ExpectedJson = "42",
                DatabaseConnectionName = "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;",
                CreatedByUserId = userId
            };

            // Act
            var result = await _service.RunSingleSqlTestAsync(test, userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Passed", result.ErrorMessage);
            Assert.Equal("42", result.Response);
        }

        [Fact]
        public async Task RunSingleSqlTestAsync_MissingConnectionString_ReturnsError()
        {
            // Arrange
            var userId = "user1";
            var test = new SqlTest
            {
                Id = 1,
                Name = "Test SQL",
                SqlQuery = "SELECT * FROM TestTable",
                TestType = SqlTestType.ResultSet,
                ExpectedJson = "[{\"Id\":1}]",
                DatabaseConnectionName = null,
                CreatedByUserId = userId
            };

            // Act
            var result = await _service.RunSingleSqlTestAsync(test, userId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("no database connection string specified", result.ErrorMessage);
        }
    }

    public class JwtServiceTests
    {
        private readonly JwtService _service;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly ApplicationDbContext _context;

        public JwtServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["Jwt:Key"]).Returns("SuperSecretKey12345678901234567890");
            _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            _service = new JwtService(_configurationMock.Object, _context);
        }

        [Fact]
        public async Task GenerateTokensAsync_ValidUser_ReturnsTokens()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user1",
                Email = "test@example.com",
                UserName = "TestUser"
            };

            // Act
            var (accessToken, refreshToken) = await _service.GenerateTokensAsync(user);

            // Assert
            Assert.NotNull(accessToken);
            Assert.NotNull(refreshToken);
            var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            Assert.NotNull(storedToken);
            Assert.Equal(user.Id, storedToken.UserId);
            Assert.False(storedToken.IsRevoked);
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_ValidToken_RevokesToken()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Token = "test-refresh-token",
                UserId = "user1",
                Expires = DateTime.UtcNow.AddDays(1),
                IsRevoked = false
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // Act
            await _service.RevokeRefreshTokenAsync(refreshToken.Token);

            // Assert
            var updatedToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken.Token);
            Assert.True(updatedToken.IsRevoked);
        }

        [Fact]
        public async Task GetRefreshTokenAsync_ExpiredToken_ReturnsNull()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Token = "expired-token",
                UserId = "user1",
                Expires = DateTime.UtcNow.AddDays(-1),
                IsRevoked = false
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetRefreshTokenAsync(refreshToken.Token);

            // Assert
            Assert.NotNull(result); // Token exists but is expired
            Assert.True(result.IsExpired);
        }
    }

    public class SchedulerServiceTests
    {
        private readonly SchedulerService _service;
        private readonly Mock<ISchedulerFactory> _schedulerFactoryMock;
        private readonly Mock<IScheduler> _schedulerMock;

        public SchedulerServiceTests()
        {
            _schedulerFactoryMock = new Mock<ISchedulerFactory>();
            _schedulerMock = new Mock<IScheduler>();
            _schedulerMock.Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(DateTimeOffset.UtcNow);
            _service = new SchedulerService(_schedulerFactoryMock.Object);
        }

        [Fact]
        public async Task ScheduleScenarioAsync_ValidSchedule_CreatesJob()
        {
            // Arrange
            var schedule = new ScenarioScheduleDto
            {
                ScenarioId = "1",
                UserId = "user1",
                StartTime = DateTime.UtcNow,
                CronExpression = "0 0 12 * * ?"
            };

            _schedulerMock.Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
                .Returns((Task<DateTimeOffset>)Task.CompletedTask);

            // Act
            await _service.ScheduleScenarioAsync(schedule);

            // Assert
            _schedulerMock.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task CancelScheduledScenarioAsync_ValidJob_ReturnsTrue()
        {
            // Arrange
            var scenarioId = "1";
            var userId = "user1";
            _schedulerMock.Setup(s => s.DeleteJob(It.Is<JobKey>(k => k.Name == $"RunScenarioJob-{scenarioId}-{userId}"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CancelScheduledScenarioAsync(scenarioId, userId);

            // Assert
            Assert.True(result);
            _schedulerMock.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetAllScheduledScenariosAsync_ReturnsScheduledScenarios()
        {
            // Arrange
            var jobKey = new JobKey("RunScenarioJob-1-user1");
            var trigger = new Mock<ICronTrigger>();
            trigger.Setup(t => t.GetNextFireTimeUtc()).Returns(DateTimeOffset.UtcNow);
            trigger.Setup(t => t.CronExpressionString).Returns("0 0 12 * * ?");
            _schedulerMock.Setup(s => s.GetJobGroupNames(It.IsAny<CancellationToken>())).ReturnsAsync(new List<string> { "default" });
            _schedulerMock.Setup(s => s.GetJobKeys(It.IsAny<GroupMatcher<JobKey>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { jobKey });
            _schedulerMock.Setup(s => s.GetJobDetail(jobKey, It.IsAny<CancellationToken>())).ReturnsAsync(new Mock<IJobDetail>().Object);
            _schedulerMock.Setup(s => s.GetTriggersOfJob(jobKey, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { trigger.Object });

            // Act
            var result = await _service.GetAllScheduledScenariosAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ScenarioId);
            Assert.Equal("user1", result[0].UserId);
        }
    }

    public class TestComparisonServiceTests
    {
        private readonly TestComparisonService _service;

        public TestComparisonServiceTests()
        {
            _service = new TestComparisonService();
        }

        [Fact]
        public void CompareApiTest_SameJsonAndStatusCode_ReturnsTrue()
        {
            // Arrange
            var expectedResponse = "{\"success\":true}";
            var actualResponse = "{\"success\":true}";
            var expectedStatusCode = 200;
            var actualStatusCode = 200;
            string error;

            // Act
            var result = _service.CompareApiTest(expectedResponse, actualResponse, expectedStatusCode, actualStatusCode, out error);

            // Assert
            Assert.True(result);
            Assert.Null(error);
        }

        [Fact]
        public void CompareApiTest_DifferentStatusCode_ReturnsFalse()
        {
            // Arrange
            var expectedResponse = "{\"success\":true}";
            var actualResponse = "{\"success\":true}";
            var expectedStatusCode = 200;
            var actualStatusCode = 404;
            string error;

            // Act
            var result = _service.CompareApiTest(expectedResponse, actualResponse, expectedStatusCode, actualStatusCode, out error);

            // Assert
            Assert.False(result);
            Assert.Contains("Очікуваний статус-код: 200, фактичний: 404", error);
        }

        [Fact]
        public void CompareSqlTest_SameJson_ReturnsTrue()
        {
            // Arrange
            var sqlTest = new SqlTest
            {
                ExpectedJson = "[{\"Id\":1}]"
            };
            var actualResultJson = "[{\"Id\":1}]";
            string error;

            // Act
            var result = _service.CompareSqlTest(sqlTest, actualResultJson, out error);

            // Assert
            Assert.True(result);
            Assert.Null(error);
        }

        [Fact]
        public void CompareSqlTest_EmptyExpectedJson_ReturnsFalse()
        {
            // Arrange
            var sqlTest = new SqlTest
            {
                ExpectedJson = ""
            };
            var actualResultJson = "[{\"Id\":1}]";
            string error;

            // Act
            var result = _service.CompareSqlTest(sqlTest, actualResultJson, out error);

            // Assert
            Assert.False(result);
            Assert.Contains("Очікуване значення не задане", error);
        }
    }

    public class TestStatisticsServiceTests
    {
        private readonly TestStatisticsService _service;

        public TestStatisticsServiceTests()
        {
            _service = new TestStatisticsService();
        }

        [Fact]
        public void ComputeStatistics_ValidResults_ReturnsCorrectStats()
        {
            // Arrange
            var results = new List<TestResult>
        {
            new TestResult { IsSuccess = true, ApiTestId = 1, ExecutedAt = DateTime.UtcNow, DurationMilliseconds = 100 },
            new TestResult { IsSuccess = false, SqlTestId = 2, ExecutedAt = DateTime.UtcNow.AddMinutes(1), DurationMilliseconds = 200 }
        };

            // Act
            var stats = _service.ComputeStatistics(results);

            // Assert
            Assert.Equal(2, stats.TotalTests);
            Assert.Equal(1, stats.TotalSuccess);
            Assert.Equal(1, stats.TotalFailed);
            Assert.Equal(1, stats.ResultsByType["API"].Success);
            Assert.Equal(1, stats.ResultsByType["SQL"].Failed);
            Assert.Equal(2, stats.ExecutionTrend.Count);
        }

        [Fact]
        public void ComputeStatistics_EmptyResults_ReturnsEmptyStats()
        {
            // Arrange
            var results = new List<TestResult>();

            // Act
            var stats = _service.ComputeStatistics(results);

            // Assert
            Assert.Equal(0, stats.TotalTests);
            Assert.Equal(0, stats.TotalSuccess);
            Assert.Equal(0, stats.TotalFailed);
            Assert.Empty(stats.ResultsByType);
            Assert.Empty(stats.ExecutionTrend);
        }
    }

    public class ApiTestsControllerTests
    {
        private readonly ApiTestsController _controller;
        private readonly ApplicationDbContext _context;
        private readonly Mock<IValidator<ApiTestDto>> _validatorMock;

        public ApiTestsControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _validatorMock = new Mock<IValidator<ApiTestDto>>();

            _controller = new ApiTestsController(_context, _validatorMock.Object);

            // Mock user identity
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
            new Claim(ClaimTypes.NameIdentifier, "user1")
            }, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task CreateApiTest_ValidDto_ReturnsCreated()
        {
            // Arrange
            var dto = new ApiTestDto
            {
                Id = 1,
                Name = "Test API",
                Url = "https://api.example.com",
                Method = "GET",
                ExpectedStatusCode = 200
            };
            _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            // Act
            var result = await _controller.CreateApiTest(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(201, createdResult.StatusCode);
            var createdDto = Assert.IsType<ApiTestDto>(createdResult.Value);
            Assert.Equal(dto.Name, createdDto.Name);
        }

        [Fact]
        public async Task GetApiTest_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            var id = 999;

            // Act
            var result = await _controller.GetApiTest(id);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }
    }

    public class ApiTestScenariosControllerTests
    {
        private readonly ApiTestScenariosController _controller;
        private readonly ApplicationDbContext _context;

        public ApiTestScenariosControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _controller = new ApiTestScenariosController(_context);

            // Mock user identity
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
            new Claim(ClaimTypes.NameIdentifier, "user1")
            }, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task CreateScenario_ValidDto_ReturnsCreated()
        {
            // Arrange
            var userId = "user1";
            var test = new ApiTest { Id = 1, Name = "Test API", CreatedByUserId = userId };
            _context.ApiTests.Add(test);
            await _context.SaveChangesAsync();

            var dto = new ApiTestScenarioDto
            {
                Id = 1,
                ScenarioName = "Test Scenario",
                TestIds = new List<int> { 1 },
                CreatedByUserId = userId
            };

            // Act
            var result = await _controller.CreateScenario(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(201, createdResult.StatusCode);
            var createdDto = Assert.IsType<ApiTestScenarioDto>(createdResult.Value);
            Assert.Equal(dto.ScenarioName, createdDto.ScenarioName);
        }

        [Fact]
        public async Task CreateScenario_InvalidTestIds_ReturnsBadRequest()
        {
            // Arrange
            var dto = new ApiTestScenarioDto
            {
                Id = 1,
                ScenarioName = "Test Scenario",
                TestIds = new List<int> { 999 },
                CreatedByUserId = "user1"
            };

            // Act
            var result = await _controller.CreateScenario(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
    }

    public class SchedulerControllerTests
    {
        private readonly SchedulerController _controller;
        private readonly Mock<SchedulerService> _schedulerServiceMock;
        private readonly Mock<IValidator<ScenarioScheduleDto>> _validatorMock;

        public SchedulerControllerTests()
        {
            _schedulerServiceMock = new Mock<SchedulerService>();
            _validatorMock = new Mock<IValidator<ScenarioScheduleDto>>();
            _controller = new SchedulerController(_schedulerServiceMock.Object);

            // Mock user identity
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
            new Claim(ClaimTypes.NameIdentifier, "user1")
            }, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task ScheduleScenario_ValidDto_ReturnsOk()
        {
            // Arrange
            var schedule = new ScenarioScheduleDto
            {
                ScenarioId = "1",
                UserId = "user1",
                StartTime = DateTime.UtcNow,
                CronExpression = "0 0 12 * * ?"
            };
            _validatorMock.Setup(v => v.ValidateAsync(schedule, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());
            _schedulerServiceMock.Setup(s => s.ScheduleScenarioAsync(schedule))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ScheduleScenario(schedule);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Scenario scheduled successfully", okResult.Value);
        }

        [Fact]
        public async Task CancelScenario_ValidIds_ReturnsOk()
        {
            // Arrange
            var scenarioId = "1";
            var userId = "user1";
            _schedulerServiceMock.Setup(s => s.CancelScheduledScenarioAsync(scenarioId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CancelScenario(scenarioId, userId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }
    }


    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseContent;
        private readonly TimeSpan _delay;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string responseContent, TimeSpan? delay = null)
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
            _delay = delay ?? TimeSpan.Zero;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(_delay, cancellationToken);
            return new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content = new StringContent(_responseContent)
            };
        }
    }
}