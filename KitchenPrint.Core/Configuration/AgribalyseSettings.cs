namespace KitchenPrint.API.Core.Configuration
{
    public class AgribalyseSettings
    {
        public string BaseUrl { get; set; } = "https://data.ademe.fr/data-fair/api/v1/datasets/agribalyse-31-synthese/lines";
        public string SearchField { get; set; } = "Nom_du_Produit_en_Français";
        public int PageSize { get; set; } = 5;
        public int CacheDurationMinutes { get; set; } = 1440;
    }
}
