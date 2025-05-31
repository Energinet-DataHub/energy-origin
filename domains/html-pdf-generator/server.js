const express = require('express');
const { chromium } = require('playwright-core');
const app = express();

app.use(express.json({limit: '300mb'}));

app.get('/health', (_, res) => {
    res.sendStatus(200);
});

app.post('/generate-pdf', async (req, res) => {
    const { html } = req.body;
    let browser;

    if (!html) {
        return res.status(400).end('HTML content is required');
    }

    try {
        await Promise.race([
            (async () => {
                browser = await chromium.launch({ headless: true });
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
                    footerTemplate: ``
                });

                res.set({
                    'Content-Type': 'application/pdf',
                    'Content-Length': buffer.length
                });

                res.end(buffer);
            })(),
            new Promise((_, reject) => setTimeout(() => reject(new Error('timeout')), 30000))
        ]);
    } catch (error) {
        console.error(error);
        if (error.message === 'timeout') {
            res.status(408).end('The request has timed out');
        } else {
            res.status(500).end('An unexpected error occurred while processing the PDF');
        }
    } finally {
        if (browser) {
            await browser.close();
        }
    }
});

app.listen(8080, () => console.log('Server started'));
