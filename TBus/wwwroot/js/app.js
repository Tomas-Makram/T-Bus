// ==========================================
// TBus - Core API & Utility Layer
// ==========================================

const API_BASE = '/api/v1';

// ---- Auth Storage ----
const Auth = {
  getToken: () => localStorage.getItem('tbus_token'),
  getUser:  () => JSON.parse(localStorage.getItem('tbus_user') || 'null'),
  getSession: () => localStorage.getItem('tbus_session'),
  getRefreshToken: () => localStorage.getItem('tbus_refresh'),

  save(data) {
    localStorage.setItem('tbus_token',   data.token);
    localStorage.setItem('tbus_session', data.sessionId);
    localStorage.setItem('tbus_refresh', data.refreshToken);

    // Decode JWT payload to extract role (encrypted) + driver_id + full_name
    let role = '', driverId = '', fullName = '';
    try {
      const payload = JSON.parse(atob(data.token.split('.')[1]));
      // role claim key used by ASP.NET
      role      = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload['role'] || '';
      driverId  = payload['driver_id'] || '';
      fullName  = payload['full_name'] || '';
    } catch {}

    localStorage.setItem('tbus_user', JSON.stringify({
      id:       data.userID,
      name:     data.userName || fullName || '',
      fullName: fullName,
      role:     role,        // still encrypted from server
      driverId: driverId,
    }));
  },

  clear() {
    ['tbus_token','tbus_session','tbus_refresh','tbus_user'].forEach(k => localStorage.removeItem(k));
  },

  isLoggedIn: () => !!localStorage.getItem('tbus_token'),
};

// ---- HTTP Client ----
async function http(method, path, body = null) {
  const headers = { 'Content-Type': 'application/json' };
  const token = Auth.getToken();
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const opts = { method, headers };
  if (body) opts.body = JSON.stringify(body);

  const res = await fetch(API_BASE + path, opts);

  if (res.status === 401) {
    const refreshed = await tryRefresh();
    if (refreshed) return http(method, path, body);
    Auth.clear();
    window.location.href = '/pages/login.html';
    return null;
  }

  return res.json().catch(() => ({ success: false, message: 'خطأ في الاستجابة' }));
}

async function tryRefresh() {
  const sessionId     = Auth.getSession();
  const refreshToken  = Auth.getRefreshToken();
  if (!sessionId || !refreshToken) return false;

  const res = await fetch(API_BASE + '/authenticate/refresh-token', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ sessionId, refreshToken }),
  });

  if (!res.ok) return false;

  const data = await res.json();
  if (data.success && data.data) {
    Auth.save(data.data);
    return true;
  }
  return false;
}

const api = {
  get:    (path)        => http('GET',    path),
  post:   (path, body)  => http('POST',   path, body),
  put:    (path, body)  => http('PUT',    path, body),
  delete: (path)        => http('DELETE', path),
};

// ---- Theme ----
const Theme = {
  init() {
    const saved = localStorage.getItem('tbus_theme') || 'light';
    this.apply(saved);
  },
  apply(mode) {
    document.documentElement.setAttribute('data-theme', mode);
    localStorage.setItem('tbus_theme', mode);
    const btn = document.getElementById('themeToggle');
    if (btn) btn.innerHTML = mode === 'dark' ? svgSun() : svgMoon();
  },
  toggle() {
    const current = document.documentElement.getAttribute('data-theme');
    this.apply(current === 'dark' ? 'light' : 'dark');
  },
};

function svgMoon() {
  return `<svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
    <path d="M21 12.79A9 9 0 1 1 11.21 3a7 7 0 0 0 9.79 9.79z"/>
  </svg>`;
}
function svgSun() {
  return `<svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
    <circle cx="12" cy="12" r="5"/>
    <line x1="12" y1="1" x2="12" y2="3"/>
    <line x1="12" y1="21" x2="12" y2="23"/>
    <line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/>
    <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/>
    <line x1="1" y1="12" x2="3" y2="12"/>
    <line x1="21" y1="12" x2="23" y2="12"/>
    <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/>
    <line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/>
  </svg>`;
}

