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

  const btn = 'background:#444;color:#fff;border:0;border-radius:6px;padding:3px 9px;cursor:pointer;font-size:14px';

  box.innerHTML =
    '<span>🔊</span>' +
    '<input id="__volSli" type="range" min="0" max="100" value="' + actual + '" style="width:160px">' +
    '<span id="__volVal" style="min-width:34px;text-align:right">' + actual + '%</span>' +
    '<button id="__volFs" title="Pantalla completa" style="' + btn + '">⛶</button>' +
    '<button id="__volX"  title="Cerrar"           style="' + btn + ';margin-left:2px">✕</button>';

  document.body.appendChild(box);

  const sli = box.querySelector('#__volSli');
  const val = box.querySelector('#__volVal');

  const aplicar = () => {
    const v = sli.value / 100;
    document.querySelectorAll('video').forEach(el => { el.volume = v; el.muted = v === 0; });
    val.textContent = sli.value + '%';
  };
  sli.addEventListener('input', aplicar);

  const elegirVideo = () => {
    const vs = [...document.querySelectorAll('video')];
    const reproduciendo = vs.find(v => !v.paused && !v.ended && v.readyState > 2);
    if (reproduciendo) return reproduciendo;
    return vs.map(v => {
      const r = v.getBoundingClientRect();
      return { v, area: Math.max(0, r.width) * Math.max(0, r.height) };
    }).sort((a,b) => b.area - a.area)[0]?.v || vs[0];
  };

  const pedirFs = el => (el.requestFullscreen || el.webkitRequestFullscreen || el.webkitEnterFullscreen || el.mozRequestFullScreen || el.msRequestFullscreen).call(el);

  box.querySelector('#__volFs').onclick = async () => {
    if (document.fullscreenElement || document.webkitFullscreenElement) {
      (document.exitFullscreen || document.webkitExitFullscreen).call(document);
      return;
    }
    const video = elegirVideo();
    if (!video) return;
    try { await pedirFs(video); }
    catch (e1) {
      try { await pedirFs(video.parentElement); }
      catch (e2) { alert('No se pudo entrar en pantalla completa: ' + (e2.message || e1.message)); }
    }
  };

  const seek = segs => {
    const v = elegirVideo();
    if (!v || !isFinite(v.duration)) return;
    v.currentTime = Math.max(0, Math.min(v.duration, v.currentTime + segs));
    val.textContent = (segs > 0 ? '+' : '') + segs + 's';
    clearTimeout(box.__t);
    box.__t = setTimeout(() => val.textContent = sli.value + '%', 700);
  };

  const onKey = e => {
    const t = e.target;
    const escribiendo = t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.isContentEditable);
    if (escribiendo) return;
    if (e.key === 'ArrowLeft')  { e.preventDefault(); e.stopPropagation(); seek(-5); }
    if (e.key === 'ArrowRight') { e.preventDefault(); e.stopPropagation(); seek(+5); }
  };
  window.addEventListener('keydown', onKey, true);

  box.querySelector('#__volX').onclick = () => {
    window.removeEventListener('keydown', onKey, true);
    box.remove();
  };

  let drag = false, offX = 0, offY = 0;
  box.addEventListener('mousedown', e => {
    if (['__volSli','__volX','__volFs'].includes(e.target.id)) return;
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
