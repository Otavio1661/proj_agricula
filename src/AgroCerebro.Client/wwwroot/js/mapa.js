window.mapa = (() => {
  let map, layerTalhoes, layerDraw, overlays = {}, drawing = false, pts = [], marker;
  const areaHa = ll => { // projeção equiretangular local + shoelace
    if (ll.length < 3) return 0;
    const R = 6378137, lat0 = ll.reduce((a, p) => a + p.lat, 0) / ll.length * Math.PI / 180;
    const xy = ll.map(p => [R * p.lng * Math.PI / 180 * Math.cos(lat0), R * p.lat * Math.PI / 180]);
    let s = 0; for (let i = 0; i < xy.length; i++) { const j = (i + 1) % xy.length; s += xy[i][0] * xy[j][1] - xy[j][0] * xy[i][1]; }
    return Math.abs(s) / 2 / 10000;
  };
  const redraw = () => {
    layerDraw.clearLayers();
    pts.forEach(p => L.circleMarker(p, { radius: 5, color: '#e67e22' }).addTo(layerDraw));
    if (pts.length > 1) L.polygon(pts, { color: '#e67e22', dashArray: '4' }).addTo(layerDraw);
  };
  const vari = (url, cb) => {
    const img = new Image(); img.crossOrigin = 'anonymous';
    img.onload = () => {
      const c = document.createElement('canvas'); c.width = img.naturalWidth; c.height = img.naturalHeight;
      const x = c.getContext('2d'); x.drawImage(img, 0, 0);
      const d = x.getImageData(0, 0, c.width, c.height), a = d.data;
      for (let i = 0; i < a.length; i += 4) {
        const r = a[i], g = a[i + 1], b = a[i + 2], den = g + r - b;
        let v = den === 0 ? 0 : (g - r) / den; v = Math.max(-0.1, Math.min(0.4, v)); const t = (v + 0.1) / 0.5;
        a[i] = Math.round(220 * (1 - t) + 20 * t); a[i + 1] = Math.round(50 + 160 * t); a[i + 2] = 40;
      }
      x.putImageData(d, 0, 0); cb(c.toDataURL('image/png'));
    };
    img.src = url;
  };
  return {
    init(id, lat, lon, ref) {
      if (map) { map.remove(); map = null; }
      const osm = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19, attribution: '© OpenStreetMap' });
      const sat = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', { maxZoom: 19, attribution: 'Esri World Imagery' });
      map = L.map(id, { layers: [sat] }).setView([lat, lon], 15);
      L.control.layers({ 'Satélite': sat, 'Mapa': osm }).addTo(map);
      layerTalhoes = L.featureGroup().addTo(map); layerDraw = L.featureGroup().addTo(map);
      marker = L.marker([lat, lon]).addTo(map);
      map.on('click', e => { if (!drawing) return; pts.push(e.latlng); redraw(); });
      overlays = {};
    },
    // Visão "Todas as fazendas": marcador + talhões coloridos por fazenda, enquadrando tudo.
    initTodas(id, fazendas) {
      if (map) { map.remove(); map = null; }
      const osm = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19, attribution: '© OpenStreetMap' });
      const sat = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', { maxZoom: 19, attribution: 'Esri World Imagery' });
      map = L.map(id, { layers: [sat] }).setView([-14, -52], 4);
      L.control.layers({ 'Satélite': sat, 'Mapa': osm }).addTo(map);
      layerTalhoes = L.featureGroup().addTo(map); layerDraw = L.featureGroup().addTo(map);
      overlays = {};
      const all = L.featureGroup();
      fazendas.forEach(f => {
        L.marker([f.lat, f.lon]).bindTooltip(f.nome, { permanent: true, direction: 'top', offset: [-14, -8] }).addTo(all);
        (f.talhoes || []).forEach(t => { if (!t.geoJson) return; try {
          L.geoJSON(JSON.parse(t.geoJson), { style: { color: f.cor, weight: 2, fillColor: f.cor, fillOpacity: .22 } })
            .bindTooltip(t.nome + ' · ' + f.nome).addTo(all);
        } catch { } });
      });
      all.addTo(layerTalhoes);
      if (all.getLayers().length) map.fitBounds(all.getBounds().pad(.2), { maxZoom: 15 });
    },
    focus(lat, lon, z) { if (map) map.setView([lat, lon], z || 14); },
    fitAll() { if (map && layerTalhoes.getLayers().length) map.fitBounds(layerTalhoes.getBounds().pad(.2), { maxZoom: 15 }); },
    addOverlayQuiet(id, url, b) {
      if (overlays[id]) map.removeLayer(overlays[id].l);
      overlays[id] = { l: L.imageOverlay(url, [[b[0], b[1]], [b[2], b[3]]], { opacity: .8 }).addTo(map), url, vari: false };
    },
    setTalhoes(list) {
      layerTalhoes.clearLayers();
      list.forEach(t => { if (!t.geoJson) return; try {
        L.geoJSON(JSON.parse(t.geoJson), { style: { color: '#7CFC00', weight: 2, fillOpacity: .12 } }).bindTooltip(t.nome, { permanent: true, direction: 'center' }).addTo(layerTalhoes);
      } catch { } });
      if (layerTalhoes.getLayers().length) map.fitBounds(layerTalhoes.getBounds().pad(.2));
    },
    startDraw() { drawing = true; pts = []; redraw(); },
    undo() { pts.pop(); redraw(); },
    cancelDraw() { drawing = false; pts = []; redraw(); },
    finishDraw() {
      drawing = false; if (pts.length < 3) { pts = []; redraw(); return null; }
      const ring = pts.map(p => [p.lng, p.lat]); ring.push(ring[0]);
      const res = { geoJson: JSON.stringify({ type: 'Polygon', coordinates: [ring] }), areaHa: Math.round(areaHa(pts) * 100) / 100 };
      pts = []; redraw(); return res;
    },
    addOverlay(id, url, b, tipo) {
      if (overlays[id]) map.removeLayer(overlays[id].l);
      const l = L.imageOverlay(url, [[b[0], b[1]], [b[2], b[3]]], { opacity: .8 }).addTo(map);
      overlays[id] = { l, url, vari: false };
      map.fitBounds([[b[0], b[1]], [b[2], b[3]]]);
    },
    clearOverlays() { Object.values(overlays).forEach(o => map.removeLayer(o.l)); overlays = {}; },
    setOpacity(id, v) { overlays[id]?.l.setOpacity(v); },
    setVari(id, on) {
      const o = overlays[id]; if (!o) return;
      if (on) vari(o.url, u => o.l.setUrl(u)); else o.l.setUrl(o.url);
    },
    getBounds() { const b = map.getBounds(); return [b.getSouth(), b.getWest(), b.getNorth(), b.getEast()]; },
    dispose() { if (map) { map.remove(); map = null; } }
  };
})();
