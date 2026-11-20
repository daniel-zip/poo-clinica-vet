const dialogoPerfis = document.querySelector("#escolher-perfil");
const botaoPerfis = document.querySelector("#abrir-perfis");
const perfis = Array.from(dialogoPerfis.querySelectorAll("[data-profile-area]"));
const links = Array.from(document.querySelectorAll("[data-area-link]"));
const paineis = Array.from(document.querySelectorAll("[data-area-panel]"));
const linkInicio = document.querySelector("[data-home-link]");
const chavePerfil = "vetcare-perfil-demonstracao";

function lerPerfil() {
    try {
        const area = localStorage.getItem(chavePerfil);
        return perfis.find(perfil => perfil.dataset.profileArea === area) ?? perfis[0];
    } catch {
        return perfis[0];
    }
}

let perfilAtual = lerPerfil();

function podeVisitar(area) {
    return perfilAtual.dataset.profileArea === "administracao" ||
        perfilAtual.dataset.profileArea === area;
}

function exibirArea() {
    if (paineis.length === 0) {
        return;
    }

    const solicitada = location.hash.slice(1);
    const areaExiste = paineis.some(painel => painel.dataset.areaPanel === solicitada);
    const area = areaExiste && podeVisitar(solicitada)
        ? solicitada
        : perfilAtual.dataset.profileArea;

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
    document.querySelector("[data-profile-name]").textContent =
        perfilAtual.querySelector(".profile-details strong").textContent;
    document.querySelector("[data-profile-role]").textContent =
        perfilAtual.querySelector(".profile-details span").textContent;

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

    linkInicio.href = links.find(link => link.dataset.areaLink === perfilAtual.dataset.profileArea)
        .dataset.destination;
    exibirArea();
}

for (const link of links) {
    link.dataset.destination = link.getAttribute("href");
}

for (const perfil of perfis) {
    perfil.addEventListener("click", () => {
        perfilAtual = perfil;
        try {
            localStorage.setItem(chavePerfil, perfil.dataset.profileArea);
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
