const SVG_WIDTH = 754;
const SVG_HEIGHT = 400;
const CHART_MARGIN = { top: 10, right: 10, bottom: 86, left: 10 };
const COLORS = {
    unmatched: "#DFDFDF",
    overmatched: "#59ACE8",
    matched: "#82CF76",
    averageLine: "#FF0000",
    text: "#002433",
    background: "#F9FAFB",
    hourText: "rgb(194,194,194)"
};

/**
 * Main function to generate the energy chart SVG
 * @param {Object} params - Chart parameters and data
 * @returns {string} SVG string
 */
function generateEnergySVG({
                               consumptionData,
                               productionData,
                               hourlyCoverage = 50,
                               dailyCoverage = 77,
                               weeklyCoverage = 88,
                               monthlyCoverage = 95,
                               annualCoverage = 100,
                               period = "ÅRET 2024"
                           } = {}) {
    // Process the data
    const processedData = processData(consumptionData, productionData);

    // Calculate chart dimensions
    const chartWidth = SVG_WIDTH - CHART_MARGIN.left - CHART_MARGIN.right;
    const chartHeight = SVG_HEIGHT - CHART_MARGIN.top - CHART_MARGIN.bottom;

    // Generate the SVG elements
    const xAxis = generateXAxis(chartWidth, chartHeight);
    const bars = generateBars(processedData, chartWidth, chartHeight);
    const averageLine = generateAverageLine(processedData, chartWidth, chartHeight);
    const legend = generateLegend(chartWidth);

    // Combine all elements into a single SVG
    const svg = `
<svg version="1.1"
     class="highcharts-root"
     xmlns="http://www.w3.org/2000/svg"
     viewBox="0 0 ${SVG_WIDTH} ${SVG_HEIGHT}"
     preserveAspectRatio="xMidYMid meet"
     style="width: 100%; height: auto; display: block;"
     role="img"
     aria-label="">
       <defs>
    <filter id="drop-shadow">
      <feDropShadow dx="1" dy="1" flood-color="#000000" flood-opacity="0.75" stdDeviation="2.5"></feDropShadow>
    </filter>
    <clipPath id="chart-area">
      <rect x="0" y="0" width="${chartWidth}" height="${chartHeight}" fill="none"></rect>
    </clipPath>
  </defs>

  <!-- Background -->
  <rect fill="${COLORS.background}" class="highcharts-background" x="0" y="0" width="${SVG_WIDTH}" height="${SVG_HEIGHT}" rx="0" ry="0"></rect>
  <rect fill="none" class="highcharts-plot-background" x="${CHART_MARGIN.left}" y="${CHART_MARGIN.top}" width="${chartWidth}" height="${chartHeight}"></rect>

  <!-- Grid lines -->
  <g class="highcharts-grid highcharts-xaxis-grid" data-z-index="1">
    ${generateGridLines(chartWidth, chartHeight)}
  </g>

  <!-- X axis -->
  <g class="highcharts-axis highcharts-xaxis" data-z-index="2">
    <path fill="none" class="highcharts-axis-line" stroke="#ffffff" stroke-width="1" d="M ${CHART_MARGIN.left} ${CHART_MARGIN.top + chartHeight + 0.5} L ${CHART_MARGIN.left + chartWidth} ${CHART_MARGIN.top + chartHeight + 0.5}"></path>
  </g>

  <!-- Series group -->
  <g class="highcharts-series-group" data-z-index="3" transform="translate(${CHART_MARGIN.left},${CHART_MARGIN.top})">
    <!-- Unmatched series -->
    <g class="highcharts-series highcharts-series-0 highcharts-column-series" data-z-index="0.1" opacity="1" clip-path="url(#chart-area)">
      ${bars.unmatched}
    </g>

    <!-- Overmatched series -->
    <g class="highcharts-series highcharts-series-1 highcharts-column-series" data-z-index="0.1" opacity="1" clip-path="url(#chart-area)">
      ${bars.overmatched}
    </g>

    <!-- Matched series -->
    <g class="highcharts-series highcharts-series-2 highcharts-column-series" data-z-index="0.1" opacity="1" clip-path="url(#chart-area)">
      ${bars.matched}
    </g>

    <!-- Average consumption line -->
    <g class="highcharts-series highcharts-series-3 highcharts-line-series" data-z-index="10" opacity="1" clip-path="url(#chart-area)">
      ${averageLine}
    </g>
  </g>

  <!-- X axis labels -->
  <g class="highcharts-axis-labels highcharts-xaxis-labels" data-z-index="7">
    ${xAxis}
  </g>

  <!-- Legend -->
  <g class="highcharts-legend" data-z-index="7" transform="translate(119,355)">
    ${legend}
  </g>
</svg>`;

    return {
        svg,
        metrics: {
            hourly: hourlyCoverage,
            daily: dailyCoverage,
            weekly: weeklyCoverage,
            monthly: monthlyCoverage,
            annual: annualCoverage
        }
    };
}

