// Fonds Hennessey Portal - Application Wizard
// Multi-step form with draft saving and// Configuration
const __TENANT = document.documentElement.getAttribute('data-theme') || 'hennessey';
const __API_BASE = window.__API_BASE__ || `https://ca-ticket-masala.kindgrass-f8932dd8.westeurope.azurecontainerapps.io`;
const API_ENDPOINT = `${__API_BASE}/api/portal/submit`;

let currentStep = 1;
const totalSteps = 5;

// Initialize
document.addEventListener('DOMContentLoaded', function () {
    loadDraft();
    updateProgress();
    setupFileInputs();
    setupCoFundingToggle();
});

// Navigation
const nextBtn = document.getElementById('nextBtn');
const prevBtn = document.getElementById('prevBtn');
const submitBtn = document.getElementById('submitBtn');

nextBtn.addEventListener('click', function () {
    if (validateStep(currentStep)) {
        currentStep++;
        showStep(currentStep);
    }
});

prevBtn.addEventListener('click', function () {
    currentStep--;
    showStep(currentStep);
});

function showStep(step) {
    // Hide all steps
    document.querySelectorAll('.form-step').forEach(s => s.classList.remove('active'));
    document.querySelectorAll('.step').forEach(s => s.classList.remove('active', 'completed'));

    // Show current step
    document.querySelector(`.form-step[data-step="${step}"]`).classList.add('active');
    document.querySelector(`.step[data-step="${step}"]`).classList.add('active');

    // Mark completed steps
    for (let i = 1; i < step; i++) {
        document.querySelector(`.step[data-step="${i}"]`).classList.add('completed');
    }

    // Update buttons
    prevBtn.style.display = step === 1 ? 'none' : 'inline-flex';
    nextBtn.style.display = step === totalSteps ? 'none' : 'inline-flex';
    submitBtn.style.display = step === totalSteps ? 'inline-flex' : 'none';

    // Update review if on last step
    if (step === totalSteps) {
        updateReview();
    }

    updateProgress();
    currentStep = step;
}

function updateProgress() {
    const progress = (currentStep / totalSteps) * 100;
    document.getElementById('progressFill').style.width = `${progress}%`;
}

function validateStep(step) {
    const stepElement = document.querySelector(`.form-step[data-step="${step}"]`);
    const inputs = stepElement.querySelectorAll('input[required], select[required], textarea[required]');

    for (let input of inputs) {
        if (!input.value) {
            input.focus();
            alert('Please fill in all required fields');
            return false;
        }
    }

    // Additional validation for specific steps
    if (step === 2) {
        const abstract = document.getElementById('abstract').value;
        if (abstract.length < 100) {
            alert('Project abstract must be at least 100 characters');
            return false;
        }
    }

    return true;
}

function updateReview() {
    const reviewContent = document.getElementById('reviewContent');
    const data = getFormData();

    reviewContent.innerHTML = `
        <p><strong>Project Title:</strong> ${data.projectTitle}</p>
        <p><strong>Grant Type:</strong> ${data.grantType}</p>
        <p><strong>Principal Investigator:</strong> ${data.principalInvestigator}</p>
        <p><strong>Institution:</strong> ${data.institution}</p>
        <p><strong>Research Field:</strong> ${data.researchField}</p>
        <p><strong>Duration:</strong> ${data.duration} months</p>
        <p><strong>Requested Amount:</strong> €${parseInt(data.requestedAmount).toLocaleString()}</p>
        <p><strong>Proposal Document:</strong> ${document.getElementById('proposal').files[0]?.name || 'Not uploaded'}</p>
    `;
}

// File inputs
function setupFileInputs() {
    const proposalInput = document.getElementById('proposal');
    const cvInput = document.getElementById('cv');

    proposalInput.addEventListener('change', function (e) {
        document.getElementById('proposalName').textContent = e.target.files[0]?.name || 'Choose PDF file...';
    });

    cvInput.addEventListener('change', function (e) {
        document.getElementById('cvName').textContent = e.target.files[0]?.name || 'Choose PDF file...';
    });
}

// Co-funding toggle
function setupCoFundingToggle() {
    const coFundingCheckbox = document.getElementById('coFunding');
    const coFundingDetails = document.getElementById('coFundingDetails');

    coFundingCheckbox.addEventListener('change', function () {
        coFundingDetails.style.display = this.checked ? 'block' : 'none';
    });
}

// Draft saving
const saveDraftBtn = document.getElementById('saveDraftBtn');

saveDraftBtn.addEventListener('click', function () {
    const data = getFormData();
    localStorage.setItem('hennesseyDraft', JSON.stringify(data));
    alert('Draft saved successfully!');
});

function loadDraft() {
    const draft = localStorage.getItem('hennesseyDraft');
    if (draft && confirm('A saved draft was found. Would you like to load it?')) {
        const data = JSON.parse(draft);
        setFormData(data);
    }
}

function getFormData() {
    return {
        projectTitle: document.getElementById('projectTitle').value,
        grantType: document.getElementById('grantType').value,
        principalInvestigator: document.getElementById('principalInvestigator').value,
        institution: document.getElementById('institution').value,
        piEmail: document.getElementById('piEmail').value,
        researchField: document.getElementById('researchField').value,
        abstract: document.getElementById('abstract').value,
        objectives: document.getElementById('objectives').value,
        duration: document.getElementById('duration').value,
        startDate: document.getElementById('startDate').value,
        requestedAmount: document.getElementById('requestedAmount').value,
        budgetJustification: document.getElementById('budgetJustification').value,
        coFunding: document.getElementById('coFunding').checked,
        coFundingSources: document.getElementById('coFundingSources').value,
        ethics: document.getElementById('ethics').value
    };
}

function setFormData(data) {
    Object.keys(data).forEach(key => {
        const element = document.getElementById(key);
        if (element) {
            if (element.type === 'checkbox') {
                element.checked = data[key];
            } else {
                element.value = data[key];
            }
        }
    });
}

// Form submission
const form = document.getElementById('grantForm');
const loadingOverlay = document.getElementById('loadingOverlay');

form.addEventListener('submit', async function (e) {
    e.preventDefault();

    if (!validateStep(totalSteps)) {
        return;
    }

    loadingOverlay.style.display = 'flex';

    try {
        const formData = new FormData();
        const data = getFormData();

        formData.append('CustomerName', data.principalInvestigator);
        formData.append('CustomerEmail', data.piEmail);
        formData.append('Description', `**${data.projectTitle}**\n\n${data.abstract}\n\n**Objectives:**\n${data.objectives}`);
        formData.append('WorkItemType', data.grantType);
        formData.append('PriorityScore', '10');
        formData.append('Tags', `Institution:${data.institution},Field:${data.researchField},Amount:${data.requestedAmount}`);

        // Add proposal file
        const proposalFile = document.getElementById('proposal').files[0];
        if (proposalFile) {
            formData.append('Attachment', proposalFile);
        }

        const response = await fetch(API_ENDPOINT, {
            method: 'POST',
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            localStorage.removeItem('hennesseyDraft');
            sessionStorage.setItem('submissionResult', JSON.stringify(result));
            window.location.href = 'success.html';
        } else {
            throw new Error(result.message || 'Submission failed');
        }

    } catch (error) {
        console.error('Submission error:', error);
        alert('An error occurred while submitting your application. Please try again.');
        loadingOverlay.style.display = 'none';
    }
});
