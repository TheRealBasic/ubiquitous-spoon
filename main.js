// Neon Groove Manager main script
// Major systems:
// - Game state and constants: configuration, item definitions, save/load
// - Rendering: grid tiles, placed objects, customers and staff
// - Customer logic: spawning, movement, satisfaction targeting
// - Staff logic: passive effects on income, capacity, satisfaction
// - Economy: income ticks, XP/leveling, open/close operations
// - Build mode: placement, selection, moving, selling
// - Saving/loading via localStorage

const GRID_W = 12;
const GRID_H = 8;
const SAVE_KEY = 'neon-groove-save';
const STARTING_MONEY = 500;
const ENTRANCE_POS = { x: 0, y: Math.floor(GRID_H / 2) };

const shopItems = [
  { id: 'bar', name: 'Bar Counter', price: 200, category: 'furniture', type: 'bar', unlockLevel: 1, description: 'Customers buy drinks here. Bartenders boost it.' },
  { id: 'table', name: 'Lounge Table', price: 120, category: 'furniture', type: 'table', unlockLevel: 1, description: 'Seating makes guests stay longer.' },
  { id: 'dance', name: 'Dance Floor', price: 150, category: 'furniture', type: 'dance', unlockLevel: 1, description: 'Guests dance here. DJs improve satisfaction.' },
  { id: 'light', name: 'Light Rig', price: 90, category: 'decor', type: 'light', unlockLevel: 2, description: 'Boosts ambiance and small XP bonus.' },
  { id: 'speaker', name: 'Speaker Stack', price: 180, category: 'decor', type: 'speaker', unlockLevel: 2, description: 'Raises music quality; helps dance floor income.' },
  { id: 'vip', name: 'VIP Table', price: 300, category: 'furniture', type: 'vip', unlockLevel: 3, description: 'High spenders love this.' },
  { id: 'stage', name: 'Mini Stage', price: 260, category: 'decor', type: 'stage', unlockLevel: 3, description: 'Guests cheer; better tips at night.' },
  { id: 'bartender', name: 'Bartender', price: 180, category: 'staff', type: 'bartender', unlockLevel: 1, description: 'Increases bar efficiency.' },
  { id: 'dj', name: 'DJ', price: 220, category: 'staff', type: 'dj', unlockLevel: 2, description: 'Keeps the dance floor vibrant.' },
  { id: 'bouncer', name: 'Bouncer', price: 160, category: 'staff', type: 'bouncer', unlockLevel: 1, description: 'Controls overcrowding and keeps order.' },
];

const state = {
  money: STARTING_MONEY,
  xp: 0,
  level: 1,
  open: false,
  mode: 'live',
  timeMinutes: 18 * 60,
  objects: [], // placed objects on grid
  customers: [],
  selectedShopItem: null,
  selectedObjectId: null,
  pendingMove: false,
};

const dom = {};
let tiles = [];
let cashSound;
let clickSound;

function initAudio() {
  const cashData = 'data:audio/wav;base64,UklGRiQAAABXQVZFZm10IBAAAAABAAEAESsAACJWAAACABAAZGF0YQAAAAA='; // tiny silent header
  const clickData = 'data:audio/wav;base64,UklGRiQAAABXQVZFZm10IBAAAAABAAEAESsAACJWAAACABAAZGF0YQAAAAA=';
  cashSound = document.getElementById('cash-sound');
  clickSound = document.getElementById('click-sound');
  cashSound.src = cashData;
  clickSound.src = clickData;
  // Add quick oscillator feedback for better audibility
  const playOsc = (freq, duration) => {
    const ctx = new (window.AudioContext || window.webkitAudioContext)();
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.frequency.value = freq;
    osc.connect(gain);
    gain.connect(ctx.destination);
    gain.gain.setValueAtTime(0.08, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + duration);
    osc.start();
    osc.stop(ctx.currentTime + duration);
  };
  cashSound.playTone = () => playOsc(620, 0.2);
  clickSound.playTone = () => playOsc(320, 0.1);
}

function levelXpNeeded(level) {
  return 200 + (level - 1) * 120;
}

