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
 const offers=await source.readFile(new URL('../../backend/NovaHaven.Infrastructure/Persistence/Repositories/Commerce/EfCommerceOfferRepository.cs',import.meta.url),'utf8');
 const checkoutRepository=await source.readFile(new URL('../../backend/NovaHaven.Infrastructure/Persistence/Repositories/Commerce/EfCommerceOrderRepository.cs',import.meta.url),'utf8');
 const controller=await source.readFile(new URL('../../backend/NovaHaven.Api/Controllers/CommerceOffersController.cs',import.meta.url),'utf8');
 const checkout=await source.readFile(new URL('../../backend/NovaHaven.Application/Features/Commerce/Services/CommerceOrderService.cs',import.meta.url),'utf8');
 assert.match(offers,/offer\.State == CommerceOfferState\.Published && offer\.PublishedRevisionId == revision\.Id/);
 assert.match(offers,/revision\.Slug == slug/);
 assert.match(checkoutRepository,/offer\.State == CommerceOfferState\.Published && offer\.PublishedRevisionId == revision\.Id/);
 assert.match(checkoutRepository,/slugs\.Contains\(revision\.Slug\)/);
 assert.match(controller,/CommerceOfferService/);
 assert.match(checkout,/OfferSlug = offer\.Slug/);
});

test('checkout errors can receive keyboard focus after submission',async()=>{
 const checkout=await source.readFile(new URL('../../apps/web/app/checkout/CheckoutClient.tsx',import.meta.url),'utf8');
 assert.match(checkout,/errorRef=useRef<HTMLParagraphElement\|null>/);
 assert.match(checkout,/errorRef\.current\?\.focus\(\)/);
 assert.match(checkout,/ref=\{errorRef\} tabIndex=\{-1\}[^>]*role="alert"/);
});

test('local demo checkout never asks for personal/payment details or promises fulfillment',async()=>{
 const checkout=await source.readFile(new URL('../../apps/web/app/checkout/CheckoutClient.tsx',import.meta.url),'utf8');
 assert.doesNotMatch(checkout,/setIgn|setEmail|setPhone|paymentMethod|VietQR|MoMo|MBBank|bankAcc|Chủ tài khoản|tự động được gửi|Chờ thanh toán/i);
 assert.match(checkout,/realCharge/);
 assert.match(checkout,/fulfilment/);
 assert.match(checkout,/không thu tiền thật/i);
 assert.match(checkout,/không giao vật phẩm/i);
});

test('homepage uses published commerce data and does not fabricate server/game claims',async()=>{
 const home=await source.readFile(new URL('../../apps/web/app/page.tsx',import.meta.url),'utf8');
 const navigation=await source.readFile(new URL('../../apps/web/app/SiteNavigation.tsx',import.meta.url),'utf8');
 assert.match(home,/commerceApi\.list\(\)/);
 assert.match(home,/commercePage\?\.total/);
 assert.doesNotMatch(home,/40\+|500\+|1,200|Máy chủ chính thức|online-dot|thời gian thực|Boss sở hữu cơ chế|Hệ Thống Nghề & Kỹ Năng Độc Bản/i);
 assert.doesNotMatch(home,/href="https:\/\/discord\.gg"/);
 assert.doesNotMatch(navigation,/play\.novahaven\.net/);
});
