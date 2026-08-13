using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Common;
using SmartWMS.Api.Data;
using SmartWMS.Api.Dtos.Inbounds;
using SmartWMS.Api.Models;

namespace SmartWMS.Api.Controllers;

/// <summary>
/// 입고 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InboundController : ControllerBase
{
    private readonly SmartWmsDbContext _dbContext;

    public InboundController(SmartWmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //입고 생성
    [HttpPost]
    public async Task<ActionResult<ApiResponse<InboundResponse>>> CreateInbound(
        CreateInboundRequest request,
        CancellationToken cancellationToken)
    {
        // 입고할 상품 조회
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

        var inbound = new Inbound {
            ProductId = product.Id,
            Quantity = request.Quantity,
            InboundDate = DateTime.Now,
            Memo = request.Memo.Trim()
        };

        // 입고 수량만큼 현재 재고 증가
        product.StockQuantity += request.Quantity;

        _dbContext.Inbounds.Add(inbound);

        // 입고 이력 추가 + 상품 재고 증가를 한 번에 저장
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new InboundResponse {
            Id = inbound.Id,
            ProductId = inbound.ProductId,
            ProductCode = product.Code,
            ProductName = product.Name,
            Quantity = inbound.Quantity,
            InboundDate = inbound.InboundDate,
            Memo = inbound.Memo
        };

        return CreatedAtAction(
            nameof(GetInbound),
            new { id = inbound.Id },
            new ApiResponse<InboundResponse> {
                Success = true,
                Message = "입고가 등록되었습니다.",
                Data = response
            });
    }

    //입고 이력 조회
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<InboundResponse>>> GetInbound(
        int id,
        CancellationToken cancellationToken)
    {
        var inbound = await _dbContext.Inbounds
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new InboundResponse {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductCode = x.Product.Code,
                ProductName = x.Product.Name,
                Quantity = x.Quantity,
                InboundDate = x.InboundDate,
                Memo = x.Memo
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (inbound is null) {
            return NotFound(new ApiErrorResponse {
                StatusCode = StatusCodes.Status404NotFound,
                Message = $"ID가 {id}인 입고 이력을 찾을 수 없습니다."
            });
        }

        return Ok(new ApiResponse<InboundResponse> {
            Success = true,
            Message = "입고 이력 조회에 성공했습니다.",
            Data = inbound
        });
    }

    // 입고 목록 조회
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<InboundResponse>>>> GetInbounds(
        CancellationToken cancellationToken)
    {
        var inbounds = await _dbContext.Inbounds
            .AsNoTracking()
            .OrderByDescending(x => x.InboundDate)
            .Select(x => new InboundResponse {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductCode = x.Product.Code,
                ProductName = x.Product.Name,
                Quantity = x.Quantity,
                InboundDate = x.InboundDate,
                Memo = x.Memo
            })
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<IEnumerable<InboundResponse>> {
            Success = true,
            Message = "입고 이력 목록 조회에 성공했습니다.",
            Data = inbounds
        });
    }

    // 상품별 입고 이력 조회
    [HttpGet("product/{productId:int}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InboundResponse>>>> GetInboundsByProduct(
        int productId,
        CancellationToken cancellationToken)
    {
        // 상품 존재 여부 확인
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

        var inbounds = await _dbContext.Inbounds
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.InboundDate)
            .Select(x => new InboundResponse {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductCode = x.Product.Code,
                ProductName = x.Product.Name,
                Quantity = x.Quantity,
                InboundDate = x.InboundDate,
                Memo = x.Memo
            })
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<IEnumerable<InboundResponse>> {
            Success = true,
            Message = "상품별 입고 이력 조회에 성공했습니다.",
            Data = inbounds
        });
    }
}