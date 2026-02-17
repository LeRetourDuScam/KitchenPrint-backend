# 🌱 Seed Data - Ingrédients de Base

## Données Environnementales de Base

Voici des ingrédients de base avec leurs facteurs d'émission CO2 et empreintes hydriques, basés sur les données Agribalyse/ADEME.

### Comment Utiliser

**Option 1 : Créer une migration de seed data**

Créez un fichier dans `TIBG.ENTITIES/Migrations/` :

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

public partial class SeedInitialIngredients : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "Ingredients",
            columns: new[] { "Name", "Category", "CarbonEmissionKgPerKg", "WaterFootprintLitersPerKg", "Season", "Origin", "ApiSource", "IsActive" },
            values: new object[,]
            {
                // Viandes
                { "Bœuf", "Viandes", 27.0m, 15400m, "all-year", "national", "Manual", true },
                { "Porc", "Viandes", 7.6m, 6000m, "all-year", "national", "Manual", true },
                { "Poulet", "Viandes", 6.9m, 4300m, "all-year", "national", "Manual", true },
                { "Agneau", "Viandes", 39.2m, 10400m, "all-year", "national", "Manual", true },

                // Poissons
                { "Saumon", "Poissons", 11.9m, 3700m, "all-year", "imported", "Manual", true },
                { "Thon", "Poissons", 6.1m, 2000m, "all-year", "imported", "Manual", true },
                { "Crevettes", "Poissons", 26.9m, 3500m, "all-year", "imported", "Manual", true },

                // Produits laitiers
                { "Lait de vache", "Produits laitiers", 1.9m, 1000m, "all-year", "local", "Manual", true },
                { "Fromage", "Produits laitiers", 13.5m, 5000m, "all-year", "national", "Manual", true },
                { "Yaourt", "Produits laitiers", 2.2m, 1200m, "all-year", "national", "Manual", true },
                { "Beurre", "Produits laitiers", 23.8m, 5500m, "all-year", "national", "Manual", true },

                // Œufs
                { "Œufs", "Œufs", 4.2m, 3300m, "all-year", "local", "Manual", true },

                // Céréales
                { "Riz", "Céréales", 4.0m, 2500m, "all-year", "imported", "Manual", true },
                { "Pâtes", "Céréales", 1.4m, 1800m, "all-year", "national", "Manual", true },
                { "Pain", "Céréales", 0.8m, 1600m, "all-year", "local", "Manual", true },
                { "Farine de blé", "Céréales", 0.7m, 1800m, "all-year", "national", "Manual", true },

                // Légumes
                { "Tomate", "Légumes", 0.7m, 214m, "summer", "local", "Manual", true },
                { "Pomme de terre", "Légumes", 0.3m, 290m, "all-year", "local", "Manual", true },
                { "Carotte", "Légumes", 0.4m, 131m, "all-year", "local", "Manual", true },
                { "Oignon", "Légumes", 0.4m, 272m, "all-year", "local", "Manual", true },
                { "Salade", "Légumes", 0.3m, 237m, "spring", "local", "Manual", true },
                { "Concombre", "Légumes", 0.5m, 353m, "summer", "local", "Manual", true },
                { "Courgette", "Légumes", 0.6m, 322m, "summer", "local", "Manual", true },
                { "Aubergine", "Légumes", 0.6m, 363m, "summer", "local", "Manual", true },
                { "Poivron", "Légumes", 0.7m, 379m, "summer", "local", "Manual", true },

                // Légumineuses
                { "Lentilles", "Légumineuses", 0.9m, 5000m, "all-year", "national", "Manual", true },
                { "Pois chiches", "Légumineuses", 1.0m, 4200m, "all-year", "imported", "Manual", true },
                { "Haricots rouges", "Légumineuses", 1.1m, 4500m, "all-year", "national", "Manual", true },

                // Fruits
                { "Pomme", "Fruits", 0.5m, 822m, "fall", "local", "Manual", true },
                { "Banane", "Fruits", 0.9m, 790m, "all-year", "imported", "Manual", true },
                { "Orange", "Fruits", 0.4m, 560m, "winter", "imported", "Manual", true },
                { "Fraise", "Fruits", 1.1m, 347m, "spring", "local", "Manual", true },

                // Huiles et matières grasses
                { "Huile d'olive", "Huiles", 3.2m, 14400m, "all-year", "imported", "Manual", true },
                { "Huile de tournesol", "Huiles", 2.3m, 6800m, "all-year", "national", "Manual", true }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(table: "Ingredients", keyColumn: "Name", keyValues: new object[]
        {
            "Bœuf", "Porc", "Poulet", "Agneau", "Saumon", "Thon", "Crevettes",
            "Lait de vache", "Fromage", "Yaourt", "Beurre", "Œufs",
            "Riz", "Pâtes", "Pain", "Farine de blé",
            "Tomate", "Pomme de terre", "Carotte", "Oignon", "Salade", "Concombre", "Courgette", "Aubergine", "Poivron",
            "Lentilles", "Pois chiches", "Haricots rouges",
            "Pomme", "Banane", "Orange", "Fraise",
            "Huile d'olive", "Huile de tournesol"
        });
    }
}
```

**Option 2 : Utiliser l'API Sync**

Appelez l'endpoint `/api/v1/ingredients/sync` avec la liste des ingrédients pour les récupérer depuis Open Food Facts :

```bash
curl -X POST http://localhost:5000/api/v1/ingredients/sync \
  -H "Content-Type: application/json" \
  -d '["tomato", "chicken", "rice", "beef", "potato", "carrot", "onion", "apple", "banana"]'
