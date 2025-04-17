#!/usr/bin/env python3
"""
Ultra-simple HTTP server to test Railway deployment
"""
import http.server
import socketserver
import os

PORT = int(os.environ.get('PORT', 8000))
print(f"Starting server on port {PORT}")

class SimpleHandler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        self.send_response(200)
        self.send_header('Content-type', 'text/html')
        self.end_headers()
        
        response = f"""
        <html>
            <head><title>Railway Test Server</title></head>
            <body>
                <h1>Railway Test Server</h1>
                <p>The server is running.</p>
                <p>Railway environment variables:</p>
                <ul>
                    <li>PORT: {os.environ.get('PORT', 'Not set')}</li>
                    <li>RAILWAY_ENVIRONMENT: {os.environ.get('RAILWAY_ENVIRONMENT', 'Not set')}</li>
                </ul>
            </body>
        </html>
        """
        
        self.wfile.write(response.encode('utf-8'))

print("Creating HTTP server...")
with socketserver.TCPServer(("", PORT), SimpleHandler) as httpd:
    print("Server started at port", PORT)
    httpd.serve_forever() 