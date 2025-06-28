document.addEventListener('DOMContentLoaded', function() {
    // Get sidebar elements
    const mobileSidebarToggle = document.getElementById('mobileSidebarToggle');
    const adminSidebar = document.getElementById('adminSidebar');
    const sidebarCollapseBtn = document.getElementById('sidebarCollapseBtn');

    // Mobile sidebar toggle
    if (mobileSidebarToggle && adminSidebar) {
        mobileSidebarToggle.addEventListener('click', function() {
            adminSidebar.classList.toggle('show-sidebar');
        });
    }

    // Desktop sidebar collapse
    if (sidebarCollapseBtn && adminSidebar) {
        sidebarCollapseBtn.addEventListener('click', function() {
            document.body.classList.toggle('sidebar-collapsed');
        });
    }

    // On mobile, hide the sidebar after clicking a link.
    document.querySelectorAll('.sidebar a.nav-link').forEach(function(link) {
        link.addEventListener('click', function(e) {
            if (window.innerWidth < 768 && adminSidebar && adminSidebar.classList.contains('show-sidebar')) {
                adminSidebar.classList.remove('show-sidebar');
            }
        });
    });

    // Delete confirmations
    document.querySelectorAll('.delete-form').forEach(function(form) {
        form.addEventListener('submit', function(e) {
            const postTitle = this.getAttribute('data-post-title');
            if (!confirm(`Are you sure you want to delete "${postTitle}"?`)) {
                e.preventDefault();
            }
        });
    });
});