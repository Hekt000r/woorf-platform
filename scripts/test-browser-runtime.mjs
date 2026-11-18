import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const directory = resolve(process.argv[2] ?? '.local/browser-obby/wwwroot');
const { dotnet } = await import(pathToFileURL(resolve(directory,'_framework/dotnet.js')).href);
const runtime = await dotnet.create();
const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
const app = exports.BrowserApp;

const packageFile = await readFile(resolve(directory,'game.json'),'utf8');
app.Start(packageFile);
let frame;
const initial = JSON.parse(app.Frame(0,0,0,false,false,0,0,960,640));
assert.ok(initial.scene.sky && initial.scene.sky.images?.length === 6,'The browser must receive the same six decoded skybox faces as native.');
assert.equal(Buffer.from(initial.scene.sky.images[0].pixels,'base64').length, initial.scene.sky.images[0].width*initial.scene.sky.images[0].height*4,
    'Sky pixels must be transported without replacing the bundled texture.');
console.log('PASS actual WASM supplies six shared decoded skybox textures');
for (let index=0;index<60;index++) frame = JSON.parse(app.Frame(index/60,0,0,false,false,0,0,960,640));
assert.equal(frame.scene.sky.images,null,'Unchanged sky textures must not be serialized again on every frame.');
assert.ok(frame.scene.itemCount >= 20,'Packaged obby must create its course and character through Luau.');
assert.ok(frame.gui.some(item => item.Kind === 'Text'),'Client LocalScript must create a visible HUD.');
console.log('PASS actual WASM executes packaged Luau game, physics and client HUD');

const fixture = { files: {
    'woorf.project.json': '{"name":"WASM runtime fixture","root":"src"}',
    'src/ServerScriptService/Main.server.luau': `
        local floor = Instance.new("Part",workspace)
        floor.Name = "BrowserFloor"; floor.Size = Vector3.new(100,1,100); floor.Anchored = true
        floor.CustomPhysicalProperties = PhysicalProperties.new(.7,.5,0)
        workspace.Gravity = 80; workspace:SetAttribute("CharacterSpawn",Vector3.new(0,4,0))
        local remote = Instance.new("RemoteEvent",game:GetService("ReplicatedStorage")); remote.Name = "Click"
        remote.OnServerEvent:Connect(function(player) player:SetAttribute("Clicked",true) end)
    `,
    'src/StarterPlayer/StarterPlayerScripts/Main.client.luau': `
        local player = game:GetService("Players").LocalPlayer
        local screen = Instance.new("ScreenGui",player:WaitForChild("PlayerGui"))
        screen.ResetOnSpawn = false
        local button = Instance.new("TextButton",screen)
        button.Position = UDim2.fromOffset(10,10); button.Size = UDim2.fromOffset(120,40); button.Text = "Click me"
        button.Activated:Connect(function() game:GetService("ReplicatedStorage").Click:FireServer() end)
        player:GetAttributeChangedSignal("Clicked"):Connect(function() button.Text = "Server replied" end)
        local box = Instance.new("TextBox",screen)
        box.Position = UDim2.fromOffset(10,70); box.Size = UDim2.fromOffset(120,40)
        box.ClearTextOnFocus = false; box.Text = "abcd"
    `
} };
app.Start(JSON.stringify(fixture));
frame = JSON.parse(app.Frame(0,0,0,false,false,0,0,960,640));
assert.equal(frame.scene.itemCount,9,'Replacing a game must not retain the previous course or scripts.');
app.Pointer(30,25,true,0); app.Pointer(30,25,false,0);
for (let index=1;index<=6;index++) frame = JSON.parse(app.Frame(index/60,0,0,false,false,0,0,960,640));
assert.ok(frame.gui.some(item => item.Content === 'Server replied'),'GUI activation must reach the authoritative VM and replicate its reply.');
console.log('PASS actual WASM GUI activation crosses isolated client/server VMs');
app.Pointer(30,85,true,0); app.Pointer(30,85,false,0);
app.KeyInput('Home',false); app.KeyInput('Right',true); app.TextInput('Z');
frame = JSON.parse(app.Frame(7/60,0,0,false,false,0,0,960,640));
assert.ok(frame.gui.some(item => item.Content === 'Zbcd'),'Browser keyboard navigation must edit the selected range.');
assert.ok(frame.gui.some(item => item.Kind === 'Rectangle' && item.Bounds.Width === 1),'Focused browser textbox must draw its caret.');
app.Blur();
console.log('PASS actual WASM textbox navigation edits selection and draws its caret');
for (let index=7;index<=66;index++) frame = JSON.parse(app.Frame(index/60,0,1,false,false,0,0,960,640));
assert.ok(Number(frame.position.split(',')[0]) > 12,'Camera-relative right input must move at the managed Humanoid walking speed.');
console.log('PASS actual WASM movement advances managed character physics');
const torso = frame.scene.batches[0].data.slice(28,44);
assert.ok(Math.abs(torso[2]) > 1 && Math.abs(torso[0]) < .05,
    'Walking right must turn the posed torso toward +X rather than keep its spawn orientation.');
console.log('PASS actual WASM character turns toward its movement direction');
const ground = Number(frame.position.split(',')[1]);
frame = JSON.parse(app.Frame(67/60,0,0,true,false,0,0,960,640));
for (let index=68;index<=76;index++) frame = JSON.parse(app.Frame(index/60,0,0,false,false,0,0,960,640));
assert.ok(Number(frame.position.split(',')[1]) > ground+2,'Grounded jump input must launch the managed character collider.');
console.log('PASS actual WASM grounded jump uses the shared physics controller');
frame = JSON.parse(app.Frame(2,0,-1,false,false,0,0,960,640));
const slowTorso = frame.scene.batches[0].data.slice(28,44);
assert.ok(slowTorso[2] < -1 && Math.abs(slowTorso[0]) < .05,
    'A long browser frame must still rotate the character toward the new movement input.');
console.log('PASS actual WASM turns the character across a long frame gap');
app.Start(JSON.stringify(fixture));
let walkingSamples = 0;
for (let index=0;index<360;index++) {
    const now = index/60;
    const sample = JSON.parse(app.Frame(now,index>120 ? 1 : 0,0,index===30,false,0,0,960,640));
    if (index<150 || index>210) continue;
    const data = sample.scene.batches[0].data;
    const leftLeg = data.slice(4*28,4*28+16), rightLeg = data.slice(5*28,5*28+16);
    if (Math.abs(leftLeg[6])>.05 && leftLeg[6]*rightLeg[6]<0) walkingSamples++;
}
assert.ok(walkingSamples>30,'Walking animation must resume after a completed jump and landing.');
console.log('PASS actual WASM resumes walking animation after landing');
for (const frequency of [144,240,30,5]) {
    app.Start(JSON.stringify(fixture));
    let moving = 0, samples = 0;
    for (let index=0;index<frequency*4;index++) {
        const now = index/frequency;
        const sample = JSON.parse(app.Frame(now,now>2 ? 1 : 0,0,index===Math.round(frequency*.5),false,0,0,960,640));
        if (now<2.5 || now>3.5) continue;
        const data = sample.scene.batches[0].data;
        if (Math.abs(data[4*28+6])>.005 && data[4*28+6]*data[5*28+6]<0) moving++;
        samples++;
    }
    assert.ok(moving/samples>.9,`Walking after landing must keep its pose between physics publications at ${frequency} Hz (${moving}/${samples}).`);
    console.log(`PASS actual WASM post-landing walk remains animated at ${frequency} Hz`);
}
await runtime.exit(0);
