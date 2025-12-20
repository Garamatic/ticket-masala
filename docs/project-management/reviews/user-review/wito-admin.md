startpagina 

    Zelfde als customer en employee 

 

dashboard 

    De cards vanboven tonen dat ik al 12 projecten heb en 28 actieve tickets, maar dat komt niet overeen met de werkelijkheid 

    Switch naar Nederlands -> de subtext van de action buttons blijft in Engels 

    Recent activity view? 

        Lijkt niet functioneel? 
        Recent Activity #TM-1234 Completed Updated project documentation for Website Redesign John Doe 2 hours ago #TM-1233 In Progress Implement new authentication system Jane Smith 5 hours ago #TM-1232 Pending Design new landing page mockups Bob Johnson Yesterday

         

 

PROJECTS 

index 

    In de tabel met alle tickets van het project zou ik de ttel zetten van de tickets en niet de IDs (zie ook customers) 

    Status filter werkt niet 

    Project status niet zichtbaar of aanpasbaar(enkel ticket statussen) 

    Geen mogelijkheid om project te editen of verwijderen 

        Blijkbaar wel mogelijk om te editen vanuit een pagina /manager/projects, maar deze is enkel bereikbaar via de admin en dan via team dashboard > back to projects 

            Hier zouden een aantal extra velden moeten bijkomen zoals project manager, desription, project type, comments and customer. Template zou ik misschien niet laten editen omdat al die autom gegenereerde tickets dan ook aangepast moeten worden 

            Er is een veld om klant te editen, maar dat werkt niet. Ook geen foutmeldingen. 

 

Project > create 

    Project type en Project template values in dropdown lijken niet te matchen. Ene gaat over renovaties en de andere over types webprojecten ofzo. 

    Additional stakeholders 

        Bevat namen van klanten, lijkt me niet correct 

    Geen mogelijkheid om projectmanager in te vullen waardoor bij project details dit op not assignedblijft staan. Dit is wel mogelijk wanneer je een project aanmaakt vi een bestaand ticket. Misschien hier ook die GERDA recommendation implementeren? 

 

Project > create from ticket 

    Source ticket: customer is "unknown", however it was created by a customer so it should be known 

    There is a red box in the "New project details" and it's unclear why 

    You can make a duplicate porjects from the ticket. SO from one ticket you can make 4 projects. I think a ticket should only belong to one projects. I'm also not sure which use case would require a stand alone ticket (no project)? So maybe we can do a check: if the ticket already belongs to a project, the button "create project" at the bottom of the ticket detail page is not visible. 

    The GERDA suggestion includes all employees, not only PMs  

     

project > details 

    "Completion target" needs to be cleaned up 

    "Token " table should be replaced by the tickets index table like in tickts-inde 

 

TICKETS 

index 

    I don't see any tickets (although they exist 

    When you load the page, the client filter is set on "John Adminsitrator" 

 

Ticket > create 

    Detail, maar ik denk dat de tijd die gelogd wordt voor created on niet klopt (1 uur achter) 

 

ticket > details 

    Kan ik niet testen momenteel aangezien ik geen tickets zie, maar ik veronderstel dezelfde comments als bij Employees 

 

ticket > edit 

    Kan ik niet testen momenteel aangezien ik geen tickets zie, maar ik veronderstel dezelfde comments als bij Employees 

     

 

 

KLANTEN 

Index 

    Zou goed zijn om ook de rol te zien van de users + een filter hiervoor (hier lijken het enjkel customers te zijn? Niet zeker wat de rol is van John Administrator) 

    Ik snap dat PMs enkel klanyten mogen zien/bewerken, maar de admin zou ook de overige Employees moeten kunnen zien + bewerken + nieuwe users aanmaken (wnr er bv eennieuwe employee bijkomt) 

 

Details 

    Als je een project aanmaakt via de knop op deze pagina wordt het project niet automatisch gelinkt aan deze persoon. Ik heb vanop de pagina van John adminsitrator  een project aangemaakt voor een nieuwe klant (Piet huysentruyt). 

        Klant wordt aangemaakt en komt in overzicht 

        Project wordt gelinkt aan de klant (Piet) 

        Project wordt niet gelinkt aan de persoon van waaruit we het nieuwe project wouden aanmaken 

        Ik zou denk ik gewoon die knop van nbew project weghalen van de detail pagina van een customer. 

    Een delete of edit knop zou goed zijn 

 

Edit 

    Pagina bestaat niet 

    Funct: wijzigen van persoonlijke info + rol 

 

Delete 

     Bestaat niet 

 

TEAM DASHBOARD 

    Nice-to-have:  

        optie om in agent workload te klikken op de agend en te zien welke tickets zijn toegewezen aan die agent 

        Optie om op een GERDA tag te klikken en de projecten te zien 

        Optie om op een recent activity te klikken en het ticket te zien --> deze kan mss ook gebruikt worden voor het dashboard? 

    Recent activity 

        Lijkt niet 100% correct te werken. Als ik vanuit GERDA dispatch een manual assign doe van een autom gegenereerd ticket, komt komt de assignment wel door in de recent activity, maar hij blijft staan op de plek die hij stond. Ik vermoed dat bij de assignment de "updated on" datum niet wordt geupdate of iets dergelijks. 

 

GERDA DIPSATCH 

    Agent availability 

        Scroll funct moet nagekeken worden. Nu zie je enkel het einde van de lijst als je echt tot het einde van de pagina scrollt 

    Pending tickets 

        Nice to have: pagination 

        Element met select all en auto assign selected zou misschien logischer zijn onder de "pending tickets" 

        Voor alle tickets staat er een melding dat GERDA niet gebruikt kan worden en het manueel moet worden geassigned. Maar als ik een ticket selecteer en dan autoassign doe, werkt het wel. 

        Als je GERDA gebruikt wordt de "assigned to" autom ingevuld voor het ticket. Maar dan kan heb je geen optie meer om het toe te wijzen aan een project. Terwijl met de manuele je beiden kan instellen. 