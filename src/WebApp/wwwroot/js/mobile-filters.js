document.addEventListener('DOMContentLoaded', function () {
    const categoryToggle = document.getElementById('categoryToggle');
    const tagToggle = document.getElementById('tagToggle');
    const categoryFilter = document.getElementById('categoryFilter');
    const tagFilter = document.getElementById('tagFilter');

    if (categoryToggle && tagToggle && categoryFilter && tagFilter) {
        categoryToggle.addEventListener('click', function () {
            toggleFilter(categoryToggle, categoryFilter, tagToggle, tagFilter);
        });

        tagToggle.addEventListener('click', function () {
            toggleFilter(tagToggle, tagFilter, categoryToggle, categoryFilter);
        });
    }

    function toggleFilter(activeBtn, activeFilter, otherBtn, otherFilter) {

        if (activeBtn.classList.contains('active')) {
            activeBtn.classList.remove('active');
            activeFilter.classList.remove('show');
        } else {

            otherBtn.classList.remove('active');
            otherFilter.classList.remove('show');

            activeBtn.classList.add('active');
            activeFilter.classList.add('show');
        }
    }
});