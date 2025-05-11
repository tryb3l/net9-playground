document.addEventListener('DOMContentLoaded', function () {
    const categorySelect = document.querySelector('.category-select');
    const tagSelect = document.querySelector('.tag-select');

    if (categorySelect) {
        console.log('Category select initialized with', categorySelect.options.length, 'options');
    }

    if (tagSelect) {
        console.log('Tag select initialized with', tagSelect.options.length, 'options');
    }

});