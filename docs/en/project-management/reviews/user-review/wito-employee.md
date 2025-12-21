startpagina 

    Zelfde als customer 

 

dashboard 

    De cards vanboven tonen dat ik al 12 projecten heb en 28 actieve tickets, maar dat komt niet overeen met de werkelijkheid 

    "pending tasks" en "completion" zijn Access Denied 

    Switch naar Nederlands -> de subtext van de action buttons blijft in Engels 

    Recent activity view? 

        Ik zie de activiteiten van andere klanten? Of enkel van acties op mijn tickets? 

        Ik heb en nieuwe user en zie 3 recent activities die niet klikbaar zijn 
        Recent Activity #TM-1234 Completed Updated project documentation for Website Redesign John Doe 2 hours ago #TM-1233 In Progress Implement new authentication system Jane Smith 5 hours ago #TM-1232 Pending Design new landing page mockups Bob Johnson Yesterday

         

 

PROJECTS 

index 

    In de tabel met alle tickets van het project zou ik de ttel zetten van de tickets en niet de IDs (zie ook customers) 

    Status filter werkt niet 

    Project status niet zichtbaar of aanpasbaar(enkel ticket statussen) 

    Geen mogelijkheid om project te editen of verwijderen 

        Blijkbaar wel mogelijk vanuit een pagina /manager/projects, maar deze is enkel bereikbaar via de admin en dan via team dashboard > back to projects 

            Hier zouden een aantal extra velden moeten bijkomen zoals project manager, desription, project type, comments and customer. Template zou ik misschien niet laten editen omdat al die autom gegenereerde tickets dan ook aangepast moeten worden 

            Er is een veld om klant te editen, maar dat werkt niet. Ook geen foutmeldingen. 

 

Project > create 

    Project type en Project template values in dropdown lijken niet te matchen. Ene gaat over renovaties en de andere over types webprojecten ofzo. 

    Additional stakeholders 

        Bevat namen van klanten, lijkt me niet correct 

    Customer sectie:  

        Maakt sense voor admins, maar niet voor klanten. Voor klanten zou dit verborgen moeteh zijn en autom de ingelogde klant zijn id doorsturen naar de backend 

        De label van de dropdown moet anagepast worden  

    Geen mogelijkheid om projectmanager in te vullen waardoor bij project details dit op not assignedblijft staan 

 

Project > create from ticket 

    Source ticket: customer is "unknown", however it was created by a customer so it should be known 

    There is a red box in the "New project details" and it's unclear why 

    You can make a duplicate porjects from the ticket. SO from one ticket you can make 4 projects. I think a ticket should only belong to one projects. I'm also not sure which use case would require a stand alone ticket (no project)? So maybe we can do a check: if the ticket already belongs to a project, the button "create project" at the bottom of the ticket detail page is not visible. 

     

project > details 

    "Completion target" needs to be cleaned up 

    "Token " table should be replaced by the tickets index table like in tickts-inde 

 

10/12/2025 

TICKETS 

    Saved filters -> can't remove a saved filter 

    Index table 

        Big gap in table between titel and acties. Could be nice to include "toegewezen aan" and "klant" and maybe "status" and maybe also "project" 

    Bulk actions:  

        Both actions are not working.  

    Would be nice to have a visual indication or filter or some way to identify tickets that are not associated with a project. 

 

Ticket > create 

    Vreemd dat je tickets kan aanmaken die niet gelinkt zijn aan een project? 

    "customer" dropdown bevat ook namen van employees denk ik? John administrator staat ertussen 

    Nieuwe ticket wordt niet opgeslagen en is niet zichtbaar in index pagina. Er is ook geen foutmelding die zegt waarom 

    Als je een project creert, moet je een client aanduiden. Stel dat we heir Emily aanduiden. 
    Daarna maken we een ticket, hier kunnen we zeggen dat dit behoort tot het nieuwe project dat we hebben aangemaakt.  

        Maar ook bij ticket moet je een klant aanduiden. Zou dit niet autom. De klant van het project moeten zijn?  

        Wanneer we gaan kijken naar de details van het ticket, is de Customer set op "unknown" terwijl dit wel werd ingegeven 

     

 

ticket > details 

    Als je eerst e knop edit gebruikt, dan canceled en dan de knop back gebruikt, kom je opnieuw op de edit page ipv de index 

    Request review button  

        --> 404 

        Also not sue what it is supposed to do? 

    Add a comment  

        404 

        Internal note toggle is also visible for the customer 

        Attachments 

            What is the public toggle? Remove? 

 

ticket > edit 

    Geen mogelijkheid om de klant te evranderen (staat wel bij create). 

    Geen mogelijkheid om aan een project toe te wijzen dat al bestaat 

 

 

KLANTEN 

Index 

    Zou goed zijn om ook de rol te zien van de users + een filter hiervoor (hier lijken het enjkel customers te zijn? Niet zeker wat de rol is van John Administrator) 

 

Details 

    Als je een project aanmaakt via de knop op deze pagina wordt het project niet automatisch gelinkt aan deze persoon. Ik heb vanop de pagina van John adminsitrator (ingelogd als mike.pm) een project aangemaakt voor een nieuwe klant (Piet huysentruyt). 

        Klant wordt aangemaakt en komt in overzicht --> check 

        Project wordt gelinkt aan de klant (Piet) --> check 

        Project wordt niet gelinkt aan de persoon van waaruit we het nieuwe project wouden aanmaken 

        Ik zou denk ik gewoon die knop van nbew project weghalen van de detail pagina van een customer. 