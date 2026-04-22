const output = document.getElementById('output');
const userSelect = document.getElementById('userSelect');
const saldoValor = document.getElementById('saldoValor');
const gamesList = document.getElementById('gamesList');
const currentBetsList = document.getElementById('currentBetsList');
const oldBetsContainer = document.getElementById('oldBetsContainer');
const showOldBetsButton = document.getElementById('showOldBetsButton');
const oddTotal = document.getElementById('oddTotal');
const ganhosPotenciais = document.getElementById('ganhosPotenciais');
const stakeInput = document.getElementById('stakeInput');
const apostarButton = document.getElementById('apostarButton');
const depositoInput = document.getElementById('depositoInput');

const sideMenu = document.getElementById('sideMenu');
const overlay = document.getElementById('overlay');
const menuToggle = document.getElementById('menuToggle');
const closeMenu = document.getElementById('closeMenu');
const selectedUserStorageKey = 'betstrike-selected-user-id';
const userNamesStorageKey = 'betstrike-user-names';

const stateLabels = {
    1: 'Agendado',
    2: 'A Decorrer',
    3: 'Finalizado',
    4: 'Cancelado',
    5: 'Adiado'
};

const teamRank = [
    'FC Porto', 'SL Benfica', 'Sporting CP',
    'SC Braga', 'FC Famalicão', 'Gil Vicente FC',
    'Moreirense FC', 'Vitória SC', 'Estoril Praia',
    'FC Arouca', 'FC Alverca', 'Rio Ave FC',
    'Santa Clara', 'CD Nacional', 'Estrela da Amadora',
    'Casa Pia AC', 'CD Tondela', 'AFS'
];

const tierStrength = [1.3, 1.2, 1.1, 1.0, 0.9, 0.8];
const forceByTeam = new Map(teamRank.map((team, i) => [team, tierStrength[Math.min(Math.floor(i / 3), tierStrength.length - 1)]]));

let users = [];
let games = [];
const selectedByGame = new Map();
let refreshTimer;
let currentUserBets = [];
let oldUserBets = [];

function showOutput(data) {
    output.textContent = typeof data === 'string' ? data : JSON.stringify(data, null, 2);
}

