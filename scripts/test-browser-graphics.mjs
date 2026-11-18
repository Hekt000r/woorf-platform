import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { createServer } from 'node:http';
import { readFile, mkdir } from 'node:fs/promises';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { runPlayer } from './browser-player-test.mjs';

const run = promisify(execFile);
const repo = resolve(fileURLToPath(new URL('..',import.meta.url)));
const isArena=process.argv.includes('--arena');
const directory = resolve(repo,isArena?'.local/arena-parity':'.local/renderer-parity');
const chrome = process.env.CHROME_PATH ?? 'C:/Program Files/Google/Chrome/Application/chrome.exe';
await mkdir(directory,{recursive:true});
const exportResult = await run('dotnet',[
    resolve(repo,'tests/Woorf.Cli.Tests/bin/Debug/net9.0/Woorf.Cli.Tests.dll'),'--renderer-parity',directory,
],{cwd:repo,timeout:30000});
process.stdout.write(exportResult.stdout);
const routes = new Map([
    ['/test.html',[resolve(repo,'scripts/browser-graphics.html'),'text/html']],
    ['/renderer.js',[resolve(repo,'src/Woorf.Browser/wwwroot/renderer.js'),'text/javascript']],
    ['/fixtures.json',[resolve(directory,'fixtures.json'),'application/json']],
    ['/player.html',[resolve(repo,isArena?'scripts/browser-arena.html':'scripts/browser-player.html'),'text/html']],
]);
const server = createServer(async (request,response) => {
    let route = routes.get(request.url);
    if (!route) {
        const root = resolve(repo,isArena?'.local/browser-arena/wwwroot':'.local/browser-obby/wwwroot');
        const path = resolve(root,'.'+decodeURIComponent(request.url.split('?')[0]));
        const extension = path.split('.').at(-1);
        if (path.startsWith(root+'/') || path.startsWith(root+'\\'))
            route = [path,({js:'text/javascript',wasm:'application/wasm',json:'application/json'})[extension] ?? 'application/octet-stream'];
    }
    if (!route) { response.writeHead(404).end(); return; }
    try { const data = await readFile(route[0]); response.writeHead(200,{'Content-Type':route[1]}); response.end(data); }
    catch { response.writeHead(404).end(); }
});
await new Promise(resolve => server.listen(0,'127.0.0.1',resolve));
try {
    const page = 'test';
    const { stdout } = await run(chrome,[
        '--headless=new','--no-first-run','--disable-background-networking',
        `--user-data-dir=${resolve(directory,'chrome-profile')}`,
        '--dump-dom','--virtual-time-budget=20000',
        '--window-size=960,640',
        `--screenshot=${resolve(directory,page+'.png')}`,
        `http://127.0.0.1:${server.address().port}/${page}.html`,
    ],{timeout:60000,maxBuffer:4*1024*1024});
    const match = stdout.match(/<pre id="result">([^<]+)<\/pre>/);
    if (!match || match[1] === 'RUNNING') throw new Error('Browser graphics checks did not complete.');
    const result = JSON.parse(match[1].replaceAll('&quot;','"').replaceAll('&amp;','&'));
    if (result.error) throw new Error(result.error);
    for (const check of result.results)
        console.log(`${check.passed ? 'PASS' : 'FAIL'} ${check.name}` + (check.mismatch === undefined ? '' : `: ${(100*check.mismatch).toFixed(3)}% native/WebGL pixels differ by more than 3/255`));
    if (!result.passed) process.exitCode = 1;
    await runPlayer(chrome,`http://127.0.0.1:${server.address().port}/player.html`,directory);
} finally { server.closeAllConnections(); server.close(); }
