import Link from 'next/link';
import {catalogApi,catalogKindLabels,type CatalogKind} from '@/lib/catalog-api';
import DemoDataNotice from '@/components/DemoDataNotice';
import PageNavigation, {parsePublicPage} from '../PageNavigation';

type Props={searchParams:Promise<{q?:string;kind?:string;page?:string}>};
const kinds=Object.keys(catalogKindLabels) as CatalogKind[];

export default async function Catalog({searchParams}:Props){
  const params=await searchParams;
  const q=(params.q??'').trim().slice(0,100);
  const kind=kinds.includes(params.kind as CatalogKind)?params.kind as CatalogKind:'';
  const page=parsePublicPage(params.page);
  const retryQuery=new URLSearchParams();
  if(q)retryQuery.set('q',q);
  if(kind)retryQuery.set('kind',kind);
  const retryHref=`/catalog${retryQuery.size?`?${retryQuery.toString()}`:''}`;
  try{
    const result=await catalogApi.items(q,kind,page);
    return <section className="content catalog-page"><header className="page-heading"><p className="eyebrow">NOVA HAVEN · VẬT PHẨM</p><h1>Danh mục vật phẩm</h1><p>Tra cứu những vật phẩm đã được biên tập và xuất bản.</p></header><DemoDataNotice/>
      <div className="catalog-layout">
        <aside className="catalog-filters" aria-label="Lọc vật phẩm">
          <form action="/catalog" className="catalog-filter-form">
            <label htmlFor="catalog-q">TÊN VẬT PHẨM</label><input id="catalog-q" name="q" type="search" placeholder="Nhập tên vật phẩm…" defaultValue={q} maxLength={100}/>
            <label htmlFor="catalog-kind">LOẠI</label><select id="catalog-kind" name="kind" defaultValue={kind}><option value="">Tất cả loại</option>{kinds.map(value=><option key={value} value={value}>{catalogKindLabels[value]}</option>)}</select>
            <button type="submit">Áp dụng bộ lọc</button>
            <Link className="filter-reset" href="/catalog">Xóa bộ lọc</Link>
          </form>
          <Link className="recipe-link" href="/catalog/recipes">Xem công thức chế tạo <span>→</span></Link>
        </aside>
        <section className="catalog-results" aria-labelledby="catalog-results-title">
          <div className="results-heading"><h2 id="catalog-results-title">Kết quả</h2><span>{result.total} vật phẩm</span></div>
          {result.items.length?<div className="article-grid">{result.items.map(item=><Link className="article-card" key={item.id} href={`/catalog/${item.slug}`}><small>{catalogKindLabels[item.kind as CatalogKind]??item.kind} · r{item.revision}</small><h2>{item.name}</h2><p>{item.summary}</p><span>Xem chi tiết →</span></Link>)}</div>:<div className="notice">Chưa có vật phẩm đã xuất bản phù hợp.</div>}
          <PageNavigation page={result.page} pageSize={result.pageSize} total={result.total} href={retryHref}/>
        </section>
      </div>
    </section>;
  }catch{return <section className="content catalog-page"><header className="page-heading"><p className="eyebrow">NOVA HAVEN · VẬT PHẨM</p><h1>Danh mục vật phẩm</h1></header><div className="public-recovery" role="alert"><div><h2>Chưa tải được danh mục vật phẩm</h2><p>Dịch vụ dữ liệu đang tạm gián đoạn. Bộ lọc hiện tại sẽ được giữ khi bạn thử lại.</p></div><div className="public-recovery-actions"><Link href={retryHref}>Thử lại</Link><Link href="/wiki">Mở thư viện Wiki</Link></div></div></section>;}
}
