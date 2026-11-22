(() => {
    const cargo = document.querySelector("[data-funcionario-cargo]");
    const grupos = cargo.form.querySelectorAll("[data-campos-cargo]");

    function mostrarCamposDoCargo() {
        for (const grupo of grupos) {
            const mostrar = grupo.dataset.camposCargo === cargo.value;
            grupo.hidden = !mostrar;
            grupo.disabled = !mostrar;

            for (const input of grupo.querySelectorAll("input")) {
                input.required = mostrar;
            }
        }
    }

    cargo.addEventListener("change", mostrarCamposDoCargo);
    mostrarCamposDoCargo();
})();
