namespace API.ReportGenerator.Rendering;

public interface IExplainerPagesRenderer
{
    string Render();
}

public class ExplainerPagesRenderer : IExplainerPagesRenderer
{
    public string Render()
    {
        var html = $$"""
                     <div class="explainer-pages">
                         <!-- Page 2 -->
                         <div class="page">
                             <div class="explainer-section">
                                 <h1>Rapport om Granulære Oprindelsesgarantier.</h1>
                                 <p class="intro-text">I det følgende kan du læse om, hvordan du kan aflæse og bruge Rapporten om Granulære Oprindelsesgarantier.</p>
                                 <p>Granulære oprindelsesgarantier (forkortet "GGO") er, som navnet antyder, en oprindelsesgaranti i en højere granulering. Dermed kan rapporten give virksomheder et overblik over, hvordan deres strømforbrug matcher med grøn energiproduktion tidsmæssigt og geografisk i en højere opløsning end hidtil muligt. Rapporten viser med høj detaljeringsgrad, hvor og hvornår strøm er produceret og matchet, og om den stammer fra VE projekter med reel klimaeffekt.</p>

                             </div>

                             <div class="explainer-section">
                                 <h2>Rapporten kan hjælpe virksomheder i bæredygtighedsindsatsen</h2>
                                 <p>Rapporten er skabt til dokumentation og som et strategisk redskab til virksomheder, der ønsker at grave et spadestik dybere i oprindelsen af deres strømforbrug. Ved aktivt brug er rapporten et muligt værktøj til at styrke bæredygtighedsindsatserne.</p>
                                 <p>Konkret kan rapporten hjælpe virksomheder med at:</p>
                                 <ul>
                                     <li>træffe endnu mere bevidste og ambitiøse valg omkring strøm</li>
                                     <li>dokumentere effekten af PPA-aftaler</li>
                                     <li>styrke ESG-rapportering med gennemsigtige og valide data</li>
                                     <li>sikre, at strømvalg bidrager til lokal og additionel udbygning af vedvarende energi</li>
                                     <li>øge fleksibelt forbrug eller planlægge batterilager/anden lager teknologi</li>
                                 </ul>
                                 <p>Rapporten skal gøre det lettere at navigere i et komplekst energimarked og understøtter valg, der både gør en forskel for klimaet og for virksomheders grønne profil.</p>
                             </div>

                             <div class="explainer-section">
                                 <h3>Hvad viser rapporten?</h3>
                                 <table>
                                     <tbody>
                                     <tr>
                                         <td><strong>Tidsspecifikt match</strong></td>
                                         <td>Hvordan virksomheden er dækket af sol – og vind energi på time-, dags-, uge-, måneds- og årsniveau.</td>
                                     </tr>
                                     <tr>
                                         <td><strong>Graf</strong></td>
                                         <td>Viser det gennemsnitlige match på timeniveau, ud fra den valgte periode.</td>
                                     </tr>
                                     <tr>
                                         <td><strong>Geografisk oprindelse</strong></td>
                                         <td>Hvilken kommune strømmen er produceret i.</td>
                                     </tr>
                                     <tr>
                                         <td><strong>Teknologi</strong></td>
                                         <td>Om strømmen stammer fra vind eller sol.</td>
                                     </tr>
                                     <tr>
                                         <td><strong>Statsstøtte</strong></td>
                                         <td>Om den grønne strøm, der er købt certifikater til, kommer fra parker med eller uden statsstøtte.</td>
                                     </tr>
                                     </tbody>
                                 </table>
                                 <p>Med ovenstående kan dannes et detaljeret billede af det grønne strømforbrug – og et udgangspunkt for at tage mere oplyste valg fremadrettet.</p>
                             </div>
                         </div>

                         <!-- Page 3 -->
                         <div class="page">

                             <div class="explainer-section">
                                 <h2>Hvordan kan rapporten bruges aktivt i bæredygtighedsarbejde?</h2>
                                 <p>Rapporten er ikke kun et regnskab – det er et strategisk redskab. Her er tre konkrete måder på, hvordan virksomheder kan bruge den.</p>
                             </div>
                             <div class="explainer-section-small">
                                 <h3>Styrk jeres grønne profil – med substans</h3>
                                 <p>Vis omverdenen, at I tilvælger et dokumenteret match af elforbrug og -produktion geografisk – hver time. Synliggør, at jeres dedikation til et grønt strømforbrug går et spadestik dybere.</p>
                                 <p>Brug rapportens data i jeres ESG-rapportering, grønne regnskaber og kommunikation.</p>
                             </div>

                             <div class="explainer-section-small">
                                 <h3>Træf præcise klimavalg –</h3>
                                 <p>Oprindelsesgarantier bidrager forskelligt til udbygning af ny grøn energi. Rapporten viser, om jeres strøm kommer fra parker med statsstøtte (ofte gamle anlæg) eller fra nye, markedsfinansierede parker – "additionelle projekter". Det giver jer muligheden for at skifte til certifikater, der reelt flytter markedet.</p>
                                 <p>Vis, at jeres PPA aftale gør en forskel. Med GGO'er kan I vise detaljeret, hvor jeres strøm kommer fra. I kan vise at I har indgået en PPA-aftale for at gøre en lokal forskel og bruger strømmen fra specifikke anlæg, når den produceres.</p>
                             </div>


                             <div class="explainer-section-small">
                                 <h3>Tag højde for jeres placering og lokale behov</h3>
                                 <p>Rapporten viser, hvor strømmen, I matcher med, bliver produceret. Det gør det muligt at vurdere, om jeres grønne strømvalg faktisk støtter op om lokal grøn udbygning – eller blot hentes fra overfyldte områder. I kan bruge denne indsigt til at prioritere køb, der skubber fossil strøm ud af nettet i netop jeres lokalområde.</p>
                             </div>
                         </div>

                         <!-- Page 4 -->
                         <div class="page">
                             <h2>Bag om certifikaterne – forstå forskellen</h2>
                             <div class="explainer-section">
                                 <h3>Lovgrundlag for Granulære Oprindelsesgarantier</h3>
                                 <p>Den 21. maj 2025 trådte en ny lov om oprindelsesgarantier for vedvarende energikilder i kraft*, som fastslår, at Energinet kan indføre øget tidsopløsning på udstedelser af oprindelsesgarantier – også kaldet granulære oprindelsesgarantier.</p>
                                 <p>Den nye lov er bekendtgørelse nr. 388 af 10. april 2025 om oprindelsesgarantier for elektricitet, gas, fjernvarme og fjernkøling fra vedvarende energikilder. Bekendtgørelsen erstatter bekendtgørelse nr. 913 af 22. juni 2023.</p>
                             </div>
                             <div class="explainer-section">
                                 <h3>Hvad er en oprindelsesgaranti – og hvorfor have det?</h3>
                                 <p>En oprindelsesgaranti er et certifikat, der dokumenterer, at en given mængde energi er produceret fra vedvarende energikilder som vind eller sol. Oprindelsesgarantier udstedes af Energinet og kan anvendes af virksomheder til at dokumentere, at deres energiforbrug er dækket af grøn energi.</p>
                                 <p>Formålet med oprindelsesgarantier er at fremme investeringer i vedvarende energi. Når en virksomhed køber en oprindelsesgaranti, sender den et økonomisk signal om efterspørgsel på grøn strøm – og understøtter dermed markedet for vedvarende energiproduktion. Samtidig bruges garantierne i bæredygtighedsrapportering, klimaregnskaber og certificeringsordninger.</p>
                             </div>

                             <div class="explainer-section">
                                 <h3>Klassiske oprindelsesgarantier – et månedligt gennemsnit</h3>
                                 <p>I det nuværende system udstedes klassiske oprindelsesgarantier typisk med en tidsopløsning på månedsniveau. Det betyder, at du som forbruger kan få dokumentation for, at du har købt grøn strøm i en given måned – men ikke nødvendigvis på samme tidspunkt, som du faktisk brugte strømmen.</p>
                                 <p>Der sker altså ikke nødvendigvis et præcist match mellem forbrug og produktion. Strømmen kan være produceret om dagen og brugt om natten – eller produceret i en anden del af Europa end hvor den blev forbrugt. Det gør systemet fleksibelt og billigt, men også mindre præcist i forhold til dokumentation og klimaeffekt.</p>
                             </div>

                             <div class="explainer-section">
                                 <h3>Timebaserede oprindelsesgarantier – øget præcision og transparens</h3>
                                 <p>Timebaserede (granulære) oprindelsesgarantier og matching dokumenterer, at strømmen er produceret i og
                                     forbrugt på samme time. Det giver mulighed for langt mere præcis og transparent dokumentation, hvilket
                                     er vigtigt i takt med øgede krav fra myndigheder, samarbejdspartnere og kunder.</p>
                                 <p>Denne type garantier kan bruges til at dokumentere reel grøn drift – f.eks. om en virksomhed kører på
                                     solenergi midt på dagen, eller på vindenergi om natten. Det giver mulighed for at synliggøre en grøn
                                     profil med substans og understøtter den grønne omstilling ved at skabe efterspørgsel på certifikater
                                     fra nye, markedsbaserede VE-anlæg.</p>
                             </div>

                         </div>

                         <!-- Page 5 -->
                         <div class="page">
                             <div class="explainer-section">
                                 <h3>Markedet for oprindelsesgarantier i dag</h3>
                                 <p>Det nuværende marked er præget af et stort udbud af certifikater (de klassiske oprindelsesgarantier) fra
                                     strøm, der produceres på tværs af Europa. De kan bruges inden for 12-18 mdr. og det fremgår ikke, om der
                                     har været et match imellem forbrug og produktion. I dette marked er udbuddet større end efterspørgslen
                                     og tilgængeligheden af det certificerede produkt (grøn strøm) i forhold til tid og geografi er ikke
                                     påkrævet – man kan bruge solcellecertifikater fra sommeren til forbrug i en november nat.</p>
                                 <p>Det betyder i praksis at priserne bliver meget lave og kan have indflydelse på ny udbygning GGO'er
                                     tilføjer noget nyt, og er en mulighed for at dokumentere præcist match mellem forbrug og grøn produktion
                                     – men styrken ligger i at virksomheder træffer valg om at investere i lokalt produceret VE og bruger
                                     strømmen når den produceres.</p>
                             </div>

                             <div class="explainer-section">
                                 <h3>Matchingteknologi i Energy Track & Trace – styr på hver eneste time</h3>
                                 <p>I Energy Track & Trace anvendes avanceret matchingteknologi til at koble forbrug af elektricitet sammen
                                     med produktion fra konkrete VE-anlæg – time for time. Teknologien sikrer, at der kun kan udstedes
                                     certifikater, hvor der faktisk er et match mellem grøn produktion og elforbrug.</p>
                                 <p>Det skaber fuld transparens og høj troværdighed i dokumentationen. Samtidig bliver det muligt at vise,
                                     om det grønne strømvalg reelt støtter op om lokal udbygning, kommer fra anlæg med eller uden statsstøtte
                                     og hvilken teknologi (vind/sol), der er tale om. Det giver et stærkt fundament for ESG-rapportering,
                                     klimamål og strategiske energivalg.</p>
                             </div>
                         </div>
                     </div>
                     """;
        return html.Trim();
    }
}
