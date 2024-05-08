using System.ComponentModel.DataAnnotations;

namespace DBAPI.DTOs
{

    public record CreateProductDTOs(
        [Required] int IdProduct,
        [Required] int IdWarehouse,
        [Required] int Amount,
        [Required] DateTime CreatedAt
        );

    public record CreateProductResponse(int IdProduct, int IdWarehouse, int Amount, DateTime CreatedAt)
    {
        public CreateProductResponse(int IdProduct, int IdWarehouse, CreateProductDTOs request) : this(IdProduct, IdWarehouse, request.Amount, request.CreatedAt) { }
    }
}