(() => {
    const especie = document.querySelector("[data-pet-especie]");
    const raca = document.querySelector("[data-pet-raca]");
    const placeholder = raca.options[0];
    const racasCadastradas = Array.from(raca.options).slice(1);

    function filtrarRacas() {
        const selecionada = raca.value;
        const disponiveis = racasCadastradas.filter(opcao => opcao.dataset.especieId === especie.value);

        placeholder.textContent = !especie.value ? "Selecione uma espécie primeiro" :
            disponiveis.length === 0 ? "Nenhuma raça cadastrada para esta espécie" : "Selecione uma raça";
        raca.replaceChildren(placeholder, ...disponiveis);
        raca.value = disponiveis.some(opcao => opcao.value === selecionada) ? selecionada : "";
    }

    especie.addEventListener("change", filtrarRacas);
    filtrarRacas();
})();
