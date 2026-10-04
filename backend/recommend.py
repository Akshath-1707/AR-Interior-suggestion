import csv
import os
from typing import List, Dict, Any, Optional

class InteriorRecommendationEngine:
    def __init__(self, catalog_path: str):
        """
        Initialize the AI Recommendation Engine with the furniture catalog CSV.
        Uses pure Python standard libraries (no external package dependencies required).
        """
        if not os.path.exists(catalog_path):
            raise FileNotFoundError(f"Catalog file not found at {catalog_path}")
        
        self.catalog: List[Dict[str, Any]] = []
        with open(catalog_path, mode='r', encoding='utf-8') as f:
            reader = csv.DictReader(f)
            for row in reader:
                self.catalog.append({
                    "id": int(row['id']),
                    "name": row['name'],
                    "category": row['category'],
                    "width_cm": float(row['width_cm']),
                    "depth_cm": float(row['depth_cm']),
                    "height_cm": float(row['height_cm']),
                    "min_clearance_cm": float(row['min_clearance_cm']),
                    "style": row['style'],
                    "color": row['color'],
                    "price_tier": row['price_tier'],
                    "model_filename": row['model_filename']
                })

    def recommend(
        self,
        available_width_cm: float,
        available_depth_cm: float,
        category: Optional[str] = None,
        preferred_style: Optional[str] = None,
        preferred_color: Optional[str] = None,
        top_n: int = 3
    ) -> List[Dict[str, Any]]:
        """
        Core Recommendation Pipeline:
        1. Category Filtering
        2. Hard Spatial Constraint Pruning (Width & Depth check)
        3. Multi-Criteria Scoring (Spatial Fit + Style Match + Color Match)
        4. Ranking & Explanation Generation
        """
        results = []

        for item in self.catalog:
            # Step 1: Filter by category if specified
            if category and category.strip() != "":
                if item['category'].lower() != category.lower().strip():
                    continue

            # Step 2: Hard Spatial Pruning (Item must physically fit in available space)
            if item['width_cm'] > available_width_cm or item['depth_cm'] > available_depth_cm:
                continue

            # Step 3: Compute Spatial Fit Score (0.0 to 1.0)
            w_diff = available_width_cm - item['width_cm']
            d_diff = available_depth_cm - item['depth_cm']
            avg_clearance = (w_diff + d_diff) / 2.0
            min_req_clearance = item['min_clearance_cm']

            if avg_clearance < min_req_clearance:
                spatial_score = max(0.2, (avg_clearance / min_req_clearance) * 0.7)
                fit_status = "Tight Fit (Limited Walkway)"
            else:
                extra_space = avg_clearance - min_req_clearance
                if extra_space <= 60.0:
                    spatial_score = 1.0
                    fit_status = "Optimal Fit (Comfortable Walkway)"
                else:
                    spatial_score = max(0.6, 1.0 - ((extra_space - 60.0) / 200.0))
                    fit_status = "Spacious Fit"

            # Step 4: Compute Style Match Score (0.0 to 1.0)
            style_score = 0.5
            if preferred_style and preferred_style.strip() != "":
                if item['style'].lower() == preferred_style.lower().strip():
                    style_score = 1.0
                else:
                    style_score = 0.3

            # Step 5: Compute Color Match Score (0.0 to 1.0)
            color_score = 0.5
            if preferred_color and preferred_color.strip() != "":
                if item['color'].lower() == preferred_color.lower().strip():
                    color_score = 1.0
                else:
                    color_score = 0.3

            # Step 6: Multi-Criteria Utility Weighting (50% Spatial, 30% Style, 20% Color)
            final_match_score = (0.50 * spatial_score) + (0.30 * style_score) + (0.20 * color_score)
            match_percentage = int(round(final_match_score * 100))

            explanation = (
                f"{fit_status}. "
                f"Remaining walkway clearance: {int(avg_clearance)}cm. "
                f"Style match: {'High' if style_score >= 0.8 else 'Moderate'}."
            )

            results.append({
                "id": item['id'],
                "name": item['name'],
                "category": item['category'],
                "style": item['style'],
                "color": item['color'],
                "dimensions": {
                    "width_cm": item['width_cm'],
                    "depth_cm": item['depth_cm'],
                    "height_cm": item['height_cm']
                },
                "match_score": match_percentage,
                "fit_status": fit_status,
                "explanation": explanation,
                "model_filename": item['model_filename']
            })

        # Step 7: Rank by final match score descending
        results.sort(key=lambda x: x['match_score'], reverse=True)
        return results[:top_n]


# Interactive Test Runner
if __name__ == "__main__":
    catalog_file = os.path.join(os.path.dirname(__file__), "data", "furniture_catalog.csv")
    engine = InteriorRecommendationEngine(catalog_file)

    print("=" * 65)
    print("  AI INTERIOR FURNITURE RECOMMENDATION ENGINE (TEST RUN)")
    print("=" * 65)

    print("\n--- Test Scenario: Space 120cm x 70cm | Category: Desk | Style: Minimalist ---")
    recommendations = engine.recommend(
        available_width_cm=120.0,
        available_depth_cm=70.0,
        category="desk",
        preferred_style="minimalist",
        preferred_color="wood",
        top_n=3
    )

    for rank, item in enumerate(recommendations, start=1):
        print(f"\nRank #{rank}: {item['name']} (Match: {item['match_score']}%)")
        print(f"  Dimensions : {item['dimensions']['width_cm']}cm (W) x {item['dimensions']['depth_cm']}cm (D) x {item['dimensions']['height_cm']}cm (H)")
        print(f"  Explanation: {item['explanation']}")
        print(f"  3D Asset   : {item['model_filename']}")
