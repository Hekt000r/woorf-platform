import { spawn } from 'node:child_process';
import { readFile, writeFile, mkdtemp } from 'node:fs/promises';
import { join } from 'node:path';

const delay = milliseconds => new Promise(resolve => setTimeout(resolve,milliseconds));

export async function runPlayer(chrome, url, directory) {
    const profile = await mkdtemp(join(directory,'chrome-player-'));
    const child = spawn(chrome,['--headless=new','--no-first-run','--disable-background-networking',
        '--remote-debugging-port=0',`--user-data-dir=${profile}`,'--window-size=960,640','about:blank'],{stdio:'ignore'});
    let client;
    try {
        const port = await readPort(profile);
        const pages = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
        const page = pages.find(page => page.type==='page');
        client = await connect(page.webSocketDebuggerUrl);
        await client.call('Page.enable');
        await client.call('Page.navigate',{url});
        const result = await waitForResult(client);
        const screenshot = await client.call('Page.captureScreenshot',{format:'png'});
        await writeFile(join(directory,'player.png'),Buffer.from(screenshot.data,'base64'));
        if (result.error) throw new Error(result.error);
        for (const check of result.results) console.log(`${check.passed ? 'PASS' : 'FAIL'} ${check.name}`);
        if (!result.passed) throw new Error('WASM browser player checks failed.');
    } finally {
        if (client) { await client.call('Browser.close').catch(() => {}); client.close(); }
        child.kill();
    }
}

async function readPort(profile) {
    for (let index=0;index<100;index++) {
        try { return Number((await readFile(join(profile,'DevToolsActivePort'),'utf8')).split('\n')[0]); }
        catch { await delay(100); }
    }
    throw new Error('Headless Chrome did not open its local test endpoint.');
}

async function connect(url) {
    const socket = new WebSocket(url), pending = new Map(); let sequence = 0;
    await new Promise((resolve,reject) => { socket.onopen=resolve; socket.onerror=reject; });
    socket.onmessage = event => {
        const message = JSON.parse(event.data), request = pending.get(message.id);
        if (!request) return;
        pending.delete(message.id);
        if (message.error) request.reject(new Error(message.error.message)); else request.resolve(message.result);
    };
    socket.onclose = () => { for (const request of pending.values()) request.reject(new Error('Browser closed.')); pending.clear(); };
    return {
        call(method,params={}) {
            return new Promise((resolve,reject) => {
                const id = ++sequence; pending.set(id,{resolve,reject}); socket.send(JSON.stringify({id,method,params}));
            });
        },
        close:() => socket.close(),
    };
}

async function waitForResult(client) {
    for (let index=0;index<450;index++) {
        const response = await client.call('Runtime.evaluate',{
            expression:'document.getElementById("result")?.textContent',returnByValue:true,
        });
        const value = response.result?.value;
        if (value && value !== 'RUNNING') return JSON.parse(value);
        await delay(100);
    }
    throw new Error('Browser player checks timed out.');
}