// ---- Toast ----
function toast(msg, type = 'success') {
  let container = document.getElementById('toastContainer');
  if (!container) {
    container = document.createElement('div');
    container.id = 'toastContainer';
    container.className = 'toast-container';
    document.body.appendChild(container);
  }

  const icons = {
    success: '✓',
    error:   '✕',
    info:    'ℹ',
  };

  const el = document.createElement('div');
  el.className = `toast ${type}`;
  el.innerHTML = `<span style="font-weight:600">${icons[type] || '•'}</span> ${msg}`;
  container.appendChild(el);
  setTimeout(() => { el.style.opacity = '0'; el.style.transition = '0.3s'; setTimeout(() => el.remove(), 300); }, 3200);
}

// ---- Modal helpers ----
function openModal(id)  { document.getElementById(id)?.classList.add('open');    document.body.style.overflow = 'hidden'; }
function closeModal(id) { document.getElementById(id)?.classList.remove('open'); document.body.style.overflow = '';       }

// Close on overlay click
document.addEventListener('click', e => {
  if (e.target.classList.contains('modal-overlay')) {
    e.target.classList.remove('open');
    document.body.style.overflow = '';
  }
});

// ---- Pagination ----
function buildPagination(container, total, page, perPage, onPage) {
  const pages = Math.ceil(total / perPage);
  container.innerHTML = '';
  if (pages <= 1) return;

  const btn = (label, p, disabled = false, active = false) => {
    const b = document.createElement('button');
    b.className = 'page-btn' + (active ? ' active' : '');
    b.textContent = label;
    b.disabled = disabled;
    if (!disabled && !active) b.onclick = () => onPage(p);
    return b;
  };

  container.appendChild(btn('«', 1, page === 1));
  container.appendChild(btn('‹', page - 1, page === 1));

  for (let i = 1; i <= pages; i++) {
    if (i === 1 || i === pages || Math.abs(i - page) <= 2) {
      container.appendChild(btn(i, i, false, i === page));
    } else if (Math.abs(i - page) === 3) {
      const dots = document.createElement('span');
      dots.textContent = '…';
      dots.style.cssText = 'padding:0 4px;color:var(--text-muted)';
      container.appendChild(dots);
    }
  }

  container.appendChild(btn('›', page + 1, page === pages));
  container.appendChild(btn('»', pages, page === pages));
}

// ---- Role helpers ----
// The server stores the role encrypted in the JWT.
// We can't decrypt it client-side (AES/DataProtection), so we rely on
// the /authenticate/my-account endpoint OR we cache the plain role after first fetch.
// Strategy: after login we call /my-account once and store the plain role.
async function resolveRole() {
  const user = Auth.getUser();
  if (!user) return null;
  // Already resolved?
  if (user.plainRole) return user.plainRole;
  // Fetch from API
  try {
    const res = await fetch(`${API_BASE}/authenticate/my-account/${user.id}`, {
      headers: { 'Authorization': `Bearer ${Auth.getToken()}` }
    });
    const data = await res.json();
    if (data.success && data.data?.role) {
      user.plainRole = data.data.role;                          // "Admin" or "Driver"
      user.fullName  = data.data.fullName  || user.fullName;   // الاسم الكامل الصح
      user.name      = data.data.fullName  || user.name;
      if (!user.driverId && data.data.driverId)
        user.driverId = data.data.driverId;
      localStorage.setItem('tbus_user', JSON.stringify(user));
      return user.plainRole;
    }
  } catch {}
  return null;
}

function getCachedRole() {
  return Auth.getUser()?.plainRole || null;
}

function isAdmin()  { return getCachedRole() === 'Admin';  }
function isDriver() { return getCachedRole() === 'Driver'; }

// ---- Guards ----
function requireAuth() {
  if (!Auth.isLoggedIn()) {
    window.location.href = '/pages/login.html';
    return false;
  }
  return true;
}

async function requireAdmin() {
  if (!requireAuth()) return false;
  const role = await resolveRole();
  if (role !== 'Admin') {
    window.location.href = '/pages/driver-home.html';
    return false;
  }
  return true;
}

async function requireDriver() {
  if (!requireAuth()) return false;
  const role = await resolveRole();
  if (role !== 'Driver') {
    window.location.href = '/pages/dashboard.html';
    return false;
  }
  return true;
}

