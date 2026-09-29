function normalizeHeading(value:string):string{
 return value.normalize('NFC').replace(/\s+/g,' ').trim().toLocaleLowerCase('vi-VN');
}

/** Removes only the first H1 when it repeats the page title already rendered by the layout. */
export function omitRepeatedTitleHeading(markdown:string,title:string):string{
 const lines=markdown.split(/\r?\n/);
 const firstContentLine=lines.findIndex(line=>line.trim().length>0);
 if(firstContentLine<0)return markdown;
 const match=lines[firstContentLine].trim().match(/^#\s+(.+?)\s*#*\s*$/);
 if(!match||normalizeHeading(match[1])!==normalizeHeading(title))return markdown;
 lines.splice(0,firstContentLine+1);
 while(lines[0]?.trim().length===0)lines.shift();
 return lines.join('\n');
}
