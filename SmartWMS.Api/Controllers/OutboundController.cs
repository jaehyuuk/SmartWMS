using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Common;
using SmartWMS.Api.Data;
using SmartWMS.Api.Dtos.Outbounds;
using SmartWMS.Api.Models;

namespace SmartWMS.Api.Controllers;

/// <summary>
/// 출고 관련 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OutboundController : ControllerBase {
    private readonly SmartWmsDbContext _dbContext;

    public OutboundController(SmartWmsDbContext dbContext) {
        _dbContext = dbContext;
    }

    // 출고 등록
    [HttpPost]
    public async Task<ActionResult<ApiResponse<OutboundResponse>>> CreateOutbound(
        CreateOutboundRequest request,
        CancellationToken cancellationToken)
    {
        // 출고할 상품 조회
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(
                x => x.Id == request.ProductId,
                cancellationToken);

        if (product is null) {
            return NotFound(new ApiErrorResponse {
                StatusCode = StatusCodes.Status404NotFound,
                Message = $"ID가 {request.ProductId}인 상품을 찾을 수 없습니다."
            });
        }

        // 현재 재고보다 많은 수량은 출고할 수 없음
        if (product.StockQuantity < request.Quantity) {
            return Conflict(new ApiErrorResponse {
                StatusCode = StatusCodes.Status409Conflict,
                Message = $"재고가 부족합니다. 현재 재고: {product.StockQuantity}, 요청 수량: {request.Quantity}"
            });
        }

        var outbound = new Outbound {
            ProductId = product.Id,
            Quantity = request.Quantity,
            OutboundDate = DateTime.Now,
            Memo = request.Memo.Trim()
        };

        // 출고 수량만큼 현재 재고 감소
        product.StockQuantity -= request.Quantity;

        _dbContext.Outbounds.Add(outbound);

        // 출고 이력 추가와 재고 감소를 한 번에 저장
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetOutbound),
            new { id = outbound.Id },
            new ApiResponse<OutboundResponse> {
                Success = true,
                Message = "출고가 등록되었습니다.",
                Data = ToResponse(outbound, product)
            });
    }

    // 출고 단건 조회
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<OutboundResponse>>> GetOutbound(
        int id,
        CancellationToken cancellationToken)
    {
        var outbound = await _dbContext.Outbounds
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new OutboundResponse {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductCode = x.Product.Code,
                ProductName = x.Product.Name,
                Quantity = x.Quantity,
                OutboundDate = x.OutboundDate,
                Memo = x.Memo
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (outbound is null) {
            return NotFound(new ApiErrorResponse {
                StatusCode = StatusCodes.Status404NotFound,
                Message = $"ID가 {id}인 출고 이력을 찾을 수 없습니다."
            });
        }

        return Ok(new ApiResponse<OutboundResponse> {
            Success = true,
            Message = "출고 이력 조회에 성공했습니다.",
            Data = outbound
        });
    }

    // 전체 출고 이력 조회
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<OutboundResponse>>>> GetOutbounds(
        CancellationToken cancellationToken)
    {
        var outbounds = await _dbContext.Outbounds
            .AsNoTracking()
            .OrderByDescending(x => x.OutboundDate)
            .Select(x => new OutboundResponse {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductCode = x.Product.Code,
                ProductName = x.Product.Name,
                Quantity = x.Quantity,
                OutboundDate = x.OutboundDate,
                Memo = x.Memo
            })
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<IEnumerable<OutboundResponse>> {
            Success = true,
            Message = "출고 이력 목록 조회에 성공했습니다.",
            Data = outbounds
        });
    }

    // 상품별 출고 이력 조회
    [HttpGet("product/{productId:int}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OutboundResponse>>>> GetOutboundsByProduct(
        int productId,
        CancellationToken cancellationToken)
    {
        // 조회할 상품이 존재하는지 확인
        var productExists = await _dbContext.Products
            .AnyAsync(
                x => x.Id == productId,
                cancellationToken);

        if (!productExists) {
            return NotFound(new ApiErrorResponse {
                StatusCode = StatusCodes.Status404NotFound,
                Message = $"ID가 {productId}인 상품을 찾을 수 없습니다."
            });
        }

        var outbounds = await _dbContext.Outbounds
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.OutboundDate)
            .Select(x => new OutboundResponse {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductCode = x.Product.Code,
                ProductName = x.Product.Name,
                Quantity = x.Quantity,
                OutboundDate = x.OutboundDate,
                Memo = x.Memo
            })
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<IEnumerable<OutboundResponse>> {
            Success = true,
            Message = "상품별 출고 이력 조회에 성공했습니다.",
            Data = outbounds
        });
    }

    // Outbound Entity를 API 응답 DTO로 변환
    private static OutboundResponse ToResponse(
        Outbound outbound,
        Product product)
    {
        return new OutboundResponse {
            Id = outbound.Id,
            ProductId = outbound.ProductId,
            ProductCode = product.Code,
            ProductName = product.Name,
            Quantity = outbound.Quantity,
            OutboundDate = outbound.OutboundDate,
            Memo = outbound.Memo
        };
    }
}