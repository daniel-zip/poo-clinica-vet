(() => {
    const campo = document.querySelector("[data-salario]");
    if (!campo) return;

    const formato = new Intl.NumberFormat("pt-BR", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
    const valorInicial = campo.value;
    campo.type = "text";
    campo.inputMode = "decimal";
    campo.placeholder = "0,00";
    campo.value = valorInicial === "" ? "" : formato.format(Number(valorInicial));
    campo.parentElement.querySelector("[data-salario-prefixo]").hidden = false;

    function lerValor() {
        return campo.value.trim().replace(/^R\$\s*/i, "").replace(/\./g, "");
    }

    campo.addEventListener("beforeinput", evento => {
        const { selectionStart: inicio, selectionEnd: fim, value } = campo;
        if (inicio !== fim) return;

        if (evento.inputType === "deleteContentBackward" && value[inicio - 1] === ".") {
            campo.setSelectionRange(inicio - 2, fim);
        } else if (evento.inputType === "deleteContentForward" && value[fim] === ".") {
            campo.setSelectionRange(inicio, fim + 2);
        }
    });

    campo.addEventListener("input", () => {
        const valor = lerValor();
        const valido = valor === "" || /^\d+(,\d{0,2})?$/.test(valor) && Number.isFinite(Number(valor.replace(",", ".")));
        campo.setCustomValidity(valido ? "" : "Informe um salário válido, com até duas casas decimais.");
        if (!valido) return;

        const aposCursor = campo.value.slice(campo.selectionStart).replace(/[^\d,]/g, "").length;
        const [reais, centavos] = valor.split(",");
        campo.value = reais.replace(/\B(?=(\d{3})+(?!\d))/g, ".") + (centavos === undefined ? "" : "," + centavos);

        let posicao = campo.value.length;
        let restantes = aposCursor;
        while (restantes > 0 && posicao > 0) {
            posicao--;
            if (campo.value[posicao] !== ".") restantes--;
        }
        campo.setSelectionRange(posicao, posicao);
    });

    campo.addEventListener("blur", () => {
        if (campo.value !== "" && campo.validity.valid) {
            campo.value = formato.format(Number(lerValor().replace(",", ".")));
        }
    });

    campo.form.addEventListener("formdata", evento => {
        // A exibição usa real brasileiro. O POST usa o separador decimal esperado pelo MVC.
        const dados = evento.formData;
        const separador = dados.getAll("__Invariant").includes(campo.name) ? "." : campo.dataset.separadorDecimal;
        dados.set(campo.name, lerValor().replace(",", separador));
    });
})();
