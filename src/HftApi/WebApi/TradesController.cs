using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using HftApi.Extensions;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Services;
using Lykke.MatchingEngine.Connector.Models.Common;
using Lykke.Service.TradesAdapter.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HftApi.WebApi
{
    [ApiController]
    [Authorize]
    [Route("api/trades")]
    public class TradesController : ControllerBase
    {
        private readonly ValidationService _validationService;
        private readonly HistoryWrapperClient _historyClient;
        private readonly ITradesAdapterClient _tradesAdapterClient;
        private readonly IMapper _mapper;

        public TradesController(
            ValidationService validationService,
            HistoryWrapperClient historyClient,
            ITradesAdapterClient tradesAdapterClient,
            IMapper mapper
            )
        {
            _validationService = validationService;
            _historyClient = historyClient;
            _tradesAdapterClient = tradesAdapterClient;
            _mapper = mapper;
        }

        /// <summary>
        /// Get trade history
        /// </summary>
        /// <remarks>Gets the trading history of an account. Also, with the use of parameters, it can returns a single order.</remarks>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<TradeModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTrades(
            [FromQuery]string assetPairId = null,
            [FromQuery]OrderAction? side = null,
            [FromQuery]int? offset = 0,
            [FromQuery]int? take = 100,
            [FromQuery]double? from = null,
            [FromQuery]double? to = null
            )
        {
            var result = await _validationService.ValidateOrdersRequestAsync(assetPairId, offset, take);

            if (result != null)
                throw HftApiException.Create(result.Code, result.Message)
                    .AddField(result.FieldName);

            DateTime? fromDate = from == null ? (DateTime?) null : DateTime.UnixEpoch.AddMilliseconds(from.Value);
            DateTime? toDate = to == null ? (DateTime?) null : DateTime.UnixEpoch.AddMilliseconds(to.Value);

            var trades = await _historyClient.GetTradersAsync(User.GetWalletId(), assetPairId, offset, take, side, fromDate, toDate);

            return Ok(ResponseModel<IReadOnlyCollection<TradeModel>>.Ok(_mapper.Map<IReadOnlyCollection<TradeModel>>(trades)));
        }

        /// <summary>
        /// Get order trades
        /// </summary>
        /// <remarks>Get trades for specific order.</remarks>
        [HttpGet("order/{orderId}")]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<TradeModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> OrderTrades(string orderId)
        {
            var trades = await _historyClient.GetOrderTradesAsync(User.GetWalletId(), orderId);
            return Ok(ResponseModel<IReadOnlyCollection<TradeModel>>.Ok(_mapper.Map<IReadOnlyCollection<TradeModel>>(trades)));
        }

        /// <summary>
        /// Get last public trades
        /// </summary>
        /// <remarks>Get last trades for specific asset pair.</remarks>
        [AllowAnonymous]
        [HttpGet("public/{assetPairId}")]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<PublicTradeModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublicTrades(string assetPairId,
            [FromQuery]int? offset = 0,
            [FromQuery]int? take = 100
            )
        {
            var data = await _tradesAdapterClient.GetTradesByAssetPairIdAsync(assetPairId, offset ?? 0, take ?? 100);

            return Ok(ResponseModel<IReadOnlyCollection<PublicTradeModel>>.Ok(_mapper.Map<IReadOnlyCollection<PublicTradeModel>>(data.Records)));
        }
    }
}
