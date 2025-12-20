General: 

    I get asked way to often to login. Almost every 5 min or so --> ik heb gezien dat je dit hebt proberen fixen met cookie timespan te veranderen, maar dit heeft niet gewerkt.  

 

startpagina 

    Taal keuze op start scherm brengt je meteen naar de login pagina 

        EN: sommeige tekst staat nog in het NL: H2, placeholder values in form, actionbutton etc. 

        NL & FR: zelfde scherm als EN 

    Wanneer je klikt op de nachtmodus, blijft de informatietekst staa en die zou moeten verdwijnen. 

    Icoontje voor in de tab 

    De Card met een voorbeeld ticket lijkt klikbaar door de cursor die veranderd 

 

dashboard 

    De cards vanboven tonen dat ik al 12 projecten heb en 28 actieve tickets, maar als ik erop klik zie ik er geen enkele 

    "pending tasks" en "completion" zijn Access Denied 

    Switch naar Nederlands -> de subtext van de action buttons blijft in Engels 

    Recent activity view? 

        Ik zie de activiteiten van andere klanten? Of enkel van acties op mijn tickets? 

        Ik heb en nieuwe user en zie 3 recent activities die niet klikbaar zijn 
        Recent Activity #TM-1234 Completed Updated project documentation for Website Redesign John Doe 2 hours ago #TM-1233 In Progress Implement new authentication system Jane Smith 5 hours ago #TM-1232 Pending Design new landing page mockups Bob Johnson Yesterday

         

 

Projects > index 

    Beetje vreemd dat er geen menu link is voor projects maar wel voor tickets 

     knop om nieuw project te creeren zou zichtbaar moeten zijn (zoals bij tickets) 

        Moet een Customer een nieuw project kunnen aanmaken in het systeem of enkel een algemene aanvraag kunnen doen, waardoor een medewerker een echt project kan aanmaken? -> nieuw Feature: Project Request System. 

    In de tabel met alle tickets van het project zou ik de ttel zetten van de tickets en niet de IDs 

 

Project > create 

    Project type en Project template values in dropdown lijken niet te matchen. Ene gaat over renovaties en de andere over types webprojecten ofzo. 

    Additional stakeholders,  

        als nieuwe user zie ik enkel mijn eigen naam --> mss weglaten voor klanten? 

        Als Emily zie ik een lijst van alle users, incl klanten zoals Carol --> lijkt me dat dit enkle door admin of PM moet worden ingevuld en enkel employees in de lijst? 

    Customer sectie:  

        Maakt sense voor admins, maar niet voor klanten. Voor klanten zou dit verborgen moeteh zijn en autom de ingelogde klant zijn id doorsturen naar de backend 

        De label van de dropdown moet anagepast worden  

     

project > details 

    je kan hier enkel geraken door door te klikken vanuit ticket details --> zou ook moeten kunnen via menu > projects > detail 

    "Completion target" needs to be cleaned up 

    "Token " table should be replaced by the tickets index table like in tickts-index 

 

tickets 

    Filter voor "customer" laten zien? Ik veronderstel dat customers enkel hun eigen tickets mogen zien? 

    Export tickets action geeft error 404 (page not found) 

    Saved filters -> can't remove a saved filter 

    Index table 

        Missing some header titles: "customer" "assigned to" and a thrid one 

    Bulk actions:  

        Both actions are not working. I also don't think a customer shoud do this? 

    Customer Emily:  

        heft geen projecten, maar wel tickets en die tickets behoren niet tot een project. Lijkt me persoonlijk wat vreemd. Ndien dit kan, moeten we de knop vanonder op d epagina ticket details "view project" inactiveren wanneer een ticket niet gelinked is aan een project (ik vermoed dat dat is wat de if else statement zou moeten doen) 

 

Ticket > create 

    Vreemd dat je customer moet selecteren, dit zou best inde backend autom assigned worden aan de customer die ingelogd is. 

    Vreemd dat je tickets kan aanmaken die niet gelinkt zijn aan een project? 

    Assign to drop-down lijkt me iets dat enkel voor PMs en Admin zichtbaar zou moeten zijn? 

    "project" dropdown zou denk ik enkel projecten van de klant mogen bevatten. In het geval van Emily zijn er geen projecten van haar, maar ze kan wel tickets aanmaken voor andere projecten 

    Nieuwe ticket wordt niet opgeslagen en is niet zichtbaar in index pagina. Er is ook geen foutmelding die zegt waarom 

 

 

ticket > details 

    Onderaan de pagina is er een syntax fout in de cshtml dnek ik. Er staat wat code in de tekst. 

    De knop "create project" leidt naar een pagina waar ik geen toegang heb. 

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

    Beetje vremed dat een klant de autom gegenereerde tickets kan aanpassen. Ik zou dan persoonlijkj de target completion date van alle tickets dan op morgen zetten 😀  

    New user: Responsible van d eticket heeft als enige waarden in de drop down de user zelf 

    Emily: kan en responsible selecteren maar dit wordt niet gesaved. Lijkt me ook niet logisch dat Amily dit zou kunnen, dus ik zou dit veld verbergen voor customers 

 