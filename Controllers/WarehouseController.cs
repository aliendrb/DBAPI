using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using DBAPI.DTOs;
using Microsoft.Extensions.Configuration;
namespace DBAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WarehouseController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public WarehouseController(IConfiguration configuration) 
        {
            _configuration = configuration;
        }

        [HttpGet("products")]
        public IActionResult GetAllProducts([FromQuery] string orderBy = "name")
        {
            var response = new List<GetProductsResponse>();

            using (var sqlConnection = new SqlConnection(_configuration.GetConnectionString("Default")))
            {
                string orderByClause;
                if (string.IsNullOrEmpty(orderBy))
                {
                    orderByClause = "ORDER BY name";
                }
                else
                {
                    orderByClause = $"ORDER BY {orderBy}";
                }

                var sqlCommand = new SqlCommand($"SELECT * FROM Product {orderByClause}", sqlConnection);
                sqlCommand.Connection.Open();
                var reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    response.Add(new GetProductsResponse(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetDecimal(3)
                        )
                    );
                }
            }
            return Ok(response);
        }

        [HttpPost("add_product")]
        public IActionResult PostProductWarehouse(CreateProductDTOs request) 
        {
            if (request == null) 
            {
                return BadRequest("no request received");
            }
            if (request.Amount <= 0) 
            {
                return BadRequest("amount has to be a positive number");
            }
            using (var sqlConnection = new SqlConnection(_configuration.GetConnectionString("Default")))
            {
                sqlConnection.Open();

                // Check if the product exists
                var productCommand = new SqlCommand(
                    $"SELECT COUNT(*) FROM Product WHERE IdProduct = {request.IdProduct}",
                    sqlConnection
                );

                int productCount = (int)productCommand.ExecuteScalar();

                if (productCount == 0)
                {
                    return NotFound("IdProduct doesn't exist");
                }

                // Check if the warehouse exists
                var warehouseCommand = new SqlCommand(
                    $"SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = {request.IdWarehouse}",
                    sqlConnection
                );

                int warehouseCount = (int)warehouseCommand.ExecuteScalar();

                if (warehouseCount == 0)
                {
                    return NotFound("IdWarehouse doesn't exist");
                }

                // Check if there is a purchase order for the product with the requested amount
                var orderCommand = new SqlCommand(
                    $"SELECT COUNT(*) FROM [Order] WHERE IdProduct = {request.IdProduct} AND Amount >= {request.Amount}",
                    sqlConnection
                );

                int orderCount = (int)orderCommand.ExecuteScalar();

                if (orderCount == 0)
                {
                    return BadRequest("There is no purchase order for the product with the requested amount.");
                }

                // Check if the order creation date is earlier than the request creation date
                var orderDateCommand = new SqlCommand(
                    $"SELECT CreatedAt FROM [Order] WHERE IdProduct = {request.IdProduct} AND Amount >= {request.Amount}",
                    sqlConnection
                );

                var orderDateReader = orderDateCommand.ExecuteReader();

                if (orderDateReader.Read())
                {
                    DateTime orderDate = orderDateReader.GetDateTime(0);

                    if (orderDate >= request.CreatedAt)
                    {
                        return BadRequest("The creation date of the purchase order is not earlier than the request creation date.");
                    }
                }
                else
                {
                    return BadRequest("There is no purchase order for the product with the requested amount.");
                }
            }
            using (var sqlConnection = new SqlConnection(_configuration.GetConnectionString("Default")))
            {
                var sqlCommand = new SqlCommand(
                    $"INSERT INTO Product_Warehouse (IdProduct, IdWarehouse, Amount, CreatedAt) values (@1, @2, @3, CONVERT(DATETIME, '{DateTime.Now}'));",
                    sqlConnection
                    );
                sqlCommand.Parameters.AddWithValue("@1", request.IdProduct);
                sqlCommand.Parameters.AddWithValue("@2", request.IdWarehouse);
                sqlCommand.Parameters.AddWithValue("@3", request.Amount);
                sqlCommand.Parameters.AddWithValue("@4", request.CreatedAt);
                sqlCommand.Connection.Open();

                var id = sqlCommand.ExecuteScalar();

                return Created($"Product_Warehouse/{id}", new CreateProductResponse(request.IdProduct, request.IdWarehouse, request));
            }
        }
    }
}
