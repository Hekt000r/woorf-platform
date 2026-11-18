export function createGuiRenderer(canvas) {
    const context = canvas.getContext('2d');
    const images = new Map();
    return (commands, width, height) => {
        canvas.width = width; canvas.height = height;
        for (const command of commands) {
            const bounds = command.Bounds, clip = command.Clip, color = command.Color;
            context.save();
            context.beginPath(); context.rect(clip.X,clip.Y,clip.Width,clip.Height); context.clip();
            context.globalAlpha = command.Opacity;
            context.fillStyle = `rgb(${color.R*255},${color.G*255},${color.B*255})`;
            if (command.Rotation) {
                context.translate(bounds.X+bounds.Width/2,bounds.Y+bounds.Height/2);
                context.rotate(command.Rotation*Math.PI/180);
                context.translate(-bounds.X-bounds.Width/2,-bounds.Y-bounds.Height/2);
            }
            if (command.Kind === 'Rectangle') {
                context.beginPath(); context.roundRect(bounds.X,bounds.Y,bounds.Width,bounds.Height,command.Radius); context.fill();
            } else if (command.Kind === 'Text') drawText(context,command);
            else if (command.Kind === 'Image' && command.Content) {
                if (!images.has(command.Content)) {
                    const image = new Image(); image.src = new URL(command.Content,location.href).href; images.set(command.Content,image);
                }
                const image = images.get(command.Content);
                if (image.complete && image.naturalWidth) drawImage(context,command,image);
            }
            context.restore();
        }
    };
}

function drawImage(context,command,image) {
    const b = command.Bounds;
    let sx = Math.max(0,command.ImageOffset.X), sy = Math.max(0,command.ImageOffset.Y);
    let sw = Math.min(command.ImageSize.X > 0 ? command.ImageSize.X : image.naturalWidth,image.naturalWidth-sx);
    let sh = Math.min(command.ImageSize.Y > 0 ? command.ImageSize.Y : image.naturalHeight,image.naturalHeight-sy);
    if (sw <= 0 || sh <= 0 || b.Width <= 0 || b.Height <= 0) return;
    let x=b.X, y=b.Y, width=b.Width, height=b.Height;
    if (command.ScaleType === 'Fit') {
        const scale = Math.min(width/sw,height/sh);
        width=sw*scale; height=sh*scale; x+=(b.Width-width)/2; y+=(b.Height-height)/2;
    } else if (command.ScaleType === 'Crop') {
        const scale = Math.max(width/sw,height/sh);
        const cropWidth=width/scale, cropHeight=height/scale;
        sx+=(sw-cropWidth)/2; sy+=(sh-cropHeight)/2; sw=cropWidth; sh=cropHeight;
    } else if (command.ScaleType === 'Tile') {
        const tileWidth=Math.max(1,command.TileSize.X),tileHeight=Math.max(1,command.TileSize.Y);
        context.beginPath(); context.rect(x,y,width,height); context.clip();
        for (let row=y;row<y+height;row+=tileHeight)
            for (let column=x;column<x+width;column+=tileWidth)
                context.drawImage(image,sx,sy,sw,sh,column,row,tileWidth,tileHeight);
        return;
    }
    context.drawImage(image,sx,sy,sw,sh,x,y,width,height);
}

function drawText(context,command) {
    const b = command.Bounds;
    const family = ['Code','RobotoMono'].includes(command.Font) ? 'monospace'
        : ['Garamond','Antique','Bodoni','BodoniItalic'].includes(command.Font) ? 'serif' : 'sans-serif';
    context.font = `${command.Italic ? 'italic ' : ''}${command.Bold ? 'bold ' : ''}${command.TextSize}px ${family}`;
    context.textAlign = command.XAlignment.toLowerCase();
    context.textBaseline = 'top';
    const lines = [];
    for (const paragraph of command.Content.split('\n')) {
        if (!command.Wrapped) { lines.push(paragraph); continue; }
        let line = '';
        for (const word of paragraph.split(' ')) {
            const next = line ? `${line} ${word}` : word;
            if (line && context.measureText(next).width > b.Width) { lines.push(line); line = word; }
            else line = next;
        }
        lines.push(line);
    }
    const lineHeight = command.TextSize*1.2, textHeight = lines.length*lineHeight;
    const x = command.XAlignment === 'Left' ? b.X : command.XAlignment === 'Right' ? b.X+b.Width : b.X+b.Width/2;
    let y = command.YAlignment === 'Top' ? b.Y : command.YAlignment === 'Bottom' ? b.Y+b.Height-textHeight : b.Y+(b.Height-textHeight)/2;
    for (const line of lines) {
        context.fillText(line,x,y);
        const width = context.measureText(line).width;
        const left = command.XAlignment === 'Left' ? x : command.XAlignment === 'Right' ? x-width : x-width/2;
        if (command.Underline) context.fillRect(left,y+command.TextSize,width,Math.max(1,command.TextSize/16));
        if (command.Strikethrough) context.fillRect(left,y+command.TextSize*.55,width,Math.max(1,command.TextSize/16));
        y += lineHeight;
    }
}
