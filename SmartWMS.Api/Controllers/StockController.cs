using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Common;
using SmartWMS.Api.Data;
using SmartWMS.Api.Dtos.Stocks;

namespace SmartWMS.Api.Controllers;

/// <summary>
/// 재고 이력 관련 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockController : ControllerBase {
    private readonly SmartWmsDbContext _dbContext;

    public StockController(SmartWmsDbContext dbContext) {
        _dbContext = dbContext;
    }

    // 전체 재고 이력 조회
    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<IEnumerable<StockHistoryResponse>>>> GetHistory(
        CancellationToken cancellationToken)
    {
        var inboundHistory = await _dbContext.Inbounds
            .AsNoTracking()
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

        var history = MergeHistory(inboundHistory, outboundHistory);

        return Ok(new ApiResponse<IEnumerable<StockHistoryResponse>> {
            Success = true,
            Message = "재고 이력 조회에 성공했습니다.",
            Data = history
        });
    }

    // 상품별 재고 이력 조회
    [HttpGet("history/product/{productId:int}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<StockHistoryResponse>>>> GetHistoryByProduct(
        int productId,
        CancellationToken cancellationToken)
    {
        var productExists = await _dbContext.Products
            .AnyAsync(x => x.Id == productId, cancellationToken);

        if (!productExists) {
            return NotFound(new ApiErrorResponse {
                StatusCode = StatusCodes.Status404NotFound,
                Message = $"ID가 {productId}인 상품을 찾을 수 없습니다."
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

        var history = MergeHistory(inboundHistory, outboundHistory);

        return Ok(new ApiResponse<IEnumerable<StockHistoryResponse>> {
            Success = true,
            Message = "상품별 재고 이력 조회에 성공했습니다.",
            Data = history
        });
    }

    // 입고 + 출고 통합 후 최신순 정렬
    private static List<StockHistoryResponse> MergeHistory(
        IEnumerable<StockHistoryResponse> inboundHistory,
        IEnumerable<StockHistoryResponse> outboundHistory)
    {
        return inboundHistory
            .Concat(outboundHistory)
            .OrderByDescending(x => x.Date)
            .ToList();
    }
}