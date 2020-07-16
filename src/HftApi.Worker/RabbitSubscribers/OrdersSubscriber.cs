using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using HftApi.Common.Domain.MyNoSqlEntities;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.MatchingEngine.Connector.Models.Events;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using MyNoSqlServer.Abstractions;

namespace HftApi.Worker.RabbitSubscribers
{
    [UsedImplicitly]
    public class OrdersSubscriber : IStartable, IDisposable
    {
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly IMyNoSqlServerDataWriter<OrderEntity> _orderWriter;
        private readonly IMapper _mapper;
        private readonly ILogFactory _logFactory;
        private RabbitMqSubscriber<ExecutionEvent> _subscriber;

        public OrdersSubscriber(
            string connectionString,
            string exchangeName,
            IMyNoSqlServerDataWriter<OrderEntity> orderWriter,
            IMapper mapper,
            ILogFactory logFactory)
        {
            _connectionString = connectionString;
            _exchangeName = exchangeName;
            _orderWriter = orderWriter;
            _mapper = mapper;
            _logFactory = logFactory;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, $"hft-{nameof(OrdersSubscriber)}")
                .UseRoutingKey(((int) Lykke.MatchingEngine.Connector.Models.Events.Common.MessageType.Order).ToString())
                .MakeDurable();

            settings.DeadLetterExchangeName = null;

            _subscriber = new RabbitMqSubscriber<ExecutionEvent>(_logFactory,
                    settings,
                    new ResilientErrorHandlingStrategy(_logFactory, settings, TimeSpan.FromSeconds(10)))
                .SetMessageDeserializer(new ProtobufMessageDeserializer<ExecutionEvent>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();
        }

        private async Task ProcessMessageAsync(ExecutionEvent message)
        {
            var orders = new List<OrderEntity>();

            foreach (var order in message.Orders)
            {
                var orderEntity = _mapper.Map<OrderEntity>(order);
                orders.Add(orderEntity);
            }

            await _orderWriter.BulkInsertOrReplaceAsync(orders);

            Task.Run(async () =>
            {
                var ordersToRemove = orders
                    .Where(x => x.Status == OrderStatus.Matched.ToString() ||
                        x.Status == OrderStatus.Cancelled.ToString() ||
                        x.Status == OrderStatus.Rejected.ToString()).ToList();

                foreach (var order in ordersToRemove)
                {
                    await _orderWriter.DeleteAsync(order.WalletId, order.Id);
                }
            });
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }
    }
}
