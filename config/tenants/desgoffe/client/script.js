// Desgoffe Portal - Guichet Citoyen
// Form submission and validation logic

import { PortalForm } from '../../../src/shared/portal-form.js';

// Dynamically derive API endpoint from the page or allow an override
const __TENANT = document.documentElement.getAttribute('data-theme') || 'desgoffe';
const __API_BASE = window.__API_BASE__ || `https://ca-ticket-masala.kindgrass-f8932dd8.westeurope.azurecontainerapps.io`;
const __API_ENDPOINT = `${__API_BASE}/api/portal/submit`;

// Configuration for Desgoffe tenant
const config = {
    formId: 'submissionForm',
    apiEndpoint: __API_ENDPOINT,
    tenant: 'desgoffe',
    locale: 'fr',
    minDescriptionLength: 10,
    customFieldId: 'quartier',
    customFieldLabel: 'Quartier',
    messages: {
        charCounter: '{length} / {min} caractères minimum',
        charCounterError: 'Veuillez saisir au moins {min} caractères',
        pdfOnly: 'Seuls les fichiers PDF sont acceptés.',
        chooseFile: 'Choisir un fichier PDF',
        submitError: 'Une erreur est survenue lors de la soumission de votre demande. Veuillez réessayer.'
    }
};

// Initialize form
const portalForm = new PortalForm(config);
portalForm.init();
