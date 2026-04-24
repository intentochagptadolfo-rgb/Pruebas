(() => {
  const videos = document.querySelectorAll('video');
  if (!videos.length) { alert('No hay <video> en esta página'); return; }

  const prev = document.getElementById('__volCtrl');
  if (prev) { prev.remove(); return; }

  const actual = Math.round((videos[0].volume || 0) * 100);

  const box = document.createElement('div');
  box.id = '__volCtrl';
  box.style.cssText = [
    'position:fixed','top:20px','right:20px','z-index:2147483647',
    'background:#1f1f1f','color:#fff','padding:10px 12px','border-radius:10px',
    'font:14px system-ui,sans-serif','box-shadow:0 6px 20px rgba(0,0,0,.45)',
    'display:flex','align-items:center','gap:10px','user-select:none'
  ].join(';');

  box.innerHTML =
    '<span>🔊</span>' +
    '<input id="__volSli" type="range" min="0" max="100" value="' + actual + '" style="width:180px">' +
    '<span id="__volVal" style="min-width:34px;text-align:right">' + actual + '%</span>' +
    '<button id="__volX" style="margin-left:4px;background:#444;color:#fff;border:0;border-radius:6px;padding:2px 8px;cursor:pointer">✕</button>';

  document.body.appendChild(box);

  const sli = box.querySelector('#__volSli');
  const val = box.querySelector('#__volVal');

  const aplicar = () => {
    const v = sli.value / 100;
    document.querySelectorAll('video').forEach(el => { el.volume = v; el.muted = v === 0; });
    val.textContent = sli.value + '%';
  };

  sli.addEventListener('input', aplicar);
  box.querySelector('#__volX').onclick = () => box.remove();

  let drag = false, offX = 0, offY = 0;
  box.addEventListener('mousedown', e => {
    if (e.target.id === '__volSli' || e.target.id === '__volX') return;
    drag = true; offX = e.clientX - box.offsetLeft; offY = e.clientY - box.offsetTop;
    box.style.right = 'auto';
  });
  document.addEventListener('mousemove', e => {
    if (!drag) return;
    box.style.left = (e.clientX - offX) + 'px';
    box.style.top  = (e.clientY - offY) + 'px';
  });
  document.addEventListener('mouseup', () => drag = false);
})();
