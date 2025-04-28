document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.getElementById('sidebarMenu');
    const sidebarToggler = document.querySelector('[data-bs-toggle="collapse"][data-bs-target="#sidebarMenu"]');
    let sidebarCollapseInstance = null;

    if (sidebar) {
        if (window.innerWidth < 768) {
            sidebar.classList.remove('show');
        }

        sidebarCollapseInstance = bootstrap.Collapse.getOrCreateInstance(sidebar);

        if (window.innerWidth < 768) {
            setTimeout(function () {
                sidebar.classList.remove('show');
                if (sidebarCollapseInstance) {
                    sidebarCollapseInstance.hide();
                }
            }, 50);
        }

        window.addEventListener('resize', function () {
            if (window.innerWidth < 768) {
                if (sidebarCollapseInstance) {
                    sidebarCollapseInstance.hide();
                }
            }
        });

        sidebar.addEventListener('mouseleave', function () {
            const openDropdownMenu = sidebar.querySelector('.dropdown-menu.show');
            if (openDropdownMenu) {
                const toggleButton = openDropdownMenu.previousElementSibling;
                if (toggleButton && toggleButton.matches('[data-bs-toggle="dropdown"]')) {
                    const dropdownInstance = bootstrap.Dropdown.getInstance(toggleButton);
                    if (dropdownInstance) {
                        dropdownInstance.hide();
                    }
                }
            }
        });

        document.addEventListener('click', function (event) {
            const isClickInsideSidebar = sidebar.contains(event.target);
            const isClickOnToggler = sidebarToggler && sidebarToggler.contains(event.target);
            const isSidebarShown = sidebar.classList.contains('show');

            if (isSidebarShown && !isClickInsideSidebar && !isClickOnToggler) {
                if (sidebarCollapseInstance) {
                    sidebarCollapseInstance.hide();
                }
            }
        });
    }
});

window.addEventListener('load', function () {
    const sidebar = document.getElementById('sidebarMenu');
    if (sidebar && window.innerWidth < 768) {
        sidebar.classList.remove('show');
        const sidebarCollapseInstance = bootstrap.Collapse.getInstance(sidebar);
        if (sidebarCollapseInstance) {
            sidebarCollapseInstance.hide();
        }
    }
});