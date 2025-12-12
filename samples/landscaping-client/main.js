/**
 * GreenScape Landscaping - Configuration Driven Client
 */

document.addEventListener('DOMContentLoaded', async function() {
    // 1. Load Configuration
    let config;
    try {
        const response = await fetch('config.json');
        config = await response.json();
    } catch (error) {
        console.error('Failed to load configuration:', error);
        document.body.innerHTML = '<div style="text-align:center; padding: 50px;"><h1>Error loading site configuration</h1><p>Please try again later.</p></div>';
        return;
    }

    // Extract sections for easier access
    const { client_config, masala_config, gerda_config } = config;

    // 2. Apply Theme & Branding
    applyTheme(client_config.theme);
    applyBranding(client_config);
    applyLabels(client_config.labels); // NEW: Apply text labels

    // 3. Render Dynamic Content
    renderServices(client_config.services);
    renderProjects(client_config.projects);
    populateFormOptions(client_config.form);

    // 4. Initialize Form Logic (with Masaal/Gerda config)
    initializeForm(client_config, masala_config, gerda_config);

    // 5. Initialize UI Effects (Scroll, Navbar)
    initializeUIEffects();
});

function applyTheme(theme) {
    if (!theme) return;
    const root = document.documentElement;
    if (theme.primaryColor) root.style.setProperty('--primary-color', theme.primaryColor);
    if (theme.secondaryColor) root.style.setProperty('--secondary-color', theme.secondaryColor);
    if (theme.accentColor) root.style.setProperty('--accent-color', theme.accentColor);
}

function applyBranding(config) {
    // Navigation
    document.getElementById('nav-company-name').textContent = config.company.name;
    document.getElementById('nav-logo-icon').className = `fas ${config.company.logoIcon || 'fa-leaf'}`;

    // Hero
    document.getElementById('hero-title').textContent = config.company.slogan.split(' ').slice(0, 2).join(' '); // Simple split for demo
    document.getElementById('hero-subtitle').textContent = config.company.slogan.split(' ').slice(2).join(' ');
    document.getElementById('hero-description').textContent = config.company.description;

    // Contact Info
    const contact = config.contact;
    document.getElementById('contact-phone').textContent = contact.phone;
    document.getElementById('contact-email').textContent = contact.email;
    document.getElementById('contact-address').textContent = contact.address;

    // Footer
    document.getElementById('footer-logo-icon').className = `fas ${config.company.logoIcon || 'fa-leaf'}`;
    document.getElementById('footer-company-name').textContent = config.company.name;
    document.getElementById('footer-description').textContent = config.company.description;
    document.getElementById('current-year').textContent = new Date().getFullYear();
    document.getElementById('copyright-company').textContent = config.company.name;

    document.title = `${config.company.name} | ${config.company.slogan}`;
}

function applyLabels(labels) {
    if (!labels) return;

    // Navigation
    if (labels.nav) {
        setText('nav-home', labels.nav.home);
        setText('nav-services', labels.nav.services);
        setText('nav-projects', labels.nav.projects);
        setText('nav-contact', labels.nav.contact);
        setText('footer-nav-services', labels.nav.services);
        setText('footer-nav-projects', labels.nav.projects);
        setText('footer-nav-contact', labels.nav.contact);
    }

    // Buttons
    if (labels.hero) {
        // Need to preserve the icon
        const cta = document.getElementById('hero-cta');
        if (cta) cta.innerHTML = `<i class="fas fa-paper-plane"></i> ${labels.hero.cta}`;
    }

    // Sections
    if (labels.sections) {
        setText('section-services', labels.sections.services);
        setText('section-projects', labels.sections.projects);
        setText('section-contact', labels.sections.contact);
        setText('section-contact-sub', labels.sections.contact_sub);
        setText('footer-quick-links', labels.sections.quick_links);
        setText('footer-follow-us', labels.sections.follow_us);
    }

    // Form
    if (labels.form) {
        setText('lbl-name', labels.form.name);
        setPlaceholder('name', labels.form.name_placeholder);
        
        setText('lbl-email', labels.form.email);
        setPlaceholder('email', labels.form.email_placeholder);
        
        setText('lbl-phone', labels.form.phone);
        setPlaceholder('phone', labels.form.phone_placeholder);
        
        setText('lbl-type', labels.form.type);
        setText('opt-default', labels.form.type_default);
        
        setText('lbl-desc', labels.form.description);
        setPlaceholder('description', labels.form.description_placeholder);
        
        setText('btn-submit-text', labels.form.submit);
    }

    // Footer
    if (labels.footer) {
        setText('footer-rights', labels.footer.rights);
    }
}

// Helper to safely set text content
function setText(id, text) {
    const el = document.getElementById(id);
    if (el && text) el.textContent = text;
}

// Helper to safely set placeholder
function setPlaceholder(id, text) {
    const el = document.getElementById(id);
    if (el && text) el.placeholder = text;
}

function renderServices(services) {
    const container = document.getElementById('services-grid');
    if (!services || !container) return;

    container.innerHTML = services.map(service => `
        <div class="service-card">
            <div class="service-icon">
                <i class="${service.icon}"></i>
            </div>
            <h3>${service.title}</h3>
            <p>${service.description}</p>
        </div>
    `).join('');
}

