document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.getElementById('adminSidebar');
    const sidebarCollapseBtn = document.getElementById('sidebarCollapseBtn');
    const mobileSidebarToggle = document.getElementById('mobileSidebarToggle');

    let isSidebarCollapsed = localStorage.getItem('adminSidebarCollapsed') === 'true';
    let isMobileView = window.innerWidth < 768;

    if (isSidebarCollapsed) {
        sidebar.classList.add('collapsed');
    }

    function toggleSidebar() {
        sidebar.classList.toggle('collapsed');
        isSidebarCollapsed = sidebar.classList.contains('collapsed');
        localStorage.setItem('adminSidebarCollapsed', isSidebarCollapsed);
    }

    sidebarCollapseBtn.addEventListener('click', function () {
        toggleSidebar();
    });

    mobileSidebarToggle.addEventListener('click', function () {
        if (window.innerWidth < 768) {
            sidebar.classList.toggle('show');
            document.body.classList.toggle('sidebar-visible');
        }
    });

    document.addEventListener('click', function (event) {
        if (window.innerWidth < 768 &&
            sidebar.classList.contains('show') &&
            !sidebar.contains(event.target) &&
            !mobileSidebarToggle.contains(event.target)) {
            sidebar.classList.remove('show');
            document.body.classList.remove('sidebar-visible');
        }
    });

    window.addEventListener('resize', function () {
        const wasMobileView = isMobileView;
        isMobileView = window.innerWidth < 768;

        if (wasMobileView !== isMobileView) {
            if (isMobileView) {
                sidebar.classList.remove('show');
                document.body.classList.remove('sidebar-visible');
            }
            else {
                sidebar.classList.remove('show');
                document.body.classList.remove('sidebar-visible');
            }
        }
    });

    const currentController = document.querySelector('.nav-link.active');
    if (currentController && currentController.classList.contains('has-submenu')) {
        const submenuId = currentController.getAttribute('href');
        const submenu = document.querySelector(submenuId);
        if (submenu) {
            submenu.classList.add('show');
        }
    }
});