/**
 * Process consumption and production data into chart data
 * @param {Array} consumptionData - Hourly consumption data
 * @param {Array} productionData - Hourly production data
 * @returns {Array} Processed data for the chart
 */
function processData(consumptionData, productionData) {
    if (!Array.isArray(consumptionData) || !Array.isArray(productionData)) {
        throw new Error("Missing or invalid consumption/production data");
    }

    const hourlyBuckets = Array.from({ length: 24 }, () => ({
        consumptionValues: [],
        productionValues: [],
    }));

    // Group values by hour of day
    for (const d of consumptionData) {
        const hour = new Date(d.timestamp).getUTCHours();
        hourlyBuckets[hour].consumptionValues.push(d.value);
    }

    for (const d of productionData) {
        const hour = new Date(d.timestamp).getUTCHours();
        hourlyBuckets[hour].productionValues.push(d.value);
    }

    // Reduce to hourly averages and compute match breakdowns
    return hourlyBuckets.map((bucket, hour) => {
        const avgConsumption =
            bucket.consumptionValues.length > 0
                ? bucket.consumptionValues.reduce((sum, v) => sum + v, 0) / bucket.consumptionValues.length
                : 0;

        const avgProduction =
            bucket.productionValues.length > 0
                ? bucket.productionValues.reduce((sum, v) => sum + v, 0) / bucket.productionValues.length
                : 0;

        const matched = Math.min(avgConsumption, avgProduction);
        const unmatched = Math.max(0, avgConsumption - avgProduction);
        const overmatched = Math.max(0, avgProduction - avgConsumption);

        return {
            hour,
            consumption: avgConsumption,
            production: avgProduction,
            matched,
            unmatched,
            overmatched,
        };
    });
}


/**
 * Generate sample data for the chart
 * @returns {Array} Sample data
 */
