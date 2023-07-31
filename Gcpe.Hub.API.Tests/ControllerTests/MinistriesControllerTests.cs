using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Gcpe.Hub.API.Controllers;
using Gcpe.Hub.API.Helpers;
using Gcpe.Hub.Data.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Gcpe.Hub.API.Tests.ControllerTests
{
    public class MinistriesControllerTests
    {
        private Mock<ILogger<MinistriesController>> logger;
        private HubDbContext context;
        private IMapper mapper;
        private DbContextOptions<HubDbContext> options;

        public MinistriesControllerTests()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            options = new DbContextOptionsBuilder<HubDbContext>()
                      .UseInMemoryDatabase(Guid.NewGuid().ToString())
                      .Options;
            context = new HubDbContext(options);
            logger = new Mock<ILogger<MinistriesController>>();
            var mockMapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            });
            mapper = mockMapper.CreateMapper();
        }

        private MinistriesController Controller(HubDbContext c = null)
        {
            return new MinistriesController(c ?? context, logger.Object, mapper);
        }

        [Fact]
        public void GetAllMinistries_ShouldReturnSuccess()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var expectedMinistries = 10;
            for (var i = 0; i < expectedMinistries; i++)
            {
                context.Ministry.Add(TestData.CreateDbMinistries($"Ministry-{i}", Guid.NewGuid()));
            }
            context.SaveChanges();

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var results = Controller().GetAllMinistries();

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            var okObjectResult = results as OkObjectResult;
            Assert.NotNull(okObjectResult);

            var model = (IList<Models.Ministry>) okObjectResult.Value;
            Assert.NotNull(model);

            Assert.Equal(model.Count(), model.Count());
        }

        [Fact]
        public void GetAllMinistries_ShouldReturnBadRequest_WhenDataSourceIsUnavailable()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var mockContext = new Mock<HubDbContext>(options);
            mockContext.Setup(m => m.Ministry).Throws(new InvalidOperationException());

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var result = Controller(mockContext.Object).GetAllMinistries() as BadRequestObjectResult;

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            result.Should().BeOfType(typeof(BadRequestObjectResult), "because the read operation should require a valid data source");
            result.StatusCode.Should().Be(400, "because HTTP Status 400 should be returned to signal a Bad Request");
        }
    }
}
