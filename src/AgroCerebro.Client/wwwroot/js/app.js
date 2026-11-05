window.agro = {
  get: k => { try { return localStorage.getItem('agro.' + k); } catch { return null; } },
  set: (k, v) => { try { localStorage.setItem('agro.' + k, v); } catch { } },
  remove: k => { try { localStorage.removeItem('agro.' + k); } catch { } },
  online: () => navigator.onLine,
  gps: () => new Promise((ok, no) => navigator.geolocation.getCurrentPosition(p => ok([p.coords.latitude, p.coords.longitude]), no)),
  watchOnline: ref => {
    const f = () => ref.invokeMethodAsync('SetOnline', navigator.onLine);
    window.addEventListener('online', f); window.addEventListener('offline', f);
  }
};
