using Microsoft.AspNetCore.Mvc;
using PbDori.Repositories;

namespace PbDori.Controllers;

[ApiController]
[Route("[controller]")]
public class MarketTrendController : ControllerBase
{
    private readonly IMarketTrendResultRepository m_marketTrendResultRepository;

    public MarketTrendController(IMarketTrendResultRepository marketTrendResultRepository)
    {
        m_marketTrendResultRepository = marketTrendResultRepository;
    }

    [HttpGet(Name = "GetMarketTrend")]
    public async Task<MarketTrendApiResult> Get()
    {
        var result = await m_marketTrendResultRepository.LoadAsync(HttpContext.RequestAborted);
        return new MarketTrendApiResult
        {
            MarketTrend = result,
            DataAvailable = result != null
        };
    }
}