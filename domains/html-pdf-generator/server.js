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
        browser = await chromium.launch({
            headless: true,
            executablePath: '/usr/bin/chromium-browser'
        });

        const context = await browser.newContext({
            offline: true,
            javaScriptEnabled: false,
            bypassCSP: false,
            acceptDownloads: false,
            ignoreHTTPSErrors: false,
            serviceWorkers: 'block',
            permissions: []
        });

        const page = await context.newPage();
        await page.setContent(html, { waitUntil: 'networkidle' });

        const buffer = await page.pdf({
            format: 'A4',
            printBackground: true,
            margin: { top: '30px', right: '30px', bottom: '30px', left: '30px' }
        });

        res.set({
            'Content-Type': 'application/pdf',
            'Content-Length': buffer.length
        });

        res.end(buffer);
    } catch (error) {
        console.error(error);
        res.status(500).end('An error occurred while generating the PDF');
    } finally {
        if (browser) {
            await browser.close();
        }
    }
});

app.listen(8080, '0.0.0.0', () => console.log('html-pdf-generator started on port 8080'));
