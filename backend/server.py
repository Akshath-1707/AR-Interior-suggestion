import json
import os
import sys
from http.server import HTTPServer, BaseHTTPRequestHandler
from recommend import InteriorRecommendationEngine

# Initialize the recommendation engine
CATALOG_PATH = os.path.join(os.path.dirname(__file__), "data", "furniture_catalog.csv")
INDEX_PATH = os.path.join(os.path.dirname(__file__), "index.html")

try:
    engine = InteriorRecommendationEngine(CATALOG_PATH)
    print(f"[SUCCESS] Catalog loaded successfully from: {CATALOG_PATH}")
except Exception as e:
    print(f"[ERROR] Failed to load catalog: {e}")
    sys.exit(1)

class APIRequestHandler(BaseHTTPRequestHandler):
    
    def _send_json_response(self, status_code: int, data: dict):
        """Helper to send JSON HTTP response with CORS headers."""
        self.send_response(status_code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        self.end_headers()
        self.wfile.write(json.dumps(data, indent=2).encode("utf-8"))

    def do_OPTIONS(self):
        """Handle CORS Preflight Requests."""
        self.send_response(200)
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        self.end_headers()

    def do_GET(self):
        """Serve Web UI on GET / or health check info."""
        if self.path == "/" or self.path == "/index.html":
            if os.path.exists(INDEX_PATH):
                self.send_response(200)
                self.send_header("Content-Type", "text/html; charset=utf-8")
                self.end_headers()
                with open(INDEX_PATH, "rb") as f:
                    self.wfile.write(f.read())
            else:
                self._send_json_response(404, {"error": "index.html not found"})
        elif self.path == "/health":
            self._send_json_response(200, {
                "status": "online",
                "service": "AI Interior Recommendation Server",
                "version": "1.0.0"
            })
        else:
            self._send_json_response(404, {"error": "Endpoint not found"})

    def do_POST(self):
        """Handle AI Recommendation Requests."""
        if self.path == "/api/v1/recommend":
            try:
                content_length = int(self.headers.get("Content-Length", 0))
                if content_length == 0:
                    self._send_json_response(400, {"error": "Empty JSON payload"})
                    return

                post_data = self.rfile.read(content_length)
                payload = json.loads(post_data.decode("utf-8"))

                # Extract parameters from JSON payload
                available_width = float(payload.get("available_width_cm", 0))
                available_depth = float(payload.get("available_depth_cm", 0))
                category = payload.get("category", None)
                preferred_style = payload.get("preferred_style", None)
                preferred_color = payload.get("preferred_color", None)
                top_n = int(payload.get("top_n", 3))

                if available_width <= 0 or available_depth <= 0:
                    self._send_json_response(400, {
                        "error": "available_width_cm and available_depth_cm must be greater than 0"
                    })
                    return

                # Get AI recommendations
                recommendations = engine.recommend(
                    available_width_cm=available_width,
                    available_depth_cm=available_depth,
                    category=category,
                    preferred_style=preferred_style,
                    preferred_color=preferred_color,
                    top_n=top_n
                )

                self._send_json_response(200, {
                    "status": "success",
                    "request_space": {
                        "width_cm": available_width,
                        "depth_cm": available_depth
                    },
                    "count": len(recommendations),
                    "recommendations": recommendations
                })

            except json.JSONDecodeError:
                self._send_json_response(400, {"error": "Invalid JSON format"})
            except Exception as e:
                self._send_json_response(500, {"error": str(e)})
        else:
            self._send_json_response(404, {"error": "Endpoint not found"})

def run_server(host="0.0.0.0", port=8000):
    server_address = (host, port)
    httpd = HTTPServer(server_address, APIRequestHandler)
    print("=" * 65)
    print(f"  AI INTERIOR WEB UI & API ONLINE AT: http://localhost:{port}")
    print(f"  API ENDPOINT: http://localhost:{port}/api/v1/recommend")
    print("=" * 65)
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        print("\nShutting down server gracefully...")
        httpd.server_close()

if __name__ == "__main__":
    run_server()
