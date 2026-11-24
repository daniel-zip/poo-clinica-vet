(() => {
    const soDigitos = valor => valor.replace(/[^0-9]/g, "");
    const ehDigito = caractere => caractere >= "0" && caractere <= "9";

    function cpf(digitos) {
        return [digitos.slice(0, 3), digitos.slice(3, 6), digitos.slice(6, 9)]
            .filter(Boolean)
            .join(".") + (digitos.length > 9 ? "-" + digitos.slice(9) : "");
    }

    function telefone(digitos) {
        if (!digitos) return "";
        if (digitos.length <= 2) return "(" + digitos;

        const fimPrefixo = digitos.length === 11 ? 7 : 6;
        const prefixo = digitos.slice(2, fimPrefixo);
        const sufixo = digitos.slice(fimPrefixo);
        return "(" + digitos.slice(0, 2) + ") " + prefixo + (sufixo ? "-" + sufixo : "");
    }

    function moverCursor(campo, quantidade) {
        let posicao = 0;
        while (quantidade > 0 && posicao < campo.value.length) {
            if (ehDigito(campo.value[posicao])) quantidade--;
            posicao++;
        }
        campo.setSelectionRange(posicao, posicao);
    }

    for (const campo of document.querySelectorAll("[data-mascara]")) {
        const formatar = campo.dataset.mascara === "cpf" ? cpf : telefone;
        const digitosIniciais = soDigitos(campo.value);
        if (digitosIniciais.length <= 11 && /^[0-9.()\s-]*$/.test(campo.value)) {
            campo.value = formatar(digitosIniciais);
        }

        campo.addEventListener("beforeinput", evento => {
            const { selectionStart: inicio, selectionEnd: fim, value } = campo;
            if (inicio !== fim) return;

            if (evento.inputType === "deleteContentBackward" && inicio > 0 && !ehDigito(value[inicio - 1])) {
                let anterior = inicio - 1;
                while (anterior > 0 && !ehDigito(value[anterior])) anterior--;
                campo.setSelectionRange(anterior, fim);
            } else if (evento.inputType === "deleteContentForward" && fim < value.length && !ehDigito(value[fim])) {
                let seguinte = fim;
                while (seguinte < value.length && !ehDigito(value[seguinte])) seguinte++;
                campo.setSelectionRange(inicio, Math.min(seguinte + 1, value.length));
            }
        });

        campo.addEventListener("input", () => {
            // Preserva a posição do cursor pelo número de dígitos, mesmo após inserir a pontuação.
            const quantidade = soDigitos(campo.value.slice(0, campo.selectionStart)).length;
            const digitos = soDigitos(campo.value).slice(0, 11);
            campo.value = formatar(digitos);
            moverCursor(campo, quantidade);
        });
    }
})();
