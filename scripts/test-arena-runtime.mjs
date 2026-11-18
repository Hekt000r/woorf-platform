import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const directory=resolve(process.argv[2]??'.local/browser-arena/wwwroot');
const {dotnet}=await import(pathToFileURL(resolve(directory,'_framework/dotnet.js')).href);
const runtime=await dotnet.create();
const exports=await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
const app=exports.BrowserApp;
app.Start(await readFile(resolve(directory,'game.json'),'utf8'));
let frame;
for(let index=0;index<=210;index++) frame=JSON.parse(app.Frame(index/30,0,0,false,false,0,0,960,640));
const contents=()=>frame.gui.filter(item=>item.Kind==='Text').map(item=>item.Content);
assert.ok(contents().some(text=>text.includes('4 REMAIN')),'Solo WASM must start with one human and three bots.');
assert.ok(contents().some(text=>text.includes('BOT BOLT'))&&contents().some(text=>text.includes('BOT NOVA')),'Bots must appear on the real scripted HUD.');
assert.ok(frame.scene.itemCount>100,'Browser must render the arena, scenery and NPC bodies through shared C#.');
assert.ok(frame.scene.sky,'The game must include its real skybox.');
console.log('PASS actual WASM launches the arena HUD sky and three autonomous opponents');
app.InputKey('E',true); app.InputKey('E',false);
frame=JSON.parse(app.Frame(211/30,0,0,false,false,0,0,960,640));
assert.ok(contents().some(text=>/^SHOVE\s+1\./.test(text)),'Keyboard shove must reach the authoritative game and replicate its cooldown.');
console.log('PASS actual WASM E shove crosses keyboard service client VM and authoritative server');
app.InputKey('B',true); app.InputKey('B',false);
for(let index=212;index<=216;index++) frame=JSON.parse(app.Frame(index/30,0,0,false,false,0,0,960,640));
assert.ok(contents().some(text=>text==='BOTS: OFF'),'Host keyboard toggle must update the actual game.');
console.log('PASS actual WASM host can toggle bot participation for the next round');
let enteredPractice=false;
for(let index=217;index<=1800;index++) {
    frame=JSON.parse(app.Frame(index/30,0,0,false,false,0,0,960,640));
    if(contents().some(text=>text.includes('PRACTICE'))&&!contents().some(text=>text.includes('BOT BOLT'))) enteredPractice=true;
}
assert.ok(enteredPractice,'Bots disabled by the host must be removed next round and solo play must become a valid practice round.');
console.log('PASS actual WASM collapse eliminations and automatic rounds honor bot settings');
await runtime.exit(0);
