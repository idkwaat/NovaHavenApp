'use client';

import {useState} from 'react';

export default function CopyIpButton({ip = 'play.novahaven.net', label = 'Sao chép IP'}: {ip?: string; label?: string}) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(ip);
      setCopied(true);
      setTimeout(() => setCopied(false), 2400);
    } catch {
      // Fallback for environments where navigator.clipboard might fail
      const textarea = document.createElement('textarea');
      textarea.value = ip;
      textarea.style.position = 'fixed';
      textarea.style.opacity = '0';
      document.body.appendChild(textarea);
      textarea.focus();
      textarea.select();
      try {
        document.execCommand('copy');
        setCopied(true);
        setTimeout(() => setCopied(false), 2400);
      } catch {
        // ignore
      }
      document.body.removeChild(textarea);
    }
  };

  return (
    <button
      type="button"
      className={`copy-ip-btn ${copied ? 'copied' : ''}`}
      onClick={handleCopy}
      aria-label={`Sao chép địa chỉ máy chủ ${ip}`}
      title="Sao chép địa chỉ máy chủ"
    >
      {copied ? (
        <>
          <svg className="btn-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <polyline points="20 6 9 17 4 12" />
          </svg>
          <span>Đã sao chép IP!</span>
        </>
      ) : (
        <>
          <svg className="btn-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
            <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
          </svg>
          <span>{label}</span>
        </>
      )}
    </button>
  );
}
