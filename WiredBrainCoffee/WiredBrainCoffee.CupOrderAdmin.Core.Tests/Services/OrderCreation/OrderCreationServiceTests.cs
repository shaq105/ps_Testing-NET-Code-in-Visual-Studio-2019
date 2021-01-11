using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using WiredBrainCoffee.CupOrderAdmin.Core.DataInterfaces;
using WiredBrainCoffee.CupOrderAdmin.Core.Model;
using WiredBrainCoffee.CupOrderAdmin.Core.Model.Enums;
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
            coffeeCupRepositoryMock.Setup(x => x.GetCoffeeCupsInStockAsync(It.IsAny<int>()))
                .ReturnsAsync((int numberOfOrderedCups) => Enumerable.Range(1, numberOfOrderedCups)
                                                                                    .Select(x => new CoffeeCup()));

            _orderCreationService = new OrderCreationService(
              orderRepositoryMock.Object, coffeeCupRepositoryMock.Object);
        }

        [TestMethod]
        public async Task ShouldStoreCreatedOrderInOrderCreationResult()
        {
            var numberOfOrderedCups = 1;
            var customer = new Customer { Id = 99 };

            var orderCreationResult =
              await _orderCreationService.CreateOrderAsync(customer, numberOfOrderedCups);

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

        [TestMethod]
        public async Task ShouldReturnStockExceededResultIfNotEnoughCupsInStock()
        {
            var numberOfOrderedCups = _numberOfCupsInStock + 1;
            var customer = new Customer();

            var orderCreationResult =
              await _orderCreationService.CreateOrderAsync(customer, numberOfOrderedCups);

            Assert.AreEqual(OrderCreationResultCode.StockExceeded, orderCreationResult.ResultCode);
            Assert.AreEqual(_numberOfCupsInStock, orderCreationResult.RemainingCupsInStock);
        }

        [TestMethod]
        public async Task ShouldThrowExceptionIfNumberOfOrderedCupsIsLessThanOne()
        {
            var numberOfOrderedCups = 0;
            var customer = new Customer();

            var exception = await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(
              () => _orderCreationService.CreateOrderAsync(customer, numberOfOrderedCups));

            Assert.AreEqual("numberOfOrderedCups", exception.ParamName);
        }

        [TestMethod]
        public async Task ShouldThrowExceptionIfCustomerIsNull()
        {
            var numberOfOrderedCups = 1;
            Customer customer = null;

            var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(
              () => _orderCreationService.CreateOrderAsync(customer, numberOfOrderedCups));

            Assert.AreEqual("customer", exception.ParamName);
        }

        [DataTestMethod]
        [DataRow(3, 5, CustomerMembership.Basic)]
        [DataRow(0, 4, CustomerMembership.Basic)]
        [DataRow(0, 1, CustomerMembership.Basic)]
        [DataRow(8, 5, CustomerMembership.Premium)]
        [DataRow(5, 4, CustomerMembership.Premium)]
        [DataRow(5, 1, CustomerMembership.Premium)]
        public void ShouldCalculateCorrectDiscountPercentage(
          double expectedDiscountInPercent,
          int numberOfOrderedCups,
          CustomerMembership customerMembership)
        {
            var discountInPercent =
              OrderCreationService.CalculateDiscountPercentage(customerMembership,
                numberOfOrderedCups);

            Assert.AreEqual(expectedDiscountInPercent, discountInPercent);
        }

        [TestMethod]
        public void ShouldThrowArgumentNullExceptionIfOrderRepositoryIsNull()
        {
            var exception = Assert.ThrowsException<ArgumentNullException>(() =>
                new OrderCreationService(null, new Mock<ICoffeeCupRepository>().Object));

            Assert.AreEqual("orderRepository", exception.ParamName);
        }

        [TestMethod]

        public void ShouldTrowArgumentNullExceptionIfCoffeeCupRepositoryIsNull()
        {
            var exception = Assert.ThrowsException<ArgumentNullException>(() =>
                new OrderCreationService(new Mock<IOrderRepository>().Object, null));

            Assert.AreEqual("coffeeCupRepository", exception.ParamName);
        }
    }
}
