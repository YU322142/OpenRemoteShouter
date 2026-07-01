namespace RemoteShouter.Services;

public static class WebUiHtml
{
    public static string Build(string nonce)
    {
        return $$"""
<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>OpenRemoteShouter</title>
  <style nonce="{{nonce}}">
    :root {
      color-scheme: light;
      font-family: "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", system-ui, sans-serif;
      background: #f5f7fb;
      color: #1f2937;
      --line: #d7dde8;
      --muted: #667085;
      --panel: #ffffff;
      --panel-soft: #f8fafc;
      --text: #1f2937;
      --teal: #087f8c;
      --teal-dark: #05616b;
      --blue: #2563eb;
      --green: #15803d;
      --amber: #b45309;
      --rose: #be123c;
      --violet: #6d28d9;
      --danger: #b42318;
      --shadow: 0 18px 44px rgba(15, 23, 42, .08);
    }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      min-height: 100vh;
      background:
        linear-gradient(180deg, #eef4f8 0, #f5f7fb 360px),
        #f5f7fb;
    }
    button, input, textarea, select { font: inherit; }
    button {
      min-height: 40px;
      border: 0;
      border-radius: 6px;
      padding: 10px 14px;
      color: #fff;
      background: var(--teal);
      font-weight: 700;
      cursor: pointer;
    }
    button:hover { background: var(--teal-dark); }
    button:disabled { cursor: not-allowed; opacity: .55; }
    button.secondary {
      color: var(--text);
      background: #e8edf4;
    }
    button.secondary:hover { background: #dbe3ee; }
    button.danger { background: var(--danger); }
    input[type="text"], input[type="password"], input[type="number"], textarea, select {
      width: 100%;
      border: 1px solid var(--line);
      border-radius: 6px;
      padding: 11px 12px;
      color: var(--text);
      background: #fff;
      outline: none;
    }
    input:focus, textarea:focus, select:focus {
      border-color: var(--teal);
      box-shadow: 0 0 0 3px rgba(8, 127, 140, .14);
    }
    textarea {
      min-height: 210px;
      resize: vertical;
      line-height: 1.55;
      font-size: 18px;
    }
    label {
      display: block;
      margin-bottom: 7px;
      color: #344054;
      font-size: 14px;
      font-weight: 700;
    }
    .shell {
      width: min(1180px, calc(100% - 32px));
      margin: 0 auto;
      padding: 26px 0 34px;
    }
    .topbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 18px;
      min-height: 58px;
      margin-bottom: 18px;
    }
    .brand {
      display: flex;
      align-items: center;
      gap: 12px;
      min-width: 0;
    }
    .mark {
      display: grid;
      place-items: center;
      width: 38px;
      height: 38px;
      border-radius: 8px;
      color: #fff;
      background: #0f766e;
      font-weight: 900;
    }
    h1 {
      margin: 0;
      font-size: 24px;
      letter-spacing: 0;
      line-height: 1.15;
    }
    .subtle { color: var(--muted); }
    .status-pill {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      min-height: 32px;
      padding: 6px 10px;
      border: 1px solid var(--line);
      border-radius: 999px;
      background: #fff;
      color: #475467;
      font-size: 13px;
      font-weight: 700;
      white-space: nowrap;
    }
    .dot {
      width: 8px;
      height: 8px;
      border-radius: 999px;
      background: #98a2b3;
    }
    .dot.ok { background: #16a34a; }
    .layout {
      display: grid;
      grid-template-columns: 300px 1fr;
      gap: 18px;
      align-items: start;
    }
    .panel {
      border: 1px solid var(--line);
      border-radius: 8px;
      background: var(--panel);
      box-shadow: var(--shadow);
    }
    .panel-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      padding: 16px 18px;
      border-bottom: 1px solid var(--line);
    }
    .panel-header h2 {
      margin: 0;
      font-size: 16px;
      letter-spacing: 0;
    }
    .panel-body { padding: 18px; }
    .stack {
      display: grid;
      gap: 14px;
    }
    .grid-2 {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 14px;
    }
    .grid-3 {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 14px;
    }
    .row {
      display: flex;
      align-items: center;
      gap: 10px;
      flex-wrap: wrap;
    }
    .row.between { justify-content: space-between; }
    .segmented {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      border: 1px solid var(--line);
      border-radius: 6px;
      overflow: hidden;
      background: #f2f5f9;
    }
    .segmented input { position: absolute; opacity: 0; pointer-events: none; }
    .segmented label {
      margin: 0;
      min-height: 42px;
      display: grid;
      place-items: center;
      color: #475467;
      cursor: pointer;
      font-weight: 800;
    }
    .segmented input:checked + label {
      color: #fff;
      background: var(--teal);
    }
    .switch {
      display: inline-flex;
      align-items: center;
      gap: 10px;
      min-height: 34px;
      color: #344054;
      font-weight: 700;
    }
    .switch input {
      width: 42px;
      height: 24px;
      appearance: none;
      border-radius: 999px;
      background: #cbd5e1;
      position: relative;
      outline: none;
      cursor: pointer;
    }
    .switch input::after {
      content: "";
      position: absolute;
      left: 3px;
      top: 3px;
      width: 18px;
      height: 18px;
      border-radius: 999px;
      background: #fff;
      transition: transform .16s ease;
    }
    .switch input:checked { background: var(--teal); }
    .switch input:checked::after { transform: translateX(18px); }
    input[type="range"] {
      width: 100%;
      accent-color: var(--teal);
    }
    .range-line {
      display: grid;
      grid-template-columns: 1fr auto;
      gap: 12px;
      align-items: center;
    }
    .range-value {
      min-width: 58px;
      text-align: right;
      color: #344054;
      font-weight: 800;
    }
    .theme-row {
      display: grid;
      grid-template-columns: 22px 1fr;
      gap: 10px;
      align-items: center;
    }
    .swatch {
      width: 18px;
      height: 18px;
      border-radius: 6px;
      border: 1px solid rgba(15, 23, 42, .16);
    }
    .swatch.cyan { background: var(--teal); }
    .swatch.blue { background: var(--blue); }
    .swatch.green { background: var(--green); }
    .swatch.amber { background: var(--amber); }
    .swatch.rose { background: var(--rose); }
    .swatch.violet { background: var(--violet); }
    .notice {
      min-height: 36px;
      padding: 10px 12px;
      border-radius: 6px;
      color: #344054;
      background: #eef6f7;
      border: 1px solid #c6e4e8;
      font-weight: 700;
    }
    .notice.error {
      color: #912018;
      background: #fff1f0;
      border-color: #f4b8b2;
    }
    .hidden { display: none !important; }
    .auth-wrap {
      width: min(440px, calc(100% - 32px));
      margin: 9vh auto 40px;
    }
    .auth-brand { margin-bottom: 16px; }
    .auth-wrap .panel { box-shadow: 0 28px 80px rgba(15, 23, 42, .14); }
    .meta-list {
      display: grid;
      gap: 10px;
      color: #475467;
      font-size: 14px;
    }
    .meta-list div {
      display: grid;
      gap: 3px;
      padding-bottom: 10px;
      border-bottom: 1px solid #edf1f6;
    }
    .meta-list strong { color: #1f2937; }
    .url-list {
      display: grid;
      gap: 8px;
      margin: 0;
      padding: 0;
      list-style: none;
    }
    .url-list button {
      width: 100%;
      min-height: 34px;
      padding: 7px 9px;
      color: #155e75;
      background: #ecfeff;
      border: 1px solid #bae6fd;
      text-align: left;
      font-weight: 700;
      overflow-wrap: anywhere;
    }
    .users {
      display: grid;
      gap: 10px;
    }
    .user-row {
      display: grid;
      grid-template-columns: 1.1fr .8fr auto;
      gap: 10px;
      align-items: center;
      padding: 12px;
      border: 1px solid var(--line);
      border-radius: 8px;
      background: var(--panel-soft);
    }
    .badges {
      display: flex;
      gap: 6px;
      flex-wrap: wrap;
    }
    .badge {
      display: inline-flex;
      min-height: 24px;
      align-items: center;
      padding: 3px 8px;
      border-radius: 999px;
      background: #e8edf4;
      color: #475467;
      font-size: 12px;
      font-weight: 800;
    }
    .badge.admin { background: #e0f2fe; color: #075985; }
    .badge.disabled { background: #fee2e2; color: #991b1b; }
    .tabs {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }
    .tab {
      color: #344054;
      background: #e8edf4;
    }
    .tab.active {
      color: #fff;
      background: var(--teal);
    }
    .toggle-block { align-content: end; }
    @media (max-width: 900px) {
      .layout { grid-template-columns: 1fr; }
      .grid-3 { grid-template-columns: 1fr; }
      .user-row { grid-template-columns: 1fr; }
      .topbar { align-items: flex-start; flex-direction: column; }
    }
    @media (max-width: 620px) {
      .shell { width: min(100% - 20px, 1180px); padding-top: 16px; }
      .grid-2 { grid-template-columns: 1fr; }
      .panel-body { padding: 14px; }
      textarea { min-height: 160px; }
      button { width: 100%; }
      .tabs button { width: auto; }
    }
  </style>
</head>
<body>
  <div id="authView" class="auth-wrap hidden">
    <div class="brand auth-brand">
      <div class="mark">ORS</div>
      <div>
        <h1>OpenRemoteShouter</h1>
        <div class="subtle">远程喊话控制台</div>
      </div>
    </div>
    <section class="panel">
      <div class="panel-header">
        <h2 id="authTitle">登录</h2>
        <span class="status-pill"><span class="dot" id="authDot"></span><span id="authModeText">账户</span></span>
      </div>
      <div class="panel-body">
        <form id="authForm" class="stack">
          <div>
            <label for="authUsername">用户名</label>
            <input id="authUsername" name="username" type="text" autocomplete="username" maxlength="32" required>
          </div>
          <div id="displayNameField">
            <label for="authDisplayName">显示名称</label>
            <input id="authDisplayName" name="displayName" type="text" maxlength="48">
          </div>
          <div>
            <label for="authPassword">密码</label>
            <input id="authPassword" name="password" type="password" autocomplete="current-password" required>
          </div>
          <button id="authSubmit" type="submit">登录</button>
          <div id="authMessage" class="notice hidden"></div>
        </form>
      </div>
    </section>
  </div>

  <div id="appView" class="shell hidden">
    <header class="topbar">
      <div class="brand">
        <div class="mark">ORS</div>
        <div>
          <h1>OpenRemoteShouter</h1>
          <div class="subtle" id="userLine">未登录</div>
        </div>
      </div>
      <div class="row">
        <span class="status-pill"><span class="dot" id="serverDot"></span><span id="serverStatus">读取状态</span></span>
        <button class="secondary" id="refreshButton" type="button">刷新</button>
        <button class="secondary" id="logoutButton" type="button">退出登录</button>
      </div>
    </header>

    <main class="layout">
      <aside class="stack">
        <section class="panel">
          <div class="panel-header"><h2>服务状态</h2></div>
          <div class="panel-body stack">
            <div class="meta-list">
              <div><span>端口</span><strong id="portText">-</strong></div>
              <div><span>语音后端</span><strong id="speechBackendText">-</strong></div>
              <div><span>日志文件</span><strong id="logPathText">-</strong></div>
            </div>
            <ul id="urlList" class="url-list"></ul>
          </div>
        </section>

        <section class="panel">
          <div class="panel-header"><h2>账户</h2></div>
          <div class="panel-body stack">
            <div class="meta-list">
              <div><span>当前用户</span><strong id="currentUserText">-</strong></div>
              <div><span>权限</span><strong id="currentRoleText">-</strong></div>
            </div>
            <form id="passwordForm" class="stack">
              <div>
                <label for="currentPassword">当前密码</label>
                <input id="currentPassword" type="password" autocomplete="current-password">
              </div>
              <div>
                <label for="newPassword">新密码</label>
                <input id="newPassword" type="password" autocomplete="new-password">
              </div>
              <button class="secondary" type="submit">修改密码</button>
            </form>
          </div>
        </section>
      </aside>

      <section class="stack">
        <section class="panel">
          <div class="panel-header">
            <h2>喊话</h2>
            <div class="tabs">
              <button id="tabShout" class="tab active" type="button">发送</button>
              <button id="tabUsers" class="tab hidden" type="button">用户</button>
            </div>
          </div>
          <div id="shoutPanel" class="panel-body">
            <form id="shoutForm" class="stack">
              <div class="grid-2">
                <div>
                  <label for="title">标题</label>
                  <input id="title" type="text" value="OpenRemoteShouter" maxlength="60">
                </div>
                <div>
                  <label for="voiceName">EdgeTTS 说话人</label>
                  <select id="voiceName"></select>
                </div>
              </div>

              <div>
                <label for="message">内容</label>
                <textarea id="message" maxlength="3000" required autofocus></textarea>
              </div>

              <div class="grid-3">
                <div>
                  <label>显示方式</label>
                  <div class="segmented">
                    <input type="radio" id="modeFullscreen" name="mode" value="fullscreen" checked>
                    <label for="modeFullscreen">全屏</label>
                    <input type="radio" id="modePopup" name="mode" value="popup">
                    <label for="modePopup">弹窗</label>
                  </div>
                </div>
                <div>
                  <label for="theme">显示色调</label>
                  <div class="theme-row">
                    <span id="themeSwatch" class="swatch cyan"></span>
                    <select id="theme">
                      <option value="cyan">青色</option>
                      <option value="blue">蓝色</option>
                      <option value="green">绿色</option>
                      <option value="amber">琥珀</option>
                      <option value="rose">玫瑰</option>
                      <option value="violet">紫色</option>
                    </select>
                  </div>
                </div>
                <div class="stack toggle-block">
                  <label class="switch"><input id="topmost" type="checkbox" checked> 置顶显示</label>
                  <label class="switch"><input id="speechEnabled" type="checkbox" checked> 语音播报</label>
                </div>
              </div>

              <div class="grid-3">
                <div>
                  <label for="durationSeconds">自动关闭</label>
                  <div class="range-line">
                    <input id="durationSeconds" type="range" min="0" max="300" step="5" value="10">
                    <span id="durationValue" class="range-value">10 秒</span>
                  </div>
                </div>
                <div>
                  <label for="speechRate">语速</label>
                  <div class="range-line">
                    <input id="speechRate" type="range" min="-100" max="100" step="5" value="0">
                    <span id="rateValue" class="range-value">0%</span>
                  </div>
                </div>
                <div>
                  <label for="speechVolume">音量</label>
                  <div class="range-line">
                    <input id="speechVolume" type="range" min="0" max="1" step="0.05" value="1">
                    <span id="volumeValue" class="range-value">100%</span>
                  </div>
                </div>
              </div>

              <div class="row">
                <button type="submit">发送喊话</button>
                <button class="secondary" type="button" id="closeButton">关闭当前显示</button>
              </div>
              <div id="result" class="notice hidden" role="status" aria-live="polite"></div>
            </form>
          </div>

          <div id="usersPanel" class="panel-body hidden">
            <div class="stack">
              <form id="createUserForm" class="grid-3">
                <div>
                  <label for="newUsernameInput">用户名</label>
                  <input id="newUsernameInput" type="text" maxlength="32">
                </div>
                <div>
                  <label for="newDisplayNameInput">显示名称</label>
                  <input id="newDisplayNameInput" type="text" maxlength="48">
                </div>
                <div>
                  <label for="newUserPasswordInput">初始密码</label>
                  <input id="newUserPasswordInput" type="password">
                </div>
                <label class="switch"><input id="newUserAdminInput" type="checkbox"> 管理员</label>
                <button type="submit">创建用户</button>
              </form>
              <div id="usersList" class="users"></div>
            </div>
          </div>
        </section>
      </section>
    </main>
  </div>

  <script nonce="{{nonce}}">
    const state = { user: null, csrfToken: null, setupRequired: false, users: [], voices: [] };
    const $ = id => document.getElementById(id);

    async function api(path, options = {}) {
      const headers = { ...(options.headers || {}) };
      if (options.body && !headers['Content-Type']) headers['Content-Type'] = 'application/json; charset=utf-8';
      if (options.method && options.method !== 'GET' && state.csrfToken) headers['X-OpenRemoteShouter-CSRF'] = state.csrfToken;
      const response = await fetch(path, { ...options, headers, credentials: 'same-origin' });
      const body = await response.json().catch(() => ({ ok: false, error: '响应格式错误。' }));
      if (!response.ok || body.ok === false) {
        const error = new Error(body.error || '请求失败。');
        error.status = response.status;
        throw error;
      }
      return body;
    }

    function showNotice(element, text, isError = false) {
      element.textContent = text;
      element.classList.toggle('error', isError);
      element.classList.remove('hidden');
    }

    function hideNotice(element) {
      element.classList.add('hidden');
      element.textContent = '';
    }

    function setAuthState(authState) {
      state.setupRequired = !!authState.setupRequired;
      state.user = authState.user || null;
      state.csrfToken = authState.csrfToken || null;
      renderShell();
    }

    function renderShell() {
      const authed = !!state.user;
      $('authView').classList.toggle('hidden', authed);
      $('appView').classList.toggle('hidden', !authed);
      if (!authed) renderAuth();
      if (authed) renderApp();
    }

    function renderAuth() {
      $('authTitle').textContent = state.setupRequired ? '创建管理员' : '登录';
      $('authSubmit').textContent = state.setupRequired ? '完成初始化' : '登录';
      $('authModeText').textContent = state.setupRequired ? '本机初始化' : '账户登录';
      $('authDot').classList.toggle('ok', state.setupRequired);
      $('displayNameField').classList.toggle('hidden', !state.setupRequired);
      $('authPassword').autocomplete = state.setupRequired ? 'new-password' : 'current-password';
    }

    async function renderApp() {
      $('userLine').textContent = `${state.user.displayName} · ${state.user.username}`;
      $('currentUserText').textContent = state.user.displayName;
      $('currentRoleText').textContent = state.user.isAdmin ? '管理员' : '普通用户';
      $('tabUsers').classList.toggle('hidden', !state.user.isAdmin);
      await Promise.all([loadStatus(), loadVoices()]);
      if (state.user.isAdmin) await loadUsers();
    }

    async function loadStatus() {
      try {
        const status = await api('/api/status');
        $('serverStatus').textContent = status.isRunning ? '运行中' : '已停止';
        $('serverDot').classList.toggle('ok', status.isRunning);
        $('portText').textContent = status.port;
        $('speechBackendText').textContent = status.speechBackend;
        $('logPathText').textContent = status.logFilePath;
        $('urlList').innerHTML = '';
        for (const url of status.urls || []) {
          const li = document.createElement('li');
          const button = document.createElement('button');
          button.type = 'button';
          button.textContent = url;
          button.addEventListener('click', async () => navigator.clipboard?.writeText(url));
          li.appendChild(button);
          $('urlList').appendChild(li);
        }
      } catch (error) {
        if (error.status === 401) return forceLogin();
        $('serverStatus').textContent = '状态异常';
        $('serverDot').classList.remove('ok');
      }
    }

    async function loadVoices() {
      const body = await api('/api/voices');
      state.voices = Array.isArray(body) ? body : [];
      $('voiceName').innerHTML = '';
      for (const voice of state.voices) {
        const option = document.createElement('option');
        option.value = voice.shortName;
        option.textContent = voice.shortName.replace('zh-CN-', '').replace('Neural', '');
        if (voice.shortName === 'zh-CN-XiaoyiNeural') option.selected = true;
        $('voiceName').appendChild(option);
      }
    }

    async function loadUsers() {
      const body = await api('/api/users');
      state.users = body.users || [];
      const list = $('usersList');
      list.innerHTML = '';
      for (const user of state.users) {
        const row = document.createElement('div');
        row.className = 'user-row';
        row.innerHTML = `
          <div>
            <strong>${escapeHtml(user.displayName)}</strong>
            <div class="subtle">${escapeHtml(user.username)}</div>
          </div>
          <div class="badges">
            ${user.isAdmin ? '<span class="badge admin">管理员</span>' : '<span class="badge">普通用户</span>'}
            ${user.isEnabled ? '<span class="badge">启用</span>' : '<span class="badge disabled">禁用</span>'}
          </div>
          <div class="row">
            <button class="secondary" data-action="toggle-admin">${user.isAdmin ? '取消管理员' : '设为管理员'}</button>
            <button class="secondary" data-action="toggle-enabled">${user.isEnabled ? '禁用' : '启用'}</button>
            <button class="danger" data-action="delete">删除</button>
          </div>`;
        row.querySelector('[data-action="toggle-admin"]').addEventListener('click', () => updateUser(user.username, { isAdmin: !user.isAdmin }));
        row.querySelector('[data-action="toggle-enabled"]').addEventListener('click', () => updateUser(user.username, { isEnabled: !user.isEnabled }));
        row.querySelector('[data-action="delete"]').addEventListener('click', () => deleteUser(user.username));
        list.appendChild(row);
      }
    }

    async function updateUser(username, payload) {
      await api(`/api/users/${encodeURIComponent(username)}`, { method: 'PUT', body: JSON.stringify(payload) });
      await loadUsers();
    }

    async function deleteUser(username) {
      await api(`/api/users/${encodeURIComponent(username)}`, { method: 'DELETE' });
      await loadUsers();
    }

    function forceLogin() {
      state.user = null;
      state.csrfToken = null;
      renderShell();
    }

    function escapeHtml(value) {
      return String(value).replace(/[&<>"']/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch]));
    }

    function syncRanges() {
      $('durationValue').textContent = Number($('durationSeconds').value) === 0 ? '手动' : `${$('durationSeconds').value} 秒`;
      $('rateValue').textContent = `${$('speechRate').value}%`;
      $('volumeValue').textContent = `${Math.round(Number($('speechVolume').value) * 100)}%`;
    }

    $('authForm').addEventListener('submit', async event => {
      event.preventDefault();
      hideNotice($('authMessage'));
      const payload = {
        username: $('authUsername').value,
        displayName: $('authDisplayName').value,
        password: $('authPassword').value
      };
      try {
        const body = await api(state.setupRequired ? '/api/auth/setup' : '/api/auth/login', {
          method: 'POST',
          body: JSON.stringify(payload)
        });
        setAuthState(body.state);
        $('authPassword').value = '';
      } catch (error) {
        showNotice($('authMessage'), error.message, true);
      }
    });

    $('logoutButton').addEventListener('click', async () => {
      try { await api('/api/auth/logout', { method: 'POST' }); } catch {}
      forceLogin();
    });

    $('refreshButton').addEventListener('click', renderApp);

    $('passwordForm').addEventListener('submit', async event => {
      event.preventDefault();
      try {
        await api('/api/auth/password', {
          method: 'POST',
          body: JSON.stringify({ currentPassword: $('currentPassword').value, newPassword: $('newPassword').value })
        });
        $('currentPassword').value = '';
        $('newPassword').value = '';
        forceLogin();
      } catch (error) {
        alert(error.message);
      }
    });

    $('shoutForm').addEventListener('submit', async event => {
      event.preventDefault();
      hideNotice($('result'));
      const payload = {
        title: $('title').value,
        message: $('message').value,
        mode: document.querySelector('input[name="mode"]:checked').value,
        theme: $('theme').value,
        durationSeconds: Number($('durationSeconds').value),
        topmost: $('topmost').checked,
        speechEnabled: $('speechEnabled').checked,
        voiceName: $('voiceName').value,
        speechRate: Number($('speechRate').value),
        speechVolume: Number($('speechVolume').value)
      };
      try {
        await api('/api/shout', { method: 'POST', body: JSON.stringify(payload) });
        showNotice($('result'), '已发送。');
      } catch (error) {
        showNotice($('result'), error.message, true);
      }
    });

    $('closeButton').addEventListener('click', async () => {
      try {
        await api('/api/close', { method: 'POST' });
        showNotice($('result'), '已关闭当前显示。');
      } catch (error) {
        showNotice($('result'), error.message, true);
      }
    });

    $('createUserForm').addEventListener('submit', async event => {
      event.preventDefault();
      await api('/api/users', {
        method: 'POST',
        body: JSON.stringify({
          username: $('newUsernameInput').value,
          displayName: $('newDisplayNameInput').value,
          password: $('newUserPasswordInput').value,
          isAdmin: $('newUserAdminInput').checked
        })
      });
      event.target.reset();
      await loadUsers();
    });

    $('tabShout').addEventListener('click', () => {
      $('tabShout').classList.add('active');
      $('tabUsers').classList.remove('active');
      $('shoutPanel').classList.remove('hidden');
      $('usersPanel').classList.add('hidden');
    });

    $('tabUsers').addEventListener('click', () => {
      $('tabUsers').classList.add('active');
      $('tabShout').classList.remove('active');
      $('usersPanel').classList.remove('hidden');
      $('shoutPanel').classList.add('hidden');
    });

    for (const id of ['durationSeconds', 'speechRate', 'speechVolume']) {
      $(id).addEventListener('input', syncRanges);
    }
    $('theme').addEventListener('change', () => {
      $('themeSwatch').className = `swatch ${$('theme').value}`;
    });

    (async function boot() {
      syncRanges();
      $('themeSwatch').className = `swatch ${$('theme').value}`;
      try {
        const body = await api('/api/auth/state');
        setAuthState(body.state);
      } catch {
        state.setupRequired = false;
        state.user = null;
        renderShell();
      }
    })();
  </script>
</body>
</html>
""";
    }
}
