// Tiny static server for the WebR prototype.
// Sets COOP/COEP so WebR can use SharedArrayBuffer (fast path).
const http = require('http');
const fs = require('fs');
const path = require('path');

const root = __dirname;
const PORT = 8091;
const TYPES = {
  '.html': 'text/html', '.js': 'text/javascript', '.mjs': 'text/javascript',
  '.css': 'text/css', '.json': 'application/json', '.map': 'application/json',
  '.wasm': 'application/wasm', '.data': 'application/octet-stream',
  '.so': 'application/octet-stream', '.svg': 'image/svg+xml',
};

http.createServer((req, res) => {
  let rel = decodeURIComponent((req.url || '/').split('?')[0]);
  if (rel === '/') rel = '/index.html';
  const fp = path.normalize(path.join(root, rel));
  if (!fp.startsWith(root) || !fs.existsSync(fp) || fs.statSync(fp).isDirectory()) {
    res.writeHead(404); return res.end('not found');
  }
  res.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
  res.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
  res.setHeader('Cross-Origin-Resource-Policy', 'cross-origin');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Content-Type', TYPES[path.extname(fp)] || 'application/octet-stream');
  fs.createReadStream(fp).pipe(res);
}).listen(PORT, () => console.log(`r-prototype on http://localhost:${PORT}`));
