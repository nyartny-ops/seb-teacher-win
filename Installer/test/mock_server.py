"""Мини-сервер SEBастьяна для автотеста SEB в CI: /api/v1/gate, /launch, /redeem (ключ TEST42)."""
import json, sys
from http.server import BaseHTTPRequestHandler, HTTPServer

REQUIRED = sys.argv[2] == "1"


class H(BaseHTTPRequestHandler):
    def reply(self, code, obj):
        b = json.dumps(obj).encode()
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(b)))
        self.end_headers()
        self.wfile.write(b)

    def do_GET(self):
        print("GET", self.path, flush=True)
        self.reply(200, {"ok": True, "required": REQUIRED, "bot": "https://t.me/"})

    def do_POST(self):
        n = int(self.headers.get("Content-Length") or 0)
        body = json.loads(self.rfile.read(n) or b"{}")
        print("POST", self.path, body, flush=True)
        if self.path.startswith("/api/v1/redeem"):
            ok = str(body.get("key", "")).upper() == "TEST42"
            return self.reply(200 if ok else 400, {"ok": ok, "message": None if ok else "Такого ключа нет."})
        self.reply(200, {"ok": True})


HTTPServer(("127.0.0.1", int(sys.argv[1])), H).serve_forever()