```

---

## 📊 Tableau Récapitulatif

| Ingrédient | CO2 (kg/kg) | Eau (L/kg) | Saison | Impact |
|------------|-------------|------------|---------|---------|
| Bœuf | 27.0 | 15400 | Toute année | ⚠️ Très élevé |
| Agneau | 39.2 | 10400 | Toute année | ⚠️ Très élevé |
| Crevettes | 26.9 | 3500 | Toute année | ⚠️ Élevé |
| Fromage | 13.5 | 5000 | Toute année | ⚠️ Élevé |
| Saumon | 11.9 | 3700 | Toute année | ⚠️ Élevé |
| Porc | 7.6 | 6000 | Toute année | ⚠️ Moyen |
| Poulet | 6.9 | 4300 | Toute année | ⚠️ Moyen |
| Œufs | 4.2 | 3300 | Toute année | 🟡 Moyen |
| Riz | 4.0 | 2500 | Toute année | 🟡 Moyen |
| Huile d'olive | 3.2 | 14400 | Toute année | 🟡 Moyen |
| Lentilles | 0.9 | 5000 | Toute année | ✅ Faible |
| Pâtes | 1.4 | 1800 | Toute année | ✅ Faible |
| Tomate | 0.7 | 214 | Été | ✅ Faible |
| Pomme de terre | 0.3 | 290 | Toute année | ✅ Très faible |
| Carotte | 0.4 | 131 | Toute année | ✅ Très faible |

---

## 🎯 Recommandations

### Substitutions Durables

**Au lieu de :**
- Bœuf (27 kg CO2) → **Poulet (6.9 kg)** ou **Lentilles (0.9 kg)**
- Fromage (13.5 kg) → **Yaourt (2.2 kg)**
- Crevettes (26.9 kg) → **Thon (6.1 kg)**

### Saisonnalité

Privilégiez les ingrédients de saison pour réduire l'impact du transport et des serres chauffées :
- **Printemps** : Salade, Fraise, Asperge
- **Été** : Tomate, Courgette, Aubergine, Poivron, Concombre
- **Automne** : Pomme, Potiron, Raisin
- **Hiver** : Orange, Chou, Poireau

---

## 📚 Sources

- **Agribalyse 3.1** - Base de données environnementale ADEME
- **Water Footprint Network** - Empreintes hydriques
- **Open Food Facts** - Données nutritionnelles et environnementales
