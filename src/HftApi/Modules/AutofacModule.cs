using System;
using Autofac;
using HftApi.Common.Configuration;
using HftApi.RabbitSubscribers;
using Lykke.Common.Log;
using Lykke.Exchange.Api.MarketData.Contract;
using Lykke.HftApi.ApiContract;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Lykke.Service.HftInternalService.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Logging;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using Swisschain.LykkeLog.Adapter;
using Swisschain.Sdk.Server.Common;

namespace HftApi.Modules
{
    public class AutofacModule : Module
    {
        private readonly AppConfig _config;

        public AutofacModule(AppConfig config)
        {
            _config = config;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AssetsService>()
                .WithParameter(TypedParameter.From(_config.Cache.AssetsCacheDuration))
                .As<IAssetsService>()
                .As<IStartable>()
                .AutoActivate();

            builder.RegisterType<OrderbooksService>()
                .As<IOrderbooksService>()
                .WithParameter(TypedParameter.From(_config.Redis.OrderBooksCacheKeyPattern))
                .SingleInstance();

            var cache = new RedisCache(new RedisCacheOptions
            {
                Configuration = _config.Redis.RedisConfiguration,
                InstanceName = _config.Redis.InstanceName
            });

            builder.RegisterInstance(cache)
                .As<IDistributedCache>()
                .SingleInstance();

            builder.RegisterMarketDataClient(new MarketDataServiceClientSettings{
                GrpcServiceUrl = _config.Services.MarketDataGrpcServiceUrl});

            builder.Register(ctx =>
            {
                var logger = ctx.Resolve<ILoggerFactory>();
                return logger.ToLykke();
            }).As<ILogFactory>();

            builder.RegisterMeClient(_config.MatchingEngine.GetIpEndPoint());

            builder.RegisterType<KeyUpdateSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("connectionString", _config.RabbitMq.ConnectionString)
                .WithParameter("exchangeName", _config.RabbitMq.ExchangeName)
                .SingleInstance();

            builder.RegisterType<OrderbooksSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("connectionString", _config.RabbitMq.MeConnectionString)
                .WithParameter("exchangeName", _config.RabbitMq.OrderbooksExchangeName)
                .SingleInstance();

            builder.RegisterType<BalancesSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("connectionString", _config.RabbitMq.MeConnectionString)
                .WithParameter("exchangeName", _config.RabbitMq.BalancesExchangeName)
                .SingleInstance();

            builder.RegisterHftInternalClient(_config.Services.HftInternalServiceUrl);

            builder.RegisterType<TokenService>()
                .As<ITokenService>()
                .SingleInstance();

            builder.RegisterType<BalanceService>()
                .As<IBalanceService>()
                .SingleInstance();

            builder.RegisterType<ValidationService>()
                .AsSelf()
                .SingleInstance();

            builder.Register(ctx =>
            {
                var client = new MyNoSqlTcpClient(() => _config.MyNoSqlServer.ReaderServiceUrl, $"{Program.AppName}-{Environment.MachineName}");
                client.Start();
                return client;
            }).AsSelf().SingleInstance();

            builder.Register(ctx =>
                new MyNoSqlReadRepository<TickerEntity>(ctx.Resolve<MyNoSqlTcpClient>(), _config.MyNoSqlServer.TickersTableName)
            ).As<IMyNoSqlServerDataReader<TickerEntity>>().SingleInstance();

            builder.Register(ctx =>
                new MyNoSqlReadRepository<PriceEntity>(ctx.Resolve<MyNoSqlTcpClient>(), _config.MyNoSqlServer.PricesTableName)
            ).As<IMyNoSqlServerDataReader<PriceEntity>>().SingleInstance();

            builder.Register(ctx =>
                new MyNoSqlReadRepository<OrderbookEntity>(ctx.Resolve<MyNoSqlTcpClient>(), _config.MyNoSqlServer.OrderbooksTableName)
            ).As<IMyNoSqlServerDataReader<OrderbookEntity>>().SingleInstance();

            builder.Register(ctx =>
                new MyNoSqlReadRepository<BalanceEntity>(ctx.Resolve<MyNoSqlTcpClient>(), _config.MyNoSqlServer.BalancesTableName)
            ).As<IMyNoSqlServerDataReader<BalanceEntity>>().SingleInstance();

            builder.Register(ctx =>
            {
                return new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<OrderbookEntity>(() =>
                        _config.MyNoSqlServer.WriterServiceUrl,
                    _config.MyNoSqlServer.OrderbooksTableName);
            }).As<IMyNoSqlServerDataWriter<OrderbookEntity>>().SingleInstance();

            builder.Register(ctx =>
            {
                return new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<BalanceEntity>(() =>
                        _config.MyNoSqlServer.WriterServiceUrl,
                    _config.MyNoSqlServer.BalancesTableName, DataSynchronizationPeriod.Immediately);
            }).As<IMyNoSqlServerDataWriter<BalanceEntity>>().SingleInstance();

            builder.RegisterType<StreamService<PriceUpdate>>().As<IStreamService<PriceUpdate>>().SingleInstance();
            builder.RegisterType<StreamService<TickerUpdate>>().As<IStreamService<TickerUpdate>>().SingleInstance();
            builder.RegisterType<StreamService<Lykke.HftApi.ApiContract.Orderbook>>().As<IStreamService<Lykke.HftApi.ApiContract.Orderbook>>().SingleInstance();
            builder.RegisterType<StreamService<Lykke.HftApi.ApiContract.Balance>>().As<IStreamService<Lykke.HftApi.ApiContract.Balance>>().SingleInstance();
            builder.RegisterType<ApplicationManager>().AsSelf().SingleInstance();
        }
    }
}

