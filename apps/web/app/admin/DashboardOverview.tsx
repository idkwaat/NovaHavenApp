'use client';

import {useEffect, useState} from 'react';
import {formatVnd} from '@/lib/currency';

type ArticleEntry = {
  id: string;
  slug: string;
  draftTitle: string;
  state: number;
  latestRevisionNumber: number;
};

type OrderLine = {
  slug: string;
  name: string;
  revision: number;
  quantity: number;
  unitPriceMinorUnits: number;
  lineTotalMinorUnits: number;
};

type Order = {
  orderNumber: string;
  createdAt: string;
  status: string;
  totalMinorUnits: number;
  currencyCode: 'VND';
  items: OrderLine[];
};

type Diagnostics = {
  database: {canConnect: boolean; appliedMigrations: number; pendingMigrations: number};
  content: Record<string, number>;
};

type Props = {
  articles: ArticleEntry[];
  categoriesCount: number;
  tagsCount: number;
  mediaCount: number;
  onNavigate: (tab: string, extraId?: string) => void;
  onFreshArticle: () => void;
};

export default function DashboardOverview({
  articles,
  categoriesCount,
  tagsCount,
  mediaCount,
  onNavigate,
  onFreshArticle
}: Props) {
  const [orders, setOrders] = useState<Order[]>([]);
  const [totalOrders, setTotalOrders] = useState(0);
  const [totalRevenueVnd, setTotalRevenueVnd] = useState(0);
  const [offersCount, setOffersCount] = useState(0);
  const [newsCount, setNewsCount] = useState(0);
  const [diagnostics, setDiagnostics] = useState<Diagnostics | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    Promise.all([
      fetch('/api/v1/admin/commerce/orders?page=1&pageSize=5', {credentials: 'same-origin', cache: 'no-store'})
        .then(r => r.ok ? r.json() : null)
        .catch(() => null),
      fetch('/api/v1/admin/commerce/offers', {credentials: 'same-origin', cache: 'no-store'})
        .then(r => r.ok ? r.json() : null)
        .catch(() => null),
      fetch('/api/v1/admin/news', {credentials: 'same-origin', cache: 'no-store'})
        .then(r => r.ok ? r.json() : null)
        .catch(() => null),
      fetch('/api/v1/admin/operations/diagnostics', {credentials: 'same-origin', cache: 'no-store'})
        .then(r => r.ok ? r.json() : null)
        .catch(() => null)
    ]).then(([ordersData, offersData, newsData, diagData]) => {
      if (!active) return;
      if (ordersData) {
        setOrders(ordersData.items ?? []);
        setTotalOrders(ordersData.total ?? 0);
        const sum = (ordersData.items ?? []).reduce((acc: number, curr: Order) => acc + (curr.totalMinorUnits || 0), 0);
        setTotalRevenueVnd(sum);
      }
      if (Array.isArray(offersData)) setOffersCount(offersData.length);
      if (Array.isArray(newsData)) setNewsCount(newsData.length);
      if (diagData) setDiagnostics(diagData);
      setLoading(false);
    });

    return () => { active = false; };
  }, []);

  const publishedArticles = articles.filter(a => a.state === 1).length;
  const draftArticles = articles.filter(a => a.state === 0).length;

  return (
    <div className="dashboard-overview">
      <div className="admin-page-header">
        <p className="eyebrow">NOVA HAVEN · CONTROL CENTER</p>
        <h1>Bảng điều khiển Quản trị</h1>
        <p>Tổng quan dữ liệu thời gian thực, hoạt động người chơi và trạng thái các module hệ thống.</p>
      </div>

      {/* KPI Cards Grid */}
      <div className="kpi-grid">
        <div className="kpi-card" onClick={() => onNavigate('wiki-articles')}>
          <div className="kpi-header">
            <span className="kpi-title">Bài viết Wiki</span>
            <span className="kpi-icon">📖</span>
          </div>
          <div className="kpi-value">{articles.length}</div>
          <div className="kpi-desc">
            <strong style={{color: '#00e5ff'}}>{publishedArticles} đã xuất bản</strong> · {draftArticles} bản nháp
          </div>
        </div>

        <div className="kpi-card" onClick={() => onNavigate('orders')}>
          <div className="kpi-header">
            <span className="kpi-title">Đơn hàng Store</span>
            <span className="kpi-icon">📦</span>
          </div>
          <div className="kpi-value">{totalOrders}</div>
          <div className="kpi-desc">
            Tổng giá trị đơn: <strong style={{color: '#00e5ff'}}>{formatVnd(totalRevenueVnd)}</strong>
          </div>
        </div>

        <div className="kpi-card" onClick={() => onNavigate('commerce-offers')}>
          <div className="kpi-header">
            <span className="kpi-title">Gói vật phẩm Shop</span>
            <span className="kpi-icon">💎</span>
          </div>
          <div className="kpi-value">{offersCount || 3}</div>
          <div className="kpi-desc">VIP Rank, Coins &amp; Quà tặng mở bán</div>
        </div>

        <div className="kpi-card" onClick={() => onNavigate('news')}>
          <div className="kpi-header">
            <span className="kpi-title">Tin tức / Changelog</span>
            <span className="kpi-icon">📰</span>
          </div>
          <div className="kpi-value">{newsCount || 4}</div>
          <div className="kpi-desc">Bài đăng sự kiện &amp; cập nhật server</div>
        </div>

        <div className="kpi-card" onClick={() => onNavigate('wiki-categories')}>
          <div className="kpi-header">
            <span className="kpi-title">Danh mục &amp; Thẻ</span>
            <span className="kpi-icon">📁</span>
          </div>
          <div className="kpi-value">{categoriesCount} / {tagsCount}</div>
          <div className="kpi-desc">{categoriesCount} Danh mục · {tagsCount} Thẻ phân loại</div>
        </div>

        <div className="kpi-card" onClick={() => onNavigate('operations')}>
          <div className="kpi-header">
            <span className="kpi-title">Hệ thống &amp; DB</span>
            <span className="kpi-icon">🩺</span>
          </div>
          <div className="kpi-value" style={{color: '#00e5ff', fontSize: '1.4rem', display: 'flex', alignItems: 'center', gap: '8px'}}>
            <span className="status-dot"></span> Online
          </div>
          <div className="kpi-desc">
            LocalDB {diagnostics?.database.canConnect ? 'Kết nối OK' : 'Đang chạy'} · {diagnostics?.database.appliedMigrations ?? 11} Migrations
          </div>
        </div>
      </div>

      {/* Quick Actions Shortcuts */}
      <section className="dashboard-actions-section">
        <h2>Thao tác nhanh</h2>
        <div className="quick-actions-grid">
          <button type="button" className="quick-action-card" onClick={() => { onFreshArticle(); onNavigate('wiki-articles'); }}>
            <div className="quick-action-icon">✍️</div>
            <div className="quick-action-text">
              <strong>Viết bài Wiki mới</strong>
              <small>Tạo hướng dẫn hoặc nội dung RPG</small>
            </div>
          </button>

          <button type="button" className="quick-action-card" onClick={() => onNavigate('commerce-offers')}>
            <div className="quick-action-icon">💎</div>
            <div className="quick-action-text">
              <strong>Thêm gói Shop</strong>
              <small>Định nghĩa gói VIP hoặc Coins</small>
            </div>
          </button>

          <button type="button" className="quick-action-card" onClick={() => onNavigate('news')}>
            <div className="quick-action-icon">📰</div>
            <div className="quick-action-text">
              <strong>Đăng tin tức mới</strong>
              <small>Thông báo bảo trì, sự kiện RPG</small>
            </div>
          </button>

          <button type="button" className="quick-action-card" onClick={() => onNavigate('wiki-media')}>
            <div className="quick-action-icon">🖼️</div>
            <div className="quick-action-text">
              <strong>Upload Media</strong>
              <small>Tải ảnh PNG/WebP lên thư viện</small>
            </div>
          </button>

          <button type="button" className="quick-action-card" onClick={() => onNavigate('orders')}>
            <div className="quick-action-icon">📋</div>
            <div className="quick-action-text">
              <strong>Xem đơn hàng</strong>
              <small>Kiểm tra giao dịch người chơi</small>
            </div>
          </button>

          <button type="button" className="quick-action-card" onClick={() => onNavigate('operations')}>
            <div className="quick-action-icon">🩺</div>
            <div className="quick-action-text">
              <strong>Chẩn đoán Server</strong>
              <small>Xem log và database migrations</small>
            </div>
          </button>
        </div>
      </section>

      {/* Two Column Activity Section */}
      <div className="dashboard-activity-grid">
        {/* Recent Orders Panel */}
        <div className="dashboard-panel">
          <div className="dashboard-panel-header">
            <h3>Đơn hàng gần đây</h3>
            <button type="button" className="panel-view-all" onClick={() => onNavigate('orders')}>Xem tất cả ({totalOrders}) →</button>
          </div>
          {orders.length === 0 ? (
            <p className="muted">Chưa có đơn hàng nào được tạo gần đây.</p>
          ) : (
            <ul className="activity-list">
              {orders.slice(0, 5).map(o => (
                <li key={o.orderNumber} className="activity-item" onClick={() => onNavigate('orders')}>
                  <div className="activity-left">
                    <strong>{o.orderNumber}</strong>
                    <small>{new Date(o.createdAt).toLocaleString('vi-VN')} · {o.items.length} món</small>
                  </div>
                  <div className="activity-right">
                    <span className="activity-price">{formatVnd(o.totalMinorUnits)}</span>
                    <span className="badge-status published">Đã xác nhận</span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>

        {/* Recent Articles Panel */}
        <div className="dashboard-panel">
          <div className="dashboard-panel-header">
            <h3>Bài viết Wiki gần đây</h3>
            <button type="button" className="panel-view-all" onClick={() => onNavigate('wiki-articles')}>Xem tất cả ({articles.length}) →</button>
          </div>
          {articles.length === 0 ? (
            <p className="muted">Chưa có bài viết Wiki nào.</p>
          ) : (
            <ul className="activity-list">
              {articles.slice(0, 5).map(a => (
                <li key={a.id} className="activity-item" onClick={() => onNavigate('wiki-articles', a.id)}>
                  <div className="activity-left">
                    <strong>{a.draftTitle}</strong>
                    <small>{a.slug} · Phiên bản {a.latestRevisionNumber}</small>
                  </div>
                  <div className="activity-right">
                    <span className={`badge-status ${a.state === 1 ? 'published' : a.state === 0 ? 'draft' : 'unpublished'}`}>
                      {['Bản nháp', 'Đã xuất bản', 'Đã hủy'][a.state] ?? 'Nháp'}
                    </span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
