using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Common;
using SmartWMS.Api.Data;
using SmartWMS.Api.Dtos.Products;
using SmartWMS.Api.Models;

namespace SmartWMS.Api.Controllers;

/// <summary>
/// 상품 관련 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly SmartWmsDbContext _dbContext;

    public ProductController(SmartWmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // 전체 상품 조회
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
        CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<IEnumerable<Product>> {
            Success = true,
            Message = "상품 목록 조회에 성공했습니다.",
            Data = products
        });
    }

    // 상품 단건 조회
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetProduct(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (product is null) {
            return NotFound(new {
                message = $"ID가 {id}인 상품을 찾을 수 없습니다."
            });
        }

        return Ok(new ApiResponse<Product> {
            Success = true,
            Message = "상품 조회에 성공했습니다.",
            Data = product
        });
    }

    // 상품 등록
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedCode =
            request.Code.Trim().ToUpperInvariant();

        var normalizedName = request.Name.Trim();

        var isDuplicateCode = await _dbContext.Products
            .AnyAsync(
                x => x.Code == normalizedCode,
                cancellationToken);

        if (isDuplicateCode) {
            return Conflict(new {
                message =
                    $"상품 코드 {normalizedCode}는 이미 사용 중입니다."
            });
        }

        var product = new Product {
            Code = normalizedCode,
            Name = normalizedName,
            StockQuantity = 0
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetProduct),
            new { id = product.Id },
            new ApiResponse<Product> {
                Success = true,
                Message = "상품이 등록되었습니다.",
                Data = product
            });
    }

    // 상품 수정
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Product>> UpdateProduct(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (product is null) {
            return NotFound(new {
                message = $"ID가 {id}인 상품을 찾을 수 없습니다."
            });
        }

        var normalizedCode =
            request.Code.Trim().ToUpperInvariant();

        var normalizedName = request.Name.Trim();

        var isDuplicateCode = await _dbContext.Products
            .AnyAsync(
                x => x.Id != id &&
                     x.Code == normalizedCode,
                cancellationToken);

        if (isDuplicateCode) {
            return Conflict(new {
                message =
                    $"상품 코드 {normalizedCode}는 이미 사용 중입니다."
            });
        }

        product.Code = normalizedCode;
        product.Name = normalizedName;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<Product> {
            Success = true,
            Message = "상품이 수정되었습니다.",
            Data = product
        });
    }

    // 상품 삭제
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (product is null) {
            return NotFound(new {
                message = $"ID가 {id}인 상품을 찾을 수 없습니다."
            });
        }

        if (product.StockQuantity > 0) {
            return Conflict(new {
                message = "재고가 남아 있는 상품은 삭제할 수 없습니다."
            });
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}