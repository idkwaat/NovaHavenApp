import Link from 'next/link';

type Props = {
  page: number;
  pageSize: number;
  total: number;
  href: string;
};

export default function PageNavigation({ page, pageSize, total, href }: Props) {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  if (pages < 2) return null;

  const separator = href.includes('?') ? '&' : '?';
  return <nav className="pager" aria-label="Phân trang">
    {page > 1
      ? <Link rel="prev" href={`${href}${separator}page=${page - 1}`}>← Trang trước</Link>
      : <span aria-hidden="true" />}
    <span aria-live="polite">Trang {page} / {pages} · {total} kết quả</span>
    {page < pages
      ? <Link rel="next" href={`${href}${separator}page=${page + 1}`}>Trang tiếp →</Link>
      : <span aria-hidden="true" />}
  </nav>;
}

export function parsePublicPage(value?: string) {
  const page = Number(value ?? 1);
  return Number.isSafeInteger(page) && page >= 1 && page <= 100_000_000 ? page : 1;
}
