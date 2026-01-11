const apiBase = '';
let token = loadToken();
let refreshToken = localStorage.getItem('spec_refresh');
let tokenExpiry = loadTokenExpiry();
let username = localStorage.getItem('spec_username');
let role = localStorage.getItem('spec_role');
let pageNumber = 1;
let pageSize = 5;

function saveToken(t, refresh, usernameVal, roleVal, expiresInSec = 60*60*12) {
  token = t;
  refreshToken = refresh;
  tokenExpiry = Date.now() + expiresInSec * 1000;
  localStorage.setItem('spec_token', token);
  localStorage.setItem('spec_refresh', refreshToken);
  localStorage.setItem('spec_token_expiry', tokenExpiry.toString());
  if (usernameVal) { localStorage.setItem('spec_username', usernameVal); username = usernameVal; }
  if (roleVal) { localStorage.setItem('spec_role', roleVal); role = roleVal; }
  updateUserDisplay();
}

function loadToken() { return localStorage.getItem('spec_token'); }
function loadTokenExpiry() { const v = localStorage.getItem('spec_token_expiry'); return v ? parseInt(v, 10) : 0; }
function clearToken() { token = null; tokenExpiry = 0; localStorage.removeItem('spec_token'); localStorage.removeItem('spec_token_expiry'); localStorage.removeItem('spec_username'); localStorage.removeItem('spec_role'); localStorage.removeItem('spec_refresh'); username = null; role = null; updateUserDisplay(); }

function updateUserDisplay() {
  const area = document.getElementById('token-area');
  if (token) {
    area.classList.remove('hidden');
    document.getElementById('token-value').textContent = token;
    document.getElementById('current-user').textContent = username || 'unknown';
    document.getElementById('current-role').textContent = role || 'none';
  } else {
    area.classList.add('hidden');
  }
}

async function tryRefreshIfNeeded() {
  if (!token) return false;
  const timeLeft = tokenExpiry - Date.now();
  // refresh if less than 5 minutes
  if (timeLeft > 5 * 60 * 1000) return true;

  if (!refreshToken) { clearToken(); return false; }

  // call refresh with stored refresh token
  const resp = await fetch('/api/auth/refresh', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ Token: refreshToken }) });
  if (!resp.ok) { clearToken(); return false; }
  const body = await resp.json();
  saveToken(body.token, body.refreshToken, body.username, body.role);
  return true;
}

async function ensureToken() {
  if (!token) return false;
  if (Date.now() > tokenExpiry) { clearToken(); return false; }
  return await tryRefreshIfNeeded();
}

// update UI user display
updateUserDisplay();

// login
document.getElementById('btn-login').addEventListener('click', async () => {
  const uname = document.getElementById('username').value;
  const r = document.getElementById('role').value;
  if (!uname) return alert('username required');

  setLoading(true);
  const resp = await fetch(`/api/auth/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ Username: uname, Role: r }) });
  setLoading(false);
  if (!resp.ok) return alert('login failed');
  const data = await resp.json();
  saveToken(data.token, data.refreshToken, data.username, data.role);
});

document.getElementById('btn-logout').addEventListener('click', async () => { 
  if (refreshToken) await fetch('/api/auth/revoke', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ RefreshToken: refreshToken }) });
  clearToken();
});

// Create
document.getElementById('btn-create').addEventListener('click', async () => {
  if (!await ensureToken()) return alert('login first');

  const title = document.getElementById('title').value;
  const content = document.getElementById('content').value;
  const category = document.getElementById('category').value;

  setLoading(true);
  const resp = await fetch(`/api/posts`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify({ title, content, category }) });
  setLoading(false);
  if (resp.status === 201) { alert('created'); loadPosts(); } else { const txt = await resp.text(); alert('error: ' + txt); }
});

// Edit form handling
function showEditForm(post) {
  const div = document.getElementById('edit-area');
  div.innerHTML = `\
    <h3>Edit Post</h3>\
    <input id="edit-title" value="${escapeHtml(post.title)}" />\
    <input id="edit-category" value="${escapeHtml(post.category)}" />\
    <textarea id="edit-content">${escapeHtml(post.content)}</textarea>\
    <button id="btn-save-edit">Save</button>\
    <button id="btn-cancel-edit">Cancel</button>\
  `;

  document.getElementById('btn-cancel-edit').addEventListener('click', () => { div.innerHTML = ''; });
  document.getElementById('btn-save-edit').addEventListener('click', async () => {
    if (!await ensureToken()) return alert('login first');
    const dto = { Title: document.getElementById('edit-title').value, Content: document.getElementById('edit-content').value, Category: document.getElementById('edit-category').value };
    const resp = await fetch(`/api/posts/${post.id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(dto) });
    if (resp.status === 204) { alert('updated'); div.innerHTML = ''; loadPosts(); } else { const txt = await resp.text(); alert('update failed ' + resp.status + ' ' + txt); }
  });
}

// loading indicator
function setLoading(on) { const el = document.getElementById('loading'); if (!el) return; el.style.display = on ? 'block' : 'none'; }

