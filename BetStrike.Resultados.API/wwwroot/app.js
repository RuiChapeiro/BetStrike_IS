const apiBase = '/api/jogos';
const output = document.getElementById('output');
const resultsList = document.getElementById('resultsList');
const sideMenu = document.getElementById('sideMenu');
const overlay = document.getElementById('overlay');
const menuToggle = document.getElementById('menuToggle');
const closeMenu = document.getElementById('closeMenu');
const refreshButton = document.getElementById('refreshButton');

const stateLabels = {
    1: '1-Agendado',
    2: '2-Em Curso',
    3: '3-Finalizado',
    4: '4-Cancelado',
    5: '5-Adiado'
};

let refreshTimer = null;

function showOutput(data) {
    output.textContent = typeof data === 'string' ? data : JSON.stringify(data, null, 2);
}

function openSwaggerTabOnce() {
    const key = 'swagger-opened-resultados';
    if (sessionStorage.getItem(key) === '1') {
        return;
    }

    const popup = window.open('/swagger', '_blank');
    if (popup) {
        sessionStorage.setItem(key, '1');
    }
}

function showMenu(open) {
    sideMenu.classList.toggle('open', open);
    overlay.classList.toggle('hidden', !open);
}

function formatDateTime(value) {
    if (!value) {
        return '-';
    }

    return new Date(value).toLocaleString('pt-PT');
}

function getDateTimeIso(value) {
    if (!value) {
        return null;
    }

    return new Date(value).toISOString();
}

async function readResponse(response) {
    const text = await response.text();
    let payload = text;

    try {
        payload = text ? JSON.parse(text) : {};
    } catch {
        payload = text;
    }

    if (!response.ok) {
        const message = payload?.mensagem || JSON.stringify(payload);
        throw new Error(`HTTP ${response.status} - ${message}`);
    }

    return payload;
}

function renderResults(jogos) {
    resultsList.innerHTML = '';

    if (!Array.isArray(jogos) || jogos.length === 0) {
        resultsList.innerHTML = '<div class="results-empty">Sem resultados disponíveis.</div>';
        return;
    }

    for (const jogo of jogos) {
        const item = document.createElement('article');
        item.className = 'result-item';

        item.innerHTML = `
            <div class="result-meta">
                <span>${jogo.codigo_Jogo}</span>
                <span>Estado ${stateLabels[jogo.estado] ?? jogo.estado}</span>
                <span>Início ${formatDateTime(jogo.dataHoraInicio)}</span>
            </div>
            <div class="result-card">
                <div class="team">${jogo.equipaCasa}</div>
                <div class="score"><span>${jogo.golosCasa}</span><span class="dash">-</span><span>${jogo.golosFora}</span></div>
                <div class="team">${jogo.equipaFora}</div>
            </div>
        `;

        resultsList.appendChild(item);
    }
}

async function carregarResultados() {
    try {
        const response = await fetch(apiBase);
        const jogos = await readResponse(response);
        renderResults(jogos);
    } catch (error) {
        resultsList.innerHTML = '<div class="results-empty">Não foi possível carregar os resultados.</div>';
        showOutput(error.message);
    }
}

async function listarJogos(data = '', estado = '') {
    const params = new URLSearchParams();

    if (data) {
        params.append('data', data);
    }

    if (estado) {
        params.append('estado', estado);
    }

    const url = params.toString() ? `${apiBase}?${params.toString()}` : apiBase;

    try {
        const response = await fetch(url);
        const jogos = await readResponse(response);
        renderResults(jogos);
    } catch (error) {
        showOutput(error.message);
    }
}

function startAutoRefresh() {
    if (refreshTimer) {
        clearInterval(refreshTimer);
    }

    refreshTimer = setInterval(carregarResultados, 5000);
}

menuToggle.addEventListener('click', () => showMenu(true));
closeMenu.addEventListener('click', () => showMenu(false));
overlay.addEventListener('click', () => showMenu(false));
refreshButton.addEventListener('click', carregarResultados);

window.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
        showMenu(false);
    }
});

document.getElementById('formCriar').addEventListener('submit', async (e) => {
    e.preventDefault();

    const f = new FormData(e.target);
    const jogo = {
        codigo_Jogo: f.get('codigo_Jogo'),
        dataHoraInicio: getDateTimeIso(f.get('dataHoraInicio')),
        equipaCasa: f.get('equipaCasa'),
        equipaFora: f.get('equipaFora'),
        golosCasa: 0,
        golosFora: 0,
        estado: Number(f.get('estado'))
    };

    try {
        const response = await fetch(apiBase, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(jogo)
        });

        const data = await readResponse(response);
        showOutput(data);
        await carregarResultados();
        e.target.reset();
    } catch (error) {
        showOutput(error.message);
    }
});

document.getElementById('formAtualizar').addEventListener('submit', async (e) => {
    e.preventDefault();

    const f = new FormData(e.target);
    const codigo = f.get('codigo');

    const jogo = {
        codigo_Jogo: codigo,
        dataHoraInicio: getDateTimeIso(f.get('dataHoraInicio')),
        equipaCasa: f.get('equipaCasa'),
        equipaFora: f.get('equipaFora'),
        golosCasa: Number(f.get('golosCasa')),
        golosFora: Number(f.get('golosFora')),
        estado: Number(f.get('estado'))
    };

    try {
        const response = await fetch(`${apiBase}/${encodeURIComponent(codigo)}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(jogo)
        });

        const data = await readResponse(response);
        showOutput(data);
        await carregarResultados();
    } catch (error) {
        showOutput(error.message);
    }
});

document.getElementById('formObterUm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const codigo = new FormData(e.target).get('codigo');

    try {
        const response = await fetch(`${apiBase}/${encodeURIComponent(codigo)}`);
        const data = await readResponse(response);
        document.getElementById('jogoUnico').textContent = JSON.stringify(data, null, 2);
        showOutput(data);
    } catch (error) {
        document.getElementById('jogoUnico').textContent = '';
        showOutput(error.message);
    }
});

document.getElementById('formRemover').addEventListener('submit', async (e) => {
    e.preventDefault();

    const codigo = new FormData(e.target).get('codigo');

    try {
        const response = await fetch(`${apiBase}/${encodeURIComponent(codigo)}`, {
            method: 'DELETE'
        });

        if (response.status === 204) {
            showOutput(`Jogo '${codigo}' removido com sucesso.`);
        } else {
            const data = await readResponse(response);
            showOutput(data);
        }

        await carregarResultados();
        e.target.reset();
    } catch (error) {
        showOutput(error.message);
    }
});

document.getElementById('formFiltros').addEventListener('submit', async (e) => {
    e.preventDefault();
    const f = new FormData(e.target);
    await listarJogos(f.get('data'), f.get('estado'));
});

document.getElementById('btnListarTodos').addEventListener('click', async () => {
    await carregarResultados();
});

carregarResultados();
startAutoRefresh();
openSwaggerTabOnce();