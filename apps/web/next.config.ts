import type {NextConfig} from 'next';

const apiOrigin = process.env.NOVA_API_ORIGIN ?? 'http://localhost:5080';
const config: NextConfig = {
  distDir: process.env.NOVA_NEXT_DIST_DIR ?? '.next',
  devIndicators: false,
  images:{remotePatterns:[
    {protocol:'https',hostname:'upload.wikimedia.org',pathname:'/wikipedia/commons/**'},
    {protocol:'https',hostname:'images.unsplash.com',pathname:'/**'},
  ]},
  async rewrites() {return [{source:'/api/:path*',destination:`${apiOrigin}/api/:path*`}];},
};
export default config;
