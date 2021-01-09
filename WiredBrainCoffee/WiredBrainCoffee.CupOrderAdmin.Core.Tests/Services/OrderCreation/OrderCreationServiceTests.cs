using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using WiredBrainCoffee.CupOrderAdmin.Core.DataInterfaces;
using WiredBrainCoffee.CupOrderAdmin.Core.Model;
using WiredBrainCoffee.CupOrderAdmin.Core.Model.Enums;
using WiredBrainCoffee.CupOrderAdmin.Core.Services.OrderCreation;

namespace WiredBrainCoffee.CupOrderAdmin.Core.Tests.Services.OrderCreation
{
    [TestFixture]
    public class OrderCreationServiceTests
    {

        private OrderCreationService _orderCreationService;
        private int _numberOfCupsInStock;

        [SetUp]
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

        [Test]
        public async Task ShouldStoreCreatedOrderInOrderCreationResult()
        {
            var numberOfOrderedCups = 1;
            var customer = new Customer
            {
                Id = 99
            };

            var orderCreationResult = 
            await _orderCreationService.CreateOrderAsync(customer, numberOfOrderedCups);

            Assert.AreEqual(OrderCreationResultCode.Success, orderCreationResult.ResultCode);
            Assert.IsNotNull(orderCreationResult.CreatedOrder);
            Assert.AreEqual(customer.Id, orderCreationResult.CreatedOrder.CustomerId);
        }

        [Test]
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

        [Test]
        public async Task ShouldReturnStockExceededResultIfNotEnoughCupsInStock()
        {
            var numberOfOrderedCups = _numberOfCupsInStock + 1;
            var customer = new Customer();

            var orderCreationResult =
                await _orderCreationService.CreateOrderAsync(customer, numberOfOrderedCups);

            Assert.AreEqual(OrderCreationResultCode.StockExceeded, orderCreationResult.ResultCode);
            Assert.AreEqual(_numberOfCupsInStock, orderCreationResult.RemainingCupsInStock);
        }

        [Test]
        public void ShouldThrowExceptionIfNumberOfOrderedCupsOsLessThanOne()
        {
            var numberOfOrderedCups = 0;
            var customer = new Customer();

            var exception = Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                _orderCreationService.CreateOrderAsync(customer, numberOfOrderedCups));

            Assert.AreEqual("numberOfOrderedCups", exception.ParamName);
        }

        [Test]
        public void ShouldThrowExceptionIfCustomerIsNull()
        {
            var numberOfOrderedCups = 1;
            Customer customer = null;

            var exception =  Assert.ThrowsAsync<ArgumentNullException>(() =>
                _orderCreationService.CreateOrderAsync(customer, numberOfOrderedCups));

            Assert.AreEqual("customer", exception.ParamName);
        }

        [TestCase(3, 5, CustomerMembership.Basic)]
        [TestCase(0, 4, CustomerMembership.Basic)]
        [TestCase(0, 1, CustomerMembership.Basic)]
        [TestCase(8, 5, CustomerMembership.Premium)]
        [TestCase(5, 4, CustomerMembership.Premium)]
        [TestCase(5, 1, CustomerMembership.Premium)]
        public void ShouldCalculateCorrectDiscountPercentage(
            double expectedDiscountInPercent,
            int numberOfOrderedCups, 
            CustomerMembership customerMembership)
        {

            var discount = OrderCreationService.CalculateDiscountPercentage(customerMembership, numberOfOrderedCups);
            Assert.AreEqual(expectedDiscountInPercent, discount);
        }

    }
}
