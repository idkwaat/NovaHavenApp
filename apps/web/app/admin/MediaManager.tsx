'use client';

import {useState} from 'react';

export type MediaItem = {
  id: string;
  originalFileName: string;
  contentType: string;
  length: number;
  width: number;
  height: number;
  createdAt: string;
  url: string;
};

type Props = {
  media: MediaItem[];
  token: string;
  csrfToken: () => Promise<string>;
  onRefresh: () => Promise<void>;
  onStatus: (msg: string) => void;
  disabled: boolean;
};

export default function MediaManager({
  media,
  token,
  csrfToken,
  onRefresh,
  onStatus,
  disabled
}: Props) {
  const [busy, setBusy] = useState(false);
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');

  const copyToClipboard = async (text: string, id: string, message: string) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopiedId(id);
      onStatus(message);
      setTimeout(() => setCopiedId(null), 2000);
    } catch {
      onStatus('Không thể sao chép vào bộ nhớ tạm.');
    }
  };

  const uploadMedia = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      onStatus('Kích thước ảnh vượt quá giới hạn 5 MB.');
      return;
    }

    setBusy(true);
    onStatus('Đang tải ảnh lên...');
    try {
      const current = token || await csrfToken();
      const form = new FormData();
      form.append('file', file);
      const response = await fetch('/api/v1/admin/wiki/media', {
        method: 'POST',
        credentials: 'same-origin',
        headers: {'X-CSRF-TOKEN': current},
        body: form
      });

      if (!response.ok) {
        throw new Error(`Upload thất bại (${response.status}).`);
      }

      await onRefresh();
      onStatus(`Đã tải lên tệp ${file.name} thành công.`);
    } catch (err) {
      onStatus((err as Error).message);
    } finally {
      setBusy(false);
    }
  };

  const filteredMedia = media.filter(item =>
    item.originalFileName.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <section className="media-manager-panel">
      <div className="admin-page-header">
        <p className="eyebrow">NOVA HAVEN · ASSET STORAGE</p>
        <h1>Thư viện Media &amp; Hình ảnh</h1>
        <p>Quản lý các tệp hình ảnh PNG, JPEG, WebP phục vụ cho bài viết Wiki, tin tức và mô tả vật phẩm.</p>
      </div>

      {/* Upload Dropzone */}
      <div className="media-upload-zone">
        <label style={{cursor: 'pointer', display: 'grid', gap: '8px', placeItems: 'center'}}>
          <span style={{fontSize: '2rem'}}>📁</span>
          <strong>Nhấn để chọn tệp tải lên Thư viện</strong>
          <small className="muted">Hỗ trợ PNG, JPEG, WebP · Kích thước tối đa 5 MB</small>
          <input
            type="file"
            accept="image/png,image/jpeg,image/webp"
            disabled={disabled || busy}
            style={{display: 'none'}}
            onChange={uploadMedia}
          />
        </label>
      </div>

      {/* Toolbar / Search */}
      <div className="form-row" style={{alignItems: 'center', justifyContent: 'space-between'}}>
        <input
          type="search"
          placeholder="Tìm kiếm tệp theo tên..."
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          style={{maxWidth: 320}}
        />
        <button
          type="button"
          className="button button-outline"
          disabled={disabled || busy}
          onClick={() => void onRefresh()}
        >
          Làm mới thư viện ({media.length} ảnh)
        </button>
      </div>

      {/* Media Grid */}
      {filteredMedia.length === 0 ? (
        <p className="muted">Không tìm thấy tệp ảnh nào phù hợp.</p>
      ) : (
        <div className="media-grid">
          {filteredMedia.map(item => {
            const mdCode = `![${item.originalFileName}](${item.url})`;
            const isCopied = copiedId === item.id;

            return (
              <div key={item.id} className="media-card">
                <div className="media-thumb-wrap">
                  <img src={item.url} alt={item.originalFileName} loading="lazy" />
                </div>
                <div className="media-info">
                  <strong title={item.originalFileName}>{item.originalFileName}</strong>
                  <small>{item.width} × {item.height} px · {(item.length / 1024).toFixed(1)} KB</small>
                </div>
                <div className="media-actions">
                  <button
                    type="button"
                    onClick={() => copyToClipboard(mdCode, item.id, 'Đã chép mã Markdown ảnh!')}
                  >
                    {isCopied ? 'Đã chép!' : 'Chép MD'}
                  </button>
                  <a
                    href={item.url}
                    target="_blank"
                    rel="noreferrer"
                    className="button button-outline"
                    style={{padding: '4px 8px', fontSize: '.72rem', textAlign: 'center'}}
                  >
                    Xem
                  </a>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </section>
  );
}
