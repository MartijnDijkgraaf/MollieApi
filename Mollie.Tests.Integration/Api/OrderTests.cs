﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mollie.Api.Models;
using Mollie.Api.Models.List;
using Mollie.Api.Models.Order;
using Mollie.Api.Models.Order.Request.PaymentSpecificParameters;
using Mollie.Api.Models.Payment;
using Mollie.Tests.Integration.Framework;
using NUnit.Framework;

namespace Mollie.Tests.Integration.Api
{
    [TestFixture]
    public class OrderTests : BaseMollieApiTestClass
    {
        [Test]
        [RetryOnApiRateLimitFailure(BaseMollieApiTestClass.NumberOfRetries)]
        public async Task CanCreateOrderWithOnlyRequiredFields()
        {
            // If: we create a order request with only the required parameters
            OrderRequest orderRequest = this.CreateOrder();

            // When: We send the order request to Mollie
            OrderResponse result = await this._orderClient.CreateOrderAsync(orderRequest);

            // Then: Make sure we get a valid response
            Assert.IsNotNull(result);
            Assert.AreEqual(orderRequest.Amount.Value, result.Amount.Value);
            Assert.AreEqual(orderRequest.Amount.Currency, result.Amount.Currency);
            Assert.AreEqual(orderRequest.OrderNumber, result.OrderNumber);
            Assert.AreEqual(orderRequest.Lines.Count(), result.Lines.Count());
            Assert.AreEqual(orderRequest.Lines.First().Type, result.Lines.First().Type);
            Assert.IsNotNull(result.Links);
            Assert.AreEqual(orderRequest.Lines.First().ImageUrl, result.Lines.First().Links.ImageUrl.Href);
            Assert.AreEqual(orderRequest.Lines.First().ProductUrl, result.Lines.First().Links.ProductUrl.Href);
            Assert.AreEqual(orderRequest.Lines.First().Metadata, result.Lines.First().Metadata);
        }

        [Test]
        [RetryOnApiRateLimitFailure(BaseMollieApiTestClass.NumberOfRetries)]
        public async Task CanCreateOrderWithMultiplePaymentMethods()
        {
            // When: we create a order request and specify multiple payment methods
            OrderRequest orderRequest = this.CreateOrder();
            orderRequest.Methods = new List<string>() {
                PaymentMethod.Ideal,
                PaymentMethod.CreditCard,
                PaymentMethod.DirectDebit
            };

            // When: We send the order request to Mollie
            OrderResponse result = await this._orderClient.CreateOrderAsync(orderRequest);

            // Then: Make sure we get a valid response
            Assert.IsNotNull(result);
            Assert.AreEqual(orderRequest.Amount.Value, result.Amount.Value);
            Assert.AreEqual(orderRequest.Amount.Currency, result.Amount.Currency);
            Assert.AreEqual(orderRequest.OrderNumber, result.OrderNumber);
        }

        [Test]
        [RetryOnApiRateLimitFailure(BaseMollieApiTestClass.NumberOfRetries)]
        public async Task CanCreateOrderWithSinglePaymentMethod()
        {
            // When: we create a order request and specify a single payment method
            OrderRequest orderRequest = this.CreateOrder();
            orderRequest.Method = PaymentMethod.CreditCard;

            // When: We send the order request to Mollie
            OrderResponse result = await this._orderClient.CreateOrderAsync(orderRequest);

            // Then: Make sure we get a valid response
            Assert.AreEqual(PaymentMethod.CreditCard, orderRequest.Methods.First());
            Assert.IsNotNull(result);
            Assert.AreEqual(orderRequest.Amount.Value, result.Amount.Value);
            Assert.AreEqual(orderRequest.Amount.Currency, result.Amount.Currency);
            Assert.AreEqual(orderRequest.OrderNumber, result.OrderNumber);
        }

        [Test]
        [RetryOnApiRateLimitFailure(BaseMollieApiTestClass.NumberOfRetries)]
        public async Task CanCreateOrderWithPaymentSpecificOptions()
        {
            // If: we create a order request with payment specific parameters
            OrderRequest orderRequest = this.CreateOrder();
            orderRequest.Payment = new IDealSpecificParameters()
            {
                Issuer = "ideal_INGBNL2A"
            };

            // When: We send the order request to Mollie
            OrderResponse result = await this._orderClient.CreateOrderAsync(orderRequest);

            // Then: Make sure we get a valid response
            Assert.IsNotNull(result);
            Assert.AreEqual(orderRequest.Amount.Value, result.Amount.Value);
            Assert.AreEqual(orderRequest.Amount.Currency, result.Amount.Currency);
            Assert.AreEqual(orderRequest.OrderNumber, result.OrderNumber);
        }

        [Test]
        [RetryOnApiRateLimitFailure(BaseMollieApiTestClass.NumberOfRetries)]
        public async Task CanRetrieveOrderAfterCreationOrder()
        {
            // If: we create a new order
            OrderRequest orderRequest = this.CreateOrder();
            OrderResponse createdOrder = await this._orderClient.CreateOrderAsync(orderRequest);

            // When: We attempt to retrieve the order
            OrderResponse retrievedOrder = await this._orderClient.GetOrderAsync(createdOrder.Id);

            // Then: Make sure we get a valid response
            Assert.IsNotNull(retrievedOrder);
            Assert.AreEqual(createdOrder.Id, retrievedOrder.Id);
        }

