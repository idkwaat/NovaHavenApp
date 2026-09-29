'use client';

import {useEffect,useState} from 'react';

type Diagnostics={
 generatedAt:string;
 database:{provider:string;canConnect:boolean;appliedMigrations:number;pendingMigrations:number;pendingMigrationNames:string[]};
 content:Record<string,number>;
 integrations:Array<{capabilityKey:string;status:string;updatedAt:string}>;
};

const labels:Record<string,string>={
 wikiArticles:'Wiki articles',
 publishedWikiArticles:'Published Wiki articles',
 knowledgeEntries:'Knowledge entries',
 publishedKnowledgeEntries:'Published knowledge',
 communityRecords:'Community records',
 publishedCommunityRecords:'Published community',
 catalogItems:'Catalog items',
 publishedCatalogItems:'Published catalog',
 rewards:'Reward definitions',
 publishedRewards:'Published rewards',
 commerceOffers:'Commerce offers',
 publishedCommerceOffers:'Published commerce',
 auditEvents:'Audit events'
};

export default function OperationsManager(){
 const[data,setData]=useState<Diagnostics|null>(null);
 const[error,setError]=useState('');
 useEffect(()=>{
  let active=true;
  fetch('/api/v1/admin/operations/diagnostics',{credentials:'same-origin',cache:'no-store'})
   .then(async response=>{if(!response.ok)throw new Error(`Không tải được diagnostics (${response.status}).`);return await response.json() as Diagnostics;})
   .then(value=>{if(active){setData(value);setError('');}})
   .catch(reason=>{if(active)setError(reason instanceof Error?reason.message:'Không tải được diagnostics.');});
  return()=>{active=false;};
 },[]);
 return <section className="category-manager" aria-labelledby="operations-heading">
  <h2 id="operations-heading">Vận hành local</h2>
  <p>Snapshot chỉ đọc cho database, migration và nội dung. Không chạy migration hoặc ghi dữ liệu từ panel này.</p>
  {error&&<p className="error" role="alert">{error}</p>}
  {!data&&!error&&<p>Đang tải trạng thái hệ thống...</p>}
  {data&&<>
   <div className="tile-grid">
    <article className="article-card"><strong>Database</strong><small>{data.database.canConnect?'Kết nối OK':'Không kết nối'} · {data.database.provider}</small></article>
    <article className="article-card"><strong>{data.database.appliedMigrations}</strong><small>Applied migrations</small></article>
    <article className="article-card"><strong>{data.database.pendingMigrations}</strong><small>Pending migrations</small></article>
   </div>
   {data.database.pendingMigrations>0&&<p className="error" role="alert">Database còn migration chưa áp dụng: {data.database.pendingMigrationNames.join(', ')}.</p>}
   <div className="tile-grid">{Object.entries(data.content).map(([key,value])=><article className="article-card" key={key}><strong>{value}</strong><small>{labels[key]??key}</small></article>)}</div>
   <h3>Integration capability</h3>
   <ul>{data.integrations.map(item=><li key={item.capabilityKey}>{item.capabilityKey} · {item.status}</li>)}</ul>
   <p className="muted">Cập nhật: {new Date(data.generatedAt).toLocaleString('vi-VN')}</p>
  </>}
 </section>;
}
