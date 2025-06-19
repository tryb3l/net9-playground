document.addEventListener('DOMContentLoaded', function () {
    const publishSwitch = document.getElementById('publishSwitch');
    const saveButtonText = document.getElementById('saveButtonText');
    const saveBtn = document.getElementById('savePostBtn');

    if (!publishSwitch || !saveButtonText || !saveBtn) {
        console.warn('Post creation form elements not found');
        return;
    }

    function updateButtonText() {
        if (publishSwitch.checked) {
            saveButtonText.textContent = 'Publish Post';
            saveBtn.className = 'btn btn-success';
        } else {
            saveButtonText.textContent = 'Save Draft';
            saveBtn.className = 'btn btn-primary';
        }
    }

    publishSwitch.addEventListener('change', updateButtonText);
    updateButtonText(); // Initial call

    // Form validation feedback
    const form = document.querySelector('form');
    if (form) {
        form.addEventListener('submit', function (e) {
            saveBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saving...';
            saveBtn.disabled = true;
        });
    }

    // Debug info
    console.log('Post create form initialized');

    // Get data from data attributes instead of inline Razor
    const categoriesCount = document.getElementById('categorySelect')?.options.length || 0;
    const tagsContainer = document.querySelector('.tag-selection-container');
    const tagsCount = tagsContainer?.querySelectorAll('input[type="checkbox"]').length || 0;

    console.log('Available categories:', categoriesCount);
    console.log('Available tags:', tagsCount);
});