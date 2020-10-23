using System;
using System.Globalization;
using AutoMapper;
using HftApi.Common.Domain.MyNoSqlEntities;
using HftApi.WebApi.Models;
using Lykke.Exchange.Api.MarketData;
using Lykke.HftApi.Domain.Entities;
using Trade = Lykke.HftApi.Domain.Entities.Trade;

namespace HftApi.Profiles
{
    public class WebProfile : Profile
    {
        public WebProfile()
        {
            CreateMap<DateTime, long>().ConvertUsing(dt => (long)(dt.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds);
            CreateMap<string, decimal>().ConvertUsing((str, res) => decimal.TryParse(str,
                NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0m);
            CreateMap<OrderEntity, OrderModel>(MemberList.Destination);
            CreateMap<Order, OrderModel>(MemberList.Destination);
            CreateMap<Trade, TradeModel>(MemberList.Destination)
                .ForMember(d => d.BaseVolume, o => o.MapFrom(x => Math.Abs(x.BaseVolume)))
                .ForMember(d => d.QuoteVolume, o => o.MapFrom(x => Math.Abs(x.QuoteVolume)))
                .ForMember(d => d.Side, o => o.MapFrom(x => x.BaseVolume < 0 ? TradeSide.Sell : TradeSide.Buy));
            CreateMap<TradeEntity, TradeModel>(MemberList.Destination)
                .ForMember(d => d.BaseVolume, o => o.MapFrom(x => Math.Abs(x.BaseVolume)))
                .ForMember(d => d.QuoteVolume, o => o.MapFrom(x => Math.Abs(x.QuoteVolume)))
                .ForMember(d => d.Side, o => o.MapFrom(x => x.BaseVolume < 0 ? TradeSide.Sell : TradeSide.Buy));
            CreateMap<TickerEntity, TickerModel>(MemberList.Destination);
            CreateMap<Orderbook, OrderbookModel>(MemberList.Destination);
            CreateMap<VolumePrice, VolumePriceModel>(MemberList.Destination);
            CreateMap<MarketSlice, TickerModel>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => DateTime.UtcNow));
            CreateMap<PriceEntity, PriceModel>(MemberList.Destination);
            CreateMap<MarketSlice, PriceModel>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => DateTime.UtcNow));
            CreateMap<Lykke.Service.TradesAdapter.AutorestClient.Models.Trade, PublicTradeModel>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.DateTime))
                .ForMember(d => d.Side, o => o.MapFrom(x => x.Action == Lykke.Service.TradesAdapter.AutorestClient.Models.TradeAction.Buy ? TradeSide.Buy : TradeSide.Sell));
        }
    }
}
