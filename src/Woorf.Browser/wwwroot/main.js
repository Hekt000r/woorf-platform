import { dotnet } from './_framework/dotnet.js';
import { createRenderer } from './renderer.js';
import { createGuiRenderer } from './gui.js';

const status = document.getElementById('status');
try {
    const runtime = await dotnet.create();
    const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
    const app = exports.BrowserApp;
    app.Start(await (await fetch('./game.json')).text());
    const canvas = document.getElementById('game');
    const render = createRenderer(canvas, JSON.parse(app.Meshes()), JSON.parse(app.Shaders()));
    const renderGui = createGuiRenderer(document.getElementById('gui'));
    const keys = new Set();
    const axis = (positive, negative) => Number(keys.has(positive)) - Number(keys.has(negative));
    const inputKey = code => /^Key[A-Z]$/.test(code) ? code.slice(3) : code==='Space'?'Space':null;
    window.addEventListener('keydown', event => {
        keys.add(event.code);
        if(inputKey(event.code)) app.InputKey(inputKey(event.code),true);
        const navigation = { ArrowLeft:'Left', ArrowRight:'Right', Home:'Home', End:'End', Delete:'Delete' };
        const key = (event.ctrlKey || event.metaKey) && event.code === 'KeyA' ? 'SelectAll' : navigation[event.key];
        if (key) { app.KeyInput(key,event.shiftKey); event.preventDefault(); }
        if (event.key.length === 1 && !event.ctrlKey && !event.metaKey) app.TextInput(event.key);
        if (event.key === 'Backspace') app.TextInput('\b');
        if (event.key === 'Enter') app.TextInput('\n');
        if (['Space','ArrowUp','ArrowDown','ArrowLeft','ArrowRight','Backspace'].includes(event.code)) event.preventDefault();
    });
    window.addEventListener('keyup', event => { keys.delete(event.code); if(inputKey(event.code)) app.InputKey(inputKey(event.code),false); });
    let isPointerDown = false;
    window.addEventListener('pointermove', event => app.Pointer(event.clientX,event.clientY,isPointerDown,0));
    window.addEventListener('pointerdown', event => { isPointerDown = true; app.Pointer(event.clientX,event.clientY,true,0); });
    window.addEventListener('pointerup', event => { isPointerDown = false; app.Pointer(event.clientX,event.clientY,false,0); });
    window.addEventListener('wheel', event => app.Pointer(event.clientX,event.clientY,isPointerDown,event.deltaY), { passive:true });
    window.addEventListener('blur', () => { keys.clear(); isPointerDown = false; app.Blur(); app.Pointer(-1,-1,false,0); });
    let start;
    function frame(time) {
        start ??= time;
        try {
            canvas.width = innerWidth; canvas.height = innerHeight;
            const scene = JSON.parse(app.Frame((time-start)/1000, axis('KeyW','KeyS'), axis('KeyD','KeyA'), keys.has('Space'), keys.has('KeyR'), axis('ArrowLeft','ArrowRight'), axis('ArrowUp','ArrowDown'), canvas.width, canvas.height));
            render(scene.scene);
            renderGui(scene.gui,canvas.width,canvas.height);
            status.textContent = 'WASD walk · Space jump · Arrows orbit · R reset';
            globalThis.woorf = { app, scene, frames: (globalThis.woorf?.frames ?? 0) + 1 };
            requestAnimationFrame(frame);
        } catch (error) { status.textContent = `Game failed: ${error.message}`; console.error(error); }
    }
    requestAnimationFrame(frame);
} catch (error) { status.textContent = `Unable to start: ${error.message}`; console.error(error); }
