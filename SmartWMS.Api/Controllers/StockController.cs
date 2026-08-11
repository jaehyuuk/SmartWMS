using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Data;
using SmartWMS.Api.Dtos.Stocks;

namespace SmartWMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly SmartWmsDbContext _dbContext;

    public StockController(SmartWmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // 전체 재고이력 조회
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<StockHistoryResponse>>> GetHistory(
        CancellationToken cancellationToken)
    {
        var inboundHistory = await _dbContext.Inbounds
            .AsNoTracking()
            .Include(x => x.Product)
            .Select(x => new StockHistoryResponse {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductCode = x.Product.Code,
                ProductName = x.Product.Name,
                Type = "INBOUND",
                Quantity = x.Quantity,
                Date = x.InboundDate,
                Memo = x.Memo
            })
            .ToListAsync(cancellationToken);

        var outboundHistory = await _dbContext.Outbounds
            .AsNoTracking()
            .Include(x => x.Product)
            .Select(x => new StockHistoryResponse {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductCode = x.Product.Code,
                ProductName = x.Product.Name,
                Type = "OUTBOUND",
                Quantity = -x.Quantity,
                Date = x.OutboundDate,
                Memo = x.Memo
            })
            .ToListAsync(cancellationToken);

        var history = inboundHistory
            .Concat(outboundHistory)
            .OrderByDescending(x => x.Date)
            .ToList();

        return Ok(history);
    }

    // 상품별 재고이력 조회
    [HttpGet("history/product/{productId:int}")]
    public async Task<ActionResult<IEnumerable<StockHistoryResponse>>> GetHistoryByProduct(
    int productId,
    CancellationToken cancellationToken)
    {
        var productExists = await _dbContext.Products
            .AnyAsync(
                x => x.Id == productId,
                cancellationToken);

        if (!productExists) {
            return NotFound(new {
                message = $"ID가 {productId}인 상품을 찾을 수 없습니다."
            });
        }

        var inboundHistory = await _dbContext.Inbounds
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => new StockHistoryResponse {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductCode = x.Product.Code,
                ProductName = x.Product.Name,
                Type = "INBOUND",
                Quantity = x.Quantity,
                Date = x.InboundDate,
                Memo = x.Memo
            })
            .ToListAsync(cancellationToken);

        var outboundHistory = await _dbContext.Outbounds
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => new StockHistoryResponse {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductCode = x.Product.Code,
                ProductName = x.Product.Name,
                Type = "OUTBOUND",
                Quantity = -x.Quantity,
                Date = x.OutboundDate,
                Memo = x.Memo
            })
            .ToListAsync(cancellationToken);

        var history = inboundHistory
            .Concat(outboundHistory)
            .OrderByDescending(x => x.Date)
            .ToList();

        return Ok(history);
    }
}