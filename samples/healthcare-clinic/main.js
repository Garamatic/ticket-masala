/**
 * TicketMasala Sample Template - Main JavaScript
 * 
 * This file handles:
 * - Loading configuration from config.json
 * - Applying theme colors to CSS variables
 * - Populating dynamic content
 * - Form submission to the TicketMasala API
 */

(function() {
    'use strict';

    let config = null;

    // ============================================
    // Initialization
    // ============================================

    document.addEventListener('DOMContentLoaded', function() {
        loadConfig();
    });

    async function loadConfig() {
        try {
            const response = await fetch('config.json');
            if (!response.ok) {
                throw new Error('Failed to load config.json');
            }
            config = await response.json();
            initializeApp();
        } catch (error) {
            console.error('Error loading configuration:', error);
            showError('Failed to load configuration. Please check config.json exists.');
        }
    }

    function initializeApp() {
        applyTheme();
        populateContent();
        setupForm();
        setupMobileMenu();
        setCurrentYear();
    }

    // ============================================
    // Theme Application
    // ============================================

    function applyTheme() {
        const theme = config.client_config.theme;
        const root = document.documentElement;

        if (theme.primaryColor) {
            root.style.setProperty('--primary', theme.primaryColor);
        }
        if (theme.primaryDark) {
            root.style.setProperty('--primary-dark', theme.primaryDark);
        }
        if (theme.primaryLight) {
            root.style.setProperty('--primary-light', theme.primaryLight);
        }
        if (theme.secondaryColor) {
            root.style.setProperty('--secondary', theme.secondaryColor);
        }
        if (theme.accentColor) {
            root.style.setProperty('--accent', theme.accentColor);
        }
        if (theme.accentLight) {
            root.style.setProperty('--accent-light', theme.accentLight);
        }

        // Apply hero background if specified
        if (theme.heroBackground) {
            const hero = document.querySelector('.hero');
            if (hero) {
                hero.style.background = theme.heroBackground;
            }
        }
    }

    // ============================================
    // Content Population
    // ============================================

    function populateContent() {
        const company = config.client_config.company;
        const contact = config.client_config.contact;

        // Page title and meta
        setTextContent('page-title', `${company.name} | ${company.slogan || ''}`);
        setMeta('meta-description', company.description);

        // Navigation
        setTextContent('nav-company-name', company.name);
        setIconClass('nav-logo-icon', company.logoIcon);

        // Hero section
        setTextContent('hero-title', company.name);
        setTextContent('hero-subtitle', company.slogan);
        setTextContent('hero-description', company.description);

        // Contact information
        if (contact) {
            setTextContent('contact-phone', contact.phone);
            setTextContent('contact-email', contact.email);
            setTextContent('contact-address', contact.address);
        }

        // Footer
        setTextContent('footer-company-name', company.name);
        setIconClass('footer-logo-icon', company.logoIcon);
        setTextContent('footer-description', company.description);
        setTextContent('copyright-company', company.name);

        // Populate dynamic sections
        populateServices();
        populateProjects();
        populateFormOptions();
        populateSocialIcons();
    }

    function populateServices() {
        const servicesGrid = document.getElementById('services-grid');
        const services = config.client_config.services || [];

        if (!servicesGrid || services.length === 0) return;

        servicesGrid.innerHTML = services.map(service => `
            <div class="service-card" data-service-id="${service.id}">
                <div class="service-icon">
                    <i class="${service.icon || 'fas fa-cog'}"></i>
                </div>
                <h3>${escapeHtml(service.title)}</h3>
                <p>${escapeHtml(service.description || '')}</p>
            </div>
        `).join('');
    }

    function populateProjects() {
        const projectsGrid = document.getElementById('projects-grid');
        const projects = config.client_config.projects || [];

        if (!projectsGrid || projects.length === 0) return;

        projectsGrid.innerHTML = projects.map(project => `
            <div class="project-card">
                <div class="project-image">
                    ${project.image 
                        ? `<img src="${escapeHtml(project.image)}" alt="${escapeHtml(project.title)}" onerror="this.parentElement.innerHTML='<i class=\\'fas fa-image project-placeholder\\'></i>'">`
                        : '<i class="fas fa-image project-placeholder"></i>'
                    }
                </div>
                <div class="project-info">
                    <h3>${escapeHtml(project.title)}</h3>
                    <p>${escapeHtml(project.description || '')}</p>
                    ${project.location ? `
                        <span class="project-meta">
                            <i class="fas fa-map-marker-alt"></i>
                            ${escapeHtml(project.location)}
                        </span>
                    ` : ''}
                </div>
            </div>
        `).join('');
    }

    function populateFormOptions() {
        const requestType = document.getElementById('requestType');
        const formConfig = config.client_config.form || {};
        const projectTypes = formConfig.projectTypes || [];

        if (!requestType || projectTypes.length === 0) return;

        // Keep the default option
        const defaultOption = requestType.querySelector('option[value=""]');
        requestType.innerHTML = '';
        if (defaultOption) {
            requestType.appendChild(defaultOption);
        }

        projectTypes.forEach(type => {
            const option = document.createElement('option');
            option.value = type.value;
            option.textContent = type.label;
            requestType.appendChild(option);
        });

        // Update form title and button if configured
        if (formConfig.title) {
            setTextContent('form-title', formConfig.title);
        }
        if (formConfig.submitButtonText) {
            const submitBtn = document.getElementById('submit-btn');
            if (submitBtn) {
                const span = submitBtn.querySelector('span');
                if (span) span.textContent = formConfig.submitButtonText;
            }
        }
    }

    function populateSocialIcons() {
        const socialContainer = document.getElementById('social-icons');
        const social = config.client_config.contact?.social || {};

        if (!socialContainer) return;

        const iconMap = {
            facebook: 'fab fa-facebook',
            instagram: 'fab fa-instagram',
            twitter: 'fab fa-twitter',
            linkedin: 'fab fa-linkedin',
            pinterest: 'fab fa-pinterest',
            youtube: 'fab fa-youtube',
            github: 'fab fa-github'
        };

        const icons = Object.entries(social)
            .filter(([_, url]) => url && url !== '#')
            .map(([platform, url]) => `
                <a href="${escapeHtml(url)}" target="_blank" rel="noopener noreferrer" aria-label="${platform}">
                    <i class="${iconMap[platform] || 'fas fa-link'}"></i>
                </a>
            `);

        if (icons.length > 0) {
            socialContainer.innerHTML = icons.join('');
        }
    }

    // ============================================
    // Form Handling
    // ============================================

    function setupForm() {
        const form = document.getElementById('requestForm');
        if (!form) return;

        form.addEventListener('submit', handleFormSubmit);
    }

    async function handleFormSubmit(event) {
        event.preventDefault();

        const form = event.target;
        const submitBtn = form.querySelector('.btn-submit');
        const messageEl = document.getElementById('formMessage');

        // Validate form
        if (!validateForm(form)) {
            return;
        }

        // Disable submit button
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Submitting...';

        // Collect form data
        const formData = {
            customerName: form.name.value.trim(),
            customerEmail: form.email.value.trim(),
            customerPhone: form.phone.value.trim(),
            requestType: form.requestType.value,
            description: form.description.value.trim(),
            sourceSite: config.masala_config.sourceSite,
            domain: config.gerda_config?.domain || 'General'
        };

        try {
            const response = await submitToMasala(formData);
            
            if (response.ok) {
                showMessage(messageEl, 'success', 'Thank you! Your request has been submitted successfully. We\'ll be in touch soon.');
                form.reset();
            } else {
                const error = await response.json().catch(() => ({}));
                throw new Error(error.message || 'Failed to submit request');
            }
        } catch (error) {
            console.error('Submission error:', error);
            showMessage(messageEl, 'error', `Submission failed: ${error.message}. Please try again or contact us directly.`);
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerHTML = '<i class="fas fa-paper-plane"></i> <span>Submit Request</span>';
        }
    }

    async function submitToMasala(data) {
        const masalaConfig = config.masala_config;
        const endpoint = masalaConfig.endpoint;
        
        const headers = {
            'Content-Type': 'application/json'
        };

        // Add authentication if configured
        if (masalaConfig.auth) {
            if (masalaConfig.auth.method === 'Bearer') {
                headers['Authorization'] = `Bearer ${masalaConfig.auth.apiKey}`;
            } else if (masalaConfig.auth.method === 'ApiKey') {
                headers['X-API-Key'] = masalaConfig.auth.apiKey;
            }
        }

        // Implement retry logic
        const maxRetries = masalaConfig.retryPolicy?.maxRetries || 3;
        const delayMs = masalaConfig.retryPolicy?.delayMs || 1000;

        let lastError;
        for (let attempt = 0; attempt < maxRetries; attempt++) {
            try {
                const response = await fetch(endpoint, {
                    method: 'POST',
                    headers: headers,
                    body: JSON.stringify({
                        title: `${data.requestType} - ${data.customerName}`,
                        description: data.description,
                        customerName: data.customerName,
                        customerEmail: data.customerEmail,
                        customerPhone: data.customerPhone,
                        source: data.sourceSite,
                        metadata: {
                            requestType: data.requestType,
                            domain: data.domain
                        }
                    })
                });

                return response;
            } catch (error) {
                lastError = error;
                if (attempt < maxRetries - 1) {
                    await sleep(delayMs * (attempt + 1));
                }
            }
        }

        throw lastError;
    }

    function validateForm(form) {
        const rules = config.gerda_config?.validationRules || {};
        let isValid = true;

        // Validate email
        if (rules.customerEmail?.pattern) {
            const emailRegex = new RegExp(rules.customerEmail.pattern);
            if (!emailRegex.test(form.email.value)) {
                form.email.setCustomValidity('Please enter a valid email address');
                isValid = false;
            } else {
                form.email.setCustomValidity('');
            }
        }

        // Validate description
        if (rules.description?.minLength) {
            if (form.description.value.trim().length < rules.description.minLength) {
                form.description.setCustomValidity(`Description must be at least ${rules.description.minLength} characters`);
                isValid = false;
            } else {
                form.description.setCustomValidity('');
            }
        }

        form.reportValidity();
        return isValid;
    }

    // ============================================
    // UI Helpers
    // ============================================

    function setupMobileMenu() {
        const toggle = document.querySelector('.mobile-menu-toggle');
        const navLinks = document.querySelector('.nav-links');

        if (toggle && navLinks) {
            toggle.addEventListener('click', function() {
                navLinks.classList.toggle('active');
                const icon = toggle.querySelector('i');
                icon.classList.toggle('fa-bars');
                icon.classList.toggle('fa-times');
            });
        }
    }

    function setCurrentYear() {
        const yearEl = document.getElementById('current-year');
        if (yearEl) {
            yearEl.textContent = new Date().getFullYear();
        }
    }

    function setTextContent(id, text) {
        const el = document.getElementById(id);
        if (el && text) {
            el.textContent = text;
        }
    }

    function setIconClass(id, iconClass) {
        const el = document.getElementById(id);
        if (el && iconClass) {
            el.className = `fas ${iconClass}`;
        }
    }

    function setMeta(id, content) {
        const el = document.getElementById(id);
        if (el && content) {
            el.setAttribute('content', content);
        }
    }

    function showMessage(el, type, message) {
        if (!el) return;
        el.className = `form-message ${type}`;
        el.textContent = message;
        el.style.display = 'block';

        // Auto-hide after 10 seconds for success
        if (type === 'success') {
            setTimeout(() => {
                el.style.display = 'none';
            }, 10000);
        }
    }

    function showError(message) {
        console.error(message);
        const hero = document.querySelector('.hero-content');
        if (hero) {
            hero.innerHTML = `
                <h1>Configuration Error</h1>
                <p>${escapeHtml(message)}</p>
            `;
        }
    }

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

})();
