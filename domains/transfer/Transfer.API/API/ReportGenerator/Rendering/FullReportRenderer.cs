using API.ReportGenerator.Processing;

namespace API.ReportGenerator.Rendering;

public interface IFullReportRenderer
{
    string Render(string styleHtml, string watermarkHtml, string headerHtml, string headlineHtml, string svgHtml,
        string otherCoverageHtml, string municipalitiesHtml, string logoHtml, string explainerPagesHtml);
}

public class FullReportRenderer : IFullReportRenderer
{
    public string Render(string styleHtml, string watermarkHtml, string headerHtml, string headlineHtml, string svgHtml,
        string otherCoverageHtml, string municipalitiesHtml, string logoHtml, string explainerPagesHtml)
    {
        var html = $$"""
                     <!DOCTYPE html>
                     <html>
                       <head>
                         <meta charset="UTF-8">
                         <meta name="viewport" content="width=device-width, initial-scale=1.0">
                         {{styleHtml}}
                         <title>Granulære Oprindelsesgarantier</title>
                       </head>
                       <body>
                           <div class="front-page">
                             {{watermarkHtml}}
                             <div class="content">
                               {{headerHtml}}
                               <div class="chart">
                                 {{headlineHtml}}
                                 {{svgHtml}}
                                 {{otherCoverageHtml}}
                               </div>
                               <div class="details">
                                 <p class="description">Granulære Oprindelsesgarantier er udelukkende udstedt på basis af sol- og vindproduktion.
                                    Herunder kan du se en fordeling af geografisk oprindelse, teknologityper samt andelen fra statsstøttede
                                    producenter.</p>
                                 <div class="sections">
                                   <div class="section-column">
                                     {{municipalitiesHtml}}
                                   </div>
                                   <div class="section-column">
                                     <h6 class="section-title">Teknologi</h6>
                                     <ul>
                                       <li>Solenergi: 38%</li>
                                       <li>Vindenergi: 62%</li>
                                     </ul>
                                   </div>
                                   <div class="section-column">
                                     <h6 class="section-title">Andel fra statsstøttede producenter</h6>
                                     <ul>
                                       <li>Ikke statsstøttede: 95%</li>
                                       <li>Statsstøttede: 5%</li>
                                     </ul>
                                   </div>
                                 </div>
                               </div>
                             </div>
                             <div class="front-page-logo-margin-top">
                                {{logoHtml}}
                             </div>

                         </div>

                         {{explainerPagesHtml}}
                       </body>
                     </html>
                     """;

        return html.Trim();
    }
}
