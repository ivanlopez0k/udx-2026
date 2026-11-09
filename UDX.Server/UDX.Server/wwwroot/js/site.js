window.cerrarLoginModal = function () {
    const elemento = document.getElementById("loginModal");

    if (!elemento) {
        console.log("No se encontró loginModal");
        return;
    }

    const modal = bootstrap.Modal.getOrCreateInstance(elemento);
    modal.hide();
};

// Navbar dynamic shadow and frosted glass on scroll
window.addEventListener('scroll', function () {
    const headerWrap = document.querySelector('.header-floating-wrap');
    if (headerWrap) {
        if (window.scrollY > 20) {
            headerWrap.classList.add('scrolled');
        } else {
            headerWrap.classList.remove('scrolled');
        }
    }
});
