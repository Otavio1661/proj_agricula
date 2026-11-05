const puppeteer = require('puppeteer-core');
const BASE = process.env.BASE || 'http://localhost:5015';
const rotas = ['', 'mapa', 'talhoes', 'recomendacoes', 'clima', 'alertas', 'estoque', 'whatsapp', 'drone', 'catalogo', 'propriedades'];
(async () => {
  const b = await puppeteer.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: 'new', args: ['--no-sandbox'] });
  const p = await b.newPage();
  await p.setViewport({ width: 1280, height: 800 });
  const erros = [];
  p.on('console', m => { if (['error', 'warning'].includes(m.type())) erros.push(`[console.${m.type()}] ${m.text()}`.slice(0, 220)); });
  p.on('pageerror', e => erros.push('[pageerror] ' + String(e).slice(0, 220)));
  p.on('response', r => { if (r.status() >= 400 && !r.url().includes('favicon')) erros.push(`[http ${r.status()}] ${r.request().method()} ${r.url()}`); });
  await p.goto(BASE + '/login', { waitUntil: 'networkidle0' });
  await p.type('#l-user', 'admin');
  await p.type('#l-senha', 'admin123!');
  await Promise.all([p.waitForNetworkIdle({ idleTime: 1500 }), p.click('button.btn-success, button.btn-primary, button[type=submit]')]);
  console.log('após login:', p.url());
  for (const r of rotas) {
    erros.length = 0;
    await p.goto(`${BASE}/${r}`, { waitUntil: 'networkidle0' });
    await new Promise(r => setTimeout(r, 1200));
    const txt = await p.evaluate(() => document.querySelector('.content')?.innerText.replace(/\s+/g, ' ').slice(0, 160));
    const sel = await p.evaluate(() => document.querySelector('.topbar select')?.innerText.replace(/\s+/g, ' '));
    await p.screenshot({ path: `shot_${r || 'painel'}.png` });
    console.log(`/${r} | prop="${sel}" | ${txt}\n   erros: ${erros.length ? erros.join(' || ') : 'nenhum'}`);
  }
  await b.close();
})().catch(e => { console.error('FALHA', e); process.exit(1); });