function openSwaggerTabOnce() {
    const key = 'swagger-opened-apostas';
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

function getSavedUserNames() {
    try {
        const raw = localStorage.getItem(userNamesStorageKey);
        const parsed = raw ? JSON.parse(raw) : {};
        return parsed && typeof parsed === 'object' ? parsed : {};
    } catch {
        return {};
    }
}

function saveUserName(userId, nome) {
    if (!userId || !nome) {
        return;
    }

    const names = getSavedUserNames();
    names[String(userId)] = nome;
    localStorage.setItem(userNamesStorageKey, JSON.stringify(names));
}

function getDisplayName(user) {
    const names = getSavedUserNames();
    const fromApi = user.nome && String(user.nome).trim();
    const fromLocal = names[String(user.utilizadorId)] && String(names[String(user.utilizadorId)]).trim();

    return fromApi || fromLocal || `Utilizador ${user.utilizadorId}`;
}

function saveSelectedUserId(userId) {
    if (!userId) {
        localStorage.removeItem(selectedUserStorageKey);
        return;
    }

    localStorage.setItem(selectedUserStorageKey, String(userId));
}

function getSavedSelectedUserId() {
    const saved = localStorage.getItem(selectedUserStorageKey);
    const value = Number(saved);
    return Number.isFinite(value) && value > 0 ? value : null;
}

function getSelectedUserId() {
    const value = Number(userSelect.value);
    return Number.isFinite(value) && value > 0 ? value : null;
}

function getForce(team) {
    return forceByTeam.get(team) ?? 1.0;
}

function formatEuro(value) {
    return `${Number(value).toFixed(2)}€`;
}

function formatDate(value) {
    return new Date(value).toLocaleString('pt-PT');
}

function parseJsonSafe(text) {
    try {
        return text ? JSON.parse(text) : {};
    } catch {
        return text;
    }
}

async function readResponse(response) {
    const text = await response.text();
    const data = parseJsonSafe(text);

    if (!response.ok) {
        const message = data?.mensagem || JSON.stringify(data);
        throw new Error(`HTTP ${response.status} - ${message}`);
    }

    return data;
}

function calculateOdds(game) {
    const homeForce = getForce(game.equipaCasa);
    const awayForce = getForce(game.equipaFora);
    const homeBonus = 1.12;

    let pHome = 0.40 * Math.pow(homeForce / awayForce, 0.55) * homeBonus;
    let pAway = 0.31 * Math.pow(awayForce / homeForce, 0.55);
    let pDraw = 0.29;

    const scoreDiff = game.golosCasa - game.golosFora;
    const minute = Math.max(0, Math.min(90, game.minutoJogo || 0));
    const progress = minute / 90;

    if (game.estado === 2) {
        if (scoreDiff > 0) {
            pHome *= 1 + scoreDiff * (0.20 + 0.48 * progress);
            pAway *= Math.max(0.35, 1 - scoreDiff * (0.22 + 0.36 * progress));
            pDraw *= Math.max(0.30, 1 - scoreDiff * (0.32 + 0.45 * progress));
        } else if (scoreDiff < 0) {
            const diff = Math.abs(scoreDiff);
            pAway *= 1 + diff * (0.20 + 0.48 * progress);
            pHome *= Math.max(0.35, 1 - diff * (0.22 + 0.36 * progress));
            pDraw *= Math.max(0.30, 1 - diff * (0.32 + 0.45 * progress));
        } else {
            pDraw *= 1 + 0.16 * progress;
        }
    }

    const minProb = 0.05;
    pHome = Math.max(minProb, pHome);
    pAway = Math.max(minProb, pAway);
    pDraw = Math.max(minProb, pDraw);

    const sum = pHome + pAway + pDraw;
    pHome /= sum;
    pAway /= sum;
    pDraw /= sum;

    const margin = 1.08;

    return {
        '1': Math.max(1.05, Math.min(15, Number((1 / (pHome * margin)).toFixed(2)))),
        'X': Math.max(1.05, Math.min(15, Number((1 / (pDraw * margin)).toFixed(2)))),
        '2': Math.max(1.05, Math.min(15, Number((1 / (pAway * margin)).toFixed(2))))
    };
}

function recalcSlip() {
    const stake = Number(stakeInput.value);
    const selectedEntry = selectedByGame.values().next().value;

    if (!Number.isFinite(stake) || stake <= 0 || !selectedEntry) {
        oddTotal.textContent = '1.00';
        ganhosPotenciais.textContent = '0.00€';
        apostarButton.disabled = true;
        return;
    }

    const oddCombinada = selectedEntry.odd;
    const potencial = stake * oddCombinada;

    oddTotal.textContent = oddCombinada.toFixed(2);
    ganhosPotenciais.textContent = formatEuro(potencial);
    apostarButton.disabled = false;
}

function renderGames() {
    gamesList.innerHTML = '';

    if (!Array.isArray(games) || games.length === 0) {
        gamesList.innerHTML = '<article class="game-block"><div class="score-card"><div class="team">Sem jogos disponíveis para aposta.</div></div></article>';
        recalcSlip();
        return;
    }

    for (const game of games) {
        const odds = calculateOdds(game);
        const selected = selectedByGame.get(game.id);

        const block = document.createElement('article');
        block.className = 'game-block';
        block.innerHTML = `
            <div class="game-meta">
                <span>${game.codigo}</span>
                <span>Jornada ${game.jornada ?? '-'}</span>
                <span>Estado: ${stateLabels[game.estado] ?? game.estado}</span>
                <span>Início ${formatDate(game.dataHoraInicio)}</span>
            </div>
            <div class="score-card">
                <div class="team">${game.equipaCasa}</div>
                <div class="score"><span>${game.golosCasa}</span><span class="dash">-</span><span>${game.golosFora}</span></div>
                <div class="team">${game.equipaFora}</div>
            </div>
            <div class="odds-row">
                <button type="button" class="odd-btn ${selected?.tipo === '1' ? 'active' : ''}" data-game-id="${game.id}" data-tipo="1" data-odd="${odds['1']}">
                    <span>1</span><span>${odds['1'].toFixed(2)}</span>
                </button>
                <button type="button" class="odd-btn ${selected?.tipo === 'X' ? 'active' : ''}" data-game-id="${game.id}" data-tipo="X" data-odd="${odds['X']}">
                    <span>X</span><span>${odds['X'].toFixed(2)}</span>
                </button>
                <button type="button" class="odd-btn ${selected?.tipo === '2' ? 'active' : ''}" data-game-id="${game.id}" data-tipo="2" data-odd="${odds['2']}">
                    <span>2</span><span>${odds['2'].toFixed(2)}</span>
                </button>
            </div>
        `;

        gamesList.appendChild(block);
    }

    document.querySelectorAll('.odd-btn').forEach((btn) => {
        btn.addEventListener('click', () => {
            const gameId = Number(btn.dataset.gameId);
            const tipo = btn.dataset.tipo;
            const odd = Number(btn.dataset.odd);

            const current = selectedByGame.get(gameId);
            if (current?.tipo === tipo) {
                selectedByGame.clear();
            } else {
                selectedByGame.clear();
                selectedByGame.set(gameId, { jogoId: gameId, tipo, odd });
            }

            renderGames();
            recalcSlip();
        });
    });

    recalcSlip();
}

function renderBets(container, bets, emptyText = null) {
    if (!container) {
        return;
    }

    container.innerHTML = '';

    if (!Array.isArray(bets) || bets.length === 0) {
        if (emptyText) {
            container.innerHTML = `<div class="bet-item">${emptyText}</div>`;
        }
        return;
    }

    const pickLabel = {
        '1': 'Vitória Casa',
        'X': 'Empate',
        '2': 'Vitória Fora'
    };

    for (const b of bets) {
        const equipasFromApi = b.equipaCasa && b.equipaFora
            ? `${b.equipaCasa} vs ${b.equipaFora}`
            : null;

        const jogo = equipasFromApi
            ? null
            : (Array.isArray(games) ? games : []).find((g) => g.codigo === b.codigoJogo);

        const equipasTexto = equipasFromApi
            || (jogo ? `${jogo.equipaCasa} vs ${jogo.equipaFora}` : `Jogo ${b.codigoJogo || b.jogoId}`);

        const estadoTexto = b.estadoDescricao || stateLabels[b.estado] || b.estado;
        const tipoTexto = pickLabel[b.tipo] ?? b.tipo;

        const el = document.createElement('div');
        el.className = 'bet-item';
        el.innerHTML = `
            <div class="bet-head">Jogo ${b.codigoJogo || b.jogoId} | ${new Date(b.dataHora).toLocaleString('pt-PT')}</div>
            <div class="bet-line-main">
                <span class="bet-teams">${equipasTexto}</span>
                <span class="bet-sep">|</span>
                <span class="bet-pick">${tipoTexto}</span>
            </div>
            <div class="bet-line"><span class="bet-label">Estado:</span> <span class="bet-value">${estadoTexto}</span></div>
            <div class="bet-line">
                <span class="bet-label">Montante:</span> <span class="bet-value">${Number(b.montante).toFixed(2)}€</span>
                <span class="bet-sep">|</span>
                <span class="bet-label">Odd:</span> <span class="bet-value">${Number(b.odd).toFixed(2)}</span>
            </div>
            <div class="bet-line-win"><span class="bet-label-win">Ganhos Potenciais:</span> <span class="bet-value">${(Number(b.montante) * Number(b.odd)).toFixed(2)}€</span></div>
        `;
        container.appendChild(el);
    }
}

function updateOldBetsVisibility(show) {
    if (!oldBetsContainer || !showOldBetsButton) {
        return;
    }

    oldBetsContainer.classList.toggle('hidden', !show);
    showOldBetsButton.textContent = show ? 'Ocultar apostas antigas' : 'Ver apostas antigas';
}

async function loadUsers() {
    const response = await fetch('/api/utilizadores');
    users = await readResponse(response);

    userSelect.innerHTML = '';

    for (const user of users) {
        if (user.nome) {
            saveUserName(user.utilizadorId, user.nome);
        }

        const option = document.createElement('option');
        option.value = user.utilizadorId;
        option.textContent = getDisplayName(user);
        userSelect.appendChild(option);
    }

    if (users.length === 0) {
        const option = document.createElement('option');
        option.textContent = 'Sem utilizadores';
        option.value = '';
        userSelect.appendChild(option);
        saldoValor.textContent = '0.00€';
        saveSelectedUserId(null);
        return;
    }

    const savedUserId = getSavedSelectedUserId();
    const savedExists = savedUserId && users.some((u) => u.utilizadorId === savedUserId);

    if (savedExists) {
        userSelect.value = String(savedUserId);
    } else if (!userSelect.value) {
        userSelect.value = String(users[0].utilizadorId);
    }

    saveSelectedUserId(getSelectedUserId());
    updateSelectedUserInfo();
}

function updateSelectedUserInfo() {
    const id = getSelectedUserId();
    const user = users.find((u) => u.utilizadorId === id);
    saldoValor.textContent = user ? formatEuro(user.saldoAtual) : '0.00€';
}

async function loadGames() {
    const response = await fetch('/api/jogos/disponiveis-aposta');
    games = await readResponse(response);

    const validGameIds = new Set(games.map((g) => g.id));
    for (const gameId of [...selectedByGame.keys()]) {
        if (!validGameIds.has(gameId)) {
            selectedByGame.delete(gameId);
        }
    }

    renderGames();
}

async function loadUserBets() {
    const userId = getSelectedUserId();

    if (!currentBetsList || !oldBetsContainer || !showOldBetsButton) {
        return;
    }

    currentBetsList.innerHTML = '';
    oldBetsContainer.innerHTML = '';
    updateOldBetsVisibility(false);

    if (!userId) {
        currentBetsList.innerHTML = '<div class="bet-item">Seleciona um utilizador.</div>';
        return;
    }

    const response = await fetch(`/api/apostas?utilizadorId=${userId}`);
    const bets = await readResponse(response);

    if (!Array.isArray(bets) || bets.length === 0) {
        currentBetsList.innerHTML = '<div class="bet-item">Sem apostas para este utilizador.</div>';
        return;
    }

    const jornadasAtivas = (Array.isArray(games) ? games : [])
        .map((g) => Number(g.jornada) || 0)
        .filter((j) => j > 0);

    const jornadaAtual = jornadasAtivas.length > 0 ? Math.max(...jornadasAtivas) : 0;

    if (jornadaAtual > 0) {
        currentUserBets = bets.filter((b) => Number(b.jornada) === jornadaAtual);
        oldUserBets = bets.filter((b) => Number(b.jornada) > 0 && Number(b.jornada) < jornadaAtual);

        const semJornada = bets.filter((b) => !Number.isFinite(Number(b.jornada)) || Number(b.jornada) <= 0);
        if (semJornada.length > 0) {
            const activeGameCodes = new Set(
                (Array.isArray(games) ? games : [])
                    .filter((g) => g.estado === 1 || g.estado === 2)
                    .map((g) => g.codigo)
            );

            currentUserBets.push(...semJornada.filter((b) => activeGameCodes.has(b.codigoJogo)));
            oldUserBets.push(...semJornada.filter((b) => !activeGameCodes.has(b.codigoJogo)));
        }
    } else {
        const activeGameCodes = new Set(
            (Array.isArray(games) ? games : [])
                .filter((g) => g.estado === 1 || g.estado === 2)
                .map((g) => g.codigo)
        );

        currentUserBets = bets.filter((b) => activeGameCodes.has(b.codigoJogo));
        oldUserBets = bets.filter((b) => !activeGameCodes.has(b.codigoJogo));
    }

    renderBets(currentBetsList, currentUserBets, 'Sem apostas da jornada atual.');
    renderBets(oldBetsContainer, oldUserBets);

    oldBetsContainer.classList.add('hidden');
    showOldBetsButton.textContent = 'Ver apostas antigas';
}

async function createUser() {
    const nome = prompt('Nome do novo utilizador:');
    if (!nome) {
        return;
    }

    const response = await fetch('/api/utilizadores/registar', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ nome })
    });

    const data = await readResponse(response);
    showOutput(data);

    saveUserName(data.id, nome);

    await loadUsers();
    userSelect.value = String(data.id);
    saveSelectedUserId(data.id);
    updateSelectedUserInfo();
    await loadUserBets();
}

