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
        const isCoreKey = ['Escape', 'Tab', 'F9'].includes(event.code)
            || (event.shiftKey && ['F3', 'F4'].includes(event.code));
        if (isCoreKey) {
            event.preventDefault();
            if (!event.repeat) app.CoreKey(event.code, event.shiftKey);
            if (event.code === "Escape" && document.pointerLockElement === canvas) document.exitPointerLock();
            return;
        }
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
    let isRightDown = false;
    const orbitCursor = document.createElement('span');
    orbitCursor.hidden = true;
    orbitCursor.setAttribute('aria-hidden', 'true');
    orbitCursor.textContent = '↖';
    Object.assign(orbitCursor.style, {
        position: 'fixed', pointerEvents: 'none', zIndex: '1',
        color: 'white', textShadow: '0 0 2px black, 0 0 3px black',
        font: 'bold 18px/1 sans-serif'
    });
    document.body.append(orbitCursor);
    const updateOrbitCursor = () => {
        orbitCursor.hidden = !isRightDown || document.pointerLockElement !== canvas || app.CameraMotion(0,0,false);
    };
    document.addEventListener('pointerlockchange', updateOrbitCursor);
    const captureMouse = () => {
        if (!document.pointerLockElement) canvas.requestPointerLock?.()?.catch?.(() => {});
    };
    canvas.addEventListener('contextmenu', event => event.preventDefault());
    window.addEventListener('pointermove', event => {
        app.CameraMotion(event.movementX,event.movementY,isRightDown);
        app.Pointer(event.clientX,event.clientY,isPointerDown,0);
    });
    window.addEventListener('pointerdown', event => {
        if (event.button === 2) {
            isRightDown = true;
            orbitCursor.style.left = event.clientX + "px";
            orbitCursor.style.top = event.clientY + "px";
        }
        if (app.CameraMotion(0,0,false) || isRightDown) captureMouse();
        if (event.button !== 0) return;
        isPointerDown = true;
        app.Pointer(event.clientX,event.clientY,true,0);
    });
    window.addEventListener('pointerup', event => {
        if (event.button === 2) {
            isRightDown = false;
            updateOrbitCursor();
            if (!app.CameraMotion(0,0,false) && document.pointerLockElement === canvas) document.exitPointerLock();
        }
        if (event.button !== 0) return;
        isPointerDown = false;
        app.Pointer(event.clientX,event.clientY,false,0);
    });
    window.addEventListener('wheel', event => {
        event.preventDefault();
        const steps = -event.deltaY / (event.deltaMode === 1 ? 3 : event.deltaMode === 2 ? 1 : 100);
        app.Pointer(event.clientX,event.clientY,isPointerDown,steps);
        if (app.CameraMotion(0,0,false)) captureMouse();
        else if (!isRightDown && document.pointerLockElement === canvas) document.exitPointerLock();
        updateOrbitCursor();
    }, { passive:false });
    window.addEventListener('blur', () => { keys.clear(); isPointerDown = false; isRightDown = false; updateOrbitCursor(); app.Blur(); app.Pointer(-1,-1,false,0); });
    let start;
    function frame(time) {
        start ??= time;
        try {
            canvas.width = innerWidth; canvas.height = innerHeight;
            const scene = JSON.parse(app.Frame((time-start)/1000, axis('KeyW','KeyS'), axis('KeyD','KeyA'), keys.has('Space'), keys.has('KeyR'), axis('ArrowLeft','ArrowRight'), axis('ArrowUp','ArrowDown'), canvas.width, canvas.height));
            render(scene.scene);
            renderGui(scene.gui,canvas.width,canvas.height);
            status.textContent = 'WASD walk · Space jump · Right-drag look · Scroll zoom · R reset';
            globalThis.woorf = { app, scene, frames: (globalThis.woorf?.frames ?? 0) + 1 };
            requestAnimationFrame(frame);
        } catch (error) { status.textContent = `Game failed: ${error.message}`; console.error(error); }
    }
    requestAnimationFrame(frame);
} catch (error) { status.textContent = `Unable to start: ${error.message}`; console.error(error); }
