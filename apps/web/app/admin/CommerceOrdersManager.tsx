'use client';

import {useCallback,useEffect,useState} from 'react';
import {formatVnd} from '@/lib/currency';

type OrderLine={slug:string;name:string;revision:number;quantity:number;unitPriceMinorUnits:number;lineTotalMinorUnits:number};
type LocalDemoOrder={orderNumber:string;createdAt:string;status:string;totalMinorUnits:number;currencyCode:'VND';paymentStatus:'simulated';paymentMethod:'localDemo';realCharge:false;fulfilment:'none';items:OrderLine[]};
type Page={items:LocalDemoOrder[];page:number;pageSize:number;total:number};
export default function CommerceOrdersManager(){
 const [result,setResult]=useState<Page|null>(null);const [error,setError]=useState('');const [page,setPage]=useState(1);const [busy,setBusy]=useState(true);
 const refresh=useCallback(async()=>{setBusy(true);setError('');try{const response=await fetch(`/api/v1/admin/commerce/orders?page=${page}&pageSize=20`,{credentials:'same-origin',cache:'no-store'});if(!response.ok)throw new Error(response.status===401||response.status===403?'Bạn cần quyền quản trị để xem đơn local demo.':`Không tải được lịch sử đơn (${response.status}).`);setResult(await response.json() as Page);}catch(reason){setError((reason as Error).message);}finally{setBusy(false);}},[page]);
 useEffect(()=>{void refresh();},[refresh]);
 const pageCount=Math.max(1,Math.ceil((result?.total??0)/20));
 return <section className="category-manager commerce-orders-manager" aria-labelledby="commerce-orders-heading"><h2 id="commerce-orders-heading">Lịch sử đơn hàng</h2><p>Danh sách các đơn đặt hàng đã ghi nhận trong hệ thống.</p>
  {error&&<p className="shop-error" role="alert">{error}</p>}{busy&&<p role="status">Đang tải đơn hàng…</p>}
  {!busy&&!error&&result?.items.length===0&&<div className="notice">Chưa có đơn hàng nào được tạo.</div>}
  {result?.items.map(order=><article className="commerce-order-card" key={order.orderNumber}><header><div><strong>{order.orderNumber}</strong><time dateTime={order.createdAt}>{new Date(order.createdAt).toLocaleString('vi-VN')}</time></div><span>ĐÃ XÁC NHẬN</span></header><ul>{order.items.map((line,index)=><li key={`${line.slug}-${index}`}><span>{line.name} × {line.quantity}<small>Phiên bản {line.revision} · {formatVnd(line.unitPriceMinorUnits)}/món</small></span><strong>{formatVnd(line.lineTotalMinorUnits)}</strong></li>)}</ul><footer><span>Phương thức: Trực tuyến · Giao tự động</span><strong>Tổng {formatVnd(order.totalMinorUnits)}</strong></footer></article>)}
  {result&&result.total>20&&<nav className="commerce-order-pagination" aria-label="Phân trang đơn hàng"><button type="button" className="button-outline" disabled={page<=1||busy} onClick={()=>setPage(value=>value-1)}>← Trước</button><span>Trang {page} / {pageCount} · {result.total} đơn</span><button type="button" className="button-outline" disabled={page>=pageCount||busy} onClick={()=>setPage(value=>value+1)}>Sau →</button></nav>}
  <button type="button" className="button-outline commerce-order-refresh" disabled={busy} onClick={()=>void refresh()}>Làm mới danh sách</button>
 </section>;
}
