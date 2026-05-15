document.addEventListener('DOMContentLoaded', function () {
    const inputElement = document.querySelector('input[type="file"].filepond');
    const featuredImageUrlInput = document.querySelector('input[name="FeaturedImageUrl"]');
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');

    if (!inputElement || !featuredImageUrlInput || !tokenInput) {
        console.warn('FilePond or required inputs not found. Uploader will not initialize.');
        return;
    }

    const antiforgeryToken = tokenInput.value;
    
    let existingUrl = null;
    try {
        const raw = featuredImageUrlInput.value;
        if (raw && raw.trim().startsWith('{')) {
            const parsed = JSON.parse(raw);
            existingUrl = parsed.large || parsed.thumbnail || Object.values(parsed)[0];
        } else if (raw) {
            existingUrl = raw;
        }
    } catch (e) {
        existingUrl = null;
    }

    FilePond.create(inputElement, {
        files: existingUrl
            ? [{
                source: existingUrl,
                options: { type: 'local' }
            }]
            : [],
        server: {
            process: {
                url: '/Admin/api/Attachments/upload',
                headers: {
                    'RequestVerificationToken': antiforgeryToken
                },
                onload: (response) => {
                    try {
                        const data = JSON.parse(response);
                        featuredImageUrlInput.value = JSON.stringify(data.urls);
                        return data.urls.thumbnail || data.urls.large;
                    } catch (e) {
                        console.error("Failed to parse server response:", response);
                        return null;
                    }
                },
                onerror: (response) => {
                    console.error("FilePond upload error:", response);
                    return 'Error uploading file';
                }
            },
            revert: null
        },
        labelIdle: `Drag & Drop your image or <span class="filepond--label-action">Browse</span>`,
        imagePreviewHeight: 170,
        stylePanelLayout: 'compact'
    });
});