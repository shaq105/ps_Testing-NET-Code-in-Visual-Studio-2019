﻿using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WiredBrainCoffee.CupOrderAdmin.Core.DataInterfaces;
using WiredBrainCoffee.CupOrderAdmin.Core.Model;
using WiredBrainCoffee.CupOrderAdmin.Core.Services.OrderCreation;

namespace WiredBrainCoffee.CupOrderAdmin.Core.Tests.Services.OrderCreation
{
    [TestClass]
    public class OrderCreationServiceTests
    {

        private OrderCreationService _orderCreationService;
        private int _numberOfCupsInStock;

        [TestInitialize]
        public void TestInitialize()
        {
            _numberOfCupsInStock = 10;

            var orderRepositoryMock = new Mock<IOrderRepository>();
            orderRepositoryMock.Setup(x => x.SaveAsync(It.IsAny<Order>()))
                .ReturnsAsync((Order x) => x);

            var coffeeCupRepositoryMock = new Mock<ICoffeeCupRepository>();
            coffeeCupRepositoryMock.Setup(x => x.GetCoffeeCupsInStockCountAsync())
                .ReturnsAsync(_numberOfCupsInStock);

            _orderCreationService = new OrderCreationService(
                orderRepositoryMock.Object, coffeeCupRepositoryMock.Object);
        }

        [TestMethod]
        public async Task ShouldStoreCreatedOrderInOrderCreationResult()
        {
            var orderRepositoryMock = new Mock<IOrderRepository>();
            orderRepositoryMock.Setup(x => x.SaveAsync(It.IsAny<Order>()))
                .ReturnsAsync((Order x) => x);

            var coffeeCupRepositoryMock = new Mock<ICoffeeCupRepository>();

            var orderCreationService = new OrderCreationService(orderRepositoryMock.Object, coffeeCupRepositoryMock.Object);
            var numberOfOrderedCups = 1;
            var customer = new Customer
            {
                Id = 99
            };

            var orderCreationResult = 
            await orderCreationService.CreateOrderAsync(customer, numberOfOrderedCups);

            Assert.AreEqual(OrderCreationResultCode.Success, orderCreationResult.ResultCode);
            Assert.IsNotNull(orderCreationResult.CreatedOrder);
            Assert.AreEqual(customer.Id, orderCreationResult.CreatedOrder.CustomerId);
        }

        [TestMethod]
        public async Task ShouldStoreRemainingCupsInStockInOrderCreationResult()
        {
            var numberOfOrderedCups = 3;
            var expectedRemainingCupsInStock = _numberOfCupsInStock - numberOfOrderedCups;
            var customer = new Customer();

            var orderCreationResult =
                await _orderCreationService.CreateOrderAsync(customer, numberOfOrderedCups);

            Assert.AreEqual(OrderCreationResultCode.Success, orderCreationResult.ResultCode);
            Assert.AreEqual(expectedRemainingCupsInStock, orderCreationResult.RemainingCupsInStock);
        }
    }
}