function saveGame() {
  const save = {
    money: state.money,
    xp: state.xp,
    level: state.level,
    open: state.open,
    mode: state.mode,
    timeMinutes: state.timeMinutes,
    objects: state.objects,
  };
  localStorage.setItem(SAVE_KEY, JSON.stringify(save));
}

function loadGame() {
  const data = localStorage.getItem(SAVE_KEY);
  if (!data) {
    placeInitialObjects();
    return;
  }
  try {
    const save = JSON.parse(data);
    Object.assign(state, save);
  } catch (e) {
    console.warn('Failed to load save', e);
    placeInitialObjects();
  }
}

function placeInitialObjects() {
  state.objects = [
    { id: 'door', name: 'Entrance', type: 'door', category: 'decor', x: ENTRANCE_POS.x, y: ENTRANCE_POS.y, price: 0 },
    { id: 'bar0', name: 'Starter Bar', type: 'bar', category: 'furniture', x: 2, y: ENTRANCE_POS.y - 1, price: 0 },
    { id: 'table0', name: 'Starter Table', type: 'table', category: 'furniture', x: 3, y: ENTRANCE_POS.y + 1, price: 0 },
    { id: 'dance0', name: 'Starter Dance', type: 'dance', category: 'furniture', x: 5, y: ENTRANCE_POS.y, price: 0 },
  ];
}

function createGrid() {
  const grid = document.getElementById('grid-container');
  grid.style.gridTemplateColumns = `repeat(${GRID_W}, 1fr)`;
  grid.style.gridTemplateRows = `repeat(${GRID_H}, 1fr)`;
  tiles = [];
  for (let y = 0; y < GRID_H; y++) {
    const row = [];
    for (let x = 0; x < GRID_W; x++) {
      const tile = document.createElement('div');
      tile.className = 'tile';
      tile.dataset.x = x;
      tile.dataset.y = y;
      tile.addEventListener('click', onTileClick);
      row.push(tile);
      grid.appendChild(tile);
    }
    tiles.push(row);
  }
}

function updateTopBar() {
  dom.money.textContent = `$${state.money.toFixed(0)}`;
  dom.level.textContent = state.level;
  dom.xp.textContent = state.xp.toFixed(0);
  const needed = levelXpNeeded(state.level);
  const percent = Math.min(100, (state.xp / needed) * 100);
  dom.xpBar.style.width = `${percent}%`;
  const hours = Math.floor(state.timeMinutes / 60) % 24;
  const minutes = state.timeMinutes % 60;
  dom.time.textContent = `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`;
  const night = isNight();
  dom.timePhase.textContent = night ? 'Night' : 'Day';
  dom.clubStatus.textContent = state.open ? 'Open' : 'Closed';
  dom.clubStatus.className = state.open ? 'open' : 'closed';
  dom.toggleOpen.textContent = state.open ? 'Close Club' : 'Open Club';
  dom.toggleMode.textContent = state.mode === 'live' ? 'Switch to Build Mode' : 'Switch to Live Mode';
}

function renderObjects() {
  tiles.flat().forEach(tile => tile.innerHTML = '');
  state.objects.forEach(obj => {
    const tile = tiles[obj.y]?.[obj.x];
    if (!tile) return;
    const el = document.createElement('div');
    el.className = `object ${obj.type}${obj.category === 'staff' ? ' staff' : ''}`;
    el.textContent = obj.name;
    tile.appendChild(el);
    if (obj.id === state.selectedObjectId) {
      tile.classList.add('highlight');
    }
  });
}

function renderCustomers() {
  state.customers.forEach(c => {
    const tile = tiles[c.y]?.[c.x];
    if (!tile) return;
    const el = document.createElement('div');
    el.className = 'character customer';
    tile.appendChild(el);
  });
}

function clearHighlights() {
  tiles.flat().forEach(t => t.classList.remove('highlight'));
}

function render() {
  clearHighlights();
  renderObjects();
  renderCustomers();
}

function getObjectAt(x, y) {
  return state.objects.find(o => o.x === x && o.y === y);
}

