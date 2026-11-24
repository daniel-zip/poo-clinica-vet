(() => {
    // A busca aceita diferenças de acentuação e de maiúsculas, como José e jose.
    function normalizarTexto(texto) {
        return texto.normalize("NFD").replace(/\p{M}/gu, "").toLocaleLowerCase("pt-BR").trim();
    }

    function digitos(texto) {
        return texto.replace(/\D/g, "");
    }

    for (const lista of document.querySelectorAll("[data-lista-filtravel]")) {
        const controles = lista.querySelector("[data-lista-controles]");
        if (!controles) continue;
        const busca = controles.querySelector("[data-lista-busca]");
        const filtros = [...controles.querySelectorAll("[data-lista-filtro]")];
        const limpar = controles.querySelector("[data-lista-limpar]");
        const resultado = lista.querySelector("[data-lista-resultado]");
        const itens = [...lista.querySelectorAll("[data-lista-item]")].map(elemento => ({
            elemento,
            texto: normalizarTexto(elemento.dataset.listaItem),
            numeros: (elemento.dataset.listaNumeros ?? "").split("|").map(digitos)
        }));

        function filtrar() {
            const consulta = normalizarTexto(busca?.value ?? "");
            const termos = consulta.split(/\s+/);
            const numero = /^[\d().\s-]+$/.test(consulta) ? digitos(consulta) : "";
            const ativos = filtros.filter(filtro => filtro.value !== "");
            let visiveis = 0;

            for (const { elemento, texto, numeros } of itens) {
                const correspondeBusca = termos.every(termo => texto.includes(termo)) ||
                    (numero !== "" && numeros.some(valor => valor.includes(numero)));
                const correspondeFiltros = ativos.every(filtro =>
                    elemento.getAttribute(`data-filtro-${filtro.dataset.listaFiltro}`) === filtro.value);
                // Ocultar a linha preserva os campos marcados no formulário de vínculo de pets.
                elemento.hidden = !(correspondeBusca && correspondeFiltros);
                if (!elemento.hidden) visiveis++;
            }

            const filtrando = consulta !== "" || ativos.length > 0;
            limpar.hidden = !filtrando;
            resultado.textContent = filtrando
                ? (visiveis ? `${visiveis} de ${itens.length} resultados` : "Nenhum resultado para os filtros selecionados.")
                : "";
        }

        busca?.addEventListener("input", filtrar);
        filtros.forEach(filtro => filtro.addEventListener("change", filtrar));
        limpar.addEventListener("click", () => {
            if (busca) busca.value = "";
            filtros.forEach(filtro => filtro.value = "");
            (busca ?? filtros[0]).focus();
            filtrar();
        });
        controles.hidden = false;
        window.addEventListener("pageshow", filtrar);
        filtrar();
    }
})();
