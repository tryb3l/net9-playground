document.addEventListener('DOMContentLoaded', function () {
    const adminSidebar = document.getElementById('adminSidebar');
    const mainWrapper = document.querySelector('.main-wrapper');
    const desktopSidebarToggle = document.getElementById('sidebarCollapseBtn');
    const mobileSidebarToggle = document.getElementById('mobileSidebarToggle');

    // --- State Persistence for Desktop ---
    const applySidebarState = () => {
        if (window.innerWidth >= 768 && localStorage.getItem('sidebarCollapsed') === 'true') {
            adminSidebar.classList.add('collapsed');
        }
    };

    // --- Desktop Toggle Handler ---
    if (desktopSidebarToggle) {
        desktopSidebarToggle.addEventListener('click', function () {
            adminSidebar.classList.toggle('collapsed');
            localStorage.setItem('sidebarCollapsed', adminSidebar.classList.contains('collapsed'));
        });
    }

    // --- Mobile Toggle Handler ---
    if (mobileSidebarToggle) {
        mobileSidebarToggle.addEventListener('click', function (e) {
            e.stopPropagation();
            adminSidebar.classList.toggle('active');
        });
    }

    if (mainWrapper) {
        mainWrapper.addEventListener('click', function () {
            if (adminSidebar.classList.contains('active')) {
                adminSidebar.classList.remove('active');
            }
        });
    }

    const userProfileDropdown = document.querySelector('#adminSidebar .sidebar-user-profile .dropdown-toggle');
    if (userProfileDropdown) {
        new bootstrap.Dropdown(userProfileDropdown);
    }

    // --- Initial State on Page Load ---
    applySidebarState();
});