// ---- Format helpers ----
function fmtDate(d) {
  if (!d) return '-';
  return new Date(d).toLocaleDateString('ar-EG', {
    year: 'numeric', month: 'short', day: 'numeric',
  });
}

function fmtDateTime(d) {
  if (!d) return '-';
  return new Date(d).toLocaleDateString('ar-EG', {
    year: 'numeric', month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

function fmtMoney(n) {
  return Number(n || 0).toLocaleString('ar-EG', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ج.م';
}

// Sidebar active link
function setSidebarActive(page) {
  document.querySelectorAll('.nav-item').forEach(el => {
    el.classList.toggle('active', el.dataset.page === page);
  });
}

// Build sidebar HTML (shared) — role-aware
function buildSidebar(activePage) {
  const user = Auth.getUser();
  const role = getCachedRole(); // "Admin" | "Driver" | null

  const adminNav = [
    { page: 'dashboard',        label: 'لوحة التحكم',    icon: '🏠', href: '/pages/dashboard.html' },
    { section: 'الإدارة' },
    { page: 'drivers',          label: 'السائقون',         icon: '👤', href: '/pages/drivers.html' },
    { page: 'buses',            label: 'الحافلات',          icon: '🚌', href: '/pages/buses.html' },
    { page: 'trips',            label: 'الرحلات',           icon: '🗺️', href: '/pages/trips.html' },
    { page: 'daily',            label: 'رحلات اليوم',      icon: '📅', href: '/pages/daily-trips.html' },
    { page: 'accounts',         label: 'الحسابات',          icon: '📊', href: '/pages/accounts.html' },
    { page: 'general-expenses', label: 'مصروفات عامة',     icon: '🧾', href: '/pages/general-expenses.html' },
    { section: 'المستخدمون' },
    { page: 'users',            label: 'المستخدمون',       icon: '👥', href: '/pages/users.html' },
    { section: 'الإعدادات' },
    { page: 'profile-settings', label: 'إعدادات حسابي',   icon: '⚙️', href: '/pages/profile-settings.html' },
  ];

  const driverNav = [
    { page: 'driver-home',      label: 'رحلاتي اليوم',        icon: '📅', href: '/pages/driver-home.html' },
    { page: 'driver-account',   label: 'حسابي',               icon: '📋', href: '/pages/driver-account.html' },
    { page: 'general-expenses', label: 'مصروفات عامة',        icon: '🧾', href: '/pages/general-expenses.html' },
    { section: 'الإعدادات' },
    { page: 'profile-settings', label: 'تغيير كلمة المرور',  icon: '🔑', href: '/pages/profile-settings.html' },
  ];

  const nav = role === 'Driver' ? driverNav : adminNav;

  let html = `
    <button class="mobile-menu-btn" id="mobileMenuBtn" type="button"
            onclick="toggleMobileSidebar()" aria-label="فتح القائمة" title="القائمة">
      ☰
    </button>

    <div class="mobile-sidebar-backdrop" id="mobileSidebarBackdrop"
         onclick="closeMobileSidebar()"></div>

    <aside class="sidebar" id="sidebar">

      <button class="sidebar-close-btn" type="button"
              onclick="closeMobileSidebar()" aria-label="إغلاق القائمة" title="إغلاق">
        ×
      </button>

      <div class="sidebar-logo">
        <h1>🚌 UBus</h1>
        <span>${role === 'Driver' ? 'بوابة السائق' : 'نظام إدارة الحافلات'}</span>
      </div>

      <nav class="sidebar-nav">`;

  nav.forEach(item => {
    if (item.section) {
      html += `<div class="nav-section-title">${item.section}</div>`;
    } else {
      html += `<a class="nav-item${item.page === activePage ? ' active' : ''}"
                  href="${item.href}"
                  data-page="${item.page}">
        <span style="font-size:18px">${item.icon}</span>
        <span>${item.label}</span>
      </a>`;
    }
  });

  html += `</nav>

      <div class="sidebar-footer">

        <button class="nav-item nav-logout" onclick="logout()" type="button">
          <span>🚪</span>
          <span>تسجيل الخروج</span>
        </button>

        ${user ? `<div class="sidebar-user-box">

          <div class="welcome-user">
            مرحباً، ${user.fullName || user.name || 'System Administrator'}
          </div>

          <div class="developer-box">

            <div class="developer-title">
              👨‍💻 Developer
            </div>

            <div class="developer-name">
              Tomas Makram
            </div>

            <a class="developer-phone"
               href="tel:01221936850">
              📞 01221936850
            </a>

            <div class="developer-actions">

              <a href="https://wa.me/201221936850"
                 target="_blank"
                 class="developer-action whatsapp">
                WhatsApp
              </a>

              <a href="mailto:Tomasmakram86627@gmail.com"
                 class="developer-action email">
                Email
              </a>

            </div>

          </div>

        </div>` : ''}

      </div>

    </aside>`;

  return html;
}

// ---- Mobile sidebar helpers ----
function openMobileSidebar() {
  const sidebar = document.getElementById('sidebar');
  const backdrop = document.getElementById('mobileSidebarBackdrop');
  const menuBtn = document.getElementById('mobileMenuBtn');

  sidebar?.classList.add('open');
  backdrop?.classList.add('show');
  menuBtn?.classList.add('hide');

  document.body.classList.add('sidebar-open');
}

function closeMobileSidebar() {
  const sidebar = document.getElementById('sidebar');
  const backdrop = document.getElementById('mobileSidebarBackdrop');
  const menuBtn = document.getElementById('mobileMenuBtn');

  sidebar?.classList.remove('open');
  backdrop?.classList.remove('show');
  menuBtn?.classList.remove('hide');

  document.body.classList.remove('sidebar-open');
}

function toggleMobileSidebar() {
  const sidebar = document.getElementById('sidebar');

  if (sidebar?.classList.contains('open')) {
    closeMobileSidebar();
  } else {
    openMobileSidebar();
  }
}

document.addEventListener('keydown', e => {
  if (e.key === 'Escape') {
    closeMobileSidebar();
  }
});

document.addEventListener('click', e => {
  const link = e.target.closest?.('.sidebar .nav-item[href]');

  if (link && window.innerWidth <= 768) {
    closeMobileSidebar();
  }
});

async function logout() {
  const user = Auth.getUser();
  const session = Auth.getSession();
  if (user && session) {
    await api.post('/authenticate/logout', { userId: user.id, sessionId: session });
  }
  Auth.clear();
  window.location.href = '/pages/login.html';
}

// Confirm dialog
// لا تعرّف دالة باسم confirm هنا؛ لأن ذلك يستبدل window.confirm الأصلي
// ويسبب Maximum call stack size exceeded عند الضغط على أزرار التأكيد.
// استخدم confirm(...) الأصلي الخاص بالمتصفح مباشرة من الصفحات.

// On DOM ready
document.addEventListener('DOMContentLoaded', () => {
  Theme.init();
  const themeBtn = document.getElementById('themeToggle');
  if (themeBtn) themeBtn.addEventListener('click', () => Theme.toggle());
});

// ---- Responsive table enhancer ----
(function responsiveTableEnhancer() {
  function enhanceTable(table) {
    if (!table || table.dataset.responsiveEnhanced === '1') return;
    const headers = Array.from(table.querySelectorAll('thead th')).map(th => th.textContent.trim());
    if (!headers.length) return;
    table.classList.add('responsive-table');
    Array.from(table.querySelectorAll('tbody tr')).forEach(row => {
      Array.from(row.children).forEach((cell, index) => {
        if (!cell.hasAttribute('data-label')) {
          cell.setAttribute('data-label', headers[index] || '');
        }
      });
    });
    table.dataset.responsiveEnhanced = '1';
  }

  function enhanceAllTables(root = document) {
    root.querySelectorAll?.('table').forEach(table => {
      table.dataset.responsiveEnhanced = '0';
      enhanceTable(table);
    });
  }

  document.addEventListener('DOMContentLoaded', () => {
    enhanceAllTables(document);
    const observer = new MutationObserver(mutations => {
      for (const mutation of mutations) {
        mutation.addedNodes.forEach(node => {
          if (node.nodeType !== 1) return;
          if (node.matches?.('table')) enhanceTable(node);
          enhanceAllTables(node);
        });
      }
    });
    observer.observe(document.body, { childList: true, subtree: true });
  });
})();