async function depositForSelectedUser() {
    const userId = getSelectedUserId();
    const valor = Number(depositoInput.value);

    if (!userId) {
        throw new Error('Seleciona um utilizador primeiro.');
    }

    if (!Number.isFinite(valor) || valor <= 0) {
        throw new Error('Valor de depósito inválido.');
    }

    const response = await fetch(`/api/utilizadores/${userId}/deposito`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(valor)
    });

    const data = await readResponse(response);
    showOutput(data);

    depositoInput.value = '';
    await loadUsers();
    userSelect.value = String(userId);
    saveSelectedUserId(userId);
    updateSelectedUserInfo();
}

async function placeBets() {
    const userId = getSelectedUserId();
    const stake = Number(stakeInput.value);
    const pick = selectedByGame.values().next().value;

    if (!userId) {
        throw new Error('Seleciona um utilizador antes de apostar.');
    }

    if (!Number.isFinite(stake) || stake <= 0) {
        throw new Error('Montante inválido.');
    }

    if (!pick) {
        throw new Error('Seleciona um resultado de um jogo para apostar.');
    }

    const response = await fetch('/api/apostas', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            jogoId: pick.jogoId,
            utilizadorId: userId,
            tipo: pick.tipo,
            montante: stake,
            odd: pick.odd
        })
    });

    const data = await readResponse(response);

    selectedByGame.clear();
    renderGames();

    showOutput(`Jogo ${pick.jogoId}: ${data.mensagem ?? 'Aposta registada.'}`);
    await loadUsers();
    userSelect.value = String(userId);
    saveSelectedUserId(userId);
    updateSelectedUserInfo();
    await loadUserBets();
}

