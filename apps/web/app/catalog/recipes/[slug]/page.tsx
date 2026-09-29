import Link from 'next/link';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeSanitize from 'rehype-sanitize';
import {omitRepeatedTitleHeading} from '@/lib/markdown-display';
import {catalogApi} from '@/lib/catalog-api';

export default async function Recipe({params}:{params:Promise<{slug:string}>}){
  const {slug}=await params;
  try{
    const recipe=await catalogApi.recipe(slug);
    const markdown=omitRepeatedTitleHeading(recipe.markdown,recipe.name);
    return <article className="content detail"><Link href="/catalog/recipes">← Quay lại công thức</Link><p className="eyebrow">CÔNG THỨC · Bản biên tập {recipe.revision}</p><h1>{recipe.name}</h1><p className="intro">{recipe.summary}</p><div className="recipe-components"><div><h2>Nguyên liệu</h2><ul>{recipe.ingredients.map(item=><li key={`${item.itemId}-in`}>{item.quantity} × <Link href={`/catalog/${item.itemSlug}`}>{item.itemName}</Link></li>)}</ul></div><div><h2>Kết quả</h2><ul>{recipe.outputs.map(item=><li key={`${item.itemId}-out`}>{item.quantity} × <Link href={`/catalog/${item.itemSlug}`}>{item.itemName}</Link></li>)}</ul></div></div><div className="markdown"><ReactMarkdown remarkPlugins={[remarkGfm]} rehypePlugins={[rehypeSanitize]} skipHtml>{markdown}</ReactMarkdown></div></article>;
  }catch{return <section className="content"><Link href="/catalog/recipes">← Quay lại Recipes</Link><h1>Không tìm thấy công thức</h1><p className="notice">Công thức chưa được xuất bản hoặc không tồn tại.</p></section>;}
}
