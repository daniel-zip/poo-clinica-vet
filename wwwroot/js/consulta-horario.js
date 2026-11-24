const formatoMesConsulta = new Intl.DateTimeFormat("pt-BR", { month: "long", year: "numeric", timeZone: "UTC" });
const formatoDiaConsulta = new Intl.DateTimeFormat("pt-BR", { dateStyle: "long", timeZone: "UTC" });
const formatoDataConsulta = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeZone: "UTC" });

// Representa dias do calendário em UTC para evitar que o fuso do navegador mude o dia selecionado.
function criarDataCalendario(ano, mes, dia) {
    const data = new Date(0);
    data.setUTCFullYear(ano, mes, dia);
    return data;
}

function diaUtilConsulta(data) {
    const dia = data.getUTCDay();
    return dia >= 1 && dia <= 5;
}

for (const campo of document.querySelectorAll("[data-horario-consulta]")) {
    const dataHora = campo.querySelector("[data-consulta-data-hora]");
    const formulario = dataHora.form;
    const veterinario = formulario.querySelector("[data-consulta-veterinario]");
    const horario = campo.querySelector("[data-consulta-horario]");
    const avisoData = campo.querySelector("[data-consulta-aviso-data]");
    const disponibilidade = campo.querySelector("[data-consulta-disponibilidade]");
    const abrir = campo.querySelector("[data-calendario-abrir]");
    const valor = campo.querySelector("[data-calendario-valor]");
    const calendario = campo.querySelector("[data-calendario]");
    const tituloMes = calendario.querySelector("[data-calendario-mes]");
    const dias = calendario.querySelector("[data-calendario-dias]");
    const anterior = calendario.querySelector("[data-calendario-anterior]");
    const proximo = calendario.querySelector("[data-calendario-proximo]");

    // Os horários passados são calculados no fuso da clínica.
    const relogioClinica = new Intl.DateTimeFormat("en-CA", {
        timeZone: campo.dataset.fusoAgendamento, year: "numeric", month: "2-digit", day: "2-digit",
        hour: "2-digit", minute: "2-digit", hourCycle: "h23"
    });
    const opcoesHorario = Array.from(horario.options).filter(opcao => opcao.value);
    const ultimoHorario = opcoesHorario.at(-1).value;
    const obterAgora = () => {
        const { year, month, day, hour, minute } = Object.fromEntries(
            relogioClinica.formatToParts(new Date()).map(({ type, value }) => [type, value])
        );
        return `${year}-${month}-${day}T${hour}:${minute}`;
    };
    const diaDisponivel = (data, agora) =>
        diaUtilConsulta(data) && `${data.toISOString().slice(0, 10)}T${ultimoHorario}` > agora;

    const agoraInicial = obterAgora();
    const inicial = abrir.dataset.dataInicial ? new Date(abrir.dataset.dataInicial + "T00:00:00Z") : null;
    let selecionada = inicial && diaDisponivel(inicial, agoraInicial) ? inicial : null;
    let mesVisivel = new Date(`${(selecionada?.toISOString() ?? agoraInicial).slice(0, 7)}-01T00:00:00Z`);
    if (inicial && !selecionada) {
        horario.value = "";
    }
    let ocupados = null;
    let ultimoPedido;
    let falhaNaConsulta = false;

    const veterinarioAtual = () => veterinario.value;
    const dataAtual = () => selecionada?.toISOString().slice(0, 10);
    const sincronizar = () => {
        const agora = obterAgora();
        const data = dataAtual();
        const temVeterinario = !!veterinarioAtual();
        abrir.disabled = !temVeterinario;
        for (const opcao of opcoesHorario) {
            const ocupado = ocupados?.has(opcao.value) ?? false;
            opcao.disabled = ocupado || (!!data && `${data}T${opcao.value}` <= agora);
            const rotulo = ocupado ? `${opcao.value} (ocupado)` : opcao.value;
            if (opcao.textContent !== rotulo) {
                opcao.textContent = rotulo;
            }
        }

        if (horario.selectedOptions[0]?.disabled) {
            horario.value = "";
        }
        const temHorarioLivre = opcoesHorario.some(opcao => !opcao.disabled);
        horario.disabled = !temVeterinario || !data || !ocupados || !temHorarioLivre;
        dataHora.value = !horario.disabled && horario.value ? `${data}T${horario.value}` : "";

        const mensagem = !temVeterinario ? "Selecione o veterinário primeiro."
            : !data ? "Selecione uma data para ver os horários."
                : falhaNaConsulta ? "Não foi possível consultar os horários. Tente novamente."
                    : !ocupados ? "Consultando horários do veterinário…"
                        : !temHorarioLivre ? "Nenhum horário disponível nesta data." : "";
        if (disponibilidade.textContent !== mensagem) {
            disponibilidade.textContent = mensagem;
        }
    };

    const consultarHorarios = async (novaSelecao = false) => {
        if (novaSelecao) {
            // Uma resposta da combinação anterior de data e veterinário não deve substituir a atual.
            ultimoPedido?.abort();
            ultimoPedido = null;
            ocupados = null;
            falhaNaConsulta = false;
        }
        const veterinarioId = veterinarioAtual();
        const data = dataAtual();
        sincronizar();
        if (!veterinarioId || !data || ultimoPedido) {
            return;
        }

        const pedido = new AbortController();
        ultimoPedido = pedido;
        try {
            const url = new URL(campo.dataset.urlHorariosOcupados, document.baseURI);
            url.searchParams.set("veterinarioId", veterinarioId);
            url.searchParams.set("data", data);
            if (formulario.dataset.consultaId) {
                // No reagendamento, a própria consulta não bloqueia seu horário atual.
                url.searchParams.set("consultaId", formulario.dataset.consultaId);
            }
            const resposta = await fetch(url, { signal: pedido.signal, cache: "no-store" });
            if (!resposta.ok) {
                throw new Error("A disponibilidade não foi carregada.");
            }

            const horarios = await resposta.json();
            if (ultimoPedido === pedido) {
                ocupados = new Set(horarios);
                falhaNaConsulta = false;
            }
        } catch {
            if (ultimoPedido === pedido) {
                ocupados = null;
                falhaNaConsulta = true;
            }
        } finally {
            if (ultimoPedido === pedido) {
                ultimoPedido = null;
                sincronizar();
            }
        }
    };

    const mostrarSelecionada = () => {
        valor.textContent = selecionada ? formatoDataConsulta.format(selecionada) : "Selecionar data";
        avisoData.hidden = true;
        abrir.removeAttribute("aria-invalid");
        sincronizar();
    };

    const mostrarMes = () => {
        const agora = obterAgora();
        const mesAtual = agora.slice(0, 7);
        if (mesVisivel.toISOString().slice(0, 7) < mesAtual) {
            mesVisivel = new Date(`${mesAtual}-01T00:00:00Z`);
        }

        const ano = mesVisivel.getUTCFullYear();
        const mes = mesVisivel.getUTCMonth();
        const totalDias = criarDataCalendario(ano, mes + 1, 0).getUTCDate();
        const espacos = mesVisivel.getUTCDay();
        const fragmento = document.createDocumentFragment();

        tituloMes.textContent = formatoMesConsulta.format(mesVisivel);
        anterior.disabled = mesVisivel.toISOString().slice(0, 7) === mesAtual;
        proximo.disabled = ano === 9999 && mes === 11;

        for (let i = 0; i < espacos; i++) {
            fragmento.append(document.createElement("span"));
        }

        for (let dia = 1; dia <= totalDias; dia++) {
            const data = criarDataCalendario(ano, mes, dia);
            const botao = document.createElement("button");
            botao.type = "button";
            botao.className = "consulta-calendario-dia";
            botao.textContent = dia;
            botao.disabled = !diaDisponivel(data, agora);
            botao.setAttribute("aria-label", formatoDiaConsulta.format(data));
            botao.setAttribute("aria-pressed", String(data.getTime() === selecionada?.getTime()));
            botao.addEventListener("click", () => {
                if (!diaDisponivel(data, obterAgora())) {
                    mostrarMes();
                    return;
                }

                if (data.getTime() !== selecionada?.getTime()) {
                    horario.value = "";
                }
                selecionada = data;
                mostrarSelecionada();
                consultarHorarios(true);
                calendario.close();
            });
            fragmento.append(botao);
        }

        dias.replaceChildren(fragmento);
    };

    const mudarMes = deslocamento => {
        mesVisivel = criarDataCalendario(mesVisivel.getUTCFullYear(), mesVisivel.getUTCMonth() + deslocamento, 1);
        mostrarMes();
    };

    abrir.addEventListener("click", () => {
        mostrarMes();
        calendario.showModal();
        (dias.querySelector('[aria-pressed="true"]') ?? dias.querySelector("button:not(:disabled)"))?.focus();
    });
    calendario.querySelector("[data-calendario-fechar]").addEventListener("click", () => calendario.close());
    anterior.addEventListener("click", () => mudarMes(-1));
    proximo.addEventListener("click", () => mudarMes(1));
    veterinario.addEventListener("change", () => {
        if (!formulario.dataset.consultaId) {
            horario.value = "";
        }
        consultarHorarios(true);
    });
    horario.addEventListener("focus", sincronizar);
    horario.addEventListener("change", sincronizar);
    formulario.addEventListener("submit", evento => {
        sincronizar();
        if (!selecionada) {
            evento.preventDefault();
            avisoData.hidden = false;
            abrir.setAttribute("aria-invalid", "true");
            abrir.focus();
        } else if (!dataHora.value) {
            evento.preventDefault();
            if (!horario.disabled) {
                horario.reportValidity();
            }
        }
    });
    mostrarSelecionada();
    consultarHorarios(true);
    window.addEventListener("focus", () => consultarHorarios());
    setInterval(() => consultarHorarios(), 30_000);
}
