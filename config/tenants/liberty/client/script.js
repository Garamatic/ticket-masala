// Liberty Systems Portal - The Newsroom
// Markdown editor with preview and code// Configuration
const __TENANT = document.documentElement.getAttribute('data-theme') || 'liberty';
const __API_BASE = window.__API_BASE__ || `https://ca-ticket-masala.kindgrass-f8932dd8.westeurope.azurecontainerapps.io`;
const API_ENDPOINT = `${__API_BASE}/api/portal/submit`;

// Tab switching
const tabButtons = document.querySelectorAll('.tab-btn');
const descriptionTextarea = document.getElementById('description');
const previewDiv = document.getElementById('preview');

tabButtons.forEach(btn => {
    btn.addEventListener('click', function () {
        const tab = this.dataset.tab;

        tabButtons.forEach(b => b.classList.remove('active'));
        this.classList.add('active');

        if (tab === 'write') {
            descriptionTextarea.style.display = 'block';
            previewDiv.style.display = 'none';
        } else {
            descriptionTextarea.style.display = 'none';
            previewDiv.style.display = 'block';
            updatePreview();
        }
    });
});

// Update markdown preview
function updatePreview() {
    const markdown = descriptionTextarea.value;
    const html = marked.parse(markdown);
    previewDiv.innerHTML = html;

    // Highlight code blocks
    previewDiv.querySelectorAll('pre code').forEach((block) => {
        hljs.highlightElement(block);
    });
}

// Auto-update preview on input (debounced)
let previewTimeout;
descriptionTextarea.addEventListener('input', function () {
    clearTimeout(previewTimeout);
    previewTimeout = setTimeout(() => {
        if (document.querySelector('.tab-btn[data-tab="preview"]').classList.contains('active')) {
            updatePreview();
        }
    }, 500);
});

// File upload
const fileInput = document.getElementById('screenshot');
const fileNameDisplay = document.getElementById('fileName');

fileInput.addEventListener('change', function (e) {
    const fileName = e.target.files[0]?.name || 'Choose file...';
    fileNameDisplay.textContent = fileName;
});

// Form submission
const form = document.getElementById('issueForm');
const loadingOverlay = document.getElementById('loadingOverlay');

form.addEventListener('submit', async function (e) {
    e.preventDefault();

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    loadingOverlay.style.display = 'flex';

    try {
        const formData = new FormData();

        formData.append('CustomerName', document.getElementById('reporterName').value);
        formData.append('CustomerEmail', document.getElementById('reporterEmail').value);
        formData.append('Description', `**${document.getElementById('title').value}**\n\n${descriptionTextarea.value}`);
        formData.append('WorkItemType', document.getElementById('issueType').value);
        formData.append('PriorityScore', document.getElementById('priority').value);

        // Add environment tag
        const environment = document.getElementById('environment').value;
        if (environment) {
            formData.append('Tags', `Environment:${environment}`);
        }

        // Add file
        const file = fileInput.files[0];
        if (file) {
            formData.append('Attachment', file);
        }

        const response = await fetch(API_ENDPOINT, {
            method: 'POST',
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            sessionStorage.setItem('submissionResult', JSON.stringify(result));
            window.location.href = 'success.html';
        } else {
            throw new Error(result.message || 'Submission failed');
        }

    } catch (error) {
        console.error('Submission error:', error);
        alert('An error occurred while submitting the issue. Please try again.');
        loadingOverlay.style.display = 'none';
    }
});
