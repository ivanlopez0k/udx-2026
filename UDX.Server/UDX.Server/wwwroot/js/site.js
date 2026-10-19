window.cerrarLoginModal = function () {
    const elemento = document.getElementById("loginModal");

    if (!elemento) {
        console.log("No se encontró loginModal");
        return;
    }

    const modal = bootstrap.Modal.getOrCreateInstance(elemento);
    modal.hide();
};