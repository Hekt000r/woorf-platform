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
        if (!event.repeat && /^Digit[0-9]$/.test(event.code) && app.CoreKey(event.key, false)) {
            event.preventDefault();
            return;
        }
        const input = inputKey(event.code);
        const consumed = input && app.InputKey(input,true);
        if (!consumed) keys.add(event.code);
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
    canvas.style.cursor = 'none';
    const sendPointer = (event, dx = 0, dy = 0, wheel = 0) => {
        if (event.pointerType === "touch") app.Pointer(event.clientX,event.clientY,isPointerDown,wheel);
        else app.GamePointer(event.clientX,event.clientY,dx,dy,isPointerDown,wheel,isRightDown);
    };
    const captureMouse = () => {
        if (!document.pointerLockElement) canvas.requestPointerLock?.()?.catch?.(() => {});
    };
    canvas.addEventListener('contextmenu', event => event.preventDefault());
    window.addEventListener('pointermove', event => {
        app.CameraMotion(event.movementX,event.movementY,isRightDown);
        sendPointer(event,event.movementX,event.movementY);
    });
    window.addEventListener('pointerdown', event => {
        if (event.button === 2) isRightDown = true;
        if (app.CameraMotion(0,0,false)) captureMouse();
        else if (event.button === 2 && event.target === canvas) canvas.setPointerCapture(event.pointerId);
        if (event.button === 0) isPointerDown = true;
        sendPointer(event);
    });
    window.addEventListener('pointerup', event => {
        if (event.button === 2) {
            isRightDown = false;
            if (!app.CameraMotion(0,0,false) && document.pointerLockElement === canvas) document.exitPointerLock();
        }
        if (event.button !== 0) return;
        isPointerDown = false;
        sendPointer(event);
    });
    const endOrbit = () => {
        isRightDown = false;
    };
    canvas.addEventListener('lostpointercapture', endOrbit);
    canvas.addEventListener('pointercancel', endOrbit);
    window.addEventListener('wheel', event => {
        event.preventDefault();
        const steps = -event.deltaY / (event.deltaMode === 1 ? 3 : event.deltaMode === 2 ? 1 : 100);
        sendPointer(event,0,0,steps);
        if (app.CameraMotion(0,0,false)) captureMouse();
        else if (document.pointerLockElement === canvas) document.exitPointerLock();
    }, { passive:false });
    window.addEventListener('blur', () => { keys.clear(); isPointerDown = false; isRightDown = false; app.Blur(); app.Pointer(-1,-1,false,0); });
    const gamepadButtons = ['ButtonA', 'ButtonB', 'ButtonX', 'ButtonY', 'ButtonL1', 'ButtonR1',
        'ButtonL2', 'ButtonR2', 'ButtonSelect', 'ButtonStart', 'ButtonL3', 'ButtonR3',
        'DPadUp', 'DPadDown', 'DPadLeft', 'DPadRight'];
    const pollGamepad = () => {
        const pads = document.hasFocus() ? navigator.getGamepads?.() : null;
        const pad = pads && Array.from(pads).find(item => item?.mapping === 'standard');
        gamepadButtons.forEach((name, index) => app.GamepadButton(name, pad?.buttons[index]?.pressed ?? false));
    };
    let start;
    function frame(time) {
        start ??= time;
        try {
            pollGamepad();
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