function startAutoRefresh() {
    clearInterval(refreshTimer);
    refreshTimer = setInterval(async () => {
        try {
            await loadGames();
            await loadUsers();
            await loadUserBets();
        } catch {
        }
    }, 7000);
}

menuToggle.addEventListener('click', async () => {
    showMenu(true);
    try {
        await loadUserBets();
    } catch (err) {
        showOutput(err.message);
    }
});

closeMenu.addEventListener('click', () => showMenu(false));
overlay.addEventListener('click', () => showMenu(false));

window.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        showMenu(false);
    }
});

userSelect.addEventListener('change', async () => {
    updateSelectedUserInfo();
    saveSelectedUserId(getSelectedUserId());
    try {
        await loadUserBets();
    } catch (err) {
        showOutput(err.message);
    }
});

stakeInput.addEventListener('input', recalcSlip);

document.getElementById('newUserButton').addEventListener('click', async () => {
    try {
        await createUser();
    } catch (err) {
        showOutput(err.message);
    }
});

document.getElementById('depositoButton').addEventListener('click', async () => {
    try {
        await depositForSelectedUser();
    } catch (err) {
        showOutput(err.message);
    }
});

if (showOldBetsButton) {
    showOldBetsButton.addEventListener('click', () => {
        const isHidden = oldBetsContainer.classList.contains('hidden');
        updateOldBetsVisibility(isHidden);
    });
}

apostarButton.addEventListener('click', async () => {
    try {
        await placeBets();
    } catch (err) {
        showOutput(err.message);
    }
});

(async function init() {
    try {
        await loadUsers();
        await loadGames();
        await loadUserBets();
        startAutoRefresh();
        recalcSlip();
        openSwaggerTabOnce();
    } catch (err) {
        showOutput(err.message);
    }
})();