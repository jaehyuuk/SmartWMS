using Microsoft.AspNetCore.Mvc;
using SmartWMS.Api.Models;

namespace SmartWMS.Api.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase {

        private static readonly List<Product> Products = new() {
            new Product {
                Id = 1,
                Code = "P001",
                Name = "노트북",
                StockQuantity = 10
            },
            new Product {
                Id = 2,
                Code = "P002",
                Name = "키보드",
                StockQuantity = 25
            },
            new Product {
                Id = 3,
                Code = "P003",
                Name = "마우스",
                StockQuantity = 40
            }
        };

        // 전체 상품 조회
        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetProducts() {
            return Ok(Products);
        }

        // 상품 단건 조회
        [HttpGet("{id:int}")]
        public ActionResult<Product> GetProduct(int id) {
            var product = Products.FirstOrDefault(x => x.Id == id);

            if (product is null) {
                return NotFound(new {
                    message = $"ID가 {id}인 상품을 찾을 수 없습니다."
                });
            }

            return Ok(product);
        }

        // 상품 등록
        [HttpPost]
        public ActionResult<Product> CreateProduct(Product product) {
            var isDuplicateCode = Products.Any(x => x.Code == product.Code);

            if (isDuplicateCode) {
                return BadRequest(new {
                    message = $"상품 코드 {product.Code}는 이미 사용 중입니다."
                });
            }

            product.Id = Products.Count == 0
                ? 1
                : Products.Max(x => x.Id) + 1;

            Products.Add(product);

            return CreatedAtAction(
                nameof(GetProduct),
                new { id = product.Id },
                product
            );
        }

        // 상품 수정
        [HttpPut("{id:int}")]
        public ActionResult<Product> UpdateProduct(int id, Product request) {
            var product = Products.FirstOrDefault(x => x.Id == id);

            if (product is null) {
                return NotFound(new {
                    message = $"ID가 {id}인 상품을 찾을 수 없습니다."
                });
            }

            var isDuplicateCode = Products.Any(x =>
                x.Id != id &&
                x.Code == request.Code
            );

            if (isDuplicateCode) {
                return BadRequest(new {
                    message = $"상품 코드 {request.Code}는 이미 사용 중입니다."
                });
            }

            product.Code = request.Code;
            product.Name = request.Name;
            product.StockQuantity = request.StockQuantity;

            return Ok(product);
        }

        // 상품 삭제
        [HttpDelete("{id:int}")]
        public IActionResult DeleteProduct(int id) {
            var product = Products.FirstOrDefault(x => x.Id == id);

            if (product is null) {
                return NotFound(new {
                    message = $"ID가 {id}인 상품을 찾을 수 없습니다."
                });
            }

            Products.Remove(product);

            return NoContent();
        }
    }
}