function onTileClick(e) {
  const x = Number(e.currentTarget.dataset.x);
  const y = Number(e.currentTarget.dataset.y);
  if (state.mode !== 'build') return;
  const existing = getObjectAt(x, y);
  if (state.pendingMove && state.selectedObjectId) {
    if (existing) {
      addLog('Tile is occupied.');
      return;
    }
    const obj = state.objects.find(o => o.id === state.selectedObjectId);
    if (obj) {
      obj.x = x;
      obj.y = y;
      state.pendingMove = false;
      state.selectedObjectId = obj.id;
      playClick();
      addLog(`Moved ${obj.name}.`);
      saveGame();
      render();
    }
    return;
  }
  if (state.selectedShopItem) {
    attemptPlace(x, y);
    return;
  }
  if (existing) {
    state.selectedObjectId = existing.id;
    dom.selectedInfo.textContent = `${existing.name} (${existing.type})`;
    render();
  }
}

function attemptPlace(x, y) {
  const item = state.selectedShopItem;
  if (!item) return;
  const existing = getObjectAt(x, y);
  if (existing) {
    addLog('Tile is occupied.');
    return;
  }
  if (item.type !== 'door' && x === ENTRANCE_POS.x && y === ENTRANCE_POS.y) {
    addLog('Entrance cannot be blocked.');
    return;
  }
  if (state.money < item.price) {
    addLog('Not enough money.');
    return;
  }
  if (state.level < item.unlockLevel) {
    addLog('Reach a higher level to unlock this.');
    return;
  }
  const id = `${item.id}-${Date.now()}`;
  const obj = { id, name: item.name, type: item.type, category: item.category, x, y, price: item.price };
  state.objects.push(obj);
  state.money -= item.price;
  state.selectedObjectId = obj.id;
  playClick();
  addLog(`Placed ${item.name}.`);
  saveGame();
  render();
  updateTopBar();
}

function setShopCategory(cat) {
  document.querySelectorAll('#category-buttons button').forEach(btn => {
    btn.classList.toggle('active', btn.dataset.category === cat);
  });
  const container = document.getElementById('shop-items');
  container.innerHTML = '';
  shopItems.filter(i => i.category === cat).forEach(item => {
    const el = document.createElement('div');
    el.className = 'shop-item';
    if (state.level < item.unlockLevel) el.classList.add('locked');
    el.innerHTML = `<div>${item.name}</div><div class="price">$${item.price}</div><div class="small">Lvl ${item.unlockLevel} • ${item.description}</div>`;
    el.addEventListener('mouseenter', (ev) => showTooltip(ev, item.description));
    el.addEventListener('mouseleave', hideTooltip);
    el.addEventListener('click', () => {
      if (state.level < item.unlockLevel) { addLog('Item locked. Level up!'); return; }
      state.selectedShopItem = item;
      state.pendingMove = false;
      state.selectedObjectId = null;
      dom.selectedInfo.textContent = `Placing: ${item.name}`;
      addLog(`Selected ${item.name} for placement.`);
      playClick();
    });
    container.appendChild(el);
  });
}

function showTooltip(ev, text) {
  const tooltip = document.getElementById('tooltip');
  tooltip.textContent = text;
  tooltip.classList.remove('hidden');
  tooltip.style.left = ev.pageX + 10 + 'px';
  tooltip.style.top = ev.pageY + 10 + 'px';
}

function hideTooltip() {
  const tooltip = document.getElementById('tooltip');
  tooltip.classList.add('hidden');
}

function addLog(msg) {
  const log = document.getElementById('log');
  const entry = document.createElement('div');
  entry.className = 'log-entry';
  entry.textContent = `[${dom.time.textContent}] ${msg}`;
  log.prepend(entry);
  while (log.childElementCount > 40) log.removeChild(log.lastChild);
}

function toggleOpen() {
  state.open = !state.open;
  addLog(state.open ? 'Club opened. Guests are arriving!' : 'Club closed. Guests are leaving.');
  if (!state.open) state.customers = [];
  updateTopBar();
  saveGame();
}

function toggleMode() {
  state.mode = state.mode === 'live' ? 'build' : 'live';
  state.selectedShopItem = null;
  state.pendingMove = false;
  dom.selectedInfo.textContent = state.mode === 'build' ? 'Build mode active' : 'Live mode active';
  addLog(state.mode === 'build' ? 'Build mode: place or move items.' : 'Live mode: running club.');
  updateTopBar();
  render();
}

