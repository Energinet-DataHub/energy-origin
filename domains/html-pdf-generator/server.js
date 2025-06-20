const http = require('http');
const { chromium } = require('playwright-core');

const parseRequestBody = async (req) => {
    return new Promise((resolve, reject) => {
        const bodyChunks = [];
        let bodySize = 0;
        const maxBodySize = 300 * 1024 * 1024;

        req.on('data', (chunk) => {
            bodyChunks.push(chunk);
            bodySize += chunk.length;

            if (bodySize > maxBodySize) {
                reject(new Error('Request body too large'));
                req.destroy();
            }
        });

        req.on('end', () => {
            if (bodyChunks.length === 0) {
                resolve({});
                return;
            }

            try {
                const bodyString = Buffer.concat(bodyChunks).toString();
                const body = JSON.parse(bodyString);
                resolve(body);
            } catch (error) {
                reject(new Error('Invalid JSON'));
            }
        });

        req.on('error', (err) => {
            reject(err);
        });
    });
};

const server = http.createServer(async (req, res) => {
    const url = req.url;
    const method = req.method;

    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

    if (method === 'OPTIONS') {
        res.statusCode = 204;
        res.end();
        return;
    }

    if (url === '/health' && method === 'GET') {
        res.statusCode = 200;
        res.end();
        return;
    }

    if (url === '/generate-pdf' && method === 'POST') {
        let browser;

        try {
            const body = await parseRequestBody(req);
            const { html } = body;

            if (!html) {
                res.statusCode = 400;
                res.end('HTML content is required');
                return;
            }

            await Promise.race([
                (async () => {
                    browser = await chromium.launch({
                        headless: true,
                        executablePath: '/usr/bin/chromium-browser',
                        args: [
                            '--no-sandbox',
                            '--disable-gpu',
                            '--disable-setuid-sandbox',
                            '--disable-dev-shm-usage',
                            '--single-process',
                            '--disable-breakpad',
                            '--disable-crash-reporter'
                        ]
                    });
                    const context = await browser.newContext({
                        offline: true,
                        javaScriptEnabled: false,
                        bypassCSP: false,
                        acceptDownloads: false,
                        ignoreHTTPSErrors: false,
                        serviceWorkers: 'block',
                        permissions: [],
                        isMobile: false
                    });
                    const page = await context.newPage();
                    await page.setContent(html, { waitUntil: 'networkidle' });

                    const buffer = await page.pdf({
                        format: 'A4',
                        printBackground: true,
                        margin: { top: '30px', right: '30px', bottom: '30px', left: '30px' },
                        displayHeaderFooter: true,
                        headerTemplate: ``,
                        footerTemplate: `
                            <div style="font-size:10px; text-align:right; width:100%; margin-right: 30px">
                                <span class="pageNumber"></span> / <span class="totalPages"></span>
                            </div>`
                    });

                    res.setHeader('Content-Type', 'application/pdf');
                    res.setHeader('Content-Length', buffer.length);
                    res.end(buffer);
                })(),
                new Promise((_, reject) => setTimeout(() => reject(new Error('timeout')), 30000))
            ]);
        } catch (error) {
            console.error(error);
            if (error.message === 'timeout') {
                res.statusCode = 408;
                res.end('The request has timed out');
            } else if (error.message === 'Request body too large') {
                res.statusCode = 413;
                res.end('Request body too large');
            } else if (error.message === 'Invalid JSON') {
                res.statusCode = 400;
                res.end('Invalid JSON in request body');
            } else {
                res.statusCode = 500;
                res.end('An unexpected error occurred while processing the PDF');
            }
        } finally {
            if (browser) {
                await browser.close();
            }
        }
        return;
    }

    res.statusCode = 404;
    res.end('Not Found');
});

server.listen(8080, () => console.log('Server started on port 8080'));
