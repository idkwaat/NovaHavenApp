import Link from 'next/link';
import {catalogApi} from '@/lib/catalog-api';
import PageNavigation, {parsePublicPage} from '../../PageNavigation';

export default async function Recipes({searchParams}:{searchParams:Promise<{page?:string}>}){
  try{
    const result=await catalogApi.recipes(parsePublicPage((await searchParams).page));
    return <section className="content"><p className="eyebrow">NOVA HAVEN RECIPES</p><h1>Công thức chế tạo</h1><p>Các công thức được biên tập và snapshot theo revision item đã xuất bản.</p>{result.items.length?<div className="article-grid">{result.items.map(item=><Link className="article-card" key={item.id} href={`/catalog/recipes/${item.slug}`}><small>RECIPE · r{item.revision}</small><h2>{item.name}</h2><p>{item.summary}</p><span>Xem công thức →</span></Link>)}</div>:<div className="notice">Chưa có công thức đã xuất bản.</div>}<PageNavigation page={result.page} pageSize={result.pageSize} total={result.total} href="/catalog/recipes"/></section>;
  }catch{return <section className="content"><p className="eyebrow">NOVA HAVEN RECIPES</p><h1>Công thức chế tạo</h1><div className="notice">Recipe API đang offline hoặc chưa có dữ liệu xuất bản.</div></section>;}
}