function sellSelected() {
  if (!state.selectedObjectId) return;
  const objIndex = state.objects.findIndex(o => o.id === state.selectedObjectId);
  if (objIndex === -1) return;
  const obj = state.objects[objIndex];
  if (obj.id === 'door') { addLog('Cannot sell the entrance.'); return; }
  const refund = Math.floor(obj.price * 0.65);
  state.money += refund;
  state.objects.splice(objIndex, 1);
  state.selectedObjectId = null;
  dom.selectedInfo.textContent = 'None';
  addLog(`Sold ${obj.name} for $${refund}.`);
  saveGame();
  updateTopBar();
  render();
}

function moveSelected() {
  if (!state.selectedObjectId) return;
  state.pendingMove = true;
  addLog('Select an empty tile to move.');
}

function openReset() {
  if (!confirm('Start a new club? This clears your save.')) return;
  localStorage.removeItem(SAVE_KEY);
  location.reload();
}

function isNight() {
  const hours = Math.floor(state.timeMinutes / 60) % 24;
  return hours >= 18 || hours < 6;
}

function tickTime() {
  state.timeMinutes = (state.timeMinutes + 5) % (24 * 60);
  const night = isNight();
  tiles.flat().forEach(tile => tile.classList.toggle('night', night));
}

function spawnCustomer() {
  if (!state.open) return;
  const capacity = 15 + getStaffCount('bouncer') * 5;
  if (state.customers.length >= capacity) return;
  const baseChance = isNight() ? 0.7 : 0.25;
  if (Math.random() > baseChance) return;
  const customer = { x: ENTRANCE_POS.x, y: ENTRANCE_POS.y, state: 'wandering', stayTimer: 0, targetType: null };
  state.customers.push(customer);
  addLog('New customer entered.');
}

function getStaffCount(type) {
  return state.objects.filter(o => o.category === 'staff' && o.type === type).length;
}

function nearestObjectOf(type) {
  let best = null;
  let bestDist = Infinity;
  state.objects.forEach(o => {
    if (o.type !== type) return;
    const dist = Math.abs(o.x - ENTRANCE_POS.x) + Math.abs(o.y - ENTRANCE_POS.y);
    if (dist < bestDist) { bestDist = dist; best = o; }
  });
  return best;
}

function pickTarget(customer) {
  const preferences = ['bar', 'dance', 'table', 'vip'];
  const choice = preferences[Math.floor(Math.random() * preferences.length)];
  const options = state.objects.filter(o => o.type === choice);
  if (!options.length) return null;
  const target = options[Math.floor(Math.random() * options.length)];
  customer.targetType = target.type;
  return target;
}

function stepCustomers() {
  const overcrowded = state.customers.length > (15 + getStaffCount('bouncer') * 5);
  state.customers = state.customers.filter(c => {
    if (!state.open && Math.random() < 0.3) return false;
    if (overcrowded && Math.random() < 0.1) return false;
    const target = pickTarget(c);
    if (!target) {
      if (Math.random() < 0.05) return false;
      return true;
    }
    if (c.x === target.x && c.y === target.y) {
      c.state = 'enjoy';
      c.stayTimer = 4 + Math.random() * 6;
    } else if (c.state !== 'enjoy') {
      const dx = Math.sign(target.x - c.x);
      const dy = Math.sign(target.y - c.y);
      if (Math.random() < 0.5) c.x += dx; else c.y += dy;
      c.x = Math.max(0, Math.min(GRID_W - 1, c.x));
      c.y = Math.max(0, Math.min(GRID_H - 1, c.y));
    }
    if (c.state === 'enjoy') {
      c.stayTimer -= 1;
      if (c.stayTimer <= 0) {
        c.state = 'wandering';
        if (Math.random() < 0.2) return false;
      }
    }
    return true;
  });
}

