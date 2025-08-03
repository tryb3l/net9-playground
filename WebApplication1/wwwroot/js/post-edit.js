document.addEventListener('DOMContentLoaded', function () {
    const inputElement = document.querySelector('input[type="file"].filepond');
    const featuredImageUrlInput = document.querySelector('input[name="FeaturedImageUrl"]');
    const antiforgeryToken = document.querySelector('input[name="__RequestVerificationToken"]').value;

    if (!inputElement || !featuredImageUrlInput || !antiforgeryToken) {
        console.warn('FilePond or required inputs not found. Uploader will not initialize.');
        return;
    }

    FilePond.create(inputElement, {
        server: {
            process: {
                url: '/Admin/api/Attachments/upload',
                headers: {
                    'RequestVerificationToken': antiforgeryToken
                },
                onload: (response) => {
                    try {
                        const data = JSON.parse(response);
                        featuredImageUrlInput.value = data.url;
                        return data.url;
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