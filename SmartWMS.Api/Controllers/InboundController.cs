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
    public async Task<ActionResult<Inbound>> CreateInbound(
        CreateInboundRequest request,
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

        var inbound = new Inbound {
            ProductId = product.Id,
            Quantity = request.Quantity,
            InboundDate = DateTime.Now,
            Memo = request.Memo.Trim()
        };

        product.StockQuantity += request.Quantity;

        _dbContext.Inbounds.Add(inbound);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetInbound),
            new { id = inbound.Id },
            new ApiResponse<Inbound> {
                Success = true,
                Message = "입고가 등록되었습니다.",
                Data = inbound
            });
    }

    //입고 이력 조회
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Inbound>> GetInbound(
        int id,
        CancellationToken cancellationToken)
    {
        var inbound = await _dbContext.Inbounds
            .AsNoTracking()
            .Include(x => x.Product)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (inbound is null) {
            return NotFound(new {
                message = $"ID가 {id}인 입고 이력을 찾을 수 없습니다."
            });
        }

        return Ok(new ApiResponse<Inbound> {
            Success = true,
            Message = "입고 이력 조회에 성공했습니다.",
            Data = inbound
        });
    }

    // 입고 목록 조회
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Inbound>>> GetInbounds(
    CancellationToken cancellationToken)
    {
        var inbounds = await _dbContext.Inbounds
            .AsNoTracking()
            .Include(x => x.Product)
            .OrderByDescending(x => x.InboundDate)
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<IEnumerable<Inbound>> {
            Success = true,
            Message = "입고 이력 목록 조회에 성공했습니다.",
            Data = inbounds
        });
    }

    // 상품별 입고 이력 조회
    [HttpGet("product/{productId:int}")]
    public async Task<ActionResult<IEnumerable<Inbound>>> GetInboundsByProduct(
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

        var inbounds = await _dbContext.Inbounds
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.InboundDate)
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<IEnumerable<Inbound>> {
            Success = true,
            Message = "상품별 입고 이력 조회에 성공했습니다.",
            Data = inbounds
        });
    }
}