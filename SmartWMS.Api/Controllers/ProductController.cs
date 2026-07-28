using Microsoft.AspNetCore.Mvc;
using SmartWMS.Api.Models;

namespace SmartWMS.Api.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase {

        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetProducts() {
            var products = new List<Product> {

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

            return Ok(products);
        }

    }
}
