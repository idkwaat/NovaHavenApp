export type ArticleCollection='news'|'wiki'|'npc'|'quest'|'location'|'season';

export type EditorialPreview={
 id:string;
 src:string;
 alt:string;
 credit:string;
 sourceUrl?:string;
 licenseUrl?:string;
};

export type ArticlePreviewInput={
 collection:ArticleCollection;
 slug:string;
 title:string;
 topic?:string;
 imageUrl?:string|null;
 imageAlt?:string|null;
};

const unsplashLicense='https://unsplash.com/license';
const ccBy30='https://creativecommons.org/licenses/by/3.0/';
const ccBy20='https://creativecommons.org/licenses/by/2.0/';

export const editorialPreviewAssets:EditorialPreview[]=[
 {
  id:'minecraft-nether',
  src:'https://upload.wikimedia.org/wikipedia/commons/c/cd/Screenshot_from_the_Minecraft_Nether.png',
  alt:'Cảnh quan Nether với địa hình đỏ sẫm trong Minecraft.',
  credit:'Xbox México · CC BY 3.0',
  sourceUrl:'https://commons.wikimedia.org/wiki/File:Screenshot_from_the_Minecraft_Nether.png',
  licenseUrl:ccBy30,
 },
 {
  id:'medieval-forest-lake',
  src:'https://upload.wikimedia.org/wikipedia/commons/3/35/Medieval_Forest_Lake_%2850035567983%29.jpg',
  alt:'Mặt hồ yên tĩnh giữa rừng cây xanh.',
  credit:'Jason Boldero · CC BY 2.0',
  sourceUrl:'https://commons.wikimedia.org/wiki/File:Medieval_Forest_Lake_(50035567983).jpg',
  licenseUrl:ccBy20,
 },
 {
  id:'einar-meadow-river',
  src:'https://images.unsplash.com/photo-1760138338534-50b64e75267c?auto=format&fit=crop&crop=entropy&fm=jpg&q=85&w=1920&h=1280',
  alt:'Đồng cỏ xanh, dòng sông và núi xa.',
  credit:'Einar Storsul · Unsplash License',
  sourceUrl:'https://unsplash.com/photos/green-meadow-with-trees-and-a-river-near-mountains-X7FEgrPDFNU',
  licenseUrl:unsplashLicense,
 },
 {
  id:'valerie-mountain-lake',
  src:'https://images.unsplash.com/photo-1755488254231-66910458a53c?auto=format&fit=crop&fm=jpg&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&ixlib=rb-4.1.0&q=85&w=1920',
  alt:'Ánh nắng trên rừng núi phản chiếu xuống mặt hồ phẳng lặng.',
  credit:'Valerie · Unsplash License',
  sourceUrl:'https://unsplash.com/photos/mountain-forest-reflected-in-calm-lake-water-gJ5n6xsse78',
  licenseUrl:unsplashLicense,
 },
];

const collectionPools:Record<ArticleCollection,Record<string,string[]>>={
 news:{default:['minecraft-nether','medieval-forest-lake','einar-meadow-river','valerie-mountain-lake']},
 wiki:{
  default:['minecraft-nether','medieval-forest-lake','einar-meadow-river','valerie-mountain-lake'],
  'khoi-hanh':['einar-meadow-river','valerie-mountain-lake','medieval-forest-lake'],
  'lop-nhan-vat':['minecraft-nether','einar-meadow-river','valerie-mountain-lake'],
  'vung-dat':['medieval-forest-lake','einar-meadow-river','valerie-mountain-lake'],
 },
 npc:{default:['minecraft-nether','medieval-forest-lake','valerie-mountain-lake']},
 quest:{default:['minecraft-nether','einar-meadow-river','valerie-mountain-lake']},
 location:{default:['medieval-forest-lake','einar-meadow-river','valerie-mountain-lake']},
 season:{default:['einar-meadow-river','medieval-forest-lake','minecraft-nether']},
};

const assetById=new Map(editorialPreviewAssets.map(asset=>[asset.id,asset]));

export function isAllowedPreviewUrl(value:string):boolean{
 const url=value.trim();
 if(/^\/api\/v1\/wiki\/media\/[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(url))return true;
 try{
  const parsed=new URL(url);
  return parsed.protocol==='https:'&&['upload.wikimedia.org','images.unsplash.com'].includes(parsed.hostname);
 }catch{return false;}
}

export function extractMarkdownPreview(markdown:string):{src:string;alt:string}|null{
 const imagePattern=/!\[([^\]]{1,200})\]\(([^\s)]+)(?:\s+["'][^"']*["'])?\)/g;
 for(const match of markdown.matchAll(imagePattern)){
  const alt=match[1]?.trim();
  const src=match[2]?.trim();
  if(alt&&src&&isAllowedPreviewUrl(src))return{src,alt};
 }
 return null;
}

function stableIndex(value:string,length:number):number{
 let hash=2166136261;
 for(const character of value){hash^=character.charCodeAt(0);hash=Math.imul(hash,16777619);}
 return (hash>>>0)%length;
}

export function articlePreviewFor(input:ArticlePreviewInput):EditorialPreview{
 const imageUrl=input.imageUrl?.trim();
 if(imageUrl&&isAllowedPreviewUrl(imageUrl)){
  return{
   id:`content-${input.slug}`,
   src:imageUrl,
   alt:input.imageAlt?.trim()||input.title,
   credit:imageUrl.startsWith('/api/')?'Ảnh gắn với bài viết':'Ảnh do biên tập viên cung cấp',
  };
 }

 const topicPool=collectionPools[input.collection][input.topic??'']??collectionPools[input.collection].default;
 const assetId=topicPool[stableIndex(input.slug,topicPool.length)];
 return assetById.get(assetId!)??editorialPreviewAssets[0]!;
}

export function fallbackPreviewFor(image:EditorialPreview):EditorialPreview{
 return editorialPreviewAssets.find(candidate=>candidate.src!==image.src)??editorialPreviewAssets[0]!;
}
