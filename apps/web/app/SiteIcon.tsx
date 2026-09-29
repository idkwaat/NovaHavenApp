import type {ReactNode} from 'react';

export type SiteIconName = 'menu' | 'home' | 'user' | 'search' | 'bell' | 'book' | 'alchemy' | 'swords' | 'community' | 'news' | 'arrow' | 'copy' | 'check' | 'map' | 'shield' | 'compass' | 'discord';

const shapes: Record<SiteIconName, ReactNode> = {
  menu: <><path d="M4 7h16"/><path d="M4 12h16"/><path d="M4 17h16"/></>,
  home: <><path d="m3.5 10 8.5-7 8.5 7"/><path d="M5.5 9v11h13V9"/><path d="M9.5 20v-6h5v6"/></>,
  user: <><circle cx="12" cy="8" r="3.2"/><path d="M5.2 20a6.8 6.8 0 0 1 13.6 0"/></>,
  search: <><circle cx="10.8" cy="10.8" r="6.4"/><path d="m15.5 15.5 5 5"/></>,
  bell: <><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9"/><path d="M10 21h4"/></>,
  book: <><path d="M12 6.2c-2.2-1.6-5.2-1.8-8-1v14c2.8-.8 5.8-.6 8 1"/><path d="M12 6.2c2.2-1.6 5.2-1.8 8-1v14c-2.8-.8-5.8-.6-8 1"/><path d="M12 6v14"/></>,
  alchemy: <><path d="M9 3h6"/><path d="M10 3v6l-5 8.5A2.3 2.3 0 0 0 7 21h10a2.3 2.3 0 0 0 2-3.5L14 9V3"/><path d="M7.2 16h9.6"/></>,
  swords: <><path d="m5 4 15 15"/><path d="m4 9 5-5"/><path d="m15 20 5-5"/><path d="m19 4-15 15"/><path d="m15 4 5 5"/><path d="m4 15 5 5"/></>,
  community: <><circle cx="9" cy="8" r="3"/><path d="M3.5 20a5.5 5.5 0 0 1 11 0"/><path d="M16 5.5a3 3 0 0 1 0 5.8"/><path d="M17 14.7a5 5 0 0 1 3.5 4.8"/></>,
  news: <><rect x="4" y="3.5" width="16" height="17" rx="1.5"/><path d="M8 8h8"/><path d="M8 12h8"/><path d="M8 16h5"/></>,
  arrow: <><path d="M4.5 12h14"/><path d="m13 6 6 6-6 6"/></>,
  copy: <><rect x="9" y="9" width="13" height="13" rx="2" ry="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/></>,
  check: <polyline points="20 6 9 17 4 12"/>,
  map: <><polygon points="3 6 9 3 15 6 21 3 21 18 15 21 9 18 3 21"/><line x1="9" y1="3" x2="9" y2="18"/><line x1="15" y1="6" x2="15" y2="21"/></>,
  shield: <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>,
  compass: <><circle cx="12" cy="12" r="10"/><polygon points="16.24 7.76 14.12 14.12 7.76 16.24 9.88 9.88 16.24 7.76"/></>,
  discord: <path d="M18.9 5.8a15.7 15.7 0 0 0-3.9-1.2.1.1 0 0 0-.1 0 10.9 10.9 0 0 0-.5 1 14.5 14.5 0 0 0-4.8 0 11.5 11.5 0 0 0-.5-1 .1.1 0 0 0-.1 0 15.6 15.6 0 0 0-3.9 1.2.1.1 0 0 0 0 .1C2.7 9.5 2 13 2.3 16.6a.1.1 0 0 0 0 .1 15.7 15.7 0 0 0 4.8 2.4.1.1 0 0 0 .1 0 11.2 11.2 0 0 0 1-1.6.1.1 0 0 0 0-.1 10.3 10.3 0 0 1-1.6-.8.1.1 0 0 1 0-.2c.1-.1.2-.2.4-.2a11.2 11.2 0 0 0 9.8 0c.1.1.3.2.4.2a.1.1 0 0 1 0 .2 10.3 10.3 0 0 1-1.6.8.1.1 0 0 0 0 .1 11.2 11.2 0 0 0 1 1.6.1.1 0 0 0 .1 0 15.7 15.7 0 0 0 4.8-2.4.1.1 0 0 0 0-.1c.4-4.1-.7-7.6-2.9-10.8a.1.1 0 0 0-.1 0zM8.5 13.7c-.9 0-1.6-.8-1.6-1.8s.7-1.8 1.6-1.8c1 0 1.7.8 1.6 1.8 0 1-.7 1.8-1.6 1.8zm7 0c-.9 0-1.6-.8-1.6-1.8s.7-1.8 1.6-1.8c1 0 1.7.8 1.6 1.8 0 1-.7 1.8-1.6 1.8z"/>,
};

export default function SiteIcon({name, className}: {name: SiteIconName; className?: string}) {
  return <svg className={className} aria-hidden="true" focusable="false" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
    {shapes[name]}
  </svg>;
}