        [Test]
        [RetryOnApiRateLimitFailure(BaseMollieApiTestClass.NumberOfRetries)]
        public async Task CanRetrieveOrderAndIncludeEmbeddedData()
        {
            // If: we create a new order
            OrderRequest orderRequest = this.CreateOrder();
            OrderResponse createdOrder = await this._orderClient.CreateOrderAsync(orderRequest);

            // When: We attempt to retrieve the order and add the include parameters
            OrderResponse retrievedOrder = await this._orderClient.GetOrderAsync(createdOrder.Id, embedPayments: true, embedShipments: true, embedRefunds: true);

            // Then: Make sure we get a valid response
            Assert.IsNotNull(retrievedOrder);
            Assert.AreEqual(createdOrder.Id, retrievedOrder.Id);
            Assert.IsNotNull(retrievedOrder.Embedded);
            Assert.IsNotNull(retrievedOrder.Embedded.Payments);
            Assert.IsNotNull(retrievedOrder.Embedded.Shipments);
            Assert.IsNotNull(retrievedOrder.Embedded.Refunds);
        }

        [Test]
        [RetryOnApiRateLimitFailure(BaseMollieApiTestClass.NumberOfRetries)]
        public async Task CanUpdateExistingOrder()
        {
            // If: we create a new order
            OrderRequest orderRequest = this.CreateOrder();
            OrderResponse createdOrder = await this._orderClient.CreateOrderAsync(orderRequest);

            // When: We attempt to update the order
            OrderUpdateRequest orderUpdateRequest = new OrderUpdateRequest()
            {
                OrderNumber = "1337",
                BillingAddress = createdOrder.BillingAddress
            };
            orderUpdateRequest.BillingAddress.City = "Den Haag";
            OrderResponse updatedOrder = await this._orderClient.UpdateOrderAsync(createdOrder.Id, orderUpdateRequest);

            // Then: Make sure the order is updated
            Assert.AreEqual(orderUpdateRequest.OrderNumber, updatedOrder.OrderNumber);
            Assert.AreEqual(orderUpdateRequest.BillingAddress.City, updatedOrder.BillingAddress.City);
        }

        [Test]
        [RetryOnApiRateLimitFailure(BaseMollieApiTestClass.NumberOfRetries)]
        public async Task CanCancelCreatedOrder()
        {
            // If: we create a new order
            OrderRequest orderRequest = this.CreateOrder();
            OrderResponse createdOrder = await this._orderClient.CreateOrderAsync(orderRequest);

            // When: We attempt to cancel the order and then retrieve it
            await this._orderClient.CancelOrderAsync(createdOrder.Id);
            OrderResponse canceledOrder = await this._orderClient.GetOrderAsync(createdOrder.Id);

            // Then: The order status should be cancelled
            Assert.AreEqual(OrderStatus.Canceled, canceledOrder.Status);
        }

        [Test]
        [RetryOnApiRateLimitFailure(BaseMollieApiTestClass.NumberOfRetries)]
        public async Task CanUpdateOrderLine()
        {
            // If: we create a new order
            OrderRequest orderRequest = this.CreateOrder();
            OrderResponse createdOrder = await this._orderClient.CreateOrderAsync(orderRequest);

            // When: We update the order line
            OrderLineUpdateRequest updateRequest = new OrderLineUpdateRequest()
            {
                Name = "A fluffy bear"
            };
            OrderResponse updatedOrder = await this._orderClient.UpdateOrderLinesAsync(createdOrder.Id, createdOrder.Lines.First().Id, updateRequest);

            // Then: The name of the order line should be updated
            Assert.AreEqual(updateRequest.Name, updatedOrder.Lines.First().Name);
        }

        [Test]
        [RetryOnApiRateLimitFailure(BaseMollieApiTestClass.NumberOfRetries)]
        public async Task CanRetrieveOrderList()
        {
            // When: Retrieve payment list with default settings
            ListResponse<OrderResponse> response = await this._orderClient.GetOrderListAsync();

            // Then
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Items);
        }

        [Test]
        [RetryOnApiRateLimitFailure(BaseMollieApiTestClass.NumberOfRetries)]
        public async Task ListOrdersNeverReturnsMorePaymentsThenTheNumberOfRequestedOrders()
        {
            // If: Number of orders requested is 5
            int numberOfOrders = 5;

            // When: Retrieve 5 orders
            ListResponse<OrderResponse> response = await this._orderClient.GetOrderListAsync(null, numberOfOrders);

            // Then
            Assert.IsTrue(response.Items.Count <= numberOfOrders);
        }

        private OrderRequest CreateOrder()
        {
            return new OrderRequest()
            {
                Amount = new Amount(Currency.EUR, "100.00"),
                OrderNumber = "16738",
                Lines = new List<OrderLineRequest>() {
                    new OrderLineRequest() {
                        Name = "A box of chocolates",
                        Type = OrderLineDetailsType.Physical,
                        Category = OrderLineDetailsCategory.Gift,
                        Quantity = 1,
                        UnitPrice = new Amount(Currency.EUR, "100.00"),
                        TotalAmount = new Amount(Currency.EUR, "100.00"),
                        VatRate = "21.00",
                        VatAmount = new Amount(Currency.EUR, "17.36"),
                        ImageUrl = "http://www.google.com/",
                        ProductUrl = "http://www.mollie.nl/",
                        Metadata =  "{\"order_id\":\"4.40\"}",
                    }
                },
                BillingAddress = new OrderAddressDetails()
                {
                    GivenName = "John",
                    FamilyName = "Smit",
                    Email = "johnsmit@gmail.com",
                    City = "Rotterdam",
                    Country = "NL",
                    PostalCode = "0000AA",
                    Region = "Zuid-Holland",
                    StreetAndNumber = "Coolsingel 1"
                },
                RedirectUrl = "http://www.google.nl",
                Locale = Locale.nl_NL
            };
        }
    }
}