import assert from 'node:assert/strict';
import test from 'node:test';
import contract from '../../contracts/openapi/wiki-v1.json' with {type:'json'};
import source from 'node:fs/promises';

test('commerce contract exposes an explicit local-demo checkout boundary',()=>{
 assert.ok(contract.paths['/api/v1/rewards']);
 assert.ok(contract.paths['/api/v1/commerce/offers']);
 assert.equal(contract.paths['/api/v1/commerce/offers/{slug}'].get.responses['200'].description,'Published commerce offer');
 assert.ok(contract.paths['/api/v1/commerce/orders'].post);
 assert.ok(contract.paths['/api/v1/admin/commerce/orders'].get);
 assert.equal(contract.components.schemas.LocalDemoCheckoutReceipt.properties.realCharge.const,false);
 assert.equal(contract.components.schemas.LocalDemoCheckoutReceipt.properties.fulfilment.const,'none');
});
test('admin offer UI configures opt-in VND local demo prices without game grants',async()=>{
 const text=await source.readFile(new URL('../../apps/web/app/admin/RewardCommerceManager.tsx',import.meta.url),'utf8');
 assert.match(text,/deliveryDescription/);assert.match(text,/providerProductCode/);assert.match(text,/isPurchasable/);assert.match(text,/priceMinorUnits/);assert.doesNotMatch(text,/grantReward|\/grant/i);
 const orders=await source.readFile(new URL('../../apps/web/app/admin/CommerceOrdersManager.tsx',import.meta.url),'utf8');
 assert.match(orders,/api\/v1\/admin\/commerce\/orders/);assert.doesNotMatch(orders,/POST|PATCH|DELETE/);
});

test('public commerce identity and checkout use the last published slug, not a pending draft slug',async()=>{
 const endpoints=await source.readFile(new URL('../../backend/NovaHaven.Api/Endpoints/CommerceEndpoints.cs',import.meta.url),'utf8');
 const checkout=await source.readFile(new URL('../../backend/NovaHaven.Api/Endpoints/CommerceOrderEndpoints.cs',import.meta.url),'utf8');
 assert.match(endpoints,/offer\.State == CommerceOfferState\.Published && revision\.Slug == slug/);
 assert.match(endpoints,/revision\.Slug == item\.Slug/);
 assert.match(checkout,/PublishedSlug = revision\.Slug/);
 assert.match(checkout,/slugs\.Contains\(revision\.Slug\)/);
 assert.match(checkout,/OfferSlug = offer\.PublishedSlug/);
});

test('checkout errors can receive keyboard focus after submission',async()=>{
 const checkout=await source.readFile(new URL('../../apps/web/app/checkout/CheckoutClient.tsx',import.meta.url),'utf8');
 assert.match(checkout,/errorRef=useRef<HTMLParagraphElement\|null>/);
 assert.match(checkout,/errorRef\.current\?\.focus\(\)/);
 assert.match(checkout,/ref=\{errorRef\} tabIndex=\{-1\}[^>]*role="alert"/);
});
