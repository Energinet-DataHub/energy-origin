using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.Controllers;
using API.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;
public class TransferAgreementsControllerTests
    {
        private TransferAgreementsController controller;
        private readonly Mock<ITransferAgreementRepository> mockTransferAgreementRepository;
        private readonly string subject = "03bad0af-caeb-46e8-809c-1d35a5863bc7";
        private readonly string atr = "d4f32241-442c-4043-8795-a4e6bf574e7f";
        private readonly string tin = "11223344";

        public TransferAgreementsControllerTests()
        {
            mockTransferAgreementRepository = new Mock<ITransferAgreementRepository>();
            mockTransferAgreementRepository
                .Setup(o => o.AddTransferAgreementToDb(It.IsAny<TransferAgreement>()))
                .ReturnsAsync((TransferAgreement transferAgreement) => transferAgreement);

            controller = CreateControllerWithMockedUser();
        }

        private TransferAgreementsController CreateControllerWithMockedUser()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new("sub", subject),
                new("atr", atr),
                new("tin", tin)
            }, "mock"));

            var controller = new TransferAgreementsController(mockTransferAgreementRepository.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return controller;
        }

        [Fact]
        public async Task Create_ShouldCallRepositoryOnce()
        {
            var request = new CreateTransferAgreement(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), "12345678");
            var userId = Guid.Parse(controller.ControllerContext.HttpContext.User.FindFirstValue("sub") ?? string.Empty);
            var actorId = controller.ControllerContext.HttpContext.User.FindFirstValue("atr");

            await controller.Create(request);

            mockTransferAgreementRepository.Verify(service => service.AddTransferAgreementToDb(It.Is<TransferAgreement>(agreement =>
                agreement.SenderId == userId &&
                agreement.ActorId == actorId &&
                agreement.StartDate == DateTimeOffset.FromUnixTimeSeconds(request.StartDate) &&
                agreement.EndDate == DateTimeOffset.FromUnixTimeSeconds(request.EndDate) &&
                agreement.ReceiverTin == request.ReceiverTin
            )), Times.Once);
        }

        [Fact]
        public async Task List_ShouldCallRepositoryOnce()
        {

            mockTransferAgreementRepository
                .Setup(o => o.GetTransferAgreementsList(Guid.Parse(subject), tin))
                .ReturnsAsync(new List<TransferAgreement>());

            controller = CreateControllerWithMockedUser();

            var result = await controller.GetTransferAgreements();

            mockTransferAgreementRepository.Verify(repository => repository.GetTransferAgreementsList(Guid.Parse(subject), tin), Times.Once);
        }

        [Fact]
        public async Task GetTransferAgreementsList_ShouldReturnCorrectNumberOfAgreements()
        {
            var transferAgreements = new List<TransferAgreement>()
            {
                new() { Id = Guid.NewGuid(), StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(1), ReceiverTin  = "11223344"} ,
                new() { Id = Guid.NewGuid(), StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(1), ReceiverTin  = "00000000"} ,
            };

            mockTransferAgreementRepository
                .Setup(o => o.GetTransferAgreementsList(Guid.Parse(subject), tin))
                .ReturnsAsync(transferAgreements);

            controller = CreateControllerWithMockedUser();

            var result = await controller.GetTransferAgreements();

            var okResult = result.Result as OkObjectResult;
            var agreements = (TransferAgreementsResponse)okResult.Value;

            agreements.Result.Count.Should().Be(transferAgreements.Count);
            mockTransferAgreementRepository.Verify(repository => repository.GetTransferAgreementsList(Guid.Parse(subject), tin), Times.Once);
        }
    }
