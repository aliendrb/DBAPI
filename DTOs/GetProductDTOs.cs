namespace DBAPI.DTOs
{
        public record GetProductsResponse(int IdProduct, string Name, string Description, decimal Price);
}
