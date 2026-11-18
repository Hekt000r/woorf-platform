// WebGL resource uploads and draw submission. Scene policy is supplied by C#.
export function createRenderer(canvas, meshes, shaders) {
    const gl = canvas.getContext('webgl2', { antialias: false });
    if (!gl) throw new Error('WebGL 2 is required.');
    const program = createProgram(gl, shaders.scene);
    const geometry = meshes.map(mesh => createMesh(gl, mesh, shaders.stride));
    const sky = createSky(gl, shaders.sky);
    const camera = gl.getUniformLocation(program,'viewProjection'), eye = gl.getUniformLocation(program,'eye');
    gl.enable(gl.DEPTH_TEST); gl.enable(gl.CULL_FACE);
    return scene => {
        gl.viewport(0,0,canvas.width,canvas.height);
        gl.clearColor(...scene.clear,1); gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);
        if (scene.sky) sky(scene.sky);
        gl.useProgram(program); gl.uniformMatrix4fv(camera,false,scene.camera); gl.uniform3fv(eye,scene.eye);
        for (let index=0;index<geometry.length;index++) {
            const mesh = geometry[index], batch = scene.batches[index];
            if (!batch.count) continue;
            gl.bindVertexArray(mesh.vao); gl.bindBuffer(gl.ARRAY_BUFFER,mesh.instances);
            gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(batch.data),gl.STREAM_DRAW);
            gl.drawElementsInstanced(gl.TRIANGLES,mesh.count,gl.UNSIGNED_INT,0,batch.count);
        }
        gl.bindVertexArray(null);
        const error = gl.getError();
        if (error !== gl.NO_ERROR) throw new Error(`WebGL error 0x${error.toString(16)}`);
    };
}

function createProgram(gl, sources) {
    const program = gl.createProgram();
    for (const [type, source] of [[gl.VERTEX_SHADER,sources.vertex],[gl.FRAGMENT_SHADER,sources.fragment]]) {
        const shader = gl.createShader(type); gl.shaderSource(shader,source); gl.compileShader(shader);
        if (!gl.getShaderParameter(shader,gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(shader));
        gl.attachShader(program,shader); gl.deleteShader(shader);
    }
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program,gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(program));
    return program;
}

function createMesh(gl, mesh, stride) {
    const vao = gl.createVertexArray(); gl.bindVertexArray(vao);
    gl.bindBuffer(gl.ARRAY_BUFFER,gl.createBuffer());
    gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(mesh.Vertices),gl.STATIC_DRAW);
    gl.enableVertexAttribArray(0); gl.vertexAttribPointer(0,3,gl.FLOAT,false,24,0);
    gl.enableVertexAttribArray(1); gl.vertexAttribPointer(1,3,gl.FLOAT,false,24,12);
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,gl.createBuffer());
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER,new Uint32Array(mesh.Indices),gl.STATIC_DRAW);
    const instances = gl.createBuffer(); gl.bindBuffer(gl.ARRAY_BUFFER,instances);
    const attribute = (index,components,offset) => {
        gl.enableVertexAttribArray(index); gl.vertexAttribPointer(index,components,gl.FLOAT,false,stride*4,offset);
        gl.vertexAttribDivisor(index,1);
    };
    for (let index=0;index<4;index++) attribute(2+index,4,index*16);
    for (let index=0;index<3;index++) attribute(7+index,3,64+index*12);
    attribute(6,3,100);
    return { vao, instances, count: mesh.Indices.length };
}

function createSky(gl, sources) {
    const program = createProgram(gl,sources), vao = gl.createVertexArray(), texture = gl.createTexture();
    const projection = gl.getUniformLocation(program,'inverseViewProjection');
    const orientation = gl.getUniformLocation(program,'inverseOrientation');
    return sky => {
        gl.bindTexture(gl.TEXTURE_CUBE_MAP,texture);
        if (sky.images) {
            sky.images.forEach((image,index) => {
                const bytes = Uint8Array.from(atob(image.pixels),character => character.charCodeAt(0));
                gl.texImage2D(gl.TEXTURE_CUBE_MAP_POSITIVE_X+index,0,gl.RGBA,image.width,image.height,0,gl.RGBA,gl.UNSIGNED_BYTE,bytes);
            });
            for (const setting of [gl.TEXTURE_MIN_FILTER,gl.TEXTURE_MAG_FILTER]) gl.texParameteri(gl.TEXTURE_CUBE_MAP,setting,gl.LINEAR);
            for (const axis of [gl.TEXTURE_WRAP_S,gl.TEXTURE_WRAP_T,gl.TEXTURE_WRAP_R]) gl.texParameteri(gl.TEXTURE_CUBE_MAP,axis,gl.CLAMP_TO_EDGE);
        }
        gl.disable(gl.DEPTH_TEST); gl.disable(gl.CULL_FACE); gl.useProgram(program);
        gl.uniformMatrix4fv(projection,false,sky.inverseViewProjection);
        gl.uniformMatrix4fv(orientation,false,sky.inverseOrientation);
        gl.bindVertexArray(vao); gl.drawArrays(gl.TRIANGLES,0,3);
        gl.enable(gl.DEPTH_TEST); gl.enable(gl.CULL_FACE);
    };
}