function generateSampleData() {
    // This pattern matches the data in the provided HTML
    return [
        { hour: 0, consumption: 80, matched: 40, unmatched: 40, overmatched: 0 },
        { hour: 1, consumption: 75, matched: 35, unmatched: 40, overmatched: 0 },
        { hour: 2, consumption: 70, matched: 30, unmatched: 40, overmatched: 0 },
        { hour: 3, consumption: 65, matched: 25, unmatched: 40, overmatched: 0 },
        { hour: 4, consumption: 60, matched: 20, unmatched: 40, overmatched: 0 },
        { hour: 5, consumption: 70, matched: 30, unmatched: 40, overmatched: 0 },
        { hour: 6, consumption: 90, matched: 60, unmatched: 30, overmatched: 0 },
        { hour: 7, consumption: 110, matched: 90, unmatched: 20, overmatched: 0 },
        { hour: 8, consumption: 130, matched: 120, unmatched: 10, overmatched: 0 },
        { hour: 9, consumption: 120, matched: 120, unmatched: 0, overmatched: 30 },
        { hour: 10, consumption: 110, matched: 110, unmatched: 0, overmatched: 60 },
        { hour: 11, consumption: 100, matched: 100, unmatched: 0, overmatched: 80 },
        { hour: 12, consumption: 110, matched: 110, unmatched: 0, overmatched: 80 },
        { hour: 13, consumption: 115, matched: 115, unmatched: 0, overmatched: 65 },
        { hour: 14, consumption: 110, matched: 110, unmatched: 0, overmatched: 50 },
        { hour: 15, consumption: 105, matched: 105, unmatched: 0, overmatched: 35 },
        { hour: 16, consumption: 115, matched: 115, unmatched: 0, overmatched: 5 },
        { hour: 17, consumption: 130, matched: 100, unmatched: 30, overmatched: 0 },
        { hour: 18, consumption: 140, matched: 80, unmatched: 60, overmatched: 0 },
        { hour: 19, consumption: 130, matched: 60, unmatched: 70, overmatched: 0 },
        { hour: 20, consumption: 120, matched: 50, unmatched: 70, overmatched: 0 },
        { hour: 21, consumption: 100, matched: 40, unmatched: 60, overmatched: 0 },
        { hour: 22, consumption: 90, matched: 30, unmatched: 60, overmatched: 0 },
        { hour: 23, consumption: 85, matched: 20, unmatched: 65, overmatched: 0 }
    ];
}

/**
 * Generate grid lines for the chart
 * @param {number} width - Chart width
 * @param {number} height - Chart height
 * @returns {string} SVG elements for grid lines
 */
function generateGridLines(width, height) {
    const hourWidth = width / 24;
    let gridLines = '';

    for (let hour = 0; hour <= 24; hour++) {
        const x = CHART_MARGIN.left + hour * hourWidth;
        gridLines += `<path fill="none" stroke="#e6e6e6" stroke-width="0" data-z-index="1" class="highcharts-grid-line" d="M ${x}.5 ${CHART_MARGIN.top} L ${x}.5 ${CHART_MARGIN.top + height}" opacity="1"></path>`;
    }

    return gridLines;
}

/**
 * Generate X axis labels
 * @param {number} width - Chart width
 * @param {number} height - Chart height
 * @returns {string} SVG elements for X axis labels
 */
function generateXAxis(width, height) {
    const hourWidth = width / 24;
    let xAxis = '';

    for (let hour = 0; hour < 24; hour++) {
        const x = CHART_MARGIN.left + hour * hourWidth + hourWidth / 2;
        const hourLabel = hour.toString().padStart(2, '0');
        xAxis += `<text x="${x}" style="cursor: default; font-size: 12px; fill: rgb(194,194,194);" text-anchor="middle" transform="translate(0,0)" y="${CHART_MARGIN.top + height + 27}" opacity="1">${hourLabel}</text>`;
    }

    return xAxis;
}

/**
 * Generate bars for the chart
 * @param {Array} data - Chart data
 * @param {number} width - Chart width
 * @param {number} height - Chart height
 * @returns {Object} SVG elements for bars
 */
function generateBars(data, width, height) {
    const hourWidth = width / 24;
    const barWidth = hourWidth * 0.8;
    const barPadding = hourWidth * 0.1;

    // Find the maximum total value (used for scaling)
    const maxValue = data.reduce((max, d) => {
        const total = d.matched + d.unmatched + d.overmatched;
        return Math.max(max, total, d.consumption);
    }, 0);

    let unmatchedBars = '';
    let overmatchedBars = '';
    let matchedBars = '';

    data.forEach((d, i) => {
        const x = i * hourWidth + barPadding;

        // Bar heights (scaled)
        const matchedHeight = height * (d.matched / maxValue);
        const unmatchedHeight = height * (d.unmatched / maxValue);
        const overmatchedHeight = height * (d.overmatched / maxValue);

        // Bar Y-positions (from bottom up)
        const yMatched = height - matchedHeight;
        const yUnmatched = yMatched - unmatchedHeight;
        const yOvermatched = yUnmatched - overmatchedHeight;

        // Draw matched (bottom layer, green)
        if (d.matched > 0) {
            matchedBars += `<rect x="${x}" y="${yMatched}" width="${barWidth}" height="${matchedHeight}" fill="${COLORS.matched}" />`;
        }

        // Draw unmatched (middle layer, gray)
        if (d.unmatched > 0) {
            unmatchedBars += `<rect x="${x}" y="${yUnmatched}" width="${barWidth}" height="${unmatchedHeight}" fill="${COLORS.unmatched}" />`;
        }

        // Draw overmatched (top layer, blue)
        if (d.overmatched > 0) {
            overmatchedBars += `<rect x="${x}" y="${yOvermatched}" width="${barWidth}" height="${overmatchedHeight}" fill="${COLORS.overmatched}" />`;
        }
    });

    return {
        matched: matchedBars,
        unmatched: unmatchedBars,
        overmatched: overmatchedBars
    };
}