function incomeTick() {
  let bartenders = getStaffCount('bartender');
  let djs = getStaffCount('dj');
  let musicBoost = state.objects.filter(o => o.type === 'speaker').length * 0.05;
  let ambianceBoost = state.objects.filter(o => o.type === 'light').length * 0.02;
  let stageBoost = state.objects.filter(o => o.type === 'stage').length * (isNight() ? 0.08 : 0.03);
  state.customers.forEach(c => {
    const obj = getObjectAt(c.x, c.y);
    if (!obj) return;
    let income = 0;
    let xpGain = 0;
    if (obj.type === 'bar') {
      income = 8 + bartenders * 2;
      xpGain = 4 + ambianceBoost * 10;
    }
    if (obj.type === 'table') {
      income = 5;
      xpGain = 2;
    }
    if (obj.type === 'vip') {
      income = 12;
      xpGain = 6;
    }
    if (obj.type === 'dance') {
      income = 4 + djs * 2 + musicBoost * 5;
      xpGain = 4 + musicBoost * 8;
    }
    if (income > 0) {
      state.money += income;
      state.xp += xpGain;
      showFloating(`+$${income}`, c.x, c.y);
      playCash();
    }
  });
  gainPassiveXp(ambianceBoost + stageBoost);
  checkLevelUp();
  updateTopBar();
  saveGame();
}

function gainPassiveXp(amount) {
  if (amount <= 0) return;
  state.xp += amount * 2;
}

function checkLevelUp() {
  const needed = levelXpNeeded(state.level);
  if (state.xp >= needed) {
    state.xp -= needed;
    state.level += 1;
    addLog(`Level up! Reached level ${state.level}.`);
    checkLevelUp();
  }
}

function showFloating(text, x, y) {
  const container = document.getElementById('floating-container');
  const gridRect = document.getElementById('grid-container').getBoundingClientRect();
  const tile = tiles[y]?.[x];
  if (!tile) return;
  const tileRect = tile.getBoundingClientRect();
  const fx = document.createElement('div');
  fx.className = 'floating-text';
  fx.textContent = text;
  fx.style.left = tileRect.left - gridRect.left + tileRect.width / 2 + 'px';
  fx.style.top = tileRect.top - gridRect.top + tileRect.height / 2 + 'px';
  container.appendChild(fx);
  setTimeout(() => fx.remove(), 1000);
}

function playCash() {
  if (cashSound.playTone) cashSound.playTone();
}
function playClick() {
  if (clickSound.playTone) clickSound.playTone();
}

function setupDom() {
  dom.money = document.getElementById('money');
  dom.level = document.getElementById('level');
  dom.xp = document.getElementById('xp');
  dom.xpBar = document.getElementById('xp-bar');
  dom.time = document.getElementById('time');
  dom.timePhase = document.getElementById('time-phase');
  dom.clubStatus = document.getElementById('club-status');
  dom.toggleOpen = document.getElementById('toggle-open');
  dom.toggleMode = document.getElementById('toggle-mode');
  dom.reset = document.getElementById('reset-game');
  dom.selectedInfo = document.getElementById('selected-info');
  dom.sellButton = document.getElementById('sell-button');
  dom.moveButton = document.getElementById('move-button');

  dom.toggleOpen.addEventListener('click', () => { toggleOpen(); playClick(); });
  dom.toggleMode.addEventListener('click', () => { toggleMode(); playClick(); });
  dom.reset.addEventListener('click', () => { playClick(); openReset(); });
  dom.sellButton.addEventListener('click', () => { sellSelected(); playClick(); });
  dom.moveButton.addEventListener('click', () => { moveSelected(); playClick(); });

  document.querySelectorAll('#category-buttons button').forEach(btn => {
    btn.addEventListener('click', () => setShopCategory(btn.dataset.category));
  });
}

function gameLoop() {
  render();
  requestAnimationFrame(gameLoop);
}

function setupIntervals() {
  setInterval(() => { tickTime(); updateTopBar(); }, 1000);
  setInterval(spawnCustomer, 1200);
  setInterval(stepCustomers, 1000);
  setInterval(incomeTick, 1500);
}

function init() {
  setupDom();
  initAudio();
  loadGame();
  createGrid();
  setShopCategory('furniture');
  updateTopBar();
  render();
  setupIntervals();
  gameLoop();
  addLog('Welcome to Neon Groove!');
}

document.addEventListener('DOMContentLoaded', init);