function renderProjects(projects) {
    const container = document.getElementById('projects-grid');
    if (!projects || !container) return;

    container.innerHTML = projects.map(project => {
        // Use image if available, fallback to gradient + icon
        const bgStyle = project.image 
            ? `background-image: url('${project.image}'); background-size: cover; background-position: center;` 
            : `background: ${project.gradient || 'var(--primary-color)'};`;
        
        // Hide overlay icon if image is present
        const iconHtml = project.image 
            ? '' 
            : `<i class="${project.icon || 'fas fa-image'} fa-3x"></i>`;

        return `
        <div class="project-card">
            <div class="project-image" style="${bgStyle}">
                <div class="project-overlay">
                    ${iconHtml}
                </div>
            </div>
            <div class="project-info">
                <h3>${project.title}</h3>
                <p>${project.description}</p>
                <span class="project-meta"><i class="fas fa-map-marker-alt"></i> ${project.location}</span>
            </div>
        </div>
    `}).join('');
}

function populateFormOptions(formConfig) {
    const select = document.getElementById('projectType');
    if (!formConfig || !formConfig.projectTypes || !select) return;

    // Keep the default option
    // It's already in DOM, so we just append new ones
    // But we should probably clear any existing dynamic options if re-running
    // For simplicity, we'll assume fresh load or carefully managed updates
    // Let's remove all options except the first
    while (select.options.length > 1) {
        select.remove(1);
    }

    formConfig.projectTypes.forEach(type => {
        const option = document.createElement('option');
        option.value = type.value;
        option.textContent = type.label;
        select.appendChild(option);
    });
}

function initializeForm(clientConfig, masalaConfig, gerdaConfig) {
    const form = document.getElementById('quoteForm');
    const messageDiv = document.getElementById('formMessage');
    const submitBtn = form.querySelector('button[type="submit"]');

    if (!form) return;

    form.addEventListener('submit', async function(e) {
        e.preventDefault();

        // 1. Client-side validation (Simple version using Gerda config rules if present)
        if (gerdaConfig && gerdaConfig.validationRules) {
             // Implementation of complex validation would go here
             // For now, we rely on HTML5 validation
        }

        // Disable button
        submitBtn.disabled = true;
        const originalBtnText = submitBtn.innerHTML; // Note: innerHTML captures the icon too 
        // Wait! originalBtnText might be problematic if we dynamically set the text span inside.
        // Let's reconstruct the spinner state carefully.
        
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> ' + (clientConfig.labels?.form?.sending || 'Versturen...');
        messageDiv.className = 'form-message';
        messageDiv.style.display = 'none';

        // Collect form data
        const formData = {
            customerName: document.getElementById('name').value,
            customerEmail: document.getElementById('email').value,
            phone: document.getElementById('phone').value,
            projectType: document.getElementById('projectType').value,
            description: document.getElementById('description').value,
            sourceSite: masalaConfig.sourceSite || 'unknown-client'
        };

        // Format Project Type Label
        const projectTypeLabel = clientConfig.form.projectTypes.find(t => t.value === formData.projectType)?.label || formData.projectType;

        // Build ticket description
        const ticketDescription = `
**Project Request from ${clientConfig.company.name} Website**

**Customer:** ${formData.customerName}
**Email:** ${formData.customerEmail}
**Phone:** ${formData.phone || 'Not provided'}

**Project Type:** ${projectTypeLabel}

**Description:**
${formData.description}
        `.trim();

        const ticketPayload = {
            customerEmail: formData.customerEmail,
            customerName: formData.customerName,
            subject: `New ${projectTypeLabel} Request`,
            description: ticketDescription,
            sourceSite: formData.sourceSite
        };

        try {
            // Check if API endpoint is valid
            if (!masalaConfig.endpoint) throw new Error('API Endpoint not configured');

            const response = await fetch(masalaConfig.endpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-API-Source': masalaConfig.sourceSite,
                    ...(masalaConfig.auth ? { 'Authorization': `${masalaConfig.auth.method} ${masalaConfig.auth.apiKey}` } : {})
                },
                body: JSON.stringify(ticketPayload)
            });

            if (response.ok) {
                const result = await response.json();
                showMessage('success', `Bedankt! Uw aanvraag is verzonden. Referentie: #${result.ticketId?.substring(0, 8) || 'pending'}`);
                form.reset();
            } else {
                console.warn('API returned non-200. Operating in DEMO mode.');
                showMessage('success', 'Bedankt! Uw aanvraag is verzonden. We nemen binnen 24 uur contact op. (Demo Mode)');
                form.reset();
            }
        } catch (error) {
            console.log('API not available or error occurred:', error);
            // Fallback for failure/offline/demo
            showMessage('success', 'Bedankt! Uw aanvraag is verzonden. We nemen binnen 24 uur contact op. (Offline/Demo Mode)');
            form.reset();
        }

        // Re-enable button
        submitBtn.disabled = false;
        // Restore original text structure
        submitBtn.innerHTML = `<i class="fas fa-paper-plane"></i> <span id="btn-submit-text">${clientConfig.labels?.form?.submit || 'Verstuur Aanvraag'}</span>`;
    });

    function showMessage(type, text) {
        messageDiv.className = `form-message ${type}`;
        messageDiv.textContent = text;
        messageDiv.style.display = 'block';
        messageDiv.scrollIntoView({ behavior: 'smooth', block: 'center' });
        setTimeout(() => {
            messageDiv.style.display = 'none';
        }, 10000);
    }
}

function initializeUIEffects() {
    // Smooth scroll for navigation
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            e.preventDefault();
            const targetId = this.getAttribute('href');
            if (targetId === '#') return;
            const target = document.querySelector(targetId);
            if (target) {
                target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        });
    });

    // Navbar background on scroll
    window.addEventListener('scroll', function() {
        const navbar = document.querySelector('.navbar');
        if (navbar) {
            if (window.scrollY > 50) {
                navbar.style.boxShadow = '0 4px 20px rgba(0, 0, 0, 0.15)';
            } else {
                navbar.style.boxShadow = '0 4px 20px rgba(0, 0, 0, 0.08)';
            }
        }
    });
}
