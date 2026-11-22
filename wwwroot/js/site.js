const dialogoPerfis = document.querySelector("#escolher-perfil");
const botaoPerfis = document.querySelector("#abrir-perfis");
const perfis = Array.from(dialogoPerfis.querySelectorAll("[data-profile-id]"));
const links = Array.from(document.querySelectorAll("[data-area-link]"));
const paineis = Array.from(document.querySelectorAll("[data-area-panel]"));
const linkInicio = document.querySelector("[data-home-link]");
const chavePerfil = "vetcare-perfil-demonstracao";

function lerPerfil() {
    let salvo;
    try {
        salvo = localStorage.getItem(chavePerfil);
    } catch {
        // O seletor continua funcionando quando o armazenamento local está bloqueado.
    }
    return perfis.find(perfil => perfil.dataset.profileId === salvo) ??
        perfis.find(perfil => perfil.dataset.profileArea === salvo) ??
        perfis.find(perfil => perfil.dataset.profileArea === "recepcao") ??
        perfis[0];
}

let perfilAtual = lerPerfil();

function podeVisitar(area) {
    const areaPerfil = perfilAtual?.dataset.profileArea;
    return areaPerfil === "administracao" || (areaPerfil ?? "administracao") === area;
}

function exibirArea() {
    if (paineis.length === 0) {
        return;
    }

    const solicitada = location.hash.slice(1);
    const areaExiste = paineis.some(painel => painel.dataset.areaPanel === solicitada);
    const area = areaExiste && podeVisitar(solicitada)
        ? solicitada
        : perfilAtual?.dataset.profileArea ?? "administracao";

    if (area !== solicitada) {
        history.replaceState(null, "", "#" + area);
    }

    for (const painel of paineis) {
        painel.hidden = painel.dataset.areaPanel !== area;
    }

    for (const link of links) {
        if (link.dataset.areaLink === area) {
            link.setAttribute("aria-current", "page");
        } else {
            link.removeAttribute("aria-current");
        }
    }
}

function exibirPerfil() {
    const area = perfilAtual?.dataset.profileArea ?? "administracao";
    document.documentElement.dataset.perfilDemonstracao = area;
    if (area === "veterinario") {
        for (const link of document.querySelectorAll("[data-rota-veterinario]")) {
            link.href = link.dataset.rotaVeterinario;
        }
    }
    document.querySelector("[data-profile-name]").textContent =
        perfilAtual?.querySelector(".profile-details strong").textContent ?? "Nenhum funcionário ativo";
    document.querySelector("[data-profile-role]").textContent =
        perfilAtual?.querySelector(".profile-details span").textContent ?? "";

    for (const perfil of perfis) {
        perfil.setAttribute("aria-pressed", String(perfil === perfilAtual));
    }

    for (const link of links) {
        if (podeVisitar(link.dataset.areaLink)) {
            link.href = link.dataset.destination;
            link.removeAttribute("aria-disabled");
        } else {
            link.removeAttribute("href");
            link.setAttribute("aria-disabled", "true");
        }
    }

    linkInicio.href = links.find(link => link.dataset.areaLink === area)
        .dataset.destination;
    configurarDesativacao();
    exibirArea();
}

function configurarDesativacao() {
    const formulario = document.querySelector("[data-desativacao-alvo]");
    if (!formulario) {
        return;
    }

    const administradorId = perfilAtual?.dataset.profileArea === "administracao"
        ? perfilAtual.dataset.profileId
        : "";
    const proprioPerfil = administradorId === formulario.dataset.desativacaoAlvo;
    const ultimoAdministrador = formulario.dataset.alvoAdministrador === "true" &&
        Number(dialogoPerfis.dataset.administradoresAtivos) <= 1;
    const aviso = document.querySelector("[data-aviso-desativacao]");
    formulario.querySelector("[data-administrador-desativacao]").value = administradorId;
    const motivo = ultimoAdministrador
        ? "É necessário ter outro administrador ativo antes de desativar este cadastro."
        : proprioPerfil
            ? "Você não pode desativar o seu próprio cadastro."
            : !administradorId
                ? "Selecione um administrador em Trocar usuário para confirmar a desativação."
                : "";
    formulario.querySelector('button[type="submit"]').disabled = motivo !== "";
    aviso.hidden = motivo === "";
    aviso.textContent = motivo;
}

for (const link of links) {
    link.dataset.destination = link.getAttribute("href");
}

for (const perfil of perfis) {
    perfil.addEventListener("click", () => {
        perfilAtual = perfil;
        try {
            localStorage.setItem(chavePerfil, perfil.dataset.profileId);
        } catch {
            // A troca continua disponível quando o navegador bloqueia o armazenamento.
        }

        exibirPerfil();
        dialogoPerfis.close();
        if (paineis.length === 0) {
            location.assign(linkInicio.href);
        }
    });
}

botaoPerfis.addEventListener("click", () => dialogoPerfis.showModal());
window.addEventListener("hashchange", () => {
    if (location.hash !== "#conteudo") {
        exibirArea();
    }
});
exibirPerfil();
