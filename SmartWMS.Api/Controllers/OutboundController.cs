using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Common;
using SmartWMS.Api.Data;
using SmartWMS.Api.Dtos.Outbounds;
using SmartWMS.Api.Models;

namespace SmartWMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OutboundController : ControllerBase
{
    private readonly SmartWmsDbContext _dbContext;

    public OutboundController(SmartWmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // 출고 생성
    [HttpPost]
    public async Task<ActionResult<Outbound>> CreateOutbound(
        CreateOutboundRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(
                x => x.Id == request.ProductId,
                cancellationToken);

        if (product is null) {
            return NotFound(new {
                message = $"ID가 {request.ProductId}인 상품을 찾을 수 없습니다."
            });
        }

        if (product.StockQuantity < request.Quantity) {
            return Conflict(new {
                message = $"재고가 부족합니다. 현재 재고: {product.StockQuantity}, 요청 수량: {request.Quantity}"
            });
        }

        var outbound = new Outbound {
            ProductId = product.Id,
            Quantity = request.Quantity,
            OutboundDate = DateTime.Now,
            Memo = request.Memo.Trim()
        };

        product.StockQuantity -= request.Quantity;

        _dbContext.Outbounds.Add(outbound);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetOutbound),
            new { id = outbound.Id },
            new ApiResponse<Outbound> {
                Success = true,
                Message = "출고가 등록되었습니다.",
                Data = outbound
            });
    }

    // 출고 이력 조회
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Outbound>> GetOutbound(
        int id,
        CancellationToken cancellationToken)
    {
        var outbound = await _dbContext.Outbounds
            .AsNoTracking()
            .Include(x => x.Product)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (outbound is null) {
            return NotFound(new {
                message = $"ID가 {id}인 출고 이력을 찾을 수 없습니다."
            });
        }

        return Ok(new ApiResponse<Outbound> {
            Success = true,
            Message = "출고 이력 조회에 성공했습니다.",
            Data = outbound
        });
    }

    // 출고 전체 이력 조회
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Outbound>>> GetOutbounds(
    CancellationToken cancellationToken)
    {
        var outbounds = await _dbContext.Outbounds
            .AsNoTracking()
            .Include(x => x.Product)
            .OrderByDescending(x => x.OutboundDate)
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<IEnumerable<Outbound>> {
            Success = true,
            Message = "출고 이력 목록 조회에 성공했습니다.",
            Data = outbounds
        });
    }

    // 상품별 출고조회
    [HttpGet("product/{productId:int}")]
    public async Task<ActionResult<IEnumerable<Outbound>>> GetOutboundsByProduct(
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

        var outbounds = await _dbContext.Outbounds
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.OutboundDate)
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<IEnumerable<Outbound>> {
            Success = true,
            Message = "상품별 출고 이력 조회에 성공했습니다.",
            Data = outbounds
        });
    }
}