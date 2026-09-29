import assert from 'node:assert/strict';
import test from 'node:test';
import {cartCount, readCart, removeCartItem, setCartQuantity} from '../../apps/web/src/lib/commerce-cart.ts';

test('cart storage safely recovers malformed and duplicate rows',()=>{
 assert.deepEqual(readCart('not json'),[]);
 assert.deepEqual(readCart(JSON.stringify([{slug:' map ',quantity:2},{slug:'map',quantity:3},{slug:'BAD!',quantity:1}])),[{slug:'map',quantity:5}]);
});
test('quantity controls clamp to supported bounds and removing zero deletes a row',()=>{
 const one=setCartQuantity([], 'starter-pack', 0);
 const added=setCartQuantity(one,'starter-pack',1);
 assert.deepEqual(setCartQuantity(added,'starter-pack',150),[{slug:'starter-pack',quantity:99}]);
 assert.deepEqual(setCartQuantity(added,'starter-pack',0),[]);
});
test('cart count sums quantities and item removal is immutable',()=>{
 const cart=[{slug:'map',quantity:2},{slug:'sword',quantity:4}];
 assert.equal(cartCount(cart),6);
 assert.deepEqual(removeCartItem(cart,'map'),[{slug:'sword',quantity:4}]);
 assert.equal(cart.length,2);
});
