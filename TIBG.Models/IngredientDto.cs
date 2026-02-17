namespace TIBG.Models
{
    /// <summary>
    /// DTO for Ingredient responses
    /// </summary>
    public class IngredientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public decimal CarbonEmissionKgPerKg { get; set; }
        public decimal WaterFootprintLitersPerKg { get; set; }
        public string? Season { get; set; }
        public string? Origin { get; set; }
        public string? ApiSource { get; set; }
    }

    /// <summary>
    /// Request for ingredient search with filters
    /// </summary>
    public class IngredientSearchRequest
    {
        public string? Query { get; set; }
        public string? Category { get; set; }
        public string? Season { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Response for paginated ingredient list
    /// </summary>
    public class IngredientListResponse
    {
        public List<IngredientDto> Ingredients { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