/**
 * Generate average consumption line
 * @param {Array} data - Chart data
 * @param {number} width - Chart width
 * @param {number} height - Chart height
 * @returns {string} SVG elements for the line
 */
function generateAverageLine(data, width, height) {
    const hourWidth = width / 24;

    // Find the maximum value for scaling
    const maxValue = data.reduce((max, d) => {
        const total = d.matched + d.unmatched + d.overmatched;
        return Math.max(max, total, d.consumption);
    }, 0);

    // Scale function to convert values to y-coordinates
    const scaleY = value => height * (1 - value / maxValue);

    // Generate points for the line
    const points = data.map((d, i) => {
        const x = i * hourWidth + hourWidth / 2;
        const y = scaleY(d.consumption);
        return `${x},${y}`;
    }).join(' ');

    return `<path fill="none" d="M ${points}" class="highcharts-graph" data-z-index="1" stroke="${COLORS.averageLine}" stroke-width="2" stroke-linejoin="round" stroke-linecap="round"></path>`;
}

/**
 * Generate legend for the chart
 * @param {number} width - Chart width
 * @returns {string} SVG elements for the legend
 */
function generateLegend(width) {
    const legendItems = [
        { color: COLORS.unmatched, label: "Ikke matchet" },
        { color: COLORS.overmatched, label: "Overmatchet" },
        { color: COLORS.matched, label: "Matchet" },
        { color: COLORS.averageLine, label: "Yearly Average Consumption", isLine: true }
    ];

    let legend = '<rect fill="none" class="highcharts-legend-box" rx="0" ry="0" stroke="#999999" stroke-width="0" x="0" y="0" width="517" height="30"></rect><g data-z-index="1"><g>';

    let xOffset = 8;

    legendItems.forEach((item, i) => {
        if (item.isLine) {
            legend += `<g class="highcharts-legend-item highcharts-line-series" data-z-index="1" transform="translate(${xOffset},3)">
        <path fill="none" class="highcharts-graph" stroke-width="2" stroke-linecap="round" d="M 1 13 L 15 13" stroke="${item.color}"></path>
        <text x="21" y="17" text-anchor="start" data-z-index="2" style="font-size: 12px; fill: rgb(51, 51, 51);">${item.label}</text>
      </g>`;
        } else {
            legend += `<g class="highcharts-legend-item highcharts-column-series" data-z-index="1" transform="translate(${xOffset},3)">
        <text x="21" y="17" text-anchor="start" data-z-index="2" style="font-size: 12px; fill: rgb(51, 51, 51);">${item.label}</text>
        <rect x="2" y="6" rx="6" ry="6" width="12" height="12" fill="${item.color}" class="highcharts-point" data-z-index="3"></rect>
      </g>`;
        }

        // Adjust offset based on text length
        xOffset += item.label.length * 7 + 35;
    });

    legend += '</g></g>';

    return legend;
}

