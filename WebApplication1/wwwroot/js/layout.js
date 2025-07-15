document.addEventListener('DOMContentLoaded', function () {
    const mobileToggle = document.querySelector('.mobile-menu-toggle');
    const navbarContent = document.querySelector('.navbar-content');

    if (mobileToggle && navbarContent) {
        mobileToggle.removeAttribute('data-bs-toggle');
        mobileToggle.removeAttribute('data-bs-target');
        navbarContent.classList.remove('collapse');

        mobileToggle.setAttribute('aria-expanded', 'false');
        mobileToggle.classList.remove('opened');
        navbarContent.classList.remove('show');
        navbarContent.style.display = '';

        mobileToggle.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const isExpanded = mobileToggle.getAttribute('aria-expanded') === 'true';
            const newState = !isExpanded;

            console.log('Toggle clicked. Current state:', isExpanded, 'New state:', newState);

            mobileToggle.setAttribute('aria-expanded', newState);

            if (newState) {
                mobileToggle.classList.add('opened');
                navbarContent.classList.add('show');
                document.body.style.overflow = 'hidden';
                console.log('Menu opened');
            } else {
                mobileToggle.classList.remove('opened');
                navbarContent.classList.remove('show');
                document.body.style.overflow = '';
                console.log('Menu closed');
            }
        });

        const navLinks = navbarContent.querySelectorAll('.nav-link');
        navLinks.forEach(link => {
            link.addEventListener('click', () => {
                if (window.innerWidth <= 768) {
                    console.log('Nav link clicked - closing menu');
                    mobileToggle.setAttribute('aria-expanded', 'false');
                    mobileToggle.classList.remove('opened');
                    navbarContent.classList.remove('show');
                    document.body.style.overflow = '';
                }
            });
        });

        // Handle window resize
        window.addEventListener('resize', () => {
            if (window.innerWidth > 768) {
                console.log('Window resized to desktop - resetting menu');
                mobileToggle.setAttribute('aria-expanded', 'false');
                mobileToggle.classList.remove('opened');
                navbarContent.classList.remove('show');
                document.body.style.overflow = '';
            }
        });

        document.addEventListener('click', (e) => {
            if (window.innerWidth <= 768 &&
                !mobileToggle.contains(e.target) &&
                !navbarContent.contains(e.target) &&
                navbarContent.classList.contains('show')) {

                console.log('Clicked outside - closing menu');
                mobileToggle.setAttribute('aria-expanded', 'false');
                mobileToggle.classList.remove('opened');
                navbarContent.classList.remove('show');
                document.body.style.overflow = '';
            }
        });

        mobileToggle.addEventListener('shown.bs.collapse', function (e) {
            e.preventDefault();
        });

        mobileToggle.addEventListener('hidden.bs.collapse', function (e) {
            e.preventDefault();
        });
    }

    const navbar = document.querySelector('.mat-navbar');

    if (navbar) {
        window.addEventListener('scroll', function () {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;

            if (scrollTop > 50) {
                navbar.classList.add('scrolled');
            } else {
                navbar.classList.remove('scrolled');
            }
        });
    }
});