using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Common;
using SmartWMS.Api.Data;
using SmartWMS.Api.Dtos.Common;
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
    public async Task<ActionResult<ApiResponse<PagedResponse<StockHistoryResponse>>>> GetHistory(
        [FromQuery] StockHistorySearchRequest request,
        CancellationToken cancellationToken)
    {

        var keyword = request.Keyword?.Trim();
        var type = request.Type?.Trim().ToUpperInvariant();

        if (type is not null &&
            type != "INBOUND" &&
            type != "OUTBOUND") {
            return BadRequest(new ApiErrorResponse {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "재고 이력 타입이 올바르지 않습니다.",
                Detail = "Type은 INBOUND 또는 OUTBOUND만 사용할 수 있습니다."
            });
        }

        if (request.StartDate.HasValue &&
            request.EndDate.HasValue &&
            request.StartDate.Value.Date > request.EndDate.Value.Date) {
            return BadRequest(new ApiErrorResponse {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "조회 기간이 올바르지 않습니다.",
                Detail = "StartDate는 EndDate보다 클 수 없습니다."
            });
        }

        var inboundQuery = _dbContext.Inbounds
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
            });

        var outboundQuery = _dbContext.Outbounds
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
            });

        var query = inboundQuery.Concat(outboundQuery);

        if (!string.IsNullOrWhiteSpace(keyword)) {
            query = query.Where(x =>
                x.ProductCode.Contains(keyword) ||
                x.ProductName.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(type)) {
            query = query.Where(x => x.Type == type);
        }

        if (request.StartDate.HasValue) {
            var startDate = request.StartDate.Value.Date;

            query = query.Where(x =>
                x.Date >= startDate);
        }

        if (request.EndDate.HasValue) {
            var endDate = request.EndDate.Value.Date.AddDays(1);

            query = query.Where(x =>
                x.Date < endDate);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)request.PageSize);

        var items = await query
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var pagedResponse = new PagedResponse<StockHistoryResponse> {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return Ok(new ApiResponse<PagedResponse<StockHistoryResponse>> {
            Success = true,
            Message = "재고 이력을 조회했습니다.",
            Data = pagedResponse
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