// load posts and show comments
async function loadPosts() {
  setLoading(true);
  const category = document.getElementById('filter-category').value;
  const minLikes = document.getElementById('filter-minlikes').value;

  const params = new URLSearchParams();
  params.set('pageNumber', pageNumber);
  params.set('pageSize', pageSize);
  if (category) params.set('category', category);
  if (minLikes) params.set('minLikes', minLikes);

  const resp = await fetch(`/api/posts?` + params.toString());
  setLoading(false);
  if (!resp.ok) return alert('failed loading posts');
  const data = await resp.json();

  const list = document.getElementById('posts-list');
  list.innerHTML = '';
  data.Items.forEach(p => {
    const el = document.createElement('div');
    el.className = 'post';
    el.innerHTML = `
      <h3>${escapeHtml(p.title)}</h3>
      <p>${escapeHtml(p.content)}</p>
      <p><strong>Category:</strong> ${escapeHtml(p.category)} | <strong>Likes:</strong> ${p.likesCount} | <strong>Comments:</strong> ${p.commentsCount} | <strong>Author:</strong> ${escapeHtml(p.authorId)}</p>
      <button class="btn-like" data-id="${p.id}">Like</button>
      <button class="btn-show-comments" data-id="${p.id}">Comments</button>
    `;

    // show edit/delete only for owner or admin
    if ((username && username === p.authorId) || isAdmin()) {
      const editBtn = document.createElement('button'); editBtn.textContent = 'Edit'; editBtn.className = 'btn-edit'; editBtn.setAttribute('data-id', p.id);
      el.appendChild(editBtn);

      const delBtn = document.createElement('button'); delBtn.textContent = 'Delete'; delBtn.className = 'btn-delete'; delBtn.setAttribute('data-id', p.id);
      el.appendChild(delBtn);
    }

    list.appendChild(el);
  });

  // pagination
  const pagination = document.getElementById('pagination');
  pagination.innerHTML = '';
  const meta = data.Metadata;
  if (meta.HasPrevious) {
    const prev = document.createElement('button'); prev.textContent = 'Prev'; prev.addEventListener('click', () => { pageNumber--; loadPosts(); });
    pagination.appendChild(prev);
  }
  pagination.appendChild(document.createTextNode(` Page ${meta.PageNumber} / ${meta.TotalPages} `));
  if (meta.HasNext) {
    const next = document.createElement('button'); next.textContent = 'Next'; next.addEventListener('click', () => { pageNumber++; loadPosts(); });
    pagination.appendChild(next);
  }

  // wire like buttons
  document.querySelectorAll('.btn-like').forEach(btn => {
    btn.addEventListener('click', async () => {
      const id = btn.getAttribute('data-id');
      if (!await ensureToken()) return alert('login to like');
      const r = await fetch(`/api/posts/${id}/likes`, { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } });
      if (r.status === 201) { loadPosts(); } else { alert('error liking'); }
    });
  });

  // show comments
  document.querySelectorAll('.btn-show-comments').forEach(btn => {
    btn.addEventListener('click', async () => {
      const id = btn.getAttribute('data-id');
      const r = await fetch(`/api/posts/${id}/comments`);
      if (!r.ok) return alert('failed loading comments');
      const comments = await r.json();
      showComments(id, comments);
    });
  });

  // edit
  document.querySelectorAll('.btn-edit').forEach(btn => {
    btn.addEventListener('click', async () => {
      const id = btn.getAttribute('data-id');
      const r = await fetch(`/api/posts/${id}`);
      if (!r.ok) return alert('cannot load post');
      const p = await r.json();
      showEditForm(p);
    });
  });

  // delete
  document.querySelectorAll('.btn-delete').forEach(btn => {
    btn.addEventListener('click', async () => {
      if (!await ensureToken()) return alert('login first');
      const id = btn.getAttribute('data-id');
      if (!confirm('Delete this post?')) return;
      const r = await fetch(`/api/posts/${id}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } });
      if (r.status === 204) { alert('deleted'); loadPosts(); } else { alert('delete failed'); }
    });
  });
}

function isAdmin() { return role && role === 'Admin'; }

function showComments(postId, comments) {
  const area = document.getElementById('comments-area');
  area.innerHTML = `<h3>Comments for post ${postId}</h3>`;
  comments.forEach(c => {
    const el = document.createElement('div');
    el.innerHTML = `<p>${escapeHtml(c.text)} <small>by ${escapeHtml(c.userId)}</small> ` +
      ((username && username === c.userId) || role === 'Admin' ? `<button data-id="${c.id}" class="btn-del-comment">Delete</button>` : '') + `</p>`;
    area.appendChild(el);
  });

  document.querySelectorAll('.btn-del-comment').forEach(btn => {
    btn.addEventListener('click', async () => {
      if (!await ensureToken()) return alert('login to delete');
      if (!confirm('Delete comment?')) return;
      const commentId = btn.getAttribute('data-id');
      const r = await fetch(`/api/posts/${postId}/comments/${commentId}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } });
      if (r.ok) { showComments(postId, await (await fetch(`/api/posts/${postId}/comments`)).json()); loadPosts(); } else { alert('delete failed'); }
    });
  });

  // add comment form
  const add = document.createElement('div');
  add.innerHTML = `<textarea id="new-comment"></textarea><button id="btn-add-comment">Add Comment</button>`;
  area.appendChild(add);
  document.getElementById('btn-add-comment').addEventListener('click', async () => {
    if (!await ensureToken()) return alert('login to comment');
    const text = document.getElementById('new-comment').value;
    const r = await fetch(`/api/posts/${postId}/comments`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify({ text }) });
    if (r.status === 201) { showComments(postId, await (await fetch(`/api/posts/${postId}/comments`)).json()); loadPosts(); } else { alert('failed'); }
  });
}

function escapeHtml(s) { return (s || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }

updateUserDisplay();
loadPosts();