// Sample data for testing
const sampleConsumptionData = [
    { timestamp: "2024-01-01T00:00:00Z", value: 80 },
    { timestamp: "2024-01-01T01:00:00Z", value: 75 },
    { timestamp: "2024-01-01T02:00:00Z", value: 70 },
    { timestamp: "2024-01-01T03:00:00Z", value: 65 },
    { timestamp: "2024-01-01T04:00:00Z", value: 60 },
    { timestamp: "2024-01-01T05:00:00Z", value: 70 },
    { timestamp: "2024-01-01T06:00:00Z", value: 90 },
    { timestamp: "2024-01-01T07:00:00Z", value: 110 },
    { timestamp: "2024-01-01T08:00:00Z", value: 130 },
    { timestamp: "2024-01-01T09:00:00Z", value: 120 },
    { timestamp: "2024-01-01T10:00:00Z", value: 110 },
    { timestamp: "2024-01-01T11:00:00Z", value: 100 },
    { timestamp: "2024-01-01T12:00:00Z", value: 110 },
    { timestamp: "2024-01-01T13:00:00Z", value: 115 },
    { timestamp: "2024-01-01T14:00:00Z", value: 110 },
    { timestamp: "2024-01-01T15:00:00Z", value: 105 },
    { timestamp: "2024-01-01T16:00:00Z", value: 115 },
    { timestamp: "2024-01-01T17:00:00Z", value: 130 },
    { timestamp: "2024-01-01T18:00:00Z", value: 140 },
    { timestamp: "2024-01-01T19:00:00Z", value: 130 },
    { timestamp: "2024-01-01T20:00:00Z", value: 120 },
    { timestamp: "2024-01-01T21:00:00Z", value: 100 },
    { timestamp: "2024-01-01T22:00:00Z", value: 90 },
    { timestamp: "2024-01-01T23:00:00Z", value: 85 }
];

const sampleProductionData = [
    { timestamp: "2024-01-01T00:00:00Z", value: 40 },
    { timestamp: "2024-01-01T01:00:00Z", value: 35 },
    { timestamp: "2024-01-01T02:00:00Z", value: 30 },
    { timestamp: "2024-01-01T03:00:00Z", value: 25 },
    { timestamp: "2024-01-01T04:00:00Z", value: 20 },
    { timestamp: "2024-01-01T05:00:00Z", value: 30 },
    { timestamp: "2024-01-01T06:00:00Z", value: 60 },
    { timestamp: "2024-01-01T07:00:00Z", value: 90 },
    { timestamp: "2024-01-01T08:00:00Z", value: 120 },
    { timestamp: "2024-01-01T09:00:00Z", value: 150 },
    { timestamp: "2024-01-01T10:00:00Z", value: 170 },
    { timestamp: "2024-01-01T11:00:00Z", value: 180 },
    { timestamp: "2024-01-01T12:00:00Z", value: 190 },
    { timestamp: "2024-01-01T13:00:00Z", value: 180 },
    { timestamp: "2024-01-01T14:00:00Z", value: 160 },
    { timestamp: "2024-01-01T15:00:00Z", value: 140 },
    { timestamp: "2024-01-01T16:00:00Z", value: 120 },
    { timestamp: "2024-01-01T17:00:00Z", value: 100 },
    { timestamp: "2024-01-01T18:00:00Z", value: 80 },
    { timestamp: "2024-01-01T19:00:00Z", value: 60 },
    { timestamp: "2024-01-01T20:00:00Z", value: 50 },
    { timestamp: "2024-01-01T21:00:00Z", value: 40 },
    { timestamp: "2024-01-01T22:00:00Z", value: 30 },
    { timestamp: "2024-01-01T23:00:00Z", value: 20 }
];

// Generate the SVG with sample data
const result = generateEnergySVG({
    consumptionData: sampleConsumptionData,
    productionData: sampleProductionData,
    hourlyCoverage: 50,
    dailyCoverage: 77,
    weeklyCoverage: 88,
    monthlyCoverage: 95,
    annualCoverage: 100,
    period: "ÅRET 2024"
});

// Output the SVG
console.log("SVG Output:");
console.log(result.svg);

// Export the functions
module.exports = {
    generateEnergySVG